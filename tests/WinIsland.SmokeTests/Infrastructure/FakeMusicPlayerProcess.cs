// FakeMusicPlayerProcess.cs: starts the fake music player (tests/FakeMusicPlayer) for the music
// tests, ST-14 to ST-18, and listens to what it reports. The player is a separate program, like
// Spotify, so closing it is a real "the music app closed" for WinIsland (PRD FR-10).
// It reports each button press Windows passes on to it as a line, "pressed Pause" for example,
// which is how the tests know that WinIsland's buttons and keys reached the music app (FR-8).

using System.Collections.Concurrent;
using System.Diagnostics;

namespace WinIsland.SmokeTests.Infrastructure;

internal sealed class FakeMusicPlayerProcess : IDisposable
{
    private static readonly string ExePath = Path.Combine(AppContext.BaseDirectory, "FakeMusicPlayer.exe");

    private readonly Process _process;
    private readonly ConcurrentQueue<string> _lines = new();

    private FakeMusicPlayerProcess(Process process)
    {
        _process = process;
        _process.OutputDataReceived += (_, line) =>
        {
            if (line.Data is not null)
            {
                _lines.Enqueue(line.Data);
            }
        };
        _process.BeginOutputReadLine();
    }

    public bool HasExited => _process.HasExited;

    /// <summary>Starts the player and waits until Windows knows its song.</summary>
    public static FakeMusicPlayerProcess Start()
    {
        ProcessStartInfo startInfo = new(ExePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };
        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Windows did not start {ExePath}.");

        FakeMusicPlayerProcess player = new(process);
        UiWait.Until(() => player.HasReported("ready") || process.HasExited, UiWait.Long, "the fake music player to start");
        if (process.HasExited)
        {
            throw new InvalidOperationException($"The fake music player stopped at start (exit code {process.ExitCode}).");
        }

        return player;
    }

    /// <summary>Waits until the player reports that Windows passed this button on ("Pause", "Next", ...).</summary>
    public void WaitForPress(string button) =>
        UiWait.Until(() => HasReported($"pressed {button}"), UiWait.Short, () => $"the fake music player to get '{button}'. It got: {Reported()}");

    /// <summary>Forgets earlier reports, so the next wait only counts new presses.</summary>
    public void ClearReports() => _lines.Clear();

    /// <summary>Closes the player the way a person closes a music app, and waits until it is gone.</summary>
    public void Close()
    {
        if (_process.HasExited)
        {
            return;
        }

        _process.StandardInput.WriteLine("quit");
        _process.StandardInput.Flush();
        if (!_process.WaitForExit(UiWait.Long))
        {
            _process.Kill();
            _process.WaitForExit(UiWait.Long);
        }
    }

    public void Dispose()
    {
        try
        {
            Close();
        }
        catch (InvalidOperationException)
        {
            // It already stopped; that is what we want.
        }
        catch (IOException)
        {
            // Its input closed because it stopped; that is what we want.
        }

        _process.Dispose();
    }

    private bool HasReported(string line) => _lines.Contains(line);

    private string Reported() => _lines.IsEmpty ? "nothing" : string.Join(", ", _lines);
}
