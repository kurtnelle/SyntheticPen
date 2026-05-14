using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public static class BezierFlattener
{
    public static IReadOnlyList<PointF> FlattenCubic(PointF p0, PointF c1, PointF c2, PointF p3, double tolerance)
    {
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var result = new List<PointF> { p0 };
        SubdivideCubic(p0, c1, c2, p3, tolerance, result, depth: 0);
        result.Add(p3);
        return result;
    }

    public static IReadOnlyList<PointF> FlattenQuadratic(PointF p0, PointF c, PointF p1, double tolerance)
    {
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        // Promote to cubic: c1 = p0 + 2/3(c - p0), c2 = p1 + 2/3(c - p1)
        var c1 = new PointF(p0.X + 2.0 / 3.0 * (c.X - p0.X), p0.Y + 2.0 / 3.0 * (c.Y - p0.Y));
        var c2 = new PointF(p1.X + 2.0 / 3.0 * (c.X - p1.X), p1.Y + 2.0 / 3.0 * (c.Y - p1.Y));
        return FlattenCubic(p0, c1, c2, p1, tolerance);
    }

    private const int MaxDepth = 18;

    private static void SubdivideCubic(PointF p0, PointF c1, PointF c2, PointF p3, double tol, List<PointF> output, int depth)
    {
        double d1 = PerpDistance(p0, p3, c1);
        double d2 = PerpDistance(p0, p3, c2);
        double maxD = Math.Max(d1, d2);

        if (maxD <= tol || depth >= MaxDepth) return;

        var p01 = Mid(p0, c1);
        var p12 = Mid(c1, c2);
        var p23 = Mid(c2, p3);
        var p012 = Mid(p01, p12);
        var p123 = Mid(p12, p23);
        var p0123 = Mid(p012, p123);

        SubdivideCubic(p0, p01, p012, p0123, tol, output, depth + 1);
        output.Add(p0123);
        SubdivideCubic(p0123, p123, p23, p3, tol, output, depth + 1);
    }

    private static PointF Mid(PointF a, PointF b) => new((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);

    private static double PerpDistance(PointF lineA, PointF lineB, PointF p)
    {
        double dx = lineB.X - lineA.X;
        double dy = lineB.Y - lineA.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9)
        {
            double px = p.X - lineA.X;
            double py = p.Y - lineA.Y;
            return Math.Sqrt(px * px + py * py);
        }
        double cross = Math.Abs(dx * (p.Y - lineA.Y) - dy * (p.X - lineA.X));
        return cross / len;
    }
}
