namespace SyntheticPen.Core.Playback;

public sealed record PlanOptions(
    double SpeedMultiplier = 1.0,
    double SampleHz = 200.0,
    double BaseVelocityPxPerSec = 600.0,
    double TravelSpeedFactor = 2.0,
    // Curvature-aware slowdown using the 2/3 power law of biological motion
    // (v ∝ R^(1/3)). At a curve with radius R, local velocity is scaled by
    // clamp((R/RefRadius)^Exp, MinFraction, 1). Set RefRadius=0 to disable.
    double CurvatureRefRadius = 60.0,
    double CurvaturePowerLawExp = 1.0 / 3.0,
    double MinSpeedFraction = 0.15);
