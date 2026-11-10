// HiddenPlayerWindow.cs: the window the fake music player's media session belongs to.
// Windows ties a desktop app's media session to one of its windows (the same is true for a real
// player). The window is never shown, so it never covers anything or takes focus during the tests.

using System.Windows.Forms;

namespace FakeMusicPlayer;

internal sealed class HiddenPlayerWindow : Form
{
    public HiddenPlayerWindow()
    {
        Text = "WinIsland smoke test: fake music player";
        ShowInTaskbar = false;
        // Reading the handle creates the window without showing it.
        _ = Handle;
    }
}
