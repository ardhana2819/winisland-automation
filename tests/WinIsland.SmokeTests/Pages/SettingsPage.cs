// SettingsPage.cs: WinIsland's Settings window (WinIsland PRD FR-19, FR-20, FR-32, FR-37, FR-46)
// through UI Automation. Controls are found by the names WinIsland gives them for screen readers
// (AutomationProperties.Name) or by their x:Name, which WPF reports as the automation id.
// A change in Settings is saved and applied at once, so there is no Save button to press.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

using WinIsland.SmokeTests.Infrastructure;

namespace WinIsland.SmokeTests.Pages;

internal sealed class SettingsPage
{
    public const string WindowTitle = "WinIsland settings";

    private readonly Window _window;

    private SettingsPage(Window window) => _window = window;

    /// <summary>Opens Settings from the tray menu, as a person does, and waits for the window.</summary>
    public static SettingsPage OpenFromTray(UIA3Automation automation)
    {
        TrayMenuPage.Open(automation).Click("Settings");
        Window window = UiWait.For(
            () => WinIslandWindows.All(automation).FirstOrDefault(candidate => candidate.Name == WindowTitle)?.AsWindow(),
            UiWait.Long,
            "the WinIsland settings window to open");
        return new SettingsPage(window);
    }

    /// <summary>"Version 1.8.0-beta.1", under the title.</summary>
    public string VersionText() => ById("VersionText").Name;

    public Button UpdateButton() => ByName("Update WinIsland").AsButton();

    /// <summary>The amber hotkey problem text. Null while there is none: WinIsland then collapses it.</summary>
    public string? ProblemText() =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId("SettingsProblem"))?.Name;

    public void ChooseIslandSize(string option) => Choose("Island size", option);

    public void ChooseLook(string option) => Choose("Look", option);

    public string ChosenLook() => ByName("Look").AsComboBox().SelectedItem?.Name ?? string.Empty;

    public IReadOnlyList<string> LookOptions() =>
        [.. ByName("Look").AsComboBox().Items.Select(item => item.Name)];

    public bool IsLaunchWithWindowsOn() => ByName("Launch with Windows").AsCheckBox().IsChecked == true;

    /// <summary>
    /// Presses the switch with the keyboard (focus it, then Space), which Settings supports
    /// (PRD section 7). Settings can open behind the taskbar's focus after the tray menu, so the
    /// window is brought to the front first and the key is only sent once the switch has focus.
    /// </summary>
    public void PressLaunchWithWindows()
    {
        AutomationElement toggle = ByName("Launch with Windows");
        UiWait.Until(
            () =>
            {
                _window.SetForeground();
                toggle.Focus();
                return toggle.Properties.HasKeyboardFocus.ValueOrDefault;
            },
            UiWait.Short,
            "the 'Launch with Windows' switch to have keyboard focus");
        Keyboard.Type(VirtualKeyShort.SPACE);
    }

    public void Close() => _window.Close();

    private void Choose(string comboName, string option)
    {
        ComboBox box = ByName(comboName).AsComboBox();
        box.Select(option);
        UiWait.Until(() => box.SelectedItem?.Name == option, UiWait.Short, $"'{comboName}' to show '{option}'");
        box.Collapse();
    }

    private AutomationElement ByName(string name) =>
        _window.FindFirstDescendant(condition => condition.ByName(name).And(condition.ByControlType(ControlType.Text).Not()))
        ?? throw new InvalidOperationException($"Settings has no control named '{name}'. Did WinIsland change?");

    private AutomationElement ById(string automationId) =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"Settings has no element '{automationId}'. Did WinIsland change?");
}
