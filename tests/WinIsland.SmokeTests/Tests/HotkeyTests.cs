// HotkeyTests.cs: ST-10, ST-11 and ST-13, WinIsland's global hotkeys (WinIsland PRD FR-5).
// ST-10: Ctrl+Alt+H hides the island and shows it again.
// ST-11: Ctrl+Alt+I opens the island on the music page and closes it again (FR-2, FR-21).
// ST-13: when another program already has Ctrl+Alt+I, WinIsland says so, on the island and in Settings.
// How the state is read from sizes is explained in IslandPage.

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class HotkeyTests(SmokeTestRun run)
{
    // Hidden, the pill shrinks to 40 percent; anything under 60 percent is clearly hidden.
    private const double HiddenAtMost = 0.6;

    // A shown layer is 100 percent, a resting one 88 percent: 1 / 0.88 is about 1.14.
    private const double OpenedGrowsAtLeast = 1.07;

    private const double ShownAgainWithin = 0.05;

    [Fact(DisplayName = "ST-10 Ctrl+Alt+H hides and shows the island")]
    public void ST10_HideAndShowHotkey()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        double shownHeight = island.MeasureHeight();

        Shortcuts.HideOrShowIsland();
        UiWait.ForAnimation();
        double hiddenRatio = island.MeasureHeight() / shownHeight;
        Assert.True(hiddenRatio <= HiddenAtMost, $"After Ctrl+Alt+H the island is still {hiddenRatio:P0} of its size; it should be hidden.");

        Shortcuts.HideOrShowIsland();
        UiWait.ForAnimation();
        double shownRatio = island.MeasureHeight() / shownHeight;
        Assert.True(Math.Abs(shownRatio - 1) <= ShownAgainWithin, $"After pressing Ctrl+Alt+H again the island is {shownRatio:P0} of its size; it should be back.");
    }

    [Fact(DisplayName = "ST-11 Ctrl+Alt+I opens the music page and closes it")]
    public void ST11_ExpandHotkeyOpensMusicPage()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        double restingHeight = island.MeasureHeight();

        Shortcuts.ExpandIsland();
        UiWait.ForAnimation();
        double openRatio = island.MeasureHeight() / restingHeight;
        Assert.True(openRatio >= OpenedGrowsAtLeast, $"After Ctrl+Alt+I the music page did not open (its title is {openRatio:P0} of its resting size).");
        // "Not playing" without music (FR-24), otherwise the song; ST-18 checks the empty view exactly.
        Assert.False(string.IsNullOrWhiteSpace(island.ExpandedTitleText()), "The open music page shows no title.");

        Shortcuts.ExpandIsland();
        UiWait.ForAnimation();
        double closedRatio = island.MeasureHeight() / restingHeight;
        Assert.True(Math.Abs(closedRatio - 1) <= ShownAgainWithin, $"After pressing Ctrl+Alt+I again the island did not close (music title at {closedRatio:P0}).");
    }

    [Fact(DisplayName = "ST-13 A taken hotkey shows a clear conflict message")]
    public void ST13_TakenHotkeyShowsConflict()
    {
        WinIslandProcess.StopAll();
        using HotkeyBlocker blocker = HotkeyBlocker.TakeExpandHotkey();

        IslandPage island = IslandPage.StartFresh(run.Automation);
        UiWait.Until(() => island.ConflictText().Contains("Ctrl+Alt+I", StringComparison.Ordinal), UiWait.Short, "the island's conflict banner to name Ctrl+Alt+I");

        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        string? problem = settings.ProblemText();
        Assert.True(problem is not null && problem.Contains("Ctrl+Alt+I", StringComparison.Ordinal), $"Settings does not name the taken hotkey. It shows: '{problem}'.");
        settings.Close();

        // Free the hotkey only after WinIsland is gone, so it never gets the key in the middle of the test.
        WinIslandProcess.StopAll();
    }
}
