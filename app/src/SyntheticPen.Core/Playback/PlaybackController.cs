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
        catch (InjectionBlockedException ex)
        {
            // Logged so the cause is visible in the debugger Output window /
            // attached trace listeners; state still transitions to Idle in the
            // finally block so the UI returns to a usable state.
            System.Diagnostics.Trace.TraceWarning($"Playback aborted: {ex.Message}");
        }
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
        bool needsPrime = opts.PrimeTapHold > TimeSpan.Zero;

        // Injection pacing state. The planner can place samples microseconds
        // apart and the catch-up path below would otherwise fire them with no
        // gap, overrunning the synthetic-pointer pipeline. Paced() enforces a
        // floor (larger around contact transitions) using a monotonic clock.
        var paceClock = System.Diagnostics.Stopwatch.StartNew();
        double lastInjectMs = double.NegativeInfinity;
        bool prevWasContact = false;
        double minInterval = opts.MinEventInterval.TotalMilliseconds;
        double contactSettle = opts.ContactSettle.TotalMilliseconds;

        async Task Paced(Func<Task> inject, bool contactTransition)
        {
            double need = (contactTransition || prevWasContact) ? contactSettle : minInterval;
            if (need > 0)
            {
                double deficit = need - (paceClock.Elapsed.TotalMilliseconds - lastInjectMs);
                if (deficit > 0) await Task.Delay(TimeSpan.FromMilliseconds(deficit), ct);
            }
            await inject();
            lastInjectMs = paceClock.Elapsed.TotalMilliseconds;
            prevWasContact = contactTransition;
        }

        await foreach (var p in plan.WithCancellation(ct))
        {
            var due = start + p.Offset;
            var delay = due - DateTime.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);

            // Apply this sample's pressure before any injection below so the
            // synthetic pointer renders width that tracks the extracted stroke.
            _injector.Pressure = p.Pressure;

            if (p.PenDown && !penDown)
            {
                // Move to the stroke's first point FIRST, then put the pen down at that
                // position. Synthetic-pointer injection carries position with every event;
                // calling PenDownAsync before the move issues a down at the previous cursor
                // (or worse, the injector's default (0,0)) and target apps render a phantom
                // stroke from there.
                await Paced(() => _injector.MoveAsync(p.Point, ct), contactTransition: false);

                if (needsPrime)
                {
                    needsPrime = false;
                    await Paced(() => _injector.PenDownAsync(ct), contactTransition: true);
                    await Task.Delay(opts.PrimeTapHold, ct);
                    await Paced(() => _injector.PenUpAsync(ct), contactTransition: true);
                    if (opts.PrimeTapSettle > TimeSpan.Zero)
                        await Task.Delay(opts.PrimeTapSettle, ct);
                    await Paced(() => _injector.MoveAsync(p.Point, ct), contactTransition: false);
                }

                await Paced(() => _injector.PenDownAsync(ct), contactTransition: true);
                penDown = true;
            }
            else if (!p.PenDown && penDown)
            {
                // Lift the pen before traveling so we don't smear ink across the air gap.
                await Paced(() => _injector.PenUpAsync(ct), contactTransition: true);
                penDown = false;
                await Paced(() => _injector.MoveAsync(p.Point, ct), contactTransition: false);
            }
            else
            {
                await Paced(() => _injector.MoveAsync(p.Point, ct), contactTransition: false);
            }
        }

        if (penDown) await Paced(() => _injector.PenUpAsync(ct), contactTransition: true);
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
