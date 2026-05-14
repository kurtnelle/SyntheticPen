using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public readonly record struct TimedPoint(PointF Point, TimeSpan Offset, bool PenDown);
