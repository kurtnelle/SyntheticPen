using SyntheticPen.Core.Models;

namespace SyntheticPen.Core;

public static class StrokeTransform
{
    public static IReadOnlyList<Stroke> FitToScreen(
        IReadOnlyList<Stroke> source,
        Rect sourceViewBox,
        Rect targetScreenRect,
        bool preserveAspectRatio = true)
    {
        if (source.Count == 0) return Array.Empty<Stroke>();

        double scaleX = targetScreenRect.W / sourceViewBox.W;
        double scaleY = targetScreenRect.H / sourceViewBox.H;
        double sx, sy, offsetX, offsetY;

        if (preserveAspectRatio)
        {
            double s = Math.Min(scaleX, scaleY);
            sx = sy = s;
            double fittedW = sourceViewBox.W * s;
            double fittedH = sourceViewBox.H * s;
            offsetX = targetScreenRect.X + (targetScreenRect.W - fittedW) / 2.0;
            offsetY = targetScreenRect.Y + (targetScreenRect.H - fittedH) / 2.0;
        }
        else
        {
            sx = scaleX;
            sy = scaleY;
            offsetX = targetScreenRect.X;
            offsetY = targetScreenRect.Y;
        }

        var result = new Stroke[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i].Points;
            var dst = new PointF[src.Count];
            for (int j = 0; j < src.Count; j++)
            {
                dst[j] = new PointF(
                    (src[j].X - sourceViewBox.X) * sx + offsetX,
                    (src[j].Y - sourceViewBox.Y) * sy + offsetY);
            }
            result[i] = new Stroke(dst);
        }
        return result;
    }
}
