using FluentAssertions;
using SyntheticPen.Core.Models;
using Xunit;

namespace SyntheticPen.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Stroke_carries_its_points()
    {
        var stroke = new Stroke(new[] { new PointF(0, 0), new PointF(1, 1) });
        stroke.Points.Should().HaveCount(2);
    }
}
