using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Motion;
using Xunit;

namespace SyntheticPen.Motion.Tests;

public class DefaultMotionPlannerTests
{
    private static Stroke S(params (double x, double y)[] pts)
        => new Stroke(pts.Select(p => new PointF(p.x, p.y)).ToArray());

    private readonly IMotionPlanner _planner = new DefaultMotionPlanner();

    [Fact]
    public async Task Single_stroke_emits_first_point_pen_down_at_zero_offset()
    {
        var strokes = new[] { S((0, 0), (100, 0)) };
        var pts = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 100)));

        pts[0].Point.Should().Be(new PointF(0, 0));
        pts[0].PenDown.Should().BeTrue();
        pts[0].Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Single_stroke_emits_last_point_pen_down_at_end_time()
    {
        var strokes = new[] { S((0, 0), (600, 0)) };   // length 600px @ 600px/s = 1s
        var pts = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 100)));

        pts[^1].Point.X.Should().BeApproximately(600.0, 0.1);
        pts[^1].PenDown.Should().BeTrue();
        pts[^1].Offset.TotalSeconds.Should().BeApproximately(1.0, 0.05);
    }

    [Fact]
    public async Task SpeedMultiplier_2x_halves_total_time()
    {
        var strokes = new[] { S((0, 0), (600, 0)) };
        var ptsSlow = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 100, SpeedMultiplier: 1.0)));
        var ptsFast = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 100, SpeedMultiplier: 2.0)));

        ptsFast[^1].Offset.Should().BeLessThan(ptsSlow[^1].Offset);
        ptsFast[^1].Offset.TotalSeconds.Should().BeApproximately(ptsSlow[^1].Offset.TotalSeconds / 2.0, 0.05);
    }

    [Fact]
    public async Task Two_strokes_emit_a_pen_up_travel_point_between_them()
    {
        var strokes = new[]
        {
            S((0, 0), (100, 0)),
            S((200, 0), (300, 0))
        };
        var pts = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 100)));

        var penUp = pts.Where(p => !p.PenDown).ToArray();
        penUp.Should().HaveCount(1);
        penUp[0].Point.Should().Be(new PointF(200, 0));   // travel target = start of next stroke
    }

    [Fact]
    public async Task Offsets_are_monotonically_non_decreasing()
    {
        var strokes = new[]
        {
            S((0, 0), (50, 50), (100, 0)),
            S((200, 0), (250, 50)),
        };
        var pts = await Collect(_planner.Plan(strokes, new PlanOptions(SampleHz: 200)));

        for (int i = 1; i < pts.Count; i++)
            pts[i].Offset.Should().BeGreaterThanOrEqualTo(pts[i - 1].Offset);
    }

    private static async Task<List<TimedPoint>> Collect(IAsyncEnumerable<TimedPoint> src)
    {
        var list = new List<TimedPoint>();
        await foreach (var p in src) list.Add(p);
        return list;
    }
}
