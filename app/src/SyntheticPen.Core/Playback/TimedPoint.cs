using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

/// <summary><paramref name="Pressure"/> is the pen pressure in [0,1] for this
/// sample (1 = full). Injectors that can't express pressure ignore it.</summary>
public readonly record struct TimedPoint(PointF Point, TimeSpan Offset, bool PenDown, float Pressure = 1f);
