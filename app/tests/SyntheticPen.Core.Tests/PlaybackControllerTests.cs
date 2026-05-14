using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;
using SyntheticPen.Motion;
using Xunit;

namespace SyntheticPen.Core.Tests;

public class PlaybackControllerTests
{
    private static Stroke S(params (double x, double y)[] pts)
        => new Stroke(pts.Select(p => new PointF(p.x, p.y)).ToArray());

    private sealed class FakeInjector : ICursorInjector
    {
        public List<string> Events { get; } = new();
        public Task MoveAsync(PointF p, CancellationToken ct = default) { Events.Add($"M({p.X:0.0},{p.Y:0.0})"); return Task.CompletedTask; }
        public Task PenDownAsync(CancellationToken ct = default) { Events.Add("DOWN"); return Task.CompletedTask; }
        public Task PenUpAsync(CancellationToken ct = default) { Events.Add("UP"); return Task.CompletedTask; }
    }

    private sealed class FailFastInjector : ICursorInjector
    {
        public int MoveCalls;
        public Task MoveAsync(PointF p, CancellationToken ct = default)
        {
            if (++MoveCalls == 3) throw new InjectionBlockedException("denied");
            return Task.CompletedTask;
        }
        public Task PenDownAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task PenUpAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task PlayAsync_moves_to_first_point_then_DOWN_then_moves_then_UP_for_single_stroke()
    {
        var injector = new FakeInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        var strokes = new[] { S((0, 0), (100, 0)) };

        await ctrl.PlayAsync(strokes, new PlaybackOptions(SampleHz: 100, Countdown: TimeSpan.Zero));

        // Move-to-first precedes pen-down so synthetic-pointer injection
        // puts the ink at the right screen position.
        injector.Events[0].Should().StartWith("M(");
        injector.Events[1].Should().Be("DOWN");
        injector.Events.Last().Should().Be("UP");
        injector.Events.Count(e => e.StartsWith("M(")).Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task RequestStop_during_play_transitions_to_Idle_with_final_UP()
    {
        var injector = new FakeInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        var strokes = new[] { S((0, 0), (10000, 0)) };

        var task = ctrl.PlayAsync(strokes, new PlaybackOptions(SampleHz: 50, Countdown: TimeSpan.Zero));
        await Task.Delay(50);
        ctrl.RequestStop();
        await task;

        ctrl.State.Should().Be(PlaybackState.Idle);
        injector.Events.Last().Should().Be("UP");
    }

    [Fact]
    public async Task InjectionBlockedException_cancels_playback_with_final_UP()
    {
        var injector = new FailFastInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        var strokes = new[] { S((0, 0), (1000, 0)) };

        await ctrl.PlayAsync(strokes, new PlaybackOptions(SampleHz: 50, Countdown: TimeSpan.Zero));

        ctrl.State.Should().Be(PlaybackState.Idle);
    }

    [Fact]
    public async Task Countdown_ticks_each_second_down_to_zero()
    {
        var injector = new FakeInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        var ticks = new List<TimeSpan>();
        ctrl.CountdownTick += t => ticks.Add(t);

        await ctrl.PlayAsync(new[] { S((0, 0), (10, 0)) },
            new PlaybackOptions(SampleHz: 200, Countdown: TimeSpan.FromSeconds(2)));

        ticks.Should().HaveCountGreaterThanOrEqualTo(2);
        ticks.First().Should().Be(TimeSpan.FromSeconds(2));
    }
}
