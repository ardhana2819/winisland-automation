// TestToastSender.cs: sends made-up Windows notifications for the notification tests, ST-19 to
// ST-24, as an app WinIsland counts as "another Windows app" (WinIsland PRD FR-13, FR-14).
// Windows only takes notifications from an app it knows, so the sender registers an app name,
// "WinIsland Smoke Test", for the current user, the way unpackaged apps do (a key under
// HKCU\Software\Classes\AppUserModelId). Dispose removes the notifications it sent from the
// Windows notification center and removes that key again, so the PC is left as it was.
// Tried before batch 3 on 10 Nov 2026: WinIsland showed such a notification within 0.2 seconds.
// The text is made up and unique per notification, never a real message (CLAUDE.md).

using System.Globalization;
using System.Security;

using Microsoft.Win32;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WinIsland.SmokeTests.Infrastructure;

internal sealed class TestToastSender : IDisposable
{
    public const string AppName = "WinIsland Smoke Test";
    public const string SenderName = "Test Sender";

    private const string AppId = "WinIsland.SmokeTest.Notifications";
    private const string AppKeyPath = @"Software\Classes\AppUserModelId\" + AppId;
    private const string Group = "smoke-tests";

    private readonly ToastNotifier _notifier;
    private int _sentCount;

    public TestToastSender()
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(AppKeyPath))
        {
            key.SetValue("DisplayName", AppName);
        }

        _notifier = ToastNotificationManager.CreateToastNotifier(AppId);
    }

    /// <summary>A made-up message text no real notification has, so a test can search for it.</summary>
    public static string UniqueText() =>
        "Smoke test message " + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    /// <summary>Sends one notification from "Test Sender" and returns its tag, for Remove.</summary>
    public string Send(string text)
    {
        string tag = "test" + (++_sentCount).ToString(CultureInfo.InvariantCulture);
        XmlDocument content = new();
        content.LoadXml(
            "<toast><visual><binding template='ToastGeneric'>" +
            $"<text>{SecurityElement.Escape(SenderName)}</text><text>{SecurityElement.Escape(text)}</text>" +
            "</binding></visual></toast>");
        _notifier.Show(new ToastNotification(content) { Tag = tag, Group = Group });
        return tag;
    }

    /// <summary>Removes one notification from the Windows notification center, as reading it in the app would.</summary>
    public static void Remove(string tag) => ToastNotificationManager.History.Remove(tag, Group, AppId);

    public void Dispose()
    {
        ToastNotificationManager.History.Clear(AppId);
        Registry.CurrentUser.DeleteSubKeyTree(AppKeyPath, throwOnMissingSubKey: false);
    }
}
