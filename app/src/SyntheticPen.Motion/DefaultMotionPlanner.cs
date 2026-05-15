using System.Runtime.CompilerServices;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;

namespace SyntheticPen.Motion;

public sealed class DefaultMotionPlanner : IMotionPlanner
{
    public async IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> screenStrokes,
        PlanOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (screenStrokes.Count == 0) yield break;

        double baseVelocity = options.BaseVelocityPxPerSec * options.SpeedMultiplier;
        double travelVelocity = baseVelocity * options.TravelSpeedFactor;
        TimeSpan offset = TimeSpan.Zero;

        for (int sIdx = 0; sIdx < screenStrokes.Count; sIdx++)
        {
            ct.ThrowIfCancellationRequested();

            var stroke = screenStrokes[sIdx].Points;
            if (stroke.Count < 2) continue;

            // Travel point at the start of every stroke after the first
            if (sIdx > 0)
            {
                var prevEnd = screenStrokes[sIdx - 1].Points[^1];
                double travelDist = Distance(prevEnd, stroke[0]);
                offset += TimeSpan.FromSeconds(travelDist / travelVelocity);
                yield return new TimedPoint(stroke[0], offset, PenDown: false);
            }

            // Build the arc-length table and a curvature-aware time table.
            // Local velocity dips on tight curves so the cursor traces small
            // letters / loops at a believable pace instead of zipping through.
            var (cumLen, total) = BuildLengthTable(stroke);
            var radii = ComputeCurvatureRadii(stroke);
            var cumTime = new double[stroke.Count];
            for (int i = 0; i < stroke.Count - 1; i++)
            {
                double segLen = cumLen[i + 1] - cumLen[i];
                // Worst-case radius across the segment endpoints — the segment
                // is only as fast as its slowest end.
                double rSeg = Math.Min(radii[i], radii[i + 1]);
                double v = baseVelocity * CurvatureSpeedScale(rSeg, options);
                cumTime[i + 1] = cumTime[i] + segLen / v;
            }
            double T = cumTime[^1];
            int N = Math.Max(2, (int)Math.Ceiling(T * options.SampleHz) + 1);
            var strokeStart = offset;

            for (int i = 0; i < N; i++)
            {
                ct.ThrowIfCancellationRequested();
                double u = (double)i / (N - 1);
                double s = Ease(u);
                double t = s * T;
                var pt = PointAtTime(stroke, cumTime, t);
                yield return new TimedPoint(pt, strokeStart + TimeSpan.FromSeconds(t), PenDown: true);
            }

            offset = strokeStart + TimeSpan.FromSeconds(T);
        }

        // Required to make this an async iterator (no genuine async work; honor cancellation).
        await Task.CompletedTask;
    }

    private static double Distance(PointF a, PointF b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static (double[] cum, double total) BuildLengthTable(IReadOnlyList<PointF> pts)
    {
        var cum = new double[pts.Count];
        cum[0] = 0;
        for (int i = 1; i < pts.Count; i++)
            cum[i] = cum[i - 1] + Distance(pts[i - 1], pts[i]);
        return (cum, cum[^1]);
    }

    private static PointF PointAtArcLength(IReadOnlyList<PointF> pts, double[] cum, double dist)
    {
        if (dist <= 0) return pts[0];
        if (dist >= cum[^1]) return pts[^1];
        int lo = 0, hi = cum.Length - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (cum[mid] <= dist) lo = mid; else hi = mid;
        }
        double segLen = cum[hi] - cum[lo];
        double t = segLen < 1e-9 ? 0 : (dist - cum[lo]) / segLen;
        return new PointF(
            pts[lo].X + (pts[hi].X - pts[lo].X) * t,
            pts[lo].Y + (pts[hi].Y - pts[lo].Y) * t);
    }

    private static double Ease(double u)
    {
        if (u < 0.5) return 4 * u * u * u;
        double f = -2 * u + 2;
        return 1 - f * f * f / 2.0;
    }

    /// <summary>
    /// Local radius of curvature at each vertex, approximated from the turning
    /// angle and average adjacent segment length: R ≈ ds / |dθ|. Endpoints
    /// have no defined curvature → +∞ (i.e. no slowdown).
    /// </summary>
    private static double[] ComputeCurvatureRadii(IReadOnlyList<PointF> pts)
    {
        var r = new double[pts.Count];
        r[0] = double.PositiveInfinity;
        r[^1] = double.PositiveInfinity;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            var a = pts[i - 1]; var b = pts[i]; var c = pts[i + 1];
            double d1x = b.X - a.X, d1y = b.Y - a.Y;
            double d2x = c.X - b.X, d2y = c.Y - b.Y;
            double l1 = Math.Sqrt(d1x * d1x + d1y * d1y);
            double l2 = Math.Sqrt(d2x * d2x + d2y * d2y);
            if (l1 < 1e-9 || l2 < 1e-9) { r[i] = double.PositiveInfinity; continue; }
            double cross = d1x * d2y - d1y * d2x;
            double dot = d1x * d2x + d1y * d2y;
            double absTheta = Math.Abs(Math.Atan2(cross, dot));
            if (absTheta < 1e-6) { r[i] = double.PositiveInfinity; continue; }
            r[i] = (l1 + l2) * 0.5 / absTheta;
        }
        return r;
    }

    private static double CurvatureSpeedScale(double radius, PlanOptions opts)
    {
        if (opts.CurvatureRefRadius <= 0 || double.IsPositiveInfinity(radius)) return 1.0;
        double scale = Math.Pow(Math.Max(radius, 0.01) / opts.CurvatureRefRadius, opts.CurvaturePowerLawExp);
        return Math.Clamp(scale, opts.MinSpeedFraction, 1.0);
    }

    private static PointF PointAtTime(IReadOnlyList<PointF> pts, double[] cumTime, double t)
    {
        if (t <= 0) return pts[0];
        if (t >= cumTime[^1]) return pts[^1];
        int lo = 0, hi = cumTime.Length - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (cumTime[mid] <= t) lo = mid; else hi = mid;
        }
        double dt = cumTime[hi] - cumTime[lo];
        double f = dt < 1e-9 ? 0 : (t - cumTime[lo]) / dt;
        return new PointF(
            pts[lo].X + (pts[hi].X - pts[lo].X) * f,
            pts[lo].Y + (pts[hi].Y - pts[lo].Y) * f);
    }
}
