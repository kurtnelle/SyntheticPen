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

        double velocity = options.BaseVelocityPxPerSec * options.SpeedMultiplier;
        double travelVelocity = velocity * options.TravelSpeedFactor;
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

            var (cum, total) = BuildLengthTable(stroke);
            double T = total / velocity;
            int N = Math.Max(2, (int)Math.Ceiling(T * options.SampleHz) + 1);
            var strokeStart = offset;

            for (int i = 0; i < N; i++)
            {
                ct.ThrowIfCancellationRequested();
                double u = (double)i / (N - 1);
                double s = Ease(u);
                var pt = PointAtArcLength(stroke, cum, s * total);
                var localOffset = TimeSpan.FromSeconds(u * T);
                yield return new TimedPoint(pt, strokeStart + localOffset, PenDown: true);
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
}
