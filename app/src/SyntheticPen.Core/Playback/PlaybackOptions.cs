namespace SyntheticPen.Core.Playback;

public sealed record PlaybackOptions(
    double SpeedMultiplier = 1.0,
    InjectionMode Mode = InjectionMode.Mouse,
    TimeSpan Countdown = default,
    double SampleHz = 200.0,
    bool WaitForFocusRelease = true,
    // Tap once at the first point before drawing begins. Many whiteboards
    // (e.g. Microsoft Whiteboard, OneNote canvas) drop the first sample of
    // a fresh pointer stream until the tool has been activated by a tap.
    // 0 disables priming.
    TimeSpan PrimeTapHold = default,
    TimeSpan PrimeTapSettle = default,
    // Injection pacing. The planner can schedule samples microseconds apart,
    // and when playback falls behind real time the controller would otherwise
    // fire them back-to-back to catch up — overrunning the Windows synthetic-
    // pointer pipeline (ERROR_INVALID_PARAMETER). MinEventInterval is the
    // floor between any two injected events; ContactSettle is the larger
    // isolation enforced around pen DOWN/UP so the device can process the
    // contact-state transition. Defaults degrade playback gracefully rather
    // than aborting; 0 disables pacing.
    TimeSpan MinEventInterval = default,
    TimeSpan ContactSettle = default);
