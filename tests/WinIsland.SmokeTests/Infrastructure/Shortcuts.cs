// Shortcuts.cs: presses WinIsland's default global hotkeys (WinIsland PRD FR-5) and its music
// keys (FR-8) the way a person does, through real keyboard input, so Windows delivers them to
// WinIsland exactly as in a game. Only the defaults are used: every test starts WinIsland with
// default settings.

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

    /// <summary>Space: plays or pauses the music, only while the island is open (FR-8).</summary>
    public static void PlayOrPauseMusic() => PressKeys(VirtualKeyShort.SPACE);

    /// <summary>Ctrl+Right: the next song, only while the island is open (FR-8).</summary>
    public static void NextSong() => PressKeys(VirtualKeyShort.CONTROL, VirtualKeyShort.RIGHT);

    /// <summary>Ctrl+Left: the previous song, only while the island is open (FR-8).</summary>
    public static void PreviousSong() => PressKeys(VirtualKeyShort.CONTROL, VirtualKeyShort.LEFT);

    /// <summary>Alt+Down: the notifications page, only while the island is open (FR-21).</summary>
    public static void ShowNotificationsPage() => PressKeys(VirtualKeyShort.ALT, VirtualKeyShort.DOWN);

    private static void Press(VirtualKeyShort key) => PressKeys(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, key);

    private static void PressKeys(params VirtualKeyShort[] keys)
    {
        Keyboard.TypeSimultaneously(keys);
        Thread.Sleep(AfterPress);
    }
}
