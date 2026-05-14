using SyntheticPen.Core.Models;

namespace SyntheticPen.Input;

public interface ICursorInjector
{
    Task MoveAsync(PointF point, CancellationToken ct = default);
    Task PenDownAsync(CancellationToken ct = default);
    Task PenUpAsync(CancellationToken ct = default);
}
