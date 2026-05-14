using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed record SvgDocument(IReadOnlyList<Stroke> Strokes, Rect SourceViewBox);
