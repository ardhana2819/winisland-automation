// OtherMusicApps.cs: checks that no other app's music is in the way of the music tests.
// WinIsland shows the media session Windows lists (PRD FR-41): if Spotify or a browser tab plays,
// WinIsland may rightly show that instead of the fake song, and once the fake player closes, a
// paused Spotify is still a song to show, not "Not playing" (FR-24); right after WinIsland starts,
// a paused song is shown for 5 seconds in place of the clock (FR-23). So the music tests first ask
// Windows which media sessions there are, through the same public list any app can read, and
// say clearly what to close instead of failing on a wrong song.

using Windows.Media.Control;

namespace WinIsland.SmokeTests.Infrastructure;

internal static class OtherMusicApps
{
    private const string FakePlayerName = "FakeMusicPlayer";

    /// <summary>Fails with what to do when another app's music plays now.</summary>
    public static void EnsureNonePlays()
    {
        List<string> playing = [.. OtherSessions()
            .Where(session => session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            .Select(session => session.SourceAppUserModelId)];
        Assert.True(
            playing.Count == 0,
            $"Cannot run: other music plays ({string.Join(", ", playing)}). Pause it and run the tests again.");
    }

    /// <summary>Fails with what to do when another app has a media session at all, playing or paused.</summary>
    public static void EnsureNoneOpen()
    {
        List<string> open = [.. OtherSessions().Select(session => session.SourceAppUserModelId)];
        Assert.True(
            open.Count == 0,
            $"Cannot run: other apps have music open ({string.Join(", ", open)}), even if paused, and WinIsland rightly shows that. Close them (or the browser tab with the video) and run the tests again.");
    }

    private static IEnumerable<GlobalSystemMediaTransportControlsSession> OtherSessions()
    {
        GlobalSystemMediaTransportControlsSessionManager manager =
            GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().GetAwaiter().GetResult();
        return manager.GetSessions()
            .Where(session => !session.SourceAppUserModelId.Contains(FakePlayerName, StringComparison.OrdinalIgnoreCase));
    }
}
