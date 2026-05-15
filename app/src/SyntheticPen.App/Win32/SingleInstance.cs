using System.Runtime.Versioning;
using System.Threading;

namespace SyntheticPen.App.Win32;

/// <summary>
/// Cross-process coordination between the normal app instance and the
/// <c>--tray</c> helper (or a second double-click launch):
/// <list type="bullet">
/// <item>A named mutex marks "a normal instance is alive".</item>
/// <item>A named auto-reset event is the "show yourself" doorbell. The live
/// instance owns a wait loop on it; the tray/second-launch just sets it.</item>
/// </list>
/// Session-local names (no <c>Global\</c> prefix) — per-user is the right
/// scope for a desktop utility and avoids the elevation rules on the global
/// namespace.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class SingleInstance
{
    private const string MutexName = "SyntheticPen.App.Instance";
    private const string ShowEventName = "SyntheticPen.App.Show";

    private static Mutex? _ownedMutex;

    /// <summary>
    /// Try to become THE instance. Returns true if we are the first/only one;
    /// false if another instance already holds the mutex (caller should signal
    /// it via <see cref="SignalShow"/> and exit).
    /// </summary>
    public static bool TryAcquire()
    {
        _ownedMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        return createdNew;
    }

    /// <summary>
    /// Ring the doorbell on an already-running instance. Returns true if an
    /// instance was listening (event existed and was set), false if nothing is
    /// running — in which case the caller should launch the app.
    /// </summary>
    public static bool SignalShow()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(ShowEventName);
            ev.Set();
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false; // no live instance
        }
    }

    /// <summary>
    /// Run <paramref name="onShow"/> whenever a "show" signal arrives, until
    /// the process exits. Spawns a background thread; the event is created here
    /// so <see cref="SignalShow"/> from other processes can find it.
    /// </summary>
    public static void ListenForShow(Action onShow)
    {
        var ev = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var t = new Thread(() =>
        {
            while (true)
            {
                ev.WaitOne();
                try { onShow(); } catch { /* never let the listener die */ }
            }
        })
        { IsBackground = true, Name = "SyntheticPen.ShowListener" };
        t.Start();
    }
}
