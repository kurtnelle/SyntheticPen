namespace SyntheticPen.Core.Models;

public sealed class Stroke
{
    public Stroke(IReadOnlyList<PointF> points)
    {
        Points = points;
    }

    public IReadOnlyList<PointF> Points { get; }
}
