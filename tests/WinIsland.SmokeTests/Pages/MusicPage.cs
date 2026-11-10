// MusicPage.cs: the island's expanded music page (WinIsland PRD FR-7 to FR-10, FR-24) through
// UI Automation: the song title and artist, the elapsed time, and the previous, play/pause and
// next buttons. WinIsland names these for screen readers, which is what the tests use.
// The page is part of the island window and exists all the time; it is only on screen while the
// island is open (see IslandPage for how the tests tell).

using System.Drawing;
using System.Globalization;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

namespace WinIsland.SmokeTests.Pages;

internal sealed class MusicPage
{
    public const string PreviousButton = "Previous track";
    public const string NextButton = "Next track";
    public const string PauseButton = "Pause";
    public const string PlayButton = "Play";

    private readonly Window _window;

    internal MusicPage(Window window) => _window = window;

    public string TitleText() => ById("ExpandedTitle").Name;

    public string ArtistText() => ById("ExpandedArtist").Name;

    /// <summary>How far into the song the page says it is, read from "1:24".</summary>
    public TimeSpan Elapsed()
    {
        string text = ById("ElapsedTime").Name;
        string[] formats = ["m\\:ss", "h\\:mm\\:ss"];
        return TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out TimeSpan elapsed)
            ? elapsed
            : throw new InvalidOperationException($"The elapsed time reads '{text}', which is not a time like 1:24.");
    }

    /// <summary>Whether the page has a button with this name now; play/pause changes its name with the state.</summary>
    public bool HasButton(string name) => FindButton(name) is not null;

    /// <summary>Clicks the button with the real mouse, as a person does.</summary>
    public void Click(string buttonName)
    {
        AutomationElement button = FindButton(buttonName)
            ?? throw new InvalidOperationException($"The music page has no button '{buttonName}'. Did WinIsland change?");
        Rectangle box = button.BoundingRectangle;
        Mouse.Click(new Point(box.Left + (box.Width / 2), box.Top + (box.Height / 2)));
    }

    private AutomationElement? FindButton(string name) =>
        _window.FindFirstDescendant(condition => condition.ByControlType(ControlType.Button).And(condition.ByName(name)));

    private AutomationElement ById(string automationId) =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"The music page has no element '{automationId}'. Did WinIsland change?");
}
