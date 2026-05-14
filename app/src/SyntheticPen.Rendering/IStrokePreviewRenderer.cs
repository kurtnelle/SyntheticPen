using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public interface IStrokePreviewRenderer
{
    object BuildGeometry(IReadOnlyList<Stroke> strokes);
}
