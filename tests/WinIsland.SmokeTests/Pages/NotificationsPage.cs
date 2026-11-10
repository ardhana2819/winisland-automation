// NotificationsPage.cs: the island's second expanded page, the last notifications (WinIsland PRD
// FR-15, FR-22) through UI Automation. With none it says "No notifications"; otherwise it shows a
// card per notification with the sender and the message text. WinIsland gives the cards' lines no
// automation id, so a card is found by its text, leaving out the banner, which has its own ids.
// The page is part of the island window; it is on screen after Ctrl+Alt+I and then Alt+Down.

using FlaUI.Core.AutomationElements;

namespace WinIsland.SmokeTests.Pages;

internal sealed class NotificationsPage
{
    private static readonly string[] BannerIds = ["BannerSender", "BannerPreview"];

    private readonly Window _window;

    internal NotificationsPage(Window window) => _window = window;

    /// <summary>True while the page says "No notifications". WinIsland collapses that line when there are cards.</summary>
    public bool ShowsNoNotifications() =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId("NoNotifications")) is not null;

    /// <summary>Whether a card on the page shows this text (a sender or a message).</summary>
    public bool HasCardWith(string text) =>
        _window.FindAllDescendants(condition => condition.ByName(text))
            .Any(element => !BannerIds.Contains(element.Properties.AutomationId.ValueOrDefault));
}
