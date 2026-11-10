// TrayMenuPage.cs: WinIsland's tray icon and its menu (WinIsland PRD FR-6) through UI Automation.
// Windows 11 often keeps the icon in the hidden icons flyout (the ^ on the taskbar), so the
// page looks on the taskbar first and opens that flyout when needed. Taskbar parts are found by
// their automation id and class, which stay the same in every Windows language; only the icon
// itself is found by its name, which is WinIsland's own tooltip, "WinIsland".

using System.Drawing;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

using WinIsland.SmokeTests.Infrastructure;

namespace WinIsland.SmokeTests.Pages;

internal sealed class TrayMenuPage
{
    private const string IconName = "WinIsland";
    private const string TaskbarClass = "Shell_TrayWnd";
    private const string HiddenIconsButtonClass = "SystemTray.NormalButton";
    private const string HiddenIconsButtonId = "SystemTrayIcon";
    private const string HiddenIconsFlyoutClass = "TopLevelWindowForOverflowXamlIsland";
    private const string TrayIconId = "NotifyItemIcon";

    // The flyout slides in for a moment after it appears; clicking during the slide can miss.
    private static readonly TimeSpan FlyoutSlideTime = TimeSpan.FromMilliseconds(400);

    private readonly UIA3Automation _automation;
    private readonly AutomationElement _menu;

    private TrayMenuPage(UIA3Automation automation, AutomationElement menu)
    {
        _automation = automation;
        _menu = menu;
    }

    /// <summary>Right-clicks the WinIsland tray icon and waits for its menu.</summary>
    public static TrayMenuPage Open(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);
        AutomationElement icon = FindIcon(automation);
        // The center of the icon's box: Windows reports no "clickable point" while the flyout slides in.
        Rectangle box = icon.BoundingRectangle;
        Mouse.RightClick(new Point(box.Left + (box.Width / 2), box.Top + (box.Height / 2)));
        AutomationElement menu = UiWait.For(() => FindMenu(automation), UiWait.Short, "WinIsland's tray menu to open");
        return new TrayMenuPage(automation, menu);
    }

    /// <summary>Closes the hidden icons flyout if a test or a failed test left it open.</summary>
    public static void CloseHiddenIconsFlyout(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);
        // Windows removes the flyout window when it closes, so "it exists" means "it is open".
        if (FindFlyout(automation) is null)
        {
            return;
        }

        HiddenIconsButton(automation).Invoke();
        UiWait.Until(() => FindFlyout(automation) is null, UiWait.Short, "the hidden icons flyout to close");
    }

    /// <summary>The menu's items, in order, as they read on screen.</summary>
    public IReadOnlyList<string> ItemNames() =>
        [.. Items().Select(item => item.Name)];

    /// <summary>Clicks the item with this text. The menu closes, as it does for a person.</summary>
    public void Click(string itemName)
    {
        AutomationElement item = Items().FirstOrDefault(candidate => candidate.Name == itemName)
            ?? throw new InvalidOperationException(
                $"The tray menu has no item '{itemName}'. It has: {string.Join(", ", ItemNames())}.");
        item.AsMenuItem().Invoke();
        CloseHiddenIconsFlyout(_automation);
    }

    /// <summary>Opens the menu, reads it, and closes it again without choosing anything.</summary>
    public static IReadOnlyList<string> Read(UIA3Automation automation)
    {
        TrayMenuPage page = Open(automation);
        IReadOnlyList<string> names = page.ItemNames();
        Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
        CloseHiddenIconsFlyout(automation);
        return names;
    }

    private IEnumerable<AutomationElement> Items() =>
        _menu.FindAllDescendants(condition => condition.ByControlType(ControlType.MenuItem));

    private static AutomationElement FindIcon(UIA3Automation automation)
    {
        AutomationElement desktop = automation.GetDesktop();
        AutomationElement taskbar = desktop.FindFirstChild(condition => condition.ByClassName(TaskbarClass))
            ?? throw new InvalidOperationException("The Windows taskbar was not found.");

        AutomationElement? icon = FindIconIn(taskbar);
        if (icon is not null)
        {
            return icon;
        }

        // Open the flyout only when it is closed: pressing ^ on an open flyout closes it again.
        if (FindFlyout(automation) is null)
        {
            HiddenIconsButton(automation).Invoke();
        }

        AutomationElement flyout = UiWait.For(() => FindFlyout(automation), UiWait.Short, "the hidden icons flyout to open");
        AutomationElement found = UiWait.For(() => FindIconIn(flyout), UiWait.Short, "WinIsland's tray icon in the hidden icons flyout. Is WinIsland running?");
        Thread.Sleep(FlyoutSlideTime);
        return found;
    }

    private static AutomationElement? FindIconIn(AutomationElement area) =>
        area.FindFirstDescendant(condition => condition.ByAutomationId(TrayIconId).And(condition.ByName(IconName)));

    private static AutomationElement? FindFlyout(UIA3Automation automation) =>
        automation.GetDesktop().FindFirstChild(condition => condition.ByClassName(HiddenIconsFlyoutClass));

    private static Button HiddenIconsButton(UIA3Automation automation) =>
        automation.GetDesktop()
            .FindFirstChild(condition => condition.ByClassName(TaskbarClass))?
            .FindFirstDescendant(condition => condition.ByClassName(HiddenIconsButtonClass).And(condition.ByAutomationId(HiddenIconsButtonId)))?
            .AsButton()
        ?? throw new InvalidOperationException("WinIsland's tray icon is not on the taskbar, and there is no hidden icons button.");

    private static AutomationElement? FindMenu(UIA3Automation automation) =>
        WinIslandWindows.All(automation).FirstOrDefault(window => window.ControlType == ControlType.Menu);
}
