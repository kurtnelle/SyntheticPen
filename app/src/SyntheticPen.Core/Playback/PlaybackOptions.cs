namespace SyntheticPen.Core.Playback;

public sealed record PlaybackOptions(
    double SpeedMultiplier = 1.0,
    InjectionMode Mode = InjectionMode.Mouse,
    TimeSpan Countdown = default,
    double SampleHz = 200.0,
    bool WaitForFocusRelease = true);
