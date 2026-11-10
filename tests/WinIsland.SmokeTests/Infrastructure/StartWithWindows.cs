// StartWithWindows.cs: reads, from outside WinIsland, whether Windows will start WinIsland at
// sign-in (WinIsland PRD FR-20). WinIsland uses one of two places, depending on its setup:
//   - Without package identity: the value "WinIsland" in the per-user Run list of the registry.
//   - With package identity (instant notifications set up with Install-WinIslandIdentity.ps1):
//     the identity package's Windows startup task "WinIslandStartup", shown in Windows Settings >
//     Apps > Startup. Windows gives a packaged app a private copy of the Run list, so it cannot
//     use the real one (WinIsland PR #57, found by ST-08 on 10 Nov 2026).
// The startup task's state is read where Windows keeps it in the registry, under the package's
// SystemAppData key. That place is not documented by Microsoft; it was confirmed on Diva's PC by
// switching the setting and watching the value (2 = on, 0 = off).

using Microsoft.Win32;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class StartWithWindows
{
    public const string StartupTaskId = "WinIslandStartup";

    private const string AppModelKeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel";

    // "WinIsland_" and not "WinIsland.": spike packages such as WinIsland.Spike.NotificationProbe do not count.
    private const string PackagePrefix = "WinIsland_";

    // Windows' StartupTaskState: 2 Enabled, 4 EnabledByPolicy. 0, 1 and 3 are kinds of off.
    private static readonly int[] OnStates = [2, 4];

    /// <summary>True when WinIsland's identity package is registered for this Windows user.</summary>
    public static bool HasPackageIdentity()
    {
        using RegistryKey? packages = Registry.CurrentUser.OpenSubKey($@"{AppModelKeyPath}\Repository\Packages");
        return packages?.GetSubKeyNames().Any(name => name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase)) == true;
    }

    /// <summary>The startup task's state number, or null when the identity package has no startup task.</summary>
    public static int? StartupTaskState()
    {
        using RegistryKey? appData = Registry.CurrentUser.OpenSubKey($@"{AppModelKeyPath}\SystemAppData");
        if (appData is null)
        {
            return null;
        }

        foreach (string family in appData.GetSubKeyNames().Where(name => name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            using RegistryKey? task = appData.OpenSubKey($@"{family}\{StartupTaskId}");
            if (task?.GetValue("State") is int state)
            {
                return state;
            }
        }

        return null;
    }

    /// <summary>The command in the Run list, or null when there is none.</summary>
    public static string? RunEntry()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(InstalledApp.StartupKeyPath);
        return runKey?.GetValue(InstalledApp.StartupValueName) as string;
    }

    /// <summary>Whether Windows will start WinIsland, from the place WinIsland uses on this PC.</summary>
    public static bool IsOn() =>
        HasPackageIdentity()
            ? StartupTaskState() is int state && OnStates.Contains(state)
            : RunEntry() is not null;

    /// <summary>What the PC shows now, for failure messages.</summary>
    public static string Describe() =>
        HasPackageIdentity()
            ? $"startup task state {StartupTaskState()?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing"}, Run entry {RunEntry() ?? "missing"}"
            : $"Run entry {RunEntry() ?? "missing"}";
}
