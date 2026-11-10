// IslandPage.cs: the island window as the tests see it through UI Automation.
// WinIsland keeps every layer of the island (clock, music pill, expanded view, banners) in the
// window at all times and shows only one, by fading and scaling the others (PillAnimator in
// WinIsland). UI Automation cannot see opacity, but it does report each element's size on screen,
// which follows that scaling. So the tests read the state from sizes:
//   - hidden island: the whole pill shrinks to 40 percent (HiddenScale 0.4);
//   - a layer that is not shown: it rests at 88 percent (ContentHiddenScale 0.88);
//   - the size setting (FR-32): everything scales by 85, 100, 115 or 130 percent.
// Sizes are compared with each other, never with fixed pixel numbers, so the monitor and its
// scale do not matter.
// The measuring stick is the height of the music page title (MeasureHeight). Its layer rests at
// 88 percent until the island opens, whatever Spotify or a notification does in the meantime,
// and a line's height does not change with the song's name. The clock is no good for this: it
// swaps with the music pill when music plays or pauses (found in the first run, 10 Nov 2026).

using System.Drawing;

using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

using WinIsland.SmokeTests.Infrastructure;

namespace WinIsland.SmokeTests.Pages;

internal sealed class IslandPage
{
    private readonly Window _window;

    private IslandPage(Window window) => _window = window;

    public IntPtr Handle => _window.Properties.NativeWindowHandle.Value;

    public Rectangle Bounds => _window.BoundingRectangle;

    /// <summary>Starts a fresh WinIsland with default settings and waits for its island.</summary>
    public static IslandPage StartFresh(UIA3Automation automation, bool useDefaults = true)
    {
        WinIslandProcess.StopAll();
        // A test that failed half-way can leave the taskbar's hidden icons flyout open.
        TrayMenuPage.CloseHiddenIconsFlyout(automation);
        if (useDefaults)
        {
            SettingsBackup.UseDefaults();
        }

        WinIslandProcess.Start().Dispose();
        IslandPage island = Find(automation);
        // The island fades in at start; measure only once it rests.
        UiWait.ForAnimation();
        return island;
    }

    /// <summary>Finds the island of the WinIsland that runs now.</summary>
    public static IslandPage Find(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);
        Window window = UiWait.For(() => FindIslandWindow(automation), UiWait.Long, "WinIsland's island window to appear");
        return new IslandPage(window);
    }

    /// <summary>The clock text ("14:05"), shown on the idle island.</summary>
    public string ClockText() => Text("ClockText");

    /// <summary>The island's measuring stick, see the top of this file.</summary>
    public double MeasureHeight() => Element("ExpandedTitle").BoundingRectangle.Height;

    public string ExpandedTitleText() => Text("ExpandedTitle");

    /// <summary>The song title on the compact music pill, which replaces the clock while music plays.</summary>
    public string MusicPillTitleText() => Text("MusicPillTitle");

    /// <summary>The clock's height: 100 percent while the clock shows, 88 percent while the music pill replaced it.</summary>
    public double ClockHeight() => Element("ClockText").BoundingRectangle.Height;

    /// <summary>The expanded music page, see MusicPage.</summary>
    public MusicPage Music() => new(_window);

    /// <summary>The expanded notifications page, see NotificationsPage.</summary>
    public NotificationsPage Notifications() => new(_window);

    /// <summary>The sender on the notification banner. The message banner comes before the call banner in the window.</summary>
    public string BannerSenderText() => Text("BannerSender");

    /// <summary>The banner's height: 100 percent while a banner shows, 88 percent while it rests.</summary>
    public double BannerHeight() => Element("BannerSender").BoundingRectangle.Height;

    /// <summary>Whether this text is anywhere on the island, on any layer, shown or not.</summary>
    public bool ContainsText(string text) =>
        _window.FindAllDescendants().Any(element => element.Properties.Name.ValueOrDefault?.Contains(text, StringComparison.Ordinal) == true);

    /// <summary>The hotkey conflict text, empty when there is no conflict.</summary>
    public string ConflictText() => Text("ConflictText");

    /// <summary>The middle of the clock on screen, where a person would click the island.</summary>
    public Point ClockCenter()
    {
        Rectangle bounds = Element("ClockText").BoundingRectangle;
        return new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
    }

    private static Window? FindIslandWindow(UIA3Automation automation)
    {
        // The Glass look adds a second, empty window behind the island; the island is the one with the clock.
        foreach (AutomationElement window in WinIslandWindows.All(automation))
        {
            if (window.FindFirstDescendant(condition => condition.ByAutomationId("ClockText")) is not null)
            {
                return window.AsWindow();
            }
        }

        return null;
    }

    private AutomationElement Element(string automationId) =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"The island has no element '{automationId}'. Did WinIsland change?");

    private string Text(string automationId) => Element(automationId).Name;
}
