// SettingsTests.cs: ST-05 to ST-09, the Settings window opens, saves and applies changes.
// ST-05: Settings opens from the tray and shows the installed version (FR-46).
// ST-06: the island size choice scales the island, is saved, and is kept after a restart (FR-32).
// ST-07: the look can switch between Solid and Glass (beta) without an error (FR-37).
// ST-08: "Launch with Windows" adds and removes the start with Windows entry (FR-20).
// ST-09: the Update button is there and can be pressed (FR-46). It is not pressed.

using Microsoft.Win32;

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class SettingsTests(SmokeTestRun run)
{
    // Sizes from WinIsland PRD FR-32, relative to Medium.
    private static readonly (string Option, string SavedName, double Scale)[] Sizes =
    [
        ("Small", "Small", 0.85),
        ("Medium", "Medium", 1.00),
        ("Large", "Large", 1.15),
        ("Extra large", "ExtraLarge", 1.30),
    ];

    // Sizes on screen are whole pixels; for the measuring line one pixel is about 3 percent.
    private const double ScaleTolerance = 0.05;

    [Fact(DisplayName = "ST-05 Settings opens from the tray and shows the version")]
    public void ST05_SettingsOpensAndShowsVersion()
    {
        IslandPage.StartFresh(run.Automation);

        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        Assert.Equal($"Version {InstalledApp.InstalledVersion()}", settings.VersionText());
        settings.Close();
    }

    [Fact(DisplayName = "ST-06 Island size scales the island and is kept")]
    public void ST06_IslandSizeScalesAndIsKept()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        double mediumHeight = island.MeasureHeight();
        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);

        foreach ((string option, string savedName, double scale) in Sizes)
        {
            settings.ChooseIslandSize(option);
            UiWait.ForAnimation();
            AssertScale(scale, island.MeasureHeight() / mediumHeight, $"Island size {option}");
            Assert.Equal(savedName, SavedSettings.Read("IslandSize"));
        }

        settings.Close();

        // Extra large was chosen last; it must still be there after WinIsland starts again.
        IslandPage restarted = IslandPage.StartFresh(run.Automation, useDefaults: false);
        AssertScale(1.30, restarted.MeasureHeight() / mediumHeight, "Island size Extra large after a restart");
    }

    [Fact(DisplayName = "ST-07 Look switches between Solid and Glass")]
    public void ST07_LookSwitchesBetweenSolidAndGlass()
    {
        IslandPage.StartFresh(run.Automation);
        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        Assert.Equal(["Solid", "Glass (beta)"], settings.LookOptions());
        Assert.Equal("Solid", settings.ChosenLook());

        settings.ChooseLook("Glass (beta)");
        UiWait.ForAnimation();
        Assert.Equal("Glass", SavedSettings.Read("Look"));
        Assert.True(WinIslandProcess.IsRunning(), "WinIsland stopped after switching to Glass.");
        IslandPage.Find(run.Automation);

        settings.ChooseLook("Solid");
        UiWait.ForAnimation();
        Assert.Equal("Solid", SavedSettings.Read("Look"));
        Assert.Null(settings.ProblemText());
        settings.Close();
    }

    [Fact(DisplayName = "ST-08 Launch with Windows adds and removes the startup entry")]
    public void ST08_LaunchWithWindowsSwitch()
    {
        IslandPage.StartFresh(run.Automation);
        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        bool startedOn = settings.IsLaunchWithWindowsOn();
        Assert.Equal(startedOn, StartupEntry() is not null);

        settings.PressLaunchWithWindows();
        WaitForStartupEntry(settings, present: !startedOn);
        if (!startedOn)
        {
            Assert.Equal($"\"{InstalledApp.ExePath}\"", StartupEntry(), StringComparer.OrdinalIgnoreCase);
        }

        settings.PressLaunchWithWindows();
        WaitForStartupEntry(settings, present: startedOn);
        settings.Close();
    }

    private static void WaitForStartupEntry(SettingsPage settings, bool present)
    {
        try
        {
            UiWait.Until(() => (StartupEntry() is not null) == present, UiWait.Short, "the start with Windows entry to change");
        }
        catch (TimeoutException)
        {
            // Say which half failed: the switch did not move, or it moved but nothing was saved.
            Assert.Fail(
                $"Expected the start with Windows entry to be {(present ? "there" : "gone")}. " +
                $"The switch shows {(settings.IsLaunchWithWindowsOn() ? "on" : "off")}, " +
                $"the entry is {StartupEntry() ?? "missing"}, settings.json has LaunchAtStartup = {SavedSettings.Read("LaunchAtStartup") ?? "nothing"}.");
        }
    }

    [Fact(DisplayName = "ST-09 Update button is there and enabled")]
    public void ST09_UpdateButtonIsEnabled()
    {
        Assert.True(File.Exists(InstalledApp.UpdateScriptPath), $"The update script is missing: {InstalledApp.UpdateScriptPath}.");
        IslandPage.StartFresh(run.Automation);

        SettingsPage settings = SettingsPage.OpenFromTray(run.Automation);
        Assert.True(settings.UpdateButton().IsEnabled, "The Update button is greyed out although the update script is installed.");
        settings.Close();
    }

    private static string? StartupEntry()
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(InstalledApp.StartupKeyPath);
        return runKey?.GetValue(InstalledApp.StartupValueName) as string;
    }

    private static void AssertScale(double expected, double measured, string what) =>
        Assert.True(
            Math.Abs(expected - measured) <= ScaleTolerance,
            $"{what}: the island measured {measured:P0} of Medium, expected {expected:P0}.");
}
