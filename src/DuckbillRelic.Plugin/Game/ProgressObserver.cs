using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using DuckbillRelic.Core.Catalog;
using DuckbillRelic.Core.Progress;
using Lumina.Excel.Sheets;

namespace DuckbillRelic.Plugin.Game;

/// <summary>
/// Reads, on the framework thread, only what the client already holds: the character's own containers and
/// the achievement bits exposed by Dalamud. It never requests anything from the server.
/// </summary>
internal sealed class ProgressObserver : IDisposable
{
    private static readonly GameInventoryType[] Containers =
    [
        GameInventoryType.EquippedItems, GameInventoryType.ArmoryMainHand, GameInventoryType.ArmoryOffHand,
        GameInventoryType.Inventory1, GameInventoryType.Inventory2, GameInventoryType.Inventory3, GameInventoryType.Inventory4,
    ];
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly RelicCatalog catalog;
    private readonly IFramework framework;
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly IGameInventory inventory;
    private readonly IUnlockState unlockState;
    private readonly ProgressStore store;
    private readonly Achievement[] achievementRows;
    private DateTime nextScan;
    private bool listWasLoaded;
    private volatile Snapshot? snapshot;

    public ProgressObserver(RelicCatalog catalog, IFramework framework, IClientState clientState, IPlayerState playerState,
        IGameInventory inventory, IUnlockState unlockState, IDataManager data, ProgressStore store)
    {
        this.catalog = catalog;
        this.framework = framework;
        this.clientState = clientState;
        this.playerState = playerState;
        this.inventory = inventory;
        this.unlockState = unlockState;
        this.store = store;
        var sheet = data.GetExcelSheet<Achievement>();
        achievementRows = catalog.AllAchievementIds.Select(id => sheet.GetRowOrDefault(id)).OfType<Achievement>().ToArray();
        framework.Update += OnUpdate;
        clientState.Logout += OnLogout;
    }

    /// <summary>Immutable copy for the UI. Null while no character is loaded.</summary>
    public Snapshot? Current => snapshot;

    public sealed record Snapshot(CharacterProgress Progress, IReadOnlyDictionary<(string Series, uint Job), int> LiveTiers, bool AchievementListLoaded);

    /// <summary>Apply a manual mark on the framework thread, then save.</summary>
    public void SetManualStage(string series, uint job, int stage) => framework.RunOnFrameworkThread(() =>
    {
        if (snapshot is not { } s) return;
        s.Progress.SetManualStage(series, job, stage);
        store.Save(s.Progress);
        snapshot = s with { Progress = s.Progress };
        nextScan = DateTime.MinValue;
    });

    private void OnLogout(int type, int code)
    {
        snapshot = null;
        listWasLoaded = false;
    }

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now < nextScan) return;
        nextScan = now + Interval;
        if (!clientState.IsLoggedIn || !playerState.IsLoaded || playerState.ContentId == 0)
        {
            snapshot = null;
            return;
        }
        var contentId = playerState.ContentId;
        var progress = snapshot?.Progress is { } p && p.ContentId == contentId ? p : store.Load(contentId);
        if (snapshot?.Progress.ContentId != contentId) listWasLoaded = false;
        var changed = false;
        var name = playerState.CharacterName;
        if (name.Length > 0 && progress.Name != name) { progress.Name = name; changed = true; }

        // Containers that are not loaded yet return nothing; absence is never recorded.
        var live = new Dictionary<(string, uint), int>();
        foreach (var container in Containers)
            foreach (var item in inventory.GetInventoryItems(container))
            {
                if (!catalog.TryGetItem(item.BaseItemId, out var hit) || hit.IsShield) continue;
                var key = (hit.SeriesId, hit.Job);
                if (live.GetValueOrDefault(key) < hit.Tier) live[key] = hit.Tier;
            }
        foreach (var ((series, job), tier) in live) changed |= progress.MergeObservedTier(series, job, tier);

        // A set bit is always a completed achievement; a clear bit only means "not done" while the list is loaded.
        foreach (var row in achievementRows)
            if (unlockState.IsAchievementComplete(row)) changed |= progress.AddAchievement(row.RowId);
        var listLoaded = unlockState.IsAchievementListLoaded;
        if (listLoaded && !listWasLoaded) { progress.AchievementListLoadedAt = DateTimeOffset.Now; changed = true; }
        listWasLoaded = listLoaded;

        if (changed) store.Save(progress);
        snapshot = new Snapshot(progress, live, listLoaded);
    }

    public void Dispose()
    {
        framework.Update -= OnUpdate;
        clientState.Logout -= OnLogout;
    }
}
