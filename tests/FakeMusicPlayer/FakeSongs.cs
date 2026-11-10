// FakeSongs.cs: the made-up songs the fake music player reports, in one place, so the player
// and the tests that check them can never disagree. Two songs, so Next and Previous show a change.
// The album name matters: WinIsland treats an app that names an album as a music player (PRD FR-41).

namespace FakeMusicPlayer;

public static class FakeSongs
{
    public const string Artist = "Automation";
    public const string Album = "WinIsland Smoke Tests";

    public const string FirstTitle = "WinIsland Test Song";
    public const string SecondTitle = "WinIsland Test Song 2";

    /// <summary>Every song is three minutes long, long enough for any test.</summary>
    public static readonly TimeSpan Length = TimeSpan.FromMinutes(3);

    public static readonly IReadOnlyList<string> Titles = [FirstTitle, SecondTitle];
}
