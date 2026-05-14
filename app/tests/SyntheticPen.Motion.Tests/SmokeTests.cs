using FluentAssertions;
using SyntheticPen.Motion;
using Xunit;

namespace SyntheticPen.Motion.Tests;

public class SmokeTests
{
    [Fact]
    public void Planner_type_is_resolvable()
    {
        typeof(DefaultMotionPlanner).Should().Implement<IMotionPlanner>();
    }
}
