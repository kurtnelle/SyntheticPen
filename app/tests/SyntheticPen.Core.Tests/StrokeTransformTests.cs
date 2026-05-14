using FluentAssertions;
using SyntheticPen.Core;
using SyntheticPen.Core.Models;
using Xunit;

namespace SyntheticPen.Core.Tests;

public class StrokeTransformTests
{
    private static Stroke S(params (double x, double y)[] pts)
        => new Stroke(pts.Select(p => new PointF(p.x, p.y)).ToArray());

    private static IReadOnlyList<Stroke> SS(Stroke s) => new[] { s };

    [Fact]
    public void FitToScreen_with_matching_aspect_scales_to_full_target()
    {
        var source = SS(S((0, 0), (100, 0), (100, 100), (0, 100)));
        var viewBox = new Rect(0, 0, 100, 100);
        var target = new Rect(500, 300, 200, 200);

        var fitted = StrokeTransform.FitToScreen(source, viewBox, target);

        var pts = fitted[0].Points;
        pts[0].Should().Be(new PointF(500, 300));
        pts[1].Should().Be(new PointF(700, 300));
        pts[2].Should().Be(new PointF(700, 500));
        pts[3].Should().Be(new PointF(500, 500));
    }

    [Fact]
    public void FitToScreen_preserves_aspect_ratio_by_default()
    {
        var source = SS(S((0, 0), (100, 0), (100, 50)));   // 2:1
        var viewBox = new Rect(0, 0, 100, 50);
        var target = new Rect(0, 0, 400, 400);              // square; should center vertically

        var fitted = StrokeTransform.FitToScreen(source, viewBox, target);

        // scale = min(400/100, 400/50) = 4; result is 400x200 centered -> y offset = 100
        var pts = fitted[0].Points;
        pts[0].Should().Be(new PointF(0, 100));
        pts[1].Should().Be(new PointF(400, 100));
        pts[2].Should().Be(new PointF(400, 300));
    }

    [Fact]
    public void FitToScreen_can_stretch_when_preserveAspectRatio_is_false()
    {
        var source = SS(S((0, 0), (100, 0), (100, 50)));
        var viewBox = new Rect(0, 0, 100, 50);
        var target = new Rect(0, 0, 400, 400);

        var fitted = StrokeTransform.FitToScreen(source, viewBox, target, preserveAspectRatio: false);

        var pts = fitted[0].Points;
        pts[0].Should().Be(new PointF(0, 0));
        pts[1].Should().Be(new PointF(400, 0));
        pts[2].Should().Be(new PointF(400, 400));
    }

    [Fact]
    public void FitToScreen_handles_offset_source_viewbox()
    {
        var source = SS(S((50, 25), (150, 25), (150, 125)));   // viewbox-relative
        var viewBox = new Rect(50, 25, 100, 100);
        var target = new Rect(0, 0, 200, 200);

        var fitted = StrokeTransform.FitToScreen(source, viewBox, target);

        var pts = fitted[0].Points;
        pts[0].Should().Be(new PointF(0, 0));
        pts[1].Should().Be(new PointF(200, 0));
        pts[2].Should().Be(new PointF(200, 200));
    }
}
