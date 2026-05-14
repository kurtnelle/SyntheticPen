using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Svg;
using Xunit;

namespace SyntheticPen.Svg.Tests;

public class BezierFlattenerTests
{
    private const double Tolerance = 0.25;

    [Fact]
    public void FlattenCubic_collinear_returns_endpoints()
    {
        var pts = BezierFlattener.FlattenCubic(
            new PointF(0, 0), new PointF(10, 0), new PointF(20, 0), new PointF(30, 0), Tolerance);
        pts.Should().HaveCount(2);
        pts[0].Should().Be(new PointF(0, 0));
        pts[^1].Should().Be(new PointF(30, 0));
    }

    [Fact]
    public void FlattenCubic_curved_emits_intermediate_points_within_tolerance()
    {
        var p0 = new PointF(0, 0);
        var c1 = new PointF(0, 100);
        var c2 = new PointF(100, 100);
        var p3 = new PointF(100, 0);
        var pts = BezierFlattener.FlattenCubic(p0, c1, c2, p3, Tolerance);

        pts.Should().HaveCountGreaterThan(2);
        pts[0].Should().Be(p0);
        pts[^1].Should().Be(p3);

        // Each consecutive pair should be sanely close
        for (int i = 1; i < pts.Count; i++)
        {
            var dx = pts[i].X - pts[i - 1].X;
            var dy = pts[i].Y - pts[i - 1].Y;
            Math.Sqrt(dx * dx + dy * dy).Should().BeLessThan(20);
        }
    }

    [Fact]
    public void FlattenQuadratic_emits_endpoints_and_intermediate()
    {
        var pts = BezierFlattener.FlattenQuadratic(
            new PointF(0, 0), new PointF(50, 100), new PointF(100, 0), Tolerance);
        pts[0].Should().Be(new PointF(0, 0));
        pts[^1].Should().Be(new PointF(100, 0));
        pts.Count.Should().BeGreaterThan(2);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    public void Invalid_tolerance_throws(double tol)
    {
        var act = () => BezierFlattener.FlattenCubic(
            new PointF(0, 0), new PointF(10, 0), new PointF(20, 0), new PointF(30, 0), tol);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
