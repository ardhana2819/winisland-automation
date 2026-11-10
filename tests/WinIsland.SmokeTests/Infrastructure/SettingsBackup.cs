// SettingsBackup.cs: keeps Diva's own WinIsland settings safe while the tests change them.
// Before the run it copies settings.json and the start with Windows entry; after the run it
// puts both back exactly as they were (also when they did not exist). The copy stays next to
// the original under a clear name, so it can be restored by hand if a run is cut off.
// Settings hold no message text (WinIsland PRD FR-18), so a copy on disk is safe.

using Microsoft.Win32;

namespace WinIsland.SmokeTests.Infrastructure;

internal sealed class SettingsBackup
{
    private readonly string _backupPath = InstalledApp.SettingsFile + ".smoke-test-backup";
    private bool _hadSettingsFile;
    private string? _startupCommand;

    /// <summary>Copies the settings file and the start with Windows entry.</summary>
    public void Save()
    {
        // A backup left over means the last run was cut off before it restored. That backup is
        // Diva's real settings; the current file holds test settings, so put the backup back first.
        if (File.Exists(_backupPath))
        {
            File.Copy(_backupPath, InstalledApp.SettingsFile, overwrite: true);
        }

        _hadSettingsFile = File.Exists(InstalledApp.SettingsFile);
        if (_hadSettingsFile)
        {
            File.Copy(InstalledApp.SettingsFile, _backupPath, overwrite: true);
        }

        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(InstalledApp.StartupKeyPath);
        _startupCommand = runKey?.GetValue(InstalledApp.StartupValueName) as string;
    }

    /// <summary>Puts both back as they were before the run. WinIsland must not be running.</summary>
    public void Restore()
    {
        if (_hadSettingsFile)
        {
            File.Copy(_backupPath, InstalledApp.SettingsFile, overwrite: true);
            File.Delete(_backupPath);
        }
        else if (File.Exists(InstalledApp.SettingsFile))
        {
            File.Delete(InstalledApp.SettingsFile);
        }

        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(InstalledApp.StartupKeyPath);
        if (_startupCommand is null)
        {
            runKey.DeleteValue(InstalledApp.StartupValueName, throwOnMissingValue: false);
        }
        else
        {
            runKey.SetValue(InstalledApp.StartupValueName, _startupCommand);
        }
    }

    /// <summary>Removes the settings file, so the next start uses WinIsland's defaults.</summary>
    public static void UseDefaults()
    {
        if (File.Exists(InstalledApp.SettingsFile))
        {
            File.Delete(InstalledApp.SettingsFile);
        }
    }
}
