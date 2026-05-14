using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public sealed class StrokePreviewRenderer : IStrokePreviewRenderer
{
    public object BuildGeometry(IReadOnlyList<Stroke> strokes)
        => throw new NotImplementedException("Phase 1");
}
