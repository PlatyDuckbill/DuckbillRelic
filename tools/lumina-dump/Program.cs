using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

Console.OutputEncoding = Encoding.UTF8;
const string KrPath = @"C:\Program Files (x86)\FINAL FANTASY XIV - KOREA\game\sqpack";
const string GlPath = @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack";

var mode = args.Length > 0 ? args[0] : "help";
var asm = typeof(Item).Assembly;

if (mode == "types")
{
    var rx = new Regex(args[1], RegexOptions.IgnoreCase);
    foreach (var t in asm.GetTypes().Where(t => t.Namespace == "Lumina.Excel.Sheets" && rx.IsMatch(t.Name)).OrderBy(t => t.Name))
        Console.WriteLine(t.Name);
    return;
}

var kr = new GameData(KrPath, new LuminaOptions { DefaultExcelLanguage = Language.Korean, PanicOnSheetChecksumMismatch = false });
var gl = new GameData(GlPath, new LuminaOptions { DefaultExcelLanguage = Language.English, PanicOnSheetChecksumMismatch = false });

static string Fmt(object v)
{
    if (v == null) return "null";
    if (v is ReadOnlySeString s) return s.ExtractText();
    var t = v.GetType();
    if (t.IsGenericType && t.Name.StartsWith("RowRef")) return "#" + t.GetProperty("RowId")!.GetValue(v);
    if (t.Name == "RowRef") return "#" + t.GetProperty("RowId")!.GetValue(v);
    if (v is string str) return str;
    if (v is IEnumerable e && t.Name.StartsWith("Collection"))
    {
        var parts = new List<string>();
        foreach (var x in e) parts.Add(Fmt(x));
        return "[" + string.Join(",", parts) + "]";
    }
    if (t.IsPrimitive || t.IsEnum) return v.ToString()!;
    if (t.IsValueType && t.Namespace == "Lumina.Excel.Sheets")
    {
        var ps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.Name != "ExcelPage");
        return "{" + string.Join(" ", ps.Select(p => { try { return p.Name + "=" + Fmt(p.GetValue(v)); } catch { return p.Name + "=!"; } })) + "}";
    }
    return v.ToString()!;
}

static string Dump(object row, string[] skip = null)
{
    var t = row.GetType();
    var ps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetIndexParameters().Length == 0 && p.Name is not "ExcelPage" && !(skip?.Contains(p.Name) ?? false));
    return string.Join(" | ", ps.Select(p => { try { return p.Name + "=" + Fmt(p.GetValue(row)); } catch (Exception ex) { return p.Name + "=!" + ex.GetType().Name; } }));
}

if (mode == "sheet")
{
    // sheet <TypeName> <rowRegexOnDump> [max]
    var t = asm.GetType("Lumina.Excel.Sheets." + args[1])!;
    var rx = args.Length > 2 ? new Regex(args[2], RegexOptions.IgnoreCase) : null;
    int max = args.Length > 3 ? int.Parse(args[3]) : 200;
    foreach (var (name, gd) in new[] { ("KR", kr), ("GL", gl) })
    {
        var m = typeof(GameData).GetMethods().First(m => m.Name == "GetExcelSheet" && m.IsGenericMethod && m.GetParameters().Length == 2).MakeGenericMethod(t);
        var sheet = (IEnumerable)m.Invoke(gd, new object[] { null, null })!;
        int n = 0;
        Console.WriteLine($"== {name} {args[1]}");
        foreach (var row in sheet)
        {
            var d = Dump(row);
            if (rx != null && !rx.IsMatch(d)) continue;
            Console.WriteLine(d);
            if (++n >= max) break;
        }
    }
    return;
}

if (mode == "achcat")
{
    var catKr = kr.GetExcelSheet<AchievementCategory>()!;
    var catGl = gl.GetExcelSheet<AchievementCategory>()!;
    foreach (var c in catGl)
    {
        var k = catKr.GetRowOrDefault(c.RowId);
        Console.WriteLine($"{c.RowId}\tkind={c.AchievementKind.RowId}:{c.AchievementKind.ValueNullable?.Name.ExtractText()}\t{c.Name.ExtractText()}\t{k?.Name.ExtractText()}");
    }
    return;
}

if (mode == "ach")
{
    // ach <categoryIds comma>
    var ids = args[1].Split(',').Select(uint.Parse).ToHashSet();
    var aKr = kr.GetExcelSheet<Achievement>()!;
    foreach (var a in gl.GetExcelSheet<Achievement>()!)
    {
        if (!ids.Contains(a.AchievementCategory.RowId)) continue;
        var k = aKr.GetRowOrDefault(a.RowId);
        var dataProps = Dump(a, new[] { "Name", "Description", "Icon", "Title", "AchievementCategory", "AchievementTarget", "Order", "AchievementHideCondition" });
        Console.WriteLine($"{a.RowId}\tcat={a.AchievementCategory.RowId}\t{a.Name.ExtractText()}\t|KR {k?.Name.ExtractText()}\t|{k?.Description.ExtractText()}\t|{dataProps}");
    }
    return;
}

if (mode == "item")
{
    // item <regexEN> [regexKR]
    var rx = new Regex(args[1], RegexOptions.IgnoreCase);
    var iKr = kr.GetExcelSheet<Item>()!;
    foreach (var it in gl.GetExcelSheet<Item>()!)
    {
        var en = it.Name.ExtractText();
        if (!rx.IsMatch(en)) continue;
        var k = iKr.GetRowOrDefault(it.RowId);
        Console.WriteLine($"{it.RowId}\tilvl={it.LevelItem.RowId}\tlv={it.LevelEquip}\tcjc={it.ClassJobCategory.ValueNullable?.Name.ExtractText()}\tui={it.ItemUICategory.RowId}\t{en}\t|KR {k?.Name.ExtractText()}");
    }
    return;
}

if (mode == "ids")
{
    // ids <SheetType> <id,id,...> : print KR and GL dumps for these row ids (Item/Quest etc.)
    var t = asm.GetType("Lumina.Excel.Sheets." + args[1])!;
    var ids = args[2].Split(',').Select(uint.Parse).ToList();
    foreach (var (name, gd) in new[] { ("KR", kr), ("GL", gl) })
    {
        var m = typeof(GameData).GetMethods().First(m => m.Name == "GetExcelSheet" && m.IsGenericMethod && m.GetParameters().Length == 2).MakeGenericMethod(t);
        dynamic sheet = m.Invoke(gd, new object[] { null, null })!;
        foreach (var id in ids)
        {
            var row = sheet.GetRowOrDefault(id);
            Console.WriteLine($"{name} {id}: " + (row == null ? "none" : Dump(row)));
        }
    }
    return;
}

Console.WriteLine("modes: types <rx> | sheet <Type> <rx> [max] | achcat | ach <catIds> | item <rxEN> | ids <Type> <ids>");
