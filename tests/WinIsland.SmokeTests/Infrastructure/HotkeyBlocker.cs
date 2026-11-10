// HotkeyBlocker.cs: takes a global hotkey before WinIsland starts, like another program would,
// so the test can check WinIsland's conflict message (WinIsland PRD FR-5, ST-13).
// Windows ties a hotkey to the thread that registered it, and only that thread may free it,
// so the hotkey lives on its own thread for as long as the blocker exists.

namespace WinIsland.SmokeTests.Infrastructure;

internal sealed class HotkeyBlocker : IDisposable
{
    private const int HotkeyId = 0x5157;

    private readonly Thread _owner;
    private readonly ManualResetEventSlim _registered = new();
    private readonly ManualResetEventSlim _release = new();
    private bool _succeeded;

    private HotkeyBlocker(uint modifiers, uint key)
    {
        _owner = new Thread(() =>
        {
            _succeeded = NativeMethods.RegisterHotKey(IntPtr.Zero, HotkeyId, modifiers | NativeMethods.NoRepeatModifier, key);
            _registered.Set();
            _release.Wait();
            if (_succeeded)
            {
                NativeMethods.UnregisterHotKey(IntPtr.Zero, HotkeyId);
            }
        })
        {
            IsBackground = true,
            Name = "Hotkey blocker",
        };
        _owner.Start();
        _registered.Wait();
    }

    /// <summary>Takes Ctrl+Alt+I, WinIsland's default expand hotkey.</summary>
    public static HotkeyBlocker TakeExpandHotkey()
    {
        const uint KeyI = 0x49;
        HotkeyBlocker blocker = new(NativeMethods.ControlModifier | NativeMethods.AltModifier, KeyI);
        if (!blocker._succeeded)
        {
            blocker.Dispose();
            throw new InvalidOperationException(
                "Could not take Ctrl+Alt+I for the test: another program (or a WinIsland that is still running) already has it.");
        }

        return blocker;
    }

    public void Dispose()
    {
        _release.Set();
        _owner.Join();
        _registered.Dispose();
        _release.Dispose();
    }
}
