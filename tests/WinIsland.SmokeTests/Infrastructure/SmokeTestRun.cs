// SmokeTestRun.cs: what happens once around the whole run, shared by every test.
// Before the first test: back up Diva's settings and remember whether WinIsland was running.
// After the last test: stop the test WinIsland, restore the settings, and start WinIsland again
// if it ran before, so the PC is left as it was found.
// It also holds the one UI Automation connection all tests use.

using FlaUI.UIA3;

namespace WinIsland.SmokeTests.Infrastructure;

public sealed class SmokeTestRun : IDisposable
{
    private readonly SettingsBackup _backup = new();
    private readonly bool _wasRunningBefore;

    public SmokeTestRun()
    {
        // Real pixels everywhere (screen size, mouse, UI Automation), whatever the display scale.
        // Windows refuses this when the test host already chose a DPI mode; that mode then stays,
        // and ST-02 would show it as an off-center island.
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.PerMonitorAwareV2);


        // run-smoke-tests.ps1 checks this before the installer closes WinIsland and passes it on.
        string? fromScript = Environment.GetEnvironmentVariable("WINISLAND_WAS_RUNNING");
        _wasRunningBefore = fromScript is null ? WinIslandProcess.IsRunning() : fromScript == "1";

        WinIslandProcess.StopAll();
        _backup.Save();
        Automation = new UIA3Automation();
    }

    internal UIA3Automation Automation { get; }

    public void Dispose()
    {
        WinIslandProcess.StopAll();
        _backup.Restore();
        if (_wasRunningBefore && File.Exists(InstalledApp.ExePath))
        {
            WinIslandProcess.Start().Dispose();
        }

        Automation.Dispose();
    }
}

/// <summary>Puts every test class in one collection, so they share SmokeTestRun and never run at the same time.</summary>
[CollectionDefinition(Name)]
public sealed class SmokeTestCollection : ICollectionFixture<SmokeTestRun>
{
    public const string Name = "WinIsland smoke tests";
}
