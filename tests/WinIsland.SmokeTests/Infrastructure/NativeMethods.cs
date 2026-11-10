// NativeMethods.cs: the few Windows functions the tests call directly, because UI Automation
// does not offer them: which window is in front (ST-03), whether a window is always on top
// (ST-02), taking a hotkey before WinIsland does (ST-13), and reading the screen in real
// pixels like WinIsland does, so the screen size, mouse and UI Automation all use one scale.

using System.Runtime.InteropServices;

namespace WinIsland.SmokeTests.Infrastructure;

internal static partial class NativeMethods
{
    public const int ExtendedStyleIndex = -20;
    public const long TopmostStyle = 0x00000008;

    public const uint AltModifier = 0x0001;
    public const uint ControlModifier = 0x0002;
    public const uint NoRepeatModifier = 0x4000;

    // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
    public static readonly IntPtr PerMonitorAwareV2 = new(-4);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDpiAwarenessContext(IntPtr context);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial IntPtr GetWindowLongPtr(IntPtr window, int index);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr window, int id);
}
