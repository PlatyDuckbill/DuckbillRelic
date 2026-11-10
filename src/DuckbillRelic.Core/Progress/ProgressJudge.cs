using DuckbillRelic.Core.Catalog;

namespace DuckbillRelic.Core.Progress;

/// <summary>What one job×stage cell shows. Only results, never the evidence (see <see cref="StageEvidence"/>).</summary>
public enum CellResult { NotApplicable, Done, Next, NotDone, Unknown }

/// <summary>Why a stage counts as done. Kept apart so the UI can explain without mixing sources.</summary>
public enum StageEvidence { None, Owned, Observed, Achievement, Implied, Manual }

/// <summary>Judged progress of one job in one series.</summary>
public sealed class JobProgress
{
    private readonly RelicSeries series;
    private readonly Func<int, bool> achievementDone;

    internal JobProgress(RelicSeries series, uint job, int liveStage, int itemStage, int achievementStage, int manualStage,
        bool achievementsKnown, Func<int, bool> achievementDone)
    {
        this.series = series;
        this.achievementDone = achievementDone;
        Job = job;
        LiveStage = liveStage;
        ItemStage = itemStage;
        AchievementStage = achievementStage;
        ManualStage = manualStage;
        AutoStage = Math.Max(itemStage, achievementStage);
        Reached = Math.Max(AutoStage, manualStage);
        // Before the achievement list has ever been read, any achievement stage above what is known stays unknown.
        var highest = series.HighestAchievementStage;
        UnknownUpTo = !achievementsKnown && highest > Reached ? highest : 0;
    }

    public uint Job { get; }
    public int LiveStage { get; }
    public int ItemStage { get; }
    public int AchievementStage { get; }
    public int ManualStage { get; }
    public int AutoStage { get; }
    public int Reached { get; }
    public int UnknownUpTo { get; }
    public bool IsComplete => Reached >= series.StageCount;
    public bool HasUnknown => UnknownUpTo > 0;

    public CellResult Result(int stage)
    {
        if (stage <= Reached) return CellResult.Done;
        if (stage <= UnknownUpTo) return CellResult.Unknown;
        return UnknownUpTo == 0 && stage == Reached + 1 ? CellResult.Next : CellResult.NotDone;
    }

    public StageEvidence Evidence(int stage)
    {
        if (stage > Reached) return StageEvidence.None;
        if (stage > AutoStage) return StageEvidence.Manual;
        if (stage == LiveStage) return StageEvidence.Owned;
        if (achievementDone(stage)) return StageEvidence.Achievement;
        if (stage == ItemStage) return StageEvidence.Observed;
        return StageEvidence.Implied;
    }
}

public static class ProgressJudge
{
    /// <summary>
    /// Judge one job. <paramref name="liveTier"/> is the highest tier held right now (0 if none or not read yet).
    /// <paramref name="achievementListLoaded"/> is true while the game holds the full achievement list.
    /// </summary>
    public static JobProgress Judge(RelicSeries series, uint job, CharacterProgress progress, int liveTier, bool achievementListLoaded)
    {
        var itemTier = Math.Max(progress.ObservedTier(series.Id, job), liveTier);
        var stagesDone = series.Achievements.Where(a => a.Job == job && progress.HasAchievement(a.AchievementId)).Select(a => a.Stage).ToHashSet();
        var achievementStage = stagesDone.Count == 0 ? 0 : stagesDone.Max();
        var known = achievementListLoaded || progress.AchievementListLoadedAt is not null;
        return new JobProgress(series, job, series.StageForTier(liveTier), series.StageForTier(itemTier), achievementStage,
            progress.ManualStage(series.Id, job), known, stagesDone.Contains);
    }
}
