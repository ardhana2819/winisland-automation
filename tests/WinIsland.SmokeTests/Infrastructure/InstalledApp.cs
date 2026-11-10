// InstalledApp.cs: where the WinIsland installer puts things on this PC (WinIsland PRD FR-44).
// Kept in one file so a change in the installer only needs one change here.
// The values come from WinIsland's packaging/WinIsland.iss: per-user install, fixed folder,
// the app id {F4DCDDF9-...} that names the uninstall entry, and the two Start menu shortcuts.

using System.Diagnostics;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class InstalledApp
{
    public const string ProcessName = "WinIsland.App";

    public const string Publisher = "Diva";

    // Inno Setup adds "_is1" to the app id for the uninstall entry's key name.
    public const string UninstallKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{F4DCDDF9-E3D7-49B6-AA7D-0FCDE657FA12}_is1";

    public const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public const string StartupValueName = "WinIsland";

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WinIsland");

    public static string ExePath { get; } = Path.Combine(Folder, "WinIsland.App.exe");

    public static string UpdateScriptPath { get; } = Path.Combine(Folder, "packaging", "Update-WinIsland.ps1");

    public static string StartMenuFolder { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

    public static string StartMenuShortcut { get; } = Path.Combine(StartMenuFolder, "WinIsland.lnk");

    public static string UpdateShortcut { get; } = Path.Combine(StartMenuFolder, "Update WinIsland.lnk");

    public static string SettingsFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".winisland");

    public static string SettingsFile { get; } = Path.Combine(SettingsFolder, "settings.json");

    /// <summary>The version WinIsland shows in Settings, read from the installed exe ("1.8.0-beta.1").</summary>
    public static string InstalledVersion()
    {
        if (!File.Exists(ExePath))
        {
            throw new FileNotFoundException("WinIsland is not installed on this PC.", ExePath);
        }

        // .NET writes the informational version (with "-beta.1") as the product version.
        string? version = FileVersionInfo.GetVersionInfo(ExePath).ProductVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException($"{ExePath} has no product version.");
        }

        return version;
    }
}
