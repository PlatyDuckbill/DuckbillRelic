// relic-catalog: 로컬 KR·글로벌 클라이언트의 게임 시트에서 레릭 단계별 직업 아이템 ID와 직업별 업적 대응을 뽑아
// data/relic-catalog.v1.json 으로 쓴다. 개발 중에만 쓰는 도구이며 플러그인에 포함하지 않는다.
// 근거와 규칙은 docs/research/GAME-DATA.md. 검증에 하나라도 실패하면 파일을 쓰지 않는다.
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

Console.OutputEncoding = Encoding.UTF8;
const string KrGame = @"C:\Program Files (x86)\FINAL FANTASY XIV - KOREA\game";
const string GlGame = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game";
var outPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "relic-catalog.v1.json");

var gl = new GameData(Path.Combine(GlGame, "sqpack"), new LuminaOptions { DefaultExcelLanguage = Language.English, PanicOnSheetChecksumMismatch = false });
var kr = new GameData(Path.Combine(KrGame, "sqpack"), new LuminaOptions { DefaultExcelLanguage = Language.Korean, PanicOnSheetChecksumMismatch = false });
var items = gl.GetExcelSheet<Item>()!;
var itemsKr = kr.GetExcelSheet<Item>()!;
var classJobs = gl.GetExcelSheet<ClassJob>()!;
var achievements = gl.GetExcelSheet<Achievement>()!;
var achievementsKr = kr.GetExcelSheet<Achievement>()!;
var quests = gl.GetExcelSheet<Quest>()!;
var errors = new List<string>();
void Fail(string m) { errors.Add(m); Console.Error.WriteLine("ERROR " + m); }

// ---------- 아이템 → 직업 ----------
var categoryProps = typeof(ClassJobCategory).GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.PropertyType == typeof(bool)).ToDictionary(p => p.Name);
var jobRows = classJobs.Where(c => c.RowId > 0 && c.Abbreviation.ExtractText().Length > 0).ToList();
const uint ShieldUiCategory = 11;
(uint job, bool shield) JobOf(uint itemId)
{
    var it = items.GetRowOrDefault(itemId);
    if (it is null || it.Value.RowId == 0) { Fail($"item {itemId} missing"); return (0, false); }
    if (itemsKr.GetRowOrDefault(itemId) is null) Fail($"item {itemId} missing in KR");
    var cat = it.Value.ClassJobCategory.ValueNullable;
    if (cat is null) { Fail($"item {itemId} has no ClassJobCategory"); return (0, false); }
    var flagged = jobRows.Where(c => categoryProps.TryGetValue(c.Abbreviation.ExtractText(), out var p) && (bool)p.GetValue(cat.Value)!).ToList();
    // 직업이 있는 계열(검술사·나이트 등)은 직업 행만, 제작·채집은 클래스 행 하나.
    var jobs = flagged.Where(c => c.JobIndex > 0).ToList();
    var pick = jobs.Count == 1 ? jobs[0] : flagged.Count == 1 ? flagged[0] : (ClassJob?)null;
    if (pick is null) { Fail($"item {itemId} maps to {flagged.Count} jobs"); return (0, false); }
    return (pick.Value.RowId, it.Value.ItemUICategory.RowId == ShieldUiCategory);
}

JsonObject Tier(IEnumerable<uint> ids)
{
    var map = new SortedDictionary<uint, uint>(); uint shield = 0;
    foreach (var id in ids.Where(i => i != 0))
    {
        var (job, isShield) = JobOf(id);
        if (job == 0) continue;
        if (isShield) { if (shield != 0) Fail($"two shields in tier ({shield}, {id})"); shield = id; continue; }
        if (!map.TryAdd(job, id)) Fail($"job {job} twice in tier ({map[job]}, {id})");
    }
    var o = new JsonObject { ["items"] = new JsonObject(map.Select(kv => KeyValuePair.Create(kv.Key.ToString(), (JsonNode?)kv.Value))) };
    if (shield != 0) o["shield"] = shield;
    return o;
}
// 같은 단계의 직업별 무기가 연속된 ID로 놓인 계열: 시작 ID(나이트·목수)부터 크기만큼
IEnumerable<uint> Block(uint start, int size) => Enumerable.Range(0, size).Select(i => start + (uint)i);

// ---------- 계열 정의 (근거: docs/research/GAME-DATA.md) ----------
var series = new JsonArray();
void AddSeries(string id, string kind, uint nameCategory, List<JsonObject> tiers, int[] stageTierEnds, JsonArray achs)
{
    var jobs = tiers[0]["items"]!.AsObject().Select(kv => uint.Parse(kv.Key)).ToList();
    for (var t = 0; t < tiers.Count; t++)
    {
        var set = tiers[t]["items"]!.AsObject().Select(kv => uint.Parse(kv.Key)).ToList();
        if (!set.SequenceEqual(jobs)) Fail($"{id} tier {t + 1} job set differs");
    }
    if (stageTierEnds.Last() != tiers.Count || stageTierEnds.Zip(stageTierEnds.Skip(1)).Any(p => p.First >= p.Second)) Fail($"{id} bad stage ends");
    var stageCount = stageTierEnds.Length;
    foreach (var a in achs)
    {
        var job = (uint)a!["job"]!; var stage = (int)a["stage"]!;
        if (!jobs.Contains(job)) Fail($"{id} achievement {a["id"]} job {job} not in series");
        if (stage < 1 || stage > stageCount) Fail($"{id} achievement {a["id"]} stage {stage}");
    }
    var perStage = achs.GroupBy(a => (int)a!["stage"]!).ToDictionary(g => g.Key, g => g.Count());
    foreach (var (stage, n) in perStage)
        if (n != jobs.Count && !(id == "zodiac")) Fail($"{id} stage {stage}: {n} achievements for {jobs.Count} jobs");
    if (achs.Select(a => ((int)a!["stage"]!, (uint)a["job"]!)).Distinct().Count() != achs.Count) Fail($"{id} duplicate achievement stage/job");
    series.Add(new JsonObject
    {
        ["id"] = id, ["kind"] = kind, ["nameAchievementCategory"] = nameCategory,
        ["jobs"] = new JsonArray(jobs.Select(j => (JsonNode)j).ToArray()),
        ["stageTierEnds"] = new JsonArray(stageTierEnds.Select(e => (JsonNode)e).ToArray()),
        ["tiers"] = new JsonArray(tiers.ToArray()),
        ["achievements"] = achs,
    });
    Console.WriteLine($"{id}: {jobs.Count} jobs, {tiers.Count} tiers, {stageCount} stages, {achs.Count} achievements");
}
JsonObject Ach(uint id, int stage, uint job) => new() { ["id"] = id, ["stage"] = stage, ["job"] = job };
IEnumerable<Achievement> InCategory(uint cat) => achievements.Where(a => a.AchievementCategory.RowId == cat);
uint LastNonZero(Achievement a) => a.Data.Select(d => d.RowId).LastOrDefault(v => v != 0);

// Type 24: Key = 단계 퀘스트, Data의 0이 아닌 마지막 값 = ClassJob
JsonArray Type24(uint cat, Dictionary<uint, int> questStage)
{
    var arr = new JsonArray();
    foreach (var a in InCategory(cat))
    {
        if (a.Type != 24) { Fail($"category {cat} achievement {a.RowId} type {a.Type}"); continue; }
        if (!questStage.TryGetValue(a.Key.RowId, out var stage)) { Fail($"category {cat} achievement {a.RowId} unknown quest {a.Key.RowId}"); continue; }
        arr.Add(Ach(a.RowId, stage, LastNonZero(a)));
    }
    return arr;
}

// 조디악 웨폰: RelicItem 0~7행, 1단계 업적은 Type 6(Key 퀘스트의 ClassJobRequired)
{
    var relic = gl.GetExcelSheet<RelicItem>()!;
    var itemProps = typeof(RelicItem).GetProperties().Where(p => p.PropertyType == typeof(RowRef<Item>)).ToList();
    var tiers = Enumerable.Range(0, 8).Select(r => Tier(itemProps.Select(p => ((RowRef<Item>)p.GetValue(relic.GetRow((uint)r))!).RowId))).ToList();
    var achs = new JsonArray();
    foreach (var a in InCategory(62).Where(a => a.Type == 6))
        achs.Add(Ach(a.RowId, 1, quests.GetRow(a.Key.RowId).ClassJobRequired.RowId));
    if (achs.Count != 10) Fail($"zodiac stage-1 achievements {achs.Count}");
    AddSeries("zodiac", "weapon", 63, tiers, [1, 2, 3, 4, 5, 6, 7, 8], achs);
}
// 아니마 웨폰: AnimaWeaponItem 1~8행
{
    var sheet = gl.GetExcelSheet<AnimaWeaponItem>()!;
    var tiers = Enumerable.Range(1, 8).Select(r => Tier(sheet.GetRow((uint)r).Item.Select(i => i.RowId))).ToList();
    AddSeries("anima", "weapon", 64, tiers, [1, 2, 3, 4, 5, 6, 7, 8],
        Type24(64, new() { [67750] = 3, [67820] = 4, [67864] = 5, [67915] = 6, [67932] = 7, [67940] = 8 }));
}
// 에우레카 웨폰: 16종, 단계 블록 16개(15직업 + 방패). 업적은 Type 1(진행 키) → 설명의 아이템 이름으로 직업을 대응(개발 시점만)
{
    uint[] starts = [17817, 21942, 21958, 21974, 21990, 22925, 22941, 22957, 24039, 24055, 24071, 24643, 24659, 24675, 24691, 24707];
    var tiers = starts.Select(s => Tier(Block(s, 16))).ToList();
    int[] ends = [5, 8, 11, 15, 16];
    var achs = new JsonArray();
    foreach (var a in InCategory(66))
    {
        var desc = achievementsKr.GetRow(a.RowId).Description.ExtractText();
        var hits = new List<(int stage, uint job)>();
        for (var s = 0; s < ends.Length; s++)
            foreach (var kv in tiers[ends[s] - 1]["items"]!.AsObject())
            {
                var name = itemsKr.GetRow((uint)kv.Value!).Name.ExtractText();
                if (desc.StartsWith(name + "와 ") || desc.StartsWith(name + "과 ") || desc.StartsWith(name + " 입수") || desc.StartsWith(name + "을 ") || desc.StartsWith(name + "를 "))
                    hits.Add((s + 1, uint.Parse(kv.Key)));
            }
        if (hits.Count != 1) { Fail($"eureka achievement {a.RowId} matched {hits.Count} ({desc})"); continue; }
        achs.Add(Ach(a.RowId, hits[0].stage, hits[0].job));
    }
    AddSeries("eureka", "weapon", 66, tiers, ends, achs);
}
// 레지스탕스·맨더빌·팬텀: 단계 블록
AddSeries("resistance", "weapon", 68, new uint[] { 30228, 30767, 30785, 32651, 32669, 33462 }.Select(s => Tier(Block(s, 18))).ToList(), [1, 2, 3, 4, 5, 6],
    Type24(68, new() { [69380] = 1, [69507] = 3, [69576] = 5, [69637] = 6 }));
AddSeries("manderville", "weapon", 75, new uint[] { 38400, 39144, 39920, 40932 }.Select(s => Tier(Block(s, 20))).ToList(), [1, 2, 3, 4],
    Type24(75, new() { [70188] = 1, [70261] = 2, [70307] = 3, [70342] = 4 }));
AddSeries("phantom", "weapon", 81, new uint[] { 47869, 47006, 50032, 50978, 51000 }.Select(s => Tier(Block(s, 22))).ToList(), [1, 2, 3, 4, 5],
    Type24(81, new() { [70856] = 1, [70918] = 2, [70992] = 3, [71041] = 5 }));
// 하늘강철·모웬 도구: 단계 블록 11개(목수~어부)
AddSeries("skysteel", "tool", 70, new uint[] { 29612, 29623, 29634, 30282, 30293, 31714 }.Select(s => Tier(Block(s, 11))).ToList(), [1, 2, 3, 4, 5, 6],
    Type24(70, new() { [69430] = 5, [69519] = 6 }));
AddSeries("splendorous", "tool", 78, new uint[] { 38715, 38726, 38737, 39732, 39743, 41180, 41191 }.Select(s => Tier(Block(s, 11))).ToList(), [1, 2, 3, 4, 5, 6, 7],
    Type24(78, new() { [70268] = 3, [70305] = 5, [70340] = 7 }));
// 우주 도구: WKSCosmoToolClass 1~11행(행 = 직업, 단계 칸 20개). 업적 Key = 단계 칸의 Unknown0
{
    var sheet = gl.GetExcelSheet<WKSCosmoToolClass>()!;
    var rows = Enumerable.Range(1, 11).Select(r => sheet.GetRow((uint)r)).ToList();
    var tiers = Enumerable.Range(0, 20).Select(t => Tier(rows.Select(r => r.Stages[t].Item.RowId))).ToList();
    var keyToTierJob = new Dictionary<uint, (int tier, uint job)>();
    foreach (var r in rows)
        for (var t = 0; t < 20; t++)
            if (r.Stages[t].Unknown0 != 0) keyToTierJob[r.Stages[t].Unknown0] = (t + 1, JobOf(r.Stages[t].Item.RowId).job);
    int[] ends = [9, 14, 17, 20];
    var achs = new JsonArray();
    foreach (var a in InCategory(82))
    {
        if (a.Type != 1 || !keyToTierJob.TryGetValue(a.Key.RowId, out var tj)) { Fail($"cosmic achievement {a.RowId} key {a.Key.RowId}"); continue; }
        var stage = Array.IndexOf(ends, tj.tier) + 1;
        if (stage == 0) { Fail($"cosmic achievement {a.RowId} tier {tj.tier} is not a stage end"); continue; }
        achs.Add(Ach(a.RowId, stage, tj.job));
    }
    AddSeries("cosmic", "tool", 82, tiers, ends, achs);
}

if (errors.Count > 0) { Console.Error.WriteLine($"{errors.Count} error(s). Nothing written."); return 1; }
var doc = new JsonObject
{
    ["schema"] = 1,
    ["clients"] = new JsonObject
    {
        ["kr"] = File.ReadAllText(Path.Combine(KrGame, "ffxivgame.ver")).Trim(),
        ["global"] = File.ReadAllText(Path.Combine(GlGame, "ffxivgame.ver")).Trim(),
    },
    ["series"] = series,
};
var json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n";
File.WriteAllText(outPath, json, new UTF8Encoding(false));
Console.WriteLine($"Wrote {outPath}");
return 0;
