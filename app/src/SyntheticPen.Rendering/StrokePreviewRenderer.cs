using Avalonia;
using Avalonia.Media;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public sealed class StrokePreviewRenderer : IStrokePreviewRenderer
{
    public object BuildGeometry(IReadOnlyList<Stroke> strokes)
    {
        var geo = new PathGeometry();
        foreach (var s in strokes)
        {
            if (s.Points.Count < 2) continue;
            var fig = new PathFigure
            {
                StartPoint = new Point(s.Points[0].X, s.Points[0].Y),
                IsClosed = false,
                IsFilled = false
            };
            for (int i = 1; i < s.Points.Count; i++)
                fig.Segments!.Add(new LineSegment { Point = new Point(s.Points[i].X, s.Points[i].Y) });
            geo.Figures!.Add(fig);
        }
        return geo;
    }
}
