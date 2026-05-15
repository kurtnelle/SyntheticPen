using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface ICursorInjector
{
    /// <summary>
    /// Pen pressure in [0,1] applied to subsequent events. The playback
    /// controller sets this from each <see cref="TimedPoint.Pressure"/> before
    /// moving. Injectors that can't express pressure (mouse) accept and ignore
    /// it. Default 1 (full).
    /// </summary>
    float Pressure { get; set; }

    Task MoveAsync(PointF screenPoint, CancellationToken ct = default);
    Task PenDownAsync(CancellationToken ct = default);
    Task PenUpAsync(CancellationToken ct = default);
}
