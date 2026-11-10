// WinIslandProcess.cs: starts and stops the installed WinIsland for the tests.
// Each test starts its own fresh WinIsland, so one test never sees what the last one left
// behind (an open island, a pause, a hidden island). Stopping kills the process, like the
// installer does (taskkill), because WinIsland has no window to close politely from outside.

using System.Diagnostics;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class WinIslandProcess
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

    public static bool IsRunning() => Process.GetProcessesByName(InstalledApp.ProcessName).Length > 0;

    public static int RunningCount() => Process.GetProcessesByName(InstalledApp.ProcessName).Length;

    /// <summary>Starts the installed WinIsland. Fails clearly when it is not installed.</summary>
    public static Process Start()
    {
        if (!File.Exists(InstalledApp.ExePath))
        {
            throw new FileNotFoundException(
                "WinIsland is not installed. Run run-smoke-tests.ps1 with -Setup or -Version first.",
                InstalledApp.ExePath);
        }

        // Started through the Windows shell, like from the Start menu. A direct start would hand
        // WinIsland the test runner's output pipe, and the runner then waits for WinIsland to end
        // (the likely reason run-smoke-tests.ps1 did not finish in the first run, 10 Nov 2026).
        ProcessStartInfo startInfo = new(InstalledApp.ExePath)
        {
            UseShellExecute = true,
            WorkingDirectory = InstalledApp.Folder,
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Windows did not start {InstalledApp.ExePath}.");
    }

    /// <summary>Stops every running WinIsland and waits until they are gone.</summary>
    public static void StopAll()
    {
        foreach (Process process in Process.GetProcessesByName(InstalledApp.ProcessName))
        {
            using (process)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    if (!process.WaitForExit(StopTimeout))
                    {
                        throw new TimeoutException($"WinIsland (process {process.Id}) did not stop within {StopTimeout.TotalSeconds} seconds.");
                    }
                }
                catch (InvalidOperationException)
                {
                    // It already stopped between listing and killing; that is what we want.
                }
            }
        }
    }
}
