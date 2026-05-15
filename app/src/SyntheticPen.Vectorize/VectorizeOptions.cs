namespace SyntheticPen.Vectorize;

/// <summary>Tunables for the centerline extraction pipeline.</summary>
public sealed record VectorizeOptions(
    // Longest side of the rasterized image, in pixels. Higher = finer
    // skeleton at the cost of more work. Thin features still need enough
    // pixels across to thin cleanly — see MinFeaturePx.
    int TargetResolution = 2000,

    // Minimum rendered width (px) the thinnest ink feature should occupy.
    // The rasterizer raises resolution if the natural scale would render
    // strokes thinner than this, so Zhang-Suen has material to thin.
    double MinFeaturePx = 8.0,

    // Luminance threshold (0-255) below which a pixel counts as ink.
    byte InkThreshold = 128,

    // Output spline resampling spacing in source-SVG units. Smaller =
    // denser, smoother replay path.
    double ResampleSpacing = 1.5,

    // Drop traced strokes shorter than this many skeleton pixels — removes
    // speckle / thinning nubs.
    int MinStrokePixels = 6,

    // Gamma applied to normalized pressure. >1 softens light pressure,
    // <1 boosts it.
    double PressureGamma = 1.0,

    // Curvature radius (source units) at/below which Velocity bottoms out;
    // straight runs approach 1.
    double VelocityCurvatureRef = 12.0,

    // Spur pruning: remove dead-end skeleton whiskers (thinning noise from
    // anti-aliased ink outlines) whose length is below this multiple of the
    // local stroke radius. A real stroke is longer than it is wide; a wobble
    // isn't. Only endpoint→junction branches are touched, so isolated marks
    // (i-dots, the period in "K.") are preserved. 0 disables.
    double SpurWidthFactor = 1.6);
