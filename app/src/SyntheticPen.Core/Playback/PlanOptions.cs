namespace SyntheticPen.Core.Playback;

public sealed record PlanOptions(
    double SpeedMultiplier = 1.0,
    double SampleHz = 200.0,
    double BaseVelocityPxPerSec = 600.0,
    double TravelSpeedFactor = 2.0);
