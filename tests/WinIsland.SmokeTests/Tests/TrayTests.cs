// TrayTests.cs: ST-12, the tray menu (WinIsland PRD FR-6).
// The menu has Pause for 1 hour, Hide island, Settings and Quit. Hide island turns into
// Show island while the island is hidden; Pause turns into Resume while paused, and the island
// is hidden during the pause. Every change is undone before the test ends.

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class TrayTests(SmokeTestRun run)
{
    private const string Pause = "Pause for 1 hour";
    private const string Resume = "Resume";
    private const string Hide = "Hide island (Ctrl+Alt+H)";
    private const string Show = "Show island (Ctrl+Alt+H)";

    // Same meaning as in HotkeyTests: under 60 percent of its size, the island is hidden.
    private const double HiddenAtMost = 0.6;

    [Fact(DisplayName = "ST-12 Tray menu hides, pauses and resumes the island")]
    public void ST12_TrayMenuHidesPausesAndResumes()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        double shownHeight = island.MeasureHeight();
        Assert.Equal([Pause, Hide, "Settings", "Quit"], TrayMenuPage.Read(run.Automation));

        TrayMenuPage.Open(run.Automation).Click(Hide);
        UiWait.ForAnimation();
        Assert.True(island.MeasureHeight() / shownHeight <= HiddenAtMost, "'Hide island' did not hide the island.");
        Assert.Contains(Show, TrayMenuPage.Read(run.Automation));
        TrayMenuPage.Open(run.Automation).Click(Show);
        UiWait.ForAnimation();

        TrayMenuPage.Open(run.Automation).Click(Pause);
        UiWait.ForAnimation();
        Assert.True(island.MeasureHeight() / shownHeight <= HiddenAtMost, "'Pause for 1 hour' did not hide the island.");
        Assert.Contains(Resume, TrayMenuPage.Read(run.Automation));

        TrayMenuPage.Open(run.Automation).Click(Resume);
        UiWait.ForAnimation();
        Assert.True(island.MeasureHeight() / shownHeight > HiddenAtMost, "'Resume' did not bring the island back.");
        Assert.Contains(Pause, TrayMenuPage.Read(run.Automation));
    }
}
