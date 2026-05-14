using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public interface IMotionPlanner
{
    IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> strokes,
        PlanOptions options,
        CancellationToken ct = default);
}
