namespace SyntheticPen.Vectorize;

/// <summary>
/// Fits a centripetal Catmull-Rom spline through the (stair-stepped) skeleton
/// polyline and resamples it at a uniform arc-length spacing. Centripetal
/// parameterization avoids the cusps/overshoot the uniform variant produces on
/// the sharp 1px corners a thinned skeleton is full of.
/// </summary>
public static class CatmullRomResampler
{
    public static List<(double X, double Y)> Resample(
        IReadOnlyList<(double X, double Y)> pts, double spacing)
    {
        if (pts.Count < 2) return new List<(double, double)>(pts);
        if (spacing <= 0) spacing = 1.0;

        // Densely sample the spline first, then walk it by arc length.
        var dense = new List<(double X, double Y)>();
        int n = pts.Count;

        for (int i = 0; i < n - 1; i++)
        {
            var p0 = pts[Math.Max(0, i - 1)];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[Math.Min(n - 1, i + 2)];

            // Subdivision count scaled to segment length so long spans stay smooth.
            double segLen = Dist(p1, p2);
            int steps = Math.Clamp((int)(segLen / Math.Max(0.5, spacing) * 4) + 1, 4, 64);

            for (int s = 0; s < steps; s++)
            {
                double t = (double)s / steps;
                dense.Add(CentripetalCR(p0, p1, p2, p3, t));
            }
        }
        dense.Add(pts[n - 1]);

        // Uniform arc-length resampling.
        var outp = new List<(double X, double Y)> { dense[0] };
        double acc = 0;
        for (int i = 1; i < dense.Count; i++)
        {
            var a = dense[i - 1];
            var b = dense[i];
            double d = Dist(a, b);
            if (d < 1e-9) continue;

            acc += d;
            while (acc >= spacing)
            {
                double over = acc - spacing;
                double f = 1.0 - over / d;
                outp.Add((a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f));
                acc = over;
            }
        }
        var last = dense[^1];
        if (Dist(outp[^1], last) > spacing * 0.5) outp.Add(last);
        return outp;
    }

    private static (double X, double Y) CentripetalCR(
        (double X, double Y) p0, (double X, double Y) p1,
        (double X, double Y) p2, (double X, double Y) p3, double t)
    {
        // Centripetal knot spacing: t_{i+1} = t_i + |P_{i+1}-P_i|^0.5
        double t0 = 0;
        double t1 = t0 + Math.Pow(Dist(p0, p1), 0.5);
        double t2 = t1 + Math.Pow(Dist(p1, p2), 0.5);
        double t3 = t2 + Math.Pow(Dist(p2, p3), 0.5);
        if (t1 == t0) t1 = t0 + 1e-6;
        if (t2 == t1) t2 = t1 + 1e-6;
        if (t3 == t2) t3 = t2 + 1e-6;

        double tt = t1 + (t2 - t1) * t;

        var a1 = Lerp(p0, p1, (tt - t0) / (t1 - t0));
        var a2 = Lerp(p1, p2, (tt - t1) / (t2 - t1));
        var a3 = Lerp(p2, p3, (tt - t2) / (t3 - t2));
        var b1 = Lerp(a1, a2, (tt - t0) / (t2 - t0));
        var b2 = Lerp(a2, a3, (tt - t1) / (t3 - t1));
        return Lerp(b1, b2, (tt - t1) / (t2 - t1));
    }

    private static (double X, double Y) Lerp((double X, double Y) a, (double X, double Y) b, double t)
        => (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    private static double Dist((double X, double Y) a, (double X, double Y) b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
