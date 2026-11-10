using Dalamud.Plugin.Services;
using DuckbillRelic.Core.Catalog;
using Lumina.Excel.Sheets;

namespace DuckbillRelic.Plugin.Game;

/// <summary>
/// Names read once from the running client's sheets, in the client language. Draw code never touches sheets.
/// </summary>
internal sealed class GameNames
{
    private readonly Dictionary<uint, string> jobNames = [];
    private readonly Dictionary<uint, string> jobAbbreviations = [];
    private readonly Dictionary<uint, string> itemNames = [];
    private readonly Dictionary<string, string> seriesNames = [];

    public GameNames(IDataManager data, RelicCatalog catalog)
    {
        var jobs = data.GetExcelSheet<ClassJob>();
        foreach (var job in catalog.Series.SelectMany(s => s.Jobs).Distinct())
        {
            var row = jobs.GetRowOrDefault(job);
            jobNames[job] = row?.Name.ExtractText() is { Length: > 0 } n ? n : job.ToString();
            jobAbbreviations[job] = row?.Abbreviation.ExtractText() is { Length: > 0 } a ? a : job.ToString();
        }
        var items = data.GetExcelSheet<Item>();
        foreach (var id in catalog.AllItemIds)
            itemNames[id] = items.GetRowOrDefault(id)?.Name.ExtractText() ?? "";
        var categories = data.GetExcelSheet<AchievementCategory>();
        foreach (var s in catalog.Series)
            seriesNames[s.Id] = categories.GetRowOrDefault(s.NameAchievementCategory)?.Name.ExtractText() is { Length: > 0 } c ? c : s.Id;
    }

    public string Job(uint job) => jobNames.GetValueOrDefault(job, job.ToString());
    public string JobShort(uint job) => jobAbbreviations.GetValueOrDefault(job, job.ToString());
    public string Item(uint item) => itemNames.GetValueOrDefault(item, "");
    public string Series(string id) => seriesNames.GetValueOrDefault(id, id);
}

/// <summary>
/// Checks the bundled catalog against the running client. A series whose items or achievements do not match
/// is disabled on its own; the rest keep working.
/// </summary>
internal static class CatalogValidator
{
    private const uint ZodiacStageOneCategory = 62;

    public static HashSet<string> FindMismatches(IDataManager data, RelicCatalog catalog, IPluginLog log)
    {
        var items = data.GetExcelSheet<Item>();
        var achievements = data.GetExcelSheet<Achievement>();
        var disabled = new HashSet<string>();
        foreach (var s in catalog.Series)
        {
            var problem = s.Tiers.SelectMany(t => t.ItemByJob.Values.Append(t.Shield)).Where(i => i != 0)
                .FirstOrDefault(i => items.GetRowOrDefault(i) is not { } row || row.ClassJobCategory.RowId == 0);
            if (problem != 0) { log.Warning("Relic series {Series}: item {Item} missing or not equipment.", s.Id, problem); disabled.Add(s.Id); continue; }
            foreach (var a in s.Achievements)
            {
                var row = achievements.GetRowOrDefault(a.AchievementId);
                var category = row?.AchievementCategory.RowId ?? 0;
                if (row is null || (category != s.NameAchievementCategory && !(s.Id == "zodiac" && category == ZodiacStageOneCategory)))
                {
                    log.Warning("Relic series {Series}: achievement {Achievement} missing or in category {Category}.", s.Id, a.AchievementId, category);
                    disabled.Add(s.Id);
                    break;
                }
            }
        }
        return disabled;
    }
}
