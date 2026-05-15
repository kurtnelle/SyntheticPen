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
        public float Pressure { get; set; } = 1f;
        public List<string> Events { get; } = new();
        // Pressure observed at each pen-down (the moment width is established).
        public List<float> DownPressures { get; } = new();
        public Task MoveAsync(PointF p, CancellationToken ct = default) { Events.Add($"M({p.X:0.0},{p.Y:0.0})"); return Task.CompletedTask; }
        public Task PenDownAsync(CancellationToken ct = default) { Events.Add("DOWN"); DownPressures.Add(Pressure); return Task.CompletedTask; }
        public Task PenUpAsync(CancellationToken ct = default) { Events.Add("UP"); return Task.CompletedTask; }
    }

    private sealed class FailFastInjector : ICursorInjector
    {
        public float Pressure { get; set; } = 1f;
        public int MoveCalls;
        public Task MoveAsync(PointF p, CancellationToken ct = default)
        {
            if (++MoveCalls == 3) throw new InjectionBlockedException("denied");
            return Task.CompletedTask;
        }
        public Task PenDownAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task PenUpAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class PressureRecorder : ICursorInjector
    {
        public float Pressure { get; set; } = 1f;
        public List<float> MovePressures { get; } = new();
        public Task MoveAsync(PointF p, CancellationToken ct = default) { MovePressures.Add(Pressure); return Task.CompletedTask; }
        public Task PenDownAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task PenUpAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class TimedInjector : ICursorInjector
    {
        private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
        public float Pressure { get; set; } = 1f;
        public List<(char Kind, double Ms)> Events { get; } = new();
        public Task MoveAsync(PointF p, CancellationToken ct = default) { Events.Add(('M', _sw.Elapsed.TotalMilliseconds)); return Task.CompletedTask; }
        public Task PenDownAsync(CancellationToken ct = default) { Events.Add(('D', _sw.Elapsed.TotalMilliseconds)); return Task.CompletedTask; }
        public Task PenUpAsync(CancellationToken ct = default) { Events.Add(('U', _sw.Elapsed.TotalMilliseconds)); return Task.CompletedTask; }
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

    [Fact]
    public async Task Per_point_pressure_flows_from_stroke_through_planner_to_injector()
    {
        // Pressure ramps 0 → 1 along the stroke; the injector should see it
        // applied per sample, low at the start and high at the end.
        var pts = new[] { new PointF(0, 0), new PointF(300, 0), new PointF(600, 0) };
        var pressures = new[] { 0f, 0.5f, 1f };
        var injector = new PressureRecorder();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());

        await ctrl.PlayAsync(new[] { new Stroke(pts, pressures) },
            new PlaybackOptions(SampleHz: 100, Countdown: TimeSpan.Zero));

        injector.MovePressures.Should().NotBeEmpty();
        injector.MovePressures.Min().Should().BeLessThan(0.1f);
        injector.MovePressures.Max().Should().BeGreaterThan(0.9f);
        // First sample lighter than the last.
        injector.MovePressures.First().Should().BeLessThan(injector.MovePressures.Last());
    }

    [Fact]
    public async Task Catch_up_bursts_are_paced_so_contact_transitions_are_isolated()
    {
        // Many tiny strokes at extreme speed: the planner schedules samples
        // ~0ms apart so the controller is always behind real time and, without
        // pacing, would fire DOWN/UP microseconds apart (the root cause of the
        // synthetic-pointer ERROR_INVALID_PARAMETER). With pacing every
        // contact transition must be isolated by >= ContactSettle.
        var strokes = Enumerable.Range(0, 12)
            .Select(i => S((i * 10, 0), (i * 10 + 3, 1), (i * 10 + 5, 0)))
            .ToArray();

        var injector = new TimedInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());

        var contactSettle = TimeSpan.FromMilliseconds(20);
        await ctrl.PlayAsync(strokes, new PlaybackOptions(
            SampleHz: 1000, SpeedMultiplier: 1000, Countdown: TimeSpan.Zero,
            MinEventInterval: TimeSpan.FromMilliseconds(5),
            ContactSettle: contactSettle));

        var ev = injector.Events;
        ev.Should().NotBeEmpty();
        ev.Count(e => e.Kind == 'D').Should().Be(12); // one DOWN per stroke
        ev.Count(e => e.Kind == 'U').Should().BeGreaterThanOrEqualTo(12); // per-stroke + closing

        // Mid-playback, any event adjacent to a contact transition must be
        // >= ContactSettle apart (generous tolerance for OS timer coarseness,
        // but proves the zero-gap burst is gone — unpaced these were ~0.01ms).
        // The terminal tail after the last DOWN is excluded: the closing/
        // safety pen-up (PlayAsync's SafePenUp) is intentionally immediate —
        // releasing contact must never be delayed.
        int lastDown = ev.FindLastIndex(e => e.Kind == 'D');
        double floorMs = contactSettle.TotalMilliseconds * 0.5;
        for (int i = 1; i <= lastDown; i++)
        {
            bool contactAdjacent = ev[i].Kind is 'D' or 'U' || ev[i - 1].Kind is 'D' or 'U';
            if (contactAdjacent)
                (ev[i].Ms - ev[i - 1].Ms).Should()
                    .BeGreaterThan(floorMs, $"contact transition at index {i} must be paced");
        }
    }

    [Fact]
    public async Task Strokes_without_pressure_default_to_full()
    {
        var injector = new PressureRecorder();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());

        await ctrl.PlayAsync(new[] { S((0, 0), (200, 0)) },
            new PlaybackOptions(SampleHz: 100, Countdown: TimeSpan.Zero));

        injector.MovePressures.Should().NotBeEmpty();
        injector.MovePressures.Should().OnlyContain(p => p == 1f);
    }
}
