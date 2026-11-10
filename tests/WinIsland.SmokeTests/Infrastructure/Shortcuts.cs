// Shortcuts.cs: presses WinIsland's default global hotkeys (WinIsland PRD FR-5) the way a
// person does, through real keyboard input, so Windows delivers them to WinIsland exactly as in
// a game. Only the defaults are used: every test starts WinIsland with default settings.

using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class Shortcuts
{
    // Gives Windows a moment to hand the hotkey to WinIsland before the test looks at the island.
    private static readonly TimeSpan AfterPress = TimeSpan.FromMilliseconds(200);

    /// <summary>Ctrl+Alt+I: expands or closes the island.</summary>
    public static void ExpandIsland() => Press(VirtualKeyShort.KEY_I);

    /// <summary>Ctrl+Alt+H: hides or shows the island.</summary>
    public static void HideOrShowIsland() => Press(VirtualKeyShort.KEY_H);

    private static void Press(VirtualKeyShort key)
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, key);
        Thread.Sleep(AfterPress);
    }
}
