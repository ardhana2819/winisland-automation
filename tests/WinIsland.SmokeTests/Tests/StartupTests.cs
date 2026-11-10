// StartupTests.cs: ST-02 to ST-04, WinIsland starts the way the PRD says.
// ST-02: the island sits at the top center, always on top, showing the time (FR-1, FR-2).
// ST-03: clicking the island leaves the game (here a stand-in window) in front (FR-3).
// ST-04: starting WinIsland again does not make a second island (FR-6b).

using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using FlaUI.Core.Input;

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class StartupTests(SmokeTestRun run)
{
    // Window edges can land half a pixel off after DPI scaling.
    private const int PixelTolerance = 2;

    [Fact(DisplayName = "ST-02 Island appears at the top center with the time")]
    public void ST02_IslandAppearsAtTopCenterWithTime()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);

        Rectangle screen = Screen.PrimaryScreen!.Bounds;
        Rectangle window = island.Bounds;
        int windowCenter = window.Left + (window.Width / 2);
        int screenCenter = screen.Left + (screen.Width / 2);
        Assert.InRange(windowCenter, screenCenter - PixelTolerance, screenCenter + PixelTolerance);
        Assert.InRange(window.Top, screen.Top - PixelTolerance, screen.Top + PixelTolerance);

        long extendedStyle = NativeMethods.GetWindowLongPtr(island.Handle, NativeMethods.ExtendedStyleIndex).ToInt64();
        Assert.True((extendedStyle & NativeMethods.TopmostStyle) != 0, "The island window is not always on top.");

        // The clock may tick over while the test runs, so this minute and the one before both count.
        DateTime now = DateTime.Now;
        string[] expected =
        [
            now.ToString("HH:mm", CultureInfo.InvariantCulture),
            now.AddMinutes(-1).ToString("HH:mm", CultureInfo.InvariantCulture),
        ];
        Assert.Contains(island.ClockText(), expected);
    }

    [Fact(DisplayName = "ST-03 Clicking the island does not take focus")]
    public void ST03_ClickingIslandDoesNotTakeFocus()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        using FocusProbeWindow game = new();
        game.BringToFront(run.Automation);

        Mouse.Click(island.ClockCenter());
        UiWait.ForAnimation();

        Assert.True(
            NativeMethods.GetForegroundWindow() == game.Handle,
            "After clicking the island, the stand-in game window was no longer in front.");

        // The click expanded the island; close it so the next test starts calm.
        Shortcuts.ExpandIsland();
    }

    [Fact(DisplayName = "ST-04 Starting WinIsland twice gives one island")]
    public void ST04_StartingTwiceGivesOneIsland()
    {
        IslandPage.StartFresh(run.Automation);

        using Process second = WinIslandProcess.Start();
        Assert.True(second.WaitForExit(UiWait.Long), "The second WinIsland did not close by itself.");
        Assert.Equal(1, WinIslandProcess.RunningCount());
        IslandPage.Find(run.Automation);
    }
}
