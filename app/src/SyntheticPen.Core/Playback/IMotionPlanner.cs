using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface IMotionPlanner
{
    IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> screenStrokes,
        PlanOptions options,
        CancellationToken ct = default);
}
