namespace SyntheticPen.Core.Models;

public sealed class Stroke
{
    public Stroke(IReadOnlyList<PointF> points, IReadOnlyList<float>? pressures = null)
    {
        Points = points;
        Pressures = pressures;
    }

    public IReadOnlyList<PointF> Points { get; }

    /// <summary>
    /// Optional per-point pen pressure in [0,1], same length and order as
    /// <see cref="Points"/>. Null means uniform pressure (the planner emits 1).
    /// </summary>
    public IReadOnlyList<float>? Pressures { get; }
}
