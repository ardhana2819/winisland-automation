// FakeSession.cs: the fake player's media session, the part WinIsland reads (PRD FR-7 to FR-10).
// It tells Windows the song, the artist, the album, whether it plays and where in the song it is,
// through the same System Media Transport Controls a real player uses. It reports the song's
// position only when something changes (start, play, pause, next, previous), as real players do,
// so WinIsland has to move the time on by itself between those reports (FR-9).
// Every button Windows passes on is reported to the test and acted on like a player would:
// Pause pauses, Play plays, Next and Previous switch between the two songs and start them over.

using Windows.Media;

namespace FakeMusicPlayer;

internal sealed class FakeSession : IDisposable
{
    private readonly SystemMediaTransportControls _controls;
    private readonly Action<string> _report;
    private readonly object _gate = new();
    private int _songIndex;
    private bool _isPlaying;
    private TimeSpan _positionAtLastReport;
    private DateTimeOffset _lastReportTime;

    public FakeSession(IntPtr window, Action<string> report)
    {
        _report = report;
        _controls = SystemMediaTransportControlsInterop.GetForWindow(window);
        _controls.IsEnabled = true;
        _controls.IsPlayEnabled = true;
        _controls.IsPauseEnabled = true;
        _controls.IsNextEnabled = true;
        _controls.IsPreviousEnabled = true;
        _controls.ButtonPressed += OnButtonPressed;

        lock (_gate)
        {
            ShowSong(0);
            SetPlaying(true);
        }
    }

    public void Dispose()
    {
        _controls.ButtonPressed -= OnButtonPressed;
        _controls.PlaybackStatus = MediaPlaybackStatus.Closed;
        _controls.DisplayUpdater.ClearAll();
        _controls.DisplayUpdater.Update();
        _controls.IsEnabled = false;
    }

    private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        lock (_gate)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    SetPlaying(true);
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    SetPlaying(false);
                    break;
                case SystemMediaTransportControlsButton.Next:
                    ShowSong((_songIndex + 1) % FakeSongs.Titles.Count);
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    ShowSong((_songIndex + FakeSongs.Titles.Count - 1) % FakeSongs.Titles.Count);
                    break;
                default:
                    break;
            }
        }

        _report($"pressed {args.Button}");
    }

    private void ShowSong(int index)
    {
        _songIndex = index;
        SystemMediaTransportControlsDisplayUpdater display = _controls.DisplayUpdater;
        display.ClearAll();
        display.Type = MediaPlaybackType.Music;
        display.MusicProperties.Title = FakeSongs.Titles[index];
        display.MusicProperties.Artist = FakeSongs.Artist;
        display.MusicProperties.AlbumTitle = FakeSongs.Album;
        display.Update();
        ReportPosition(TimeSpan.Zero);
    }

    private void SetPlaying(bool isPlaying)
    {
        ReportPosition(CurrentPosition());
        _isPlaying = isPlaying;
        _controls.PlaybackStatus = isPlaying ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
    }

    private TimeSpan CurrentPosition()
    {
        if (!_isPlaying)
        {
            return _positionAtLastReport;
        }

        TimeSpan position = _positionAtLastReport + (DateTimeOffset.Now - _lastReportTime);
        return position < FakeSongs.Length ? position : FakeSongs.Length;
    }

    private void ReportPosition(TimeSpan position)
    {
        _positionAtLastReport = position;
        _lastReportTime = DateTimeOffset.Now;
        SystemMediaTransportControlsTimelineProperties timeline = new()
        {
            StartTime = TimeSpan.Zero,
            EndTime = FakeSongs.Length,
            Position = position,
        };
        _controls.UpdateTimelineProperties(timeline);
    }
}
