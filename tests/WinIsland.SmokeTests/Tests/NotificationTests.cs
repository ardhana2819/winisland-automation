// NotificationTests.cs: ST-19 to ST-24, WinIsland's notifications (WinIsland PRD FR-13 to FR-18, FR-22).
// The notifications are made up and come from TestToastSender, which Windows and WinIsland see as
// another Windows app, "WinIsland Smoke Test". WinIsland shows such apps only with "Show other
// Windows apps" on (off by default), so each test turns it on in Settings first, as a person would.
// ST-19: a notification shows as a banner; with the switch off it does not (FR-13, FR-14).
// ST-20: with "Hide message text" on, the text shows nowhere on the island (FR-17).
// ST-21: the banner hides by itself after the default 6 seconds (FR-16).
// ST-22: the notifications page says "No notifications", then lists the notification (FR-22).
// ST-23: a notification removed in Windows leaves the page (FR-15).
// ST-24: the message text is never written to WinIsland's folder (FR-18).
// Whether a banner shows is read from its size, like every layer (see IslandPage).

using System.Diagnostics;
using System.Text;

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class NotificationTests(SmokeTestRun run)
{
    // A shown layer is 100 percent, a resting one 88 percent: 1 / 0.88 is about 1.14.
    private const double ShownGrowsAtLeast = 1.07;
    private const double RestsAgainWithin = 0.03;

    // FR-16: the default banner time, and room for the 1 second polling without package identity
    // plus the hide animation.
    private static readonly TimeSpan BannerTime = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan BannerTimeMargin = TimeSpan.FromSeconds(2.5);

    // How long a notification that must not show is given to show anyway: well over the 0.2
    // seconds seen with package identity and the 1 second polling without it.
    private static readonly TimeSpan NotShownWait = TimeSpan.FromSeconds(4);

    [Fact(DisplayName = "ST-19 A test notification shows as a banner")]
    public void ST19_TestNotificationShowsAsBanner()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown();
        double restingHeight = island.BannerHeight();

        string shownText = TestToastSender.UniqueText();
        sender.Send(shownText);
        WaitForBanner(island, restingHeight);
        Assert.Equal(TestToastSender.SenderName, island.BannerSenderText());
        Assert.True(island.ContainsText(shownText), $"The banner shows '{TestToastSender.SenderName}' but not the message text '{shownText}'.");
        WaitForBannerToRest(island, restingHeight, UiWait.Long);

        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        settings.SetSwitch(SettingsPage.ShowOtherApps, on: false);
        settings.Close();

        string hiddenText = TestToastSender.UniqueText();
        sender.Send(hiddenText);
        Thread.Sleep(NotShownWait);
        double ratio = island.BannerHeight() / restingHeight;
        Assert.True(ratio < ShownGrowsAtLeast, $"With 'Show other Windows apps' off, a banner still showed ({ratio:P0} of its resting size).");
        Assert.False(island.ContainsText(hiddenText), "With 'Show other Windows apps' off, the island still has the notification's text.");
    }

    [Fact(DisplayName = "ST-20 \"Hide message text\" hides the text")]
    public void ST20_HideMessageTextHidesText()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown(hideMessageText: true);
        double restingHeight = island.BannerHeight();

        string text = TestToastSender.UniqueText();
        sender.Send(text);
        WaitForBanner(island, restingHeight);

        Assert.Equal(TestToastSender.SenderName, island.BannerSenderText());
        Assert.False(island.ContainsText(text), "With 'Hide message text' on, the message text is still on the island.");
    }

    [Fact(DisplayName = "ST-21 The banner goes away by itself")]
    public void ST21_BannerGoesAwayByItself()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown();
        double restingHeight = island.BannerHeight();

        Stopwatch sinceSent = Stopwatch.StartNew();
        sender.Send(TestToastSender.UniqueText());
        WaitForBanner(island, restingHeight);

        // Halfway through its time the banner must still be there.
        TimeSpan untilHalfway = (BannerTime / 2) - sinceSent.Elapsed;
        if (untilHalfway > TimeSpan.Zero)
        {
            Thread.Sleep(untilHalfway);
        }

        double halfwayRatio = island.BannerHeight() / restingHeight;
        Assert.True(halfwayRatio >= ShownGrowsAtLeast, $"The banner was already gone after {sinceSent.Elapsed.TotalSeconds:F1} seconds; it should stay {BannerTime.TotalSeconds} seconds.");

        WaitForBannerToRest(island, restingHeight, BannerTime + BannerTimeMargin - sinceSent.Elapsed);
        Assert.True(
            sinceSent.Elapsed >= BannerTime - TimeSpan.FromSeconds(1),
            $"The banner went away after {sinceSent.Elapsed.TotalSeconds:F1} seconds; it should stay {BannerTime.TotalSeconds} seconds.");
    }

    [Fact(DisplayName = "ST-22 The notifications page lists notifications")]
    public void ST22_NotificationsPageListsNotifications()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown();
        double restingHeight = island.BannerHeight();

        NotificationsPage page = OpenNotificationsPage(island);
        Assert.True(page.ShowsNoNotifications(), "Before any notification, the notifications page does not say 'No notifications'.");
        Shortcuts.ExpandIsland();
        UiWait.ForAnimation();

        string text = TestToastSender.UniqueText();
        sender.Send(text);
        WaitForBanner(island, restingHeight);

        page = OpenNotificationsPage(island);
        UiWait.Until(
            () => page.HasCardWith(TestToastSender.SenderName) && page.HasCardWith(text) && !page.ShowsNoNotifications(),
            UiWait.Short,
            "the notifications page to list the test notification, with its sender and text");
        Shortcuts.ExpandIsland();
    }

    [Fact(DisplayName = "ST-23 A notification cleared in Windows leaves the island")]
    public void ST23_NotificationClearedInWindowsLeavesIsland()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown();
        double restingHeight = island.BannerHeight();

        string text = TestToastSender.UniqueText();
        string tag = sender.Send(text);
        WaitForBanner(island, restingHeight);

        NotificationsPage page = OpenNotificationsPage(island);
        UiWait.Until(() => page.HasCardWith(text), UiWait.Short, "the notifications page to list the test notification");

        TestToastSender.Remove(tag);
        UiWait.Until(
            () => !page.HasCardWith(text) && page.ShowsNoNotifications(),
            UiWait.Long,
            "the test notification to leave the notifications page after it was removed in Windows");
        Shortcuts.ExpandIsland();
    }

    [Fact(DisplayName = "ST-24 Message text is never written to disk")]
    public void ST24_MessageTextIsNeverWrittenToDisk()
    {
        using TestToastSender sender = new();
        IslandPage island = StartWithOtherAppsShown();
        double restingHeight = island.BannerHeight();

        string text = TestToastSender.UniqueText();
        sender.Send(text);
        WaitForBanner(island, restingHeight);
        NotificationsPage page = OpenNotificationsPage(island);
        UiWait.Until(() => page.HasCardWith(text), UiWait.Short, "the notifications page to list the test notification");
        Shortcuts.ExpandIsland();

        // Stopped first, so anything WinIsland would write on the way out is on disk too.
        WinIslandProcess.StopAll();
        List<string> filesWithText = [.. Directory.EnumerateFiles(InstalledApp.SettingsFolder, "*", SearchOption.AllDirectories)
            .Where(file => FileContains(file, text))
            .Select(Path.GetFileName)
            .OfType<string>()];
        Assert.True(filesWithText.Count == 0, $"The message text was written to: {string.Join(", ", filesWithText)}.");
    }

    // A fresh WinIsland with default settings, then the switches a person turns on in Settings.
    private IslandPage StartWithOtherAppsShown(bool hideMessageText = false)
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        settings.SetSwitch(SettingsPage.ShowOtherApps, on: true);
        if (hideMessageText)
        {
            settings.SetSwitch(SettingsPage.HideMessageText, on: true);
        }

        settings.Close();
        UiWait.Until(() => SavedSettings.Read("ShowOtherApps") == "True", UiWait.Short, "'Show other Windows apps' to be saved");
        UiWait.ForAnimation();
        return island;
    }

    private static void WaitForBanner(IslandPage island, double restingHeight) =>
        UiWait.Until(
            () => island.BannerHeight() / restingHeight >= ShownGrowsAtLeast,
            UiWait.Long,
            () => "the test notification's banner to show. If no banner came at all, check that WinIsland may read " +
                $"notifications (Windows Settings > Privacy > Notifications). The banner shows '{island.BannerSenderText()}'");

    private static void WaitForBannerToRest(IslandPage island, double restingHeight, TimeSpan timeout) =>
        UiWait.Until(
            () => Math.Abs((island.BannerHeight() / restingHeight) - 1) <= RestsAgainWithin,
            timeout > TimeSpan.Zero ? timeout : TimeSpan.Zero,
            "the banner to hide by itself");

    private static NotificationsPage OpenNotificationsPage(IslandPage island)
    {
        Shortcuts.ExpandIsland();
        UiWait.ForAnimation();
        Shortcuts.ShowNotificationsPage();
        UiWait.ForAnimation();
        return island.Notifications();
    }

    // Logs and settings are text; both common encodings are searched, so no way of writing hides it.
    // A file that cannot be read fails the test instead of being skipped, so nothing goes unchecked.
    private static bool FileContains(string path, string text)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return Encoding.UTF8.GetString(bytes).Contains(text, StringComparison.Ordinal)
            || Encoding.Unicode.GetString(bytes).Contains(text, StringComparison.Ordinal);
    }
}
