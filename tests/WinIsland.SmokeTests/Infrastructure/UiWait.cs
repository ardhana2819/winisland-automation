// UiWait.cs: waits until something on screen is true, checking a few times a second.
// WinIsland animates almost every change (0.3 to 0.6 seconds), so a test must wait for the
// result instead of checking at once. When time runs out, the error says what was expected,
// so a failed run explains itself.

namespace WinIsland.SmokeTests.Infrastructure;

internal static class UiWait
{
    public static readonly TimeSpan Short = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan Long = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(150);

    // Longest animation in WinIsland is the 0.6 second shape spring (DESIGN_SPEC section 6).
    private static readonly TimeSpan AnimationTime = TimeSpan.FromSeconds(1.2);

    /// <summary>Waits until the check returns true, or fails with the description.</summary>
    public static void Until(Func<bool> check, TimeSpan timeout, string whatWeWaitFor) =>
        Until(check, timeout, () => whatWeWaitFor);

    /// <summary>
    /// Like Until, but the description is written only when time runs out, so it can say what is
    /// on screen at that moment ("It shows ...") instead of what was there at the start.
    /// </summary>
    public static void Until(Func<bool> check, TimeSpan timeout, Func<string> whatWeWaitFor)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(whatWeWaitFor);
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (check())
            {
                return;
            }

            Thread.Sleep(CheckInterval);
        }

        if (!check())
        {
            throw new TimeoutException($"Waited {timeout.TotalSeconds} seconds for: {whatWeWaitFor()}");
        }
    }

    /// <summary>Waits until the value is found (not null), or fails with the description.</summary>
    public static T For<T>(Func<T?> find, TimeSpan timeout, string whatWeWaitFor)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(find);
        T? found = null;
        Until(() => (found = find()) is not null, timeout, whatWeWaitFor);
        return found!;
    }

    /// <summary>Lets a WinIsland animation finish before something is measured.</summary>
    public static void ForAnimation() => Thread.Sleep(AnimationTime);
}
