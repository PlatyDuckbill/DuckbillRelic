using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DuckbillRelic.Core.Catalog;
using DuckbillRelic.Core.Progress;
using DuckbillRelic.Plugin.Game;
using DuckbillRelic.Plugin.Localization;
using T = DuckbillRelic.Plugin.Localization.TextKey;

namespace DuckbillRelic.Plugin.UI;

/// <summary>
/// Minimal screen (docs/product/SCREENS.md): tabs, help, achievement notice, job × stage table and a short detail panel.
/// Cells show only results; evidence is in the tooltip and the detail line.
/// </summary>
internal sealed class MainWindow : Window
{
    // ClassJob rows in the in-game role order. Group starts get a thin divider.
    private static readonly uint[] CombatOrder = [19, 21, 32, 37, 24, 28, 33, 40, 20, 22, 30, 34, 39, 41, 23, 31, 38, 25, 27, 35, 42];
    private static readonly HashSet<uint> CombatGroupStarts = [19, 24, 20, 23, 25];
    private static readonly uint[] CraftOrder = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18];
    private static readonly HashSet<uint> CraftGroupStarts = [8, 16];
    private const uint JobIconBase = 62100;

    private static readonly Vector4 Gold = new(0.85f, 0.70f, 0.35f, 1f);
    private static readonly Vector4 Sky = new(0.49f, 0.72f, 0.85f, 1f);
    private static readonly Vector4 Todo = new(0.48f, 0.51f, 0.58f, 1f);
    private static readonly Vector4 Mark = new(0.91f, 0.89f, 0.84f, 1f);
    private static readonly Vector4 NotApplicable = new(0.08f, 0.09f, 0.11f, 1f);
    private static readonly Vector4 SeriesRow = new(0.17f, 0.19f, 0.23f, 1f);
    private static readonly Vector4 Column = new(0.49f, 0.72f, 0.85f, 0.08f);
    private static readonly Vector4 Notice = new(0.11f, 0.15f, 0.19f, 1f);

    private readonly RelicCatalog catalog;
    private readonly GameNames names;
    private readonly ProgressObserver observer;
    private readonly ITextureProvider textures;
    private readonly string? disabledNotice;
    private readonly HashSet<string> collapsed = [];
    private (string Series, uint Job, int Stage)? selection;

    public MainWindow(RelicCatalog catalog, GameNames names, ProgressObserver observer, ITextureProvider textures, IReadOnlyCollection<string> disabledSeries)
        : base("DuckbillRelic###DuckbillRelic.Main")
    {
        this.catalog = catalog;
        this.names = names;
        this.observer = observer;
        this.textures = textures;
        if (disabledSeries.Count > 0) disabledNotice = UiText.Get(T.SeriesDisabled, string.Join(", ", disabledSeries.Select(names.Series)));
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(640, 360), MaximumSize = new Vector2(float.MaxValue) };
        Size = new Vector2(1040, 640);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var snapshot = observer.Current;
        var tabStart = ImGui.GetCursorPos();
        SeriesKind? tab = null;
        if (ImGui.BeginTabBar("###DuckbillRelic.Tabs"))
        {
            if (ImGui.BeginTabItem(UiText.Label(T.TabWeapons, "Weapons"))) { tab = SeriesKind.Weapon; ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem(UiText.Label(T.TabTools, "Tools"))) { tab = SeriesKind.Tool; ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }
        var afterTabs = ImGui.GetCursorPos();
        DrawHelpButton(tabStart, snapshot);
        ImGui.SetCursorPos(afterTabs);

        if (disabledNotice is not null) ImGui.TextColored(Gold, disabledNotice);
        if (snapshot is null || tab is null)
        {
            ImGui.TextDisabled(UiText.Get(T.NotLoggedIn));
            return;
        }
        if (!snapshot.AchievementListLoaded && snapshot.Progress.AchievementListLoadedAt is null) DrawAchievementNotice();

        var detailWidth = 320 * ImGuiHelpers.GlobalScale;
        var tableWidth = Math.Max(ImGui.GetContentRegionAvail().X - detailWidth - ImGui.GetStyle().ItemSpacing.X, 200);
        if (ImGui.BeginChild("###DuckbillRelic.Matrix", new Vector2(tableWidth, 0))) DrawMatrix(tab.Value, snapshot);
        ImGui.EndChild();
        ImGui.SameLine();
        if (ImGui.BeginChild("###DuckbillRelic.Detail", Vector2.Zero, true)) DrawDetail(snapshot);
        ImGui.EndChild();
    }

    // ---------- top ----------
    private void DrawHelpButton(Vector2 tabStart, ProgressObserver.Snapshot? snapshot)
    {
        var size = ImGui.GetFrameHeight();
        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - size, tabStart.Y));
        if (ImGui.Button("?###DuckbillRelic.Help", new Vector2(size, size))) ImGui.OpenPopup("###DuckbillRelic.HelpPopup");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(UiText.Get(T.HelpButton));
        if (!ImGui.BeginPopup("###DuckbillRelic.HelpPopup")) return;
        ImGui.TextDisabled(UiText.Get(T.HelpShapes));
        LegendRow(CellResult.Done, StageEvidence.Owned, T.ResultDone);
        LegendRow(CellResult.Next, StageEvidence.None, T.ResultNext);
        LegendRow(CellResult.NotDone, StageEvidence.None, T.ResultNotDone);
        LegendRow(CellResult.Unknown, StageEvidence.None, T.ResultUnknown);
        LegendRow(CellResult.NotApplicable, StageEvidence.None, T.ResultNotApplicable);
        LegendRow(CellResult.Done, StageEvidence.Manual, T.ResultManualMark);
        ImGui.Separator();
        ImGui.TextDisabled(UiText.Get(T.HelpLastRead));
        ImGui.TextUnformatted(UiText.Get(T.HelpInventoryLive));
        ImGui.TextUnformatted(snapshot is null ? UiText.Get(T.HelpAchievementsNone)
            : snapshot.AchievementListLoaded ? UiText.Get(T.HelpAchievementsLive)
            : snapshot.Progress.AchievementListLoadedAt is { } at ? UiText.Get(T.HelpAchievementsAt, at.ToLocalTime().ToString("g", UiText.Culture))
            : UiText.Get(T.HelpAchievementsNone));
        ImGui.EndPopup();
    }

    private static void LegendRow(CellResult result, StageEvidence evidence, T label)
    {
        var size = new Vector2(ImGui.GetTextLineHeight());
        var min = ImGui.GetCursorScreenPos();
        ImGui.Dummy(size);
        DrawGlyph(ImGui.GetWindowDrawList(), min, min + size, result, evidence == StageEvidence.Manual);
        ImGui.SameLine();
        ImGui.TextUnformatted(UiText.Get(label));
    }

    private static void DrawAchievementNotice()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Notice);
        var height = ImGui.GetTextLineHeightWithSpacing() * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        if (ImGui.BeginChild("###DuckbillRelic.Notice", new Vector2(0, height)))
        {
            ImGui.TextColored(Sky, "?");
            ImGui.SameLine();
            ImGui.TextWrapped(UiText.Get(T.AchievementBanner));
        }
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    // ---------- table ----------
    private void DrawMatrix(SeriesKind kind, ProgressObserver.Snapshot snapshot)
    {
        var series = catalog.Series.Where(s => s.Kind == kind).ToList();
        var order = kind == SeriesKind.Tool ? CraftOrder : CombatOrder;
        var groupStarts = kind == SeriesKind.Tool ? CraftGroupStarts : CombatGroupStarts;
        var jobs = order.Where(j => series.Any(s => s.HasJob(j))).ToArray();
        var scale = ImGuiHelpers.GlobalScale;
        var cell = new Vector2(26 * scale, 24 * scale);
        var flags = ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoPadInnerX;
        if (!ImGui.BeginTable("###DuckbillRelic.Table", jobs.Length + 1, flags)) return;
        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableSetupColumn("###Stage", ImGuiTableColumnFlags.WidthFixed, 170 * scale);
        foreach (var job in jobs) ImGui.TableSetupColumn($"###Job{job}", ImGuiTableColumnFlags.WidthFixed, cell.X);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers, cell.Y + 4 * scale);
        for (var c = 0; c < jobs.Length; c++)
        {
            ImGui.TableSetColumnIndex(c + 1);
            DrawJobHeader(jobs[c], cell, groupStarts.Contains(jobs[c]));
        }

        var list = new List<(RelicSeries Series, Dictionary<uint, JobProgress> Jobs)>();
        foreach (var s in series)
            list.Add((s, s.Jobs.ToDictionary(j => j, j => ProgressJudge.Judge(s, j, snapshot.Progress,
                snapshot.LiveTiers.GetValueOrDefault((s.Id, j)), snapshot.AchievementListLoaded))));

        foreach (var (s, judged) in list)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, cell.Y + 4 * scale);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(SeriesRow));
            ImGui.TableSetColumnIndex(0);
            var open = !collapsed.Contains(s.Id);
            var done = judged.Values.Count(p => p.IsComplete);
            if (ImGui.Selectable($"{(open ? "▾" : "▸")} {names.Series(s.Id)}  {done}/{s.Jobs.Count}###Series.{s.Id}", false, ImGuiSelectableFlags.SpanAllColumns))
            {
                if (open) collapsed.Add(s.Id); else collapsed.Remove(s.Id);
            }
            for (var c = 0; c < jobs.Length; c++)
                if (!s.HasJob(jobs[c])) ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(NotApplicable), c + 1);
            if (!open) continue;

            for (var stage = 1; stage <= s.StageCount; stage++)
            {
                ImGui.TableNextRow(ImGuiTableRowFlags.None, cell.Y);
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(stage.ToString(CultureInfo.InvariantCulture));
                ImGui.SameLine(20 * scale);
                var selectedRow = selection is { } sel && sel.Series == s.Id && sel.Stage == stage;
                if (selectedRow) ImGui.TextColored(Gold, UiText.Stage(s.Id, stage)); else ImGui.TextUnformatted(UiText.Stage(s.Id, stage));
                for (var c = 0; c < jobs.Length; c++)
                {
                    var job = jobs[c];
                    ImGui.TableSetColumnIndex(c + 1);
                    if (!judged.TryGetValue(job, out var progress))
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(NotApplicable));
                        continue;
                    }
                    if (selection?.Job == job) ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(Column));
                    DrawCell(s, progress, stage, cell);
                }
            }
        }
        ImGui.EndTable();
    }

    private void DrawJobHeader(uint job, Vector2 cell, bool groupStart)
    {
        var min = ImGui.GetCursorScreenPos();
        var icon = cell.Y - 2 * ImGuiHelpers.GlobalScale;
        if (textures.TryGetFromGameIcon(new GameIconLookup(JobIconBase + job), out var texture) && texture.TryGetWrap(out var wrap, out _))
        {
            ImGui.SetCursorScreenPos(min + new Vector2((cell.X - icon) / 2, 1));
            ImGui.Image(wrap.Handle, new Vector2(icon));
        }
        else ImGui.TextUnformatted(names.JobShort(job));
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(names.Job(job));
        if (selection?.Job == job)
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, min.Y + cell.Y + 2), new Vector2(min.X + cell.X, min.Y + cell.Y + 2), ImGui.GetColorU32(Gold), 2);
        if (groupStart)
            ImGui.GetWindowDrawList().AddLine(min, new Vector2(min.X, min.Y + cell.Y), ImGui.GetColorU32(ImGuiCol.Separator));
    }

    private void DrawCell(RelicSeries s, JobProgress progress, int stage, Vector2 cell)
    {
        var result = progress.Result(stage);
        var evidence = progress.Evidence(stage);
        var min = ImGui.GetCursorScreenPos();
        if (ImGui.InvisibleButton($"###Cell.{s.Id}.{stage}.{progress.Job}", cell)) selection = (s.Id, progress.Job, stage);
        var max = min + cell;
        var draw = ImGui.GetWindowDrawList();
        DrawGlyph(draw, min, max, result, evidence == StageEvidence.Manual);
        if (selection is { } sel && sel.Series == s.Id && sel.Job == progress.Job && sel.Stage == stage)
            draw.AddRect(min + new Vector2(2), max - new Vector2(2), ImGui.GetColorU32(Sky), 3, ImDrawFlags.None, 2);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"{names.Job(progress.Job)} · {names.Item(s.StageItem(stage, progress.Job))}\n{ResultText(result, evidence)}");
    }

    private static void DrawGlyph(ImDrawListPtr draw, Vector2 min, Vector2 max, CellResult result, bool manual)
    {
        var center = (min + max) / 2;
        var half = 7 * ImGuiHelpers.GlobalScale;
        switch (result)
        {
            case CellResult.Done:
                draw.AddRectFilled(center - new Vector2(half), center + new Vector2(half), ImGui.GetColorU32(Gold), 2);
                if (manual)
                {
                    var corner = center + new Vector2(half, -half);
                    draw.AddCircleFilled(corner, 4.5f * ImGuiHelpers.GlobalScale, ImGui.GetColorU32(NotApplicable));
                    draw.AddCircleFilled(corner, 3f * ImGuiHelpers.GlobalScale, ImGui.GetColorU32(Mark));
                }
                break;
            case CellResult.Next:
                draw.AddRect(center - new Vector2(half - 1), center + new Vector2(half - 1), ImGui.GetColorU32(Gold), 2, ImDrawFlags.None, 2);
                break;
            case CellResult.NotDone:
                draw.AddCircleFilled(center, 3f * ImGuiHelpers.GlobalScale, ImGui.GetColorU32(Todo));
                break;
            case CellResult.Unknown:
                var size = ImGui.CalcTextSize("?");
                draw.AddText(center - size / 2, ImGui.GetColorU32(Sky), "?");
                break;
            case CellResult.NotApplicable:
                draw.AddRectFilled(center - new Vector2(half), center + new Vector2(half), ImGui.GetColorU32(NotApplicable), 2);
                draw.AddRect(center - new Vector2(half), center + new Vector2(half), ImGui.GetColorU32(ImGuiCol.Border), 2);
                break;
        }
    }

    private static string ResultText(CellResult result, StageEvidence evidence) => result switch
    {
        CellResult.Done => $"{UiText.Get(T.ResultDone)} · {EvidenceText(evidence)}",
        CellResult.Next => UiText.Get(T.ResultNext),
        CellResult.Unknown => UiText.Get(T.ResultUnknown),
        CellResult.NotApplicable => UiText.Get(T.ResultNotApplicable),
        _ => UiText.Get(T.ResultNotDone),
    };

    private static string EvidenceText(StageEvidence evidence) => evidence switch
    {
        StageEvidence.Owned => UiText.Get(T.EvidenceOwned),
        StageEvidence.Observed => UiText.Get(T.EvidenceObserved),
        StageEvidence.Achievement => UiText.Get(T.EvidenceAchievement),
        StageEvidence.Manual => UiText.Get(T.EvidenceManual),
        _ => UiText.Get(T.EvidenceImplied),
    };

    // ---------- detail ----------
    private void DrawDetail(ProgressObserver.Snapshot snapshot)
    {
        if (selection is not { } sel || catalog.Get(sel.Series) is not { } s || !s.HasJob(sel.Job) || sel.Stage > s.StageCount)
        {
            ImGui.TextDisabled(UiText.Get(T.SelectCellHint));
            return;
        }
        var progress = ProgressJudge.Judge(s, sel.Job, snapshot.Progress, snapshot.LiveTiers.GetValueOrDefault((s.Id, sel.Job)), snapshot.AchievementListLoaded);
        var result = progress.Result(sel.Stage);
        var evidence = progress.Evidence(sel.Stage);

        ImGui.TextDisabled(UiText.Get(T.DetailHeader, names.Job(sel.Job), names.Series(s.Id), sel.Stage));
        ImGui.TextWrapped(names.Item(s.StageItem(sel.Stage, sel.Job)));
        ImGui.Spacing();

        var glyph = new Vector2(ImGui.GetTextLineHeight());
        var min = ImGui.GetCursorScreenPos();
        ImGui.Dummy(glyph);
        DrawGlyph(ImGui.GetWindowDrawList(), min, min + glyph, result, evidence == StageEvidence.Manual);
        ImGui.SameLine();
        ImGui.TextUnformatted(ResultLabel(result));
        var reason = result switch
        {
            CellResult.Done => EvidenceText(evidence),
            CellResult.Unknown => UiText.Get(T.UnknownReason),
            _ => null,
        };
        if (reason is not null) ImGui.TextDisabled(reason);
        if (s.Id == "cosmic")
        {
            ImGui.Spacing();
            ImGui.PushTextWrapPos(0);
            ImGui.TextDisabled(UiText.Get(T.CosmicNote));
            ImGui.PopTextWrapPos();
        }

        ImGui.Spacing();
        if (ImGui.Button("⋯###DuckbillRelic.More")) ImGui.OpenPopup("###DuckbillRelic.MorePopup");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(UiText.Get(T.MoreActions));
        if (ImGui.BeginPopup("###DuckbillRelic.MorePopup"))
        {
            if (result != CellResult.Done && ImGui.Selectable(UiText.Label(T.MarkDoneUpTo, "MarkDone")))
                observer.SetManualStage(s.Id, sel.Job, sel.Stage);
            if (progress.ManualStage >= sel.Stage && ImGui.Selectable(UiText.Label(T.ClearManualFrom, "ClearManual")))
                observer.SetManualStage(s.Id, sel.Job, sel.Stage - 1);
            ImGui.EndPopup();
        }
    }

    private static string ResultLabel(CellResult result) => result switch
    {
        CellResult.Done => UiText.Get(T.ResultDone),
        CellResult.Next => UiText.Get(T.ResultNext),
        CellResult.Unknown => UiText.Get(T.ResultUnknown),
        CellResult.NotApplicable => UiText.Get(T.ResultNotApplicable),
        _ => UiText.Get(T.ResultNotDone),
    };
}
