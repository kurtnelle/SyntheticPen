using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public sealed class DefaultMotionPlanner : IMotionPlanner
{
    public IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> strokes,
        PlanOptions options,
        CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
