using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public readonly record struct TimedPoint(PointF Point, TimeSpan Offset, bool PenDown);
