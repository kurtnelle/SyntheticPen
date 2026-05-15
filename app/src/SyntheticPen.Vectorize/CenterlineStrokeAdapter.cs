using SyntheticPen.Core.Models;

namespace SyntheticPen.Vectorize;

/// <summary>
/// Bridges the centerline pipeline output (<see cref="StrokePoint"/> in SVG
/// space, with per-point pressure) to the playback engine's
/// <see cref="Stroke"/> model. The returned view box is the tight bounding box
/// of the extracted centerline, which <c>StrokeTransform.FitToScreen</c> maps
/// onto the calibrated target region. <see cref="StrokePoint.Velocity"/> is
/// intentionally not carried — the motion planner derives its own
/// curvature-aware velocity.
/// </summary>
public static class CenterlineStrokeAdapter
{
    public static (IReadOnlyList<Stroke> Strokes, Rect ViewBox) ToStrokes(
        IReadOnlyList<IReadOnlyList<StrokePoint>> centerlines)
    {
        var strokes = new List<Stroke>(centerlines.Count);
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var cl in centerlines)
        {
            if (cl.Count < 2) continue;
            var pts = new PointF[cl.Count];
            var pressures = new float[cl.Count];
            for (int i = 0; i < cl.Count; i++)
            {
                var p = cl[i];
                pts[i] = new PointF(p.X, p.Y);
                pressures[i] = p.Pressure;
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            strokes.Add(new Stroke(pts, pressures));
        }

        var viewBox = strokes.Count == 0
            ? new Rect(0, 0, 1, 1)
            : new Rect(minX, minY, maxX - minX, maxY - minY);
        return (strokes, viewBox);
    }
}
