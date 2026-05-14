using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public sealed class PlaybackController : IPlaybackController
{
    private readonly ICursorInjector _injector;
    private readonly IMotionPlanner _planner;
    private CancellationTokenSource? _internalCts;

    public PlaybackController(ICursorInjector injector, IMotionPlanner planner)
    {
        _injector = injector;
        _planner = planner;
    }

    public PlaybackState State { get; private set; } = PlaybackState.Idle;
    public event Action<PlaybackState>? StateChanged;
    public event Action<TimeSpan>? CountdownTick;

    public async Task PlayAsync(IReadOnlyList<Stroke> screenStrokes, PlaybackOptions opts, CancellationToken ct = default)
    {
        if (State != PlaybackState.Idle) throw new InvalidOperationException("Playback already running.");

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var lct = _internalCts.Token;

        try
        {
            await RunCountdown(opts.Countdown, lct);
            await RunPlayback(screenStrokes, opts, lct);
        }
        catch (OperationCanceledException) { /* normal stop */ }
        catch (InjectionBlockedException) { /* swallow — state transitions to Idle in finally */ }
        finally
        {
            await SafePenUp();
            ChangeState(PlaybackState.Idle);
            _internalCts.Dispose();
            _internalCts = null;
        }
    }

    public void RequestStop()
    {
        if (_internalCts is { IsCancellationRequested: false })
        {
            ChangeState(PlaybackState.Cancelling);
            _internalCts.Cancel();
        }
    }

    private async Task RunCountdown(TimeSpan total, CancellationToken ct)
    {
        if (total <= TimeSpan.Zero) return;
        ChangeState(PlaybackState.CountingDown);
        var seconds = (int)Math.Ceiling(total.TotalSeconds);
        for (int s = seconds; s >= 1; s--)
        {
            CountdownTick?.Invoke(TimeSpan.FromSeconds(s));
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
        CountdownTick?.Invoke(TimeSpan.Zero);
    }

    private async Task RunPlayback(IReadOnlyList<Stroke> strokes, PlaybackOptions opts, CancellationToken ct)
    {
        ChangeState(PlaybackState.Playing);
        var plan = _planner.Plan(strokes,
            new PlanOptions(SpeedMultiplier: opts.SpeedMultiplier, SampleHz: opts.SampleHz), ct);

        var start = DateTime.UtcNow;
        bool penDown = false;

        await foreach (var p in plan.WithCancellation(ct))
        {
            var due = start + p.Offset;
            var delay = due - DateTime.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);

            if (p.PenDown && !penDown) { await _injector.PenDownAsync(ct); penDown = true; }
            else if (!p.PenDown && penDown) { await _injector.PenUpAsync(ct); penDown = false; }

            await _injector.MoveAsync(p.Point, ct);
        }

        if (penDown) await _injector.PenUpAsync(ct);
    }

    private async Task SafePenUp()
    {
        try { await _injector.PenUpAsync(CancellationToken.None); }
        catch { /* best effort */ }
    }

    private void ChangeState(PlaybackState s)
    {
        if (State == s) return;
        State = s;
        StateChanged?.Invoke(s);
    }
}
