// SavedSettings.cs: reads one value from WinIsland's settings.json, to check that a change in
// Settings was really saved (WinIsland PRD FR-19, FR-32). Only setting names are read; the file
// never holds message text (FR-18).

using System.Text.Json;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class SavedSettings
{
    /// <summary>The saved text of a setting, for example "ExtraLarge" for "IslandSize".</summary>
    public static string? Read(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!File.Exists(InstalledApp.SettingsFile))
        {
            return null;
        }

        // WinIsland saves through a temporary file and a swap, so a read can meet the swap; retry briefly.
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(InstalledApp.SettingsFile));
                return document.RootElement.TryGetProperty(name, out JsonElement value) ? value.ToString() : null;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(100);
            }
        }
    }
}
