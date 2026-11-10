// MusicTests.cs: ST-14 to ST-18, WinIsland's music (WinIsland PRD FR-7 to FR-10, FR-24, FR-41).
// The music comes from the fake music player (tests/FakeMusicPlayer), which tells Windows it
// plays "WinIsland Test Song" by "Automation", so no real music app or music is needed.
// ST-14: the playing song shows on the compact island (FR-7, FR-41).
// ST-15: the island's buttons reach the music app (FR-8).
// ST-16: Space, Ctrl+Right and Ctrl+Left reach the music app while the island is open (FR-8).
// ST-17: the song's elapsed time moves on between the music app's reports (FR-9).
// ST-18: when the music app closes, the island shows "Not playing" and keeps running (FR-10, FR-24).
// Another app's music would rightly be shown instead, so each test first checks there is none.

using FakeMusicPlayer;

using WinIsland.SmokeTests.Infrastructure;
using WinIsland.SmokeTests.Pages;

namespace WinIsland.SmokeTests.Tests;

[Collection(SmokeTestCollection.Name)]
public sealed class MusicTests(SmokeTestRun run)
{
    // A layer that is not shown rests at 88 percent; anything under 94 percent is clearly resting.
    private const double ReplacedClockAtMost = 0.94;

    private const string EmptyTitle = "Not playing";

    // The time is read twice this far apart; it must have moved on by most of it.
    private static readonly TimeSpan TimeReadGap = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan TimeMovesAtLeast = TimeSpan.FromSeconds(3);

    [Fact(DisplayName = "ST-14 The playing song shows on the island")]
    public void ST14_PlayingSongShowsOnIsland()
    {
        // The clock is measured as the "before". Another app's paused song would already have
        // pushed the clock aside for 5 seconds after start (FR-23), so none may be open at all.
        OtherMusicApps.EnsureNoneOpen();
        IslandPage island = IslandPage.StartFresh(run.Automation);
        double clockShownHeight = island.ClockHeight();

        using FakeMusicPlayerProcess player = FakeMusicPlayerProcess.Start();
        WaitForPillTitle(island, FakeSongs.FirstTitle);
        UiWait.ForAnimation();

        double clockRatio = island.ClockHeight() / clockShownHeight;
        Assert.True(clockRatio <= ReplacedClockAtMost, $"The song's name is on the music pill, but the clock is still shown ({clockRatio:P0} of its size) instead of the pill.");
    }

    [Fact(DisplayName = "ST-15 Music buttons reach the music app")]
    public void ST15_MusicButtonsReachMusicApp()
    {
        OtherMusicApps.EnsureNonePlays();
        (IslandPage island, FakeMusicPlayerProcess player) = StartWithFakeSong();
        using (player)
        {
            MusicPage music = OpenMusicPage(island);
            Assert.Equal(FakeSongs.FirstTitle, music.TitleText());
            Assert.Equal(FakeSongs.Artist, music.ArtistText());

            music.Click(MusicPage.PauseButton);
            player.WaitForPress("Pause");
            UiWait.Until(() => music.HasButton(MusicPage.PlayButton), UiWait.Short, "the pause button to turn into a play button");

            music.Click(MusicPage.NextButton);
            player.WaitForPress("Next");
            UiWait.Until(() => music.TitleText() == FakeSongs.SecondTitle, UiWait.Short, () => $"the music page to show '{FakeSongs.SecondTitle}' after Next. It shows '{music.TitleText()}'");

            music.Click(MusicPage.PreviousButton);
            player.WaitForPress("Previous");
            UiWait.Until(() => music.TitleText() == FakeSongs.FirstTitle, UiWait.Short, () => $"the music page to show '{FakeSongs.FirstTitle}' after Previous. It shows '{music.TitleText()}'");

            Shortcuts.ExpandIsland();
        }
    }

    [Fact(DisplayName = "ST-16 Music keys work while the island is open")]
    public void ST16_MusicKeysWorkWhileIslandIsOpen()
    {
        OtherMusicApps.EnsureNonePlays();
        (IslandPage island, FakeMusicPlayerProcess player) = StartWithFakeSong();
        using (player)
        {
            // The keys go to WinIsland only while it is open; a stand-in game in front catches
            // them if they ever leak, instead of whatever window happens to be in front.
            using FocusProbeWindow game = new();
            game.BringToFront(run.Automation);
            MusicPage music = OpenMusicPage(island);

            Shortcuts.PlayOrPauseMusic();
            player.WaitForPress("Pause");

            Shortcuts.NextSong();
            player.WaitForPress("Next");
            UiWait.Until(() => music.TitleText() == FakeSongs.SecondTitle, UiWait.Short, $"the music page to show '{FakeSongs.SecondTitle}' after Ctrl+Right");

            Shortcuts.PreviousSong();
            player.WaitForPress("Previous");
            UiWait.Until(() => music.TitleText() == FakeSongs.FirstTitle, UiWait.Short, $"the music page to show '{FakeSongs.FirstTitle}' after Ctrl+Left");

            Shortcuts.ExpandIsland();
        }
    }

    [Fact(DisplayName = "ST-17 The song's time moves on")]
    public void ST17_SongTimeMovesOn()
    {
        OtherMusicApps.EnsureNonePlays();
        (IslandPage island, FakeMusicPlayerProcess player) = StartWithFakeSong();
        using (player)
        {
            // The fake player reports the position only at start, so any movement is WinIsland's own (FR-9).
            MusicPage music = OpenMusicPage(island);
            TimeSpan first = music.Elapsed();
            Thread.Sleep(TimeReadGap);
            TimeSpan second = music.Elapsed();

            Assert.True(
                second - first >= TimeMovesAtLeast,
                $"The elapsed time went from {first:m\\:ss} to {second:m\\:ss} in {TimeReadGap.TotalSeconds} seconds; it should move on with the song.");

            Shortcuts.ExpandIsland();
        }
    }

    [Fact(DisplayName = "ST-18 Closing the music app shows \"Not playing\"")]
    public void ST18_ClosingMusicAppShowsNotPlaying()
    {
        OtherMusicApps.EnsureNoneOpen();
        (IslandPage island, FakeMusicPlayerProcess player) = StartWithFakeSong();
        using (player)
        {
            player.Close();
        }

        MusicPage music = OpenMusicPage(island);
        UiWait.Until(() => music.TitleText() == EmptyTitle, UiWait.Short, () => $"the music page to show '{EmptyTitle}' after the music app closed. It shows '{music.TitleText()}'");
        Assert.True(WinIslandProcess.IsRunning(), "WinIsland stopped after the music app closed.");

        Shortcuts.ExpandIsland();
    }

    // A fresh WinIsland, then the fake player, as when you start Spotify with WinIsland running.
    private (IslandPage Island, FakeMusicPlayerProcess Player) StartWithFakeSong()
    {
        IslandPage island = IslandPage.StartFresh(run.Automation);
        FakeMusicPlayerProcess player = FakeMusicPlayerProcess.Start();
        try
        {
            WaitForPillTitle(island, FakeSongs.FirstTitle);
            return (island, player);
        }
        catch
        {
            player.Dispose();
            throw;
        }
    }

    private static void WaitForPillTitle(IslandPage island, string title) =>
        UiWait.Until(() => island.MusicPillTitleText() == title, UiWait.Long, () => $"the island to show '{title}'. It shows '{island.MusicPillTitleText()}'");

    private static MusicPage OpenMusicPage(IslandPage island)
    {
        Shortcuts.ExpandIsland();
        UiWait.ForAnimation();
        return island.Music();
    }
}
