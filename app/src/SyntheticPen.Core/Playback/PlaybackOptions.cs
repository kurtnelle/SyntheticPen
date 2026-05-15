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
    TimeSpan PrimeTapSettle = default);
