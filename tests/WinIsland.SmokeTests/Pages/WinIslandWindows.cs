// WinIslandWindows.cs: lists the top-level windows that belong to the running WinIsland
// (the island, its Glass backdrop, Settings, the tray menu). Found by process id on the desktop,
// so the tests never attach to WinIsland in a way that could close it.

using System.Diagnostics;

using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

using WinIsland.SmokeTests.Infrastructure;

namespace WinIsland.SmokeTests.Pages;

internal static class WinIslandWindows
{
    public static IReadOnlyList<AutomationElement> All(UIA3Automation automation)
    {
        ArgumentNullException.ThrowIfNull(automation);
        List<AutomationElement> windows = [];
        AutomationElement desktop = automation.GetDesktop();
        foreach (Process process in Process.GetProcessesByName(InstalledApp.ProcessName))
        {
            using (process)
            {
                windows.AddRange(desktop.FindAllChildren(condition => condition.ByProcessId(process.Id)));
            }
        }

        return windows;
    }
}
