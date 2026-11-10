// InstallTests.cs: ST-01, the installer put WinIsland on this PC correctly (WinIsland PRD FR-44).
// run-smoke-tests.ps1 runs the setup before the tests and passes its exit code on. With
// -SkipInstall there is no setup run, and the test checks the WinIsland already installed.

using Microsoft.Win32;

using WinIsland.SmokeTests.Infrastructure;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class InstallTests
{
    [Fact(DisplayName = "ST-01 Installer puts WinIsland in place")]
    public void ST01_InstallerPutsWinIslandInPlace()
    {
        string? setupExitCode = Environment.GetEnvironmentVariable("WINISLAND_SETUP_EXIT_CODE");
        if (setupExitCode is not null)
        {
            Assert.True(setupExitCode == "0", $"The setup ended with exit code {setupExitCode}, not 0. See install.log in the results folder.");
        }

        Assert.True(File.Exists(InstalledApp.ExePath), $"WinIsland.App.exe is missing from {InstalledApp.Folder}.");
        Assert.True(File.Exists(InstalledApp.StartMenuShortcut), $"The Start menu shortcut is missing: {InstalledApp.StartMenuShortcut}.");
        Assert.True(File.Exists(InstalledApp.UpdateShortcut), $"The 'Update WinIsland' shortcut is missing: {InstalledApp.UpdateShortcut}.");

        using RegistryKey? uninstallEntry = Registry.CurrentUser.OpenSubKey(InstalledApp.UninstallKeyPath);
        Assert.True(uninstallEntry is not null, "WinIsland is missing from Settings > Apps > Installed apps.");
        Assert.Equal(InstalledApp.Publisher, uninstallEntry.GetValue("Publisher") as string);

        // Only when this run installed it: a test build copied into the folder by hand (WinIsland's
        // usual way to try a batch) leaves Installed apps showing the last setup's version.
        if (setupExitCode is not null)
        {
            // The release tag starts with "v" (v1.8.0-beta.1); the exe's version does not.
            string shownVersion = (uninstallEntry.GetValue("DisplayVersion") as string ?? string.Empty).TrimStart('v');
            Assert.Equal(InstalledApp.InstalledVersion(), shownVersion);
        }
    }
}
