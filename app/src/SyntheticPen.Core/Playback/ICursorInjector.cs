using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface ICursorInjector
{
    Task MoveAsync(PointF screenPoint, CancellationToken ct = default);
    Task PenDownAsync(CancellationToken ct = default);
    Task PenUpAsync(CancellationToken ct = default);
}
