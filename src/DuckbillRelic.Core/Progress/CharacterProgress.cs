namespace DuckbillRelic.Core.Progress;

/// <summary>
/// Saved evidence for one character. Every value only grows: an upgrade hands in the previous relic,
/// so a missing item never lowers what was already seen. Manual marks are kept separately.
/// </summary>
public sealed class CharacterProgress
{
    public int Version { get; set; } = 1;
    public ulong ContentId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>series id → job → highest tier ever seen in the character's own containers.</summary>
    public Dictionary<string, Dictionary<uint, int>> ObservedTiers { get; set; } = [];
    /// <summary>Catalog achievements seen as complete. Achievements cannot be lost, so this only grows.</summary>
    public List<uint> CompletedAchievements { get; set; } = [];
    /// <summary>When the full achievement list was last available. Null until the player opens the Achievements window once.</summary>
    public DateTimeOffset? AchievementListLoadedAt { get; set; }
    /// <summary>series id → job → stage the player marked as done.</summary>
    public Dictionary<string, Dictionary<uint, int>> ManualStages { get; set; } = [];

    public int ObservedTier(string series, uint job) =>
        ObservedTiers.TryGetValue(series, out var jobs) ? jobs.GetValueOrDefault(job) : 0;

    public int ManualStage(string series, uint job) =>
        ManualStages.TryGetValue(series, out var jobs) ? jobs.GetValueOrDefault(job) : 0;

    public bool MergeObservedTier(string series, uint job, int tier)
    {
        if (tier <= 0) return false;
        if (!ObservedTiers.TryGetValue(series, out var jobs)) ObservedTiers[series] = jobs = [];
        if (jobs.GetValueOrDefault(job) >= tier) return false;
        jobs[job] = tier;
        return true;
    }

    public bool AddAchievement(uint id)
    {
        var index = CompletedAchievements.BinarySearch(id);
        if (index >= 0) return false;
        CompletedAchievements.Insert(~index, id);
        return true;
    }

    public bool HasAchievement(uint id) => CompletedAchievements.BinarySearch(id) >= 0;

    public void SetManualStage(string series, uint job, int stage)
    {
        if (!ManualStages.TryGetValue(series, out var jobs)) ManualStages[series] = jobs = [];
        if (stage <= 0) jobs.Remove(job); else jobs[job] = stage;
        if (jobs.Count == 0) ManualStages.Remove(series);
    }

    /// <summary>Restore invariants after loading a file written by hand or by an older version.</summary>
    public void Normalize()
    {
        CompletedAchievements = CompletedAchievements.Distinct().Order().ToList();
        foreach (var jobs in ObservedTiers.Values)
            foreach (var job in jobs.Where(kv => kv.Value <= 0).Select(kv => kv.Key).ToList()) jobs.Remove(job);
        foreach (var jobs in ManualStages.Values)
            foreach (var job in jobs.Where(kv => kv.Value <= 0).Select(kv => kv.Key).ToList()) jobs.Remove(job);
    }
}
