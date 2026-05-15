namespace SyntheticPen.Vectorize;

/// <summary>
/// One sample along an extracted centerline, in <b>source SVG coordinate
/// space</b>. <see cref="Pressure"/> and <see cref="Velocity"/> are normalized
/// to [0,1] and are suggestions for synthetic stylus replay, not physical
/// units.
/// </summary>
public sealed class StrokePoint
{
    public float X;
    public float Y;

    /// <summary>Local stroke radius (distance-transform value at this point)
    /// normalized against the thickest point in the document. ~1 at the heavy
    /// body of a stroke, →0 at hairline tails.</summary>
    public float Pressure;

    /// <summary>Suggested normalized traversal speed [0,1]; lower through
    /// tight curvature (consistent with the 2/3 power law used by the motion
    /// planner). 1 on straight runs.</summary>
    public float Velocity;
}
