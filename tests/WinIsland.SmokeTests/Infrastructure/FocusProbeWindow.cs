// FocusProbeWindow.cs: a small plain window that stands in for a game in the focus test (ST-03).
// The test brings it to the front, clicks the island, and checks that this window is still in
// front, which is what WinIsland PRD FR-3 promises: the island never takes keyboard focus.
// The window runs on its own thread with its own message loop, as every Windows window needs.

using System.Windows.Forms;

using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;


namespace WinIsland.SmokeTests.Infrastructure;

internal sealed class FocusProbeWindow : IDisposable
{
    public const string Title = "WinIsland smoke test: focus probe";

    private readonly Thread _thread;
    private readonly ManualResetEventSlim _shown = new();
    private Form? _form;

    public FocusProbeWindow()
    {
        _thread = new Thread(() =>
        {
            _form = new Form
            {
                Text = Title,
                Width = 480,
                Height = 240,
                StartPosition = FormStartPosition.CenterScreen,
            };
            _form.Shown += (_, _) => _shown.Set();
            Application.Run(_form);
        })
        {
            IsBackground = true,
            Name = "Focus probe window",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_shown.Wait(UiWait.Long))
        {
            throw new TimeoutException("The focus probe window did not open.");
        }

        Handle = (IntPtr)_form!.Invoke(() => _form.Handle);
    }

    public IntPtr Handle { get; }

    /// <summary>Brings the window to the front through UI Automation, like clicking it.</summary>
    public void BringToFront(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);
        Window window = automation.FromHandle(Handle).AsWindow();
        window.Focus();
        UiWait.Until(() => NativeMethods.GetForegroundWindow() == Handle, UiWait.Short, "the focus probe window to be in front");
    }

    public void Dispose()
    {
        if (_form is not null && _form.IsHandleCreated)
        {
            _form.Invoke(_form.Close);
        }

        _thread.Join(UiWait.Short);
        _shown.Dispose();
    }
}
