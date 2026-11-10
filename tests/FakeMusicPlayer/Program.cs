// Program.cs: starts the fake music player and ends it when the test asks.
// The smoke tests talk to it through its standard input and output, one line at a time:
//   it writes "ready" once Windows knows the song, then "pressed Play", "pressed Pause",
//   "pressed Next" or "pressed Previous" for each button Windows passes on;
//   it ends when it reads "quit", or when the test closes its input.
// It runs a hidden window, because Windows ties a desktop app's media session to a window.

using System.Windows.Forms;

namespace FakeMusicPlayer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using HiddenPlayerWindow window = new();
        using FakeSession session = new(window.Handle, Report);
        Report("ready");

        Thread input = new(() =>
        {
            // Null means the test closed the input or ended; either way the player stops.
            while (Console.ReadLine() is string line && line != "quit")
            {
            }

            window.BeginInvoke(Application.ExitThread);
        })
        {
            IsBackground = true,
            Name = "Test commands",
        };
        input.Start();

        Application.Run();
    }

    private static void Report(string line)
    {
        // Several Windows threads can report at once; one line must never mix with another.
        lock (typeof(Program))
        {
            Console.Out.WriteLine(line);
            Console.Out.Flush();
        }
    }
}
