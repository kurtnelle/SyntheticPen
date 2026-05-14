# SyntheticPen — Phase 1 Design

**Date:** 2026-05-14
**Status:** Approved (pending final user spec review)
**Depends on:** [2026-05-14-syntheticpen-scaffold-design.md](2026-05-14-syntheticpen-scaffold-design.md)

## 1. Goal

Make the scaffold draw. Load an SVG, preview the strokes, calibrate a target rectangle on screen, and replay the strokes as Win32 cursor or pen input — with a 3-second pre-play countdown and a global Esc emergency stop.

## 2. Scope decision

The brainstorm flagged that "Phase 1" in the brief bundles four independent subsystems: SVG parsing, preview, motion planning, mouse injection. The user opted for **one combined spec** rather than slicing into 2–4 smaller specs. This document accepts that decision but the implementation plan that follows must break the work into independently committable tasks; otherwise the slice gets stuck halfway with no usable demo.

**Out of scope** (deferred):
- Humanization, jitter, variable pressure, hand tremor (Phase 2/3).
- Loop, pause, resume controls (Phase 2).
- Path optimization, stroke reordering (Phase 2).
- Persistence of target rects, recent files, profiles (Phase 2).
- Virtual HID injection mode (Phase 3).
- Recording engine, scripting (incl. G-code), AI stroke synthesis (Phase 4).

## 3. Architecture

```
SVG file ──► SvgPathLoader ──► SvgDocument { Strokes, SourceViewBox }
                                       │
                                       ▼  StrokeTransform.FitToScreen
                              IReadOnlyList<Stroke> in screen pixels
                                       │
                                       ▼
                              MotionPlanner ──► IAsyncEnumerable<TimedPoint>
                                       │
                                       ▼
                              CursorInjector ──► Win32 SendInput / SyntheticPointer
```

`PlaybackController` owns the pipeline, runs it on a worker, raises state events, and honors a `CancellationToken` tied to the Esc hotkey.

### Coordinate units

All downstream components after `FitToScreen` operate in **absolute screen pixel coordinates**. The parser produces strokes in SVG user units; one transformation occurs at the point of `Play`; planner and injector never see SVG units. This keeps the preview pane and the injector reading the same data and avoids a second mode bit.

### Pen-down semantics

`TimedPoint.PenDown` is true within a stroke, false during the synthetic travel point(s) between strokes. The injector translates `false→true` to `MOUSEEVENTF_LEFTDOWN`, and `true→false` to `MOUSEEVENTF_LEFTUP`. The first emitted point and the last always carry an explicit pen state for clarity.

## 4. Module changes vs. scaffold

| Project | Today | After Phase 1 |
|---|---|---|
| `SyntheticPen.Core` | Stroke, PointF, PlaybackController stub | + `Rect`, `ITargetRegionProvider`, `StrokeTransform`, real `PlaybackController`, `InjectionBlockedException`, deny-list helper |
| `SyntheticPen.Svg` | Stubs | Real `SkiaSvgPathLoader`, real `BezierFlattener`, `SvgDocument`, `FlattenOptions`, `SvgParseException` |
| `SyntheticPen.Motion` | Stubs | `DefaultMotionPlanner` with constant velocity + ease-in/ease-out at each stroke boundary |
| `SyntheticPen.Input` | P/Invoke skeleton | `MouseSendInputInjector` (real), new `SyntheticPointerInjector` (`InjectSyntheticPointerInput`, Win10 1809+) |
| `SyntheticPen.Rendering` | Stub | `StrokePreviewRenderer` → returns Avalonia `PathGeometry`; tile-grid backdrop brush |
| `SyntheticPen.App` | VM no-ops | Open-SVG dialog, `CalibrationOverlay` window, countdown overlay, "PLOTTING" indicator, real Play/Stop wiring |
| `SyntheticPen.Hotkeys` *(new)* | — | `IGlobalHotkeyService` + `GlobalHotkeyService` using `SetWindowsHookEx(WH_KEYBOARD_LL)` for Esc |
| `SyntheticPen.Hotkeys.Tests` *(new)* | — | One integration test, `[Trait("Category", "Integration")]`, excluded from CI |

The new `SyntheticPen.Hotkeys` project keeps the keyboard hook plumbing out of `Input` (which is OS-level *output*) and out of `Core` (which stays platform-agnostic).

## 5. Data types

### Core

```csharp
namespace SyntheticPen.Core.Models;

public readonly record struct PointF(double X, double Y);

public readonly record struct Rect(double X, double Y, double W, double H)
{
    public bool IsEmpty => W <= 0 || H <= 0;
    public double Right => X + W;
    public double Bottom => Y + H;
}

public sealed class Stroke
{
    public Stroke(IReadOnlyList<PointF> points) => Points = points;
    public IReadOnlyList<PointF> Points { get; }
}
```

```csharp
namespace SyntheticPen.Core.Targeting;

public interface ITargetRegionProvider
{
    Rect? Current { get; }
    event Action<Rect?> Changed;
    void Set(Rect? region);
}
```

```csharp
namespace SyntheticPen.Core.Playback;

public enum PlaybackState { Idle, CountingDown, Playing, Cancelling }

// InjectionMode is defined here (not in SyntheticPen.Input) so that Core
// stays platform-agnostic and downstream layers can reference a single enum.
public enum InjectionMode { Mouse, SyntheticPointer, VirtualHid }

public sealed record PlaybackOptions(
    double SpeedMultiplier = 1.0,
    InjectionMode Mode = InjectionMode.Mouse,
    TimeSpan Countdown = default,
    double SampleHz = 200.0,
    bool WaitForFocusRelease = true);

public interface IPlaybackController
{
    PlaybackState State { get; }
    event Action<PlaybackState> StateChanged;
    event Action<TimeSpan> CountdownTick;
    Task PlayAsync(IReadOnlyList<Stroke> screenStrokes, PlaybackOptions opts, CancellationToken ct);
    void RequestStop();
}
```

```csharp
namespace SyntheticPen.Core;

public static class StrokeTransform
{
    public static IReadOnlyList<Stroke> FitToScreen(
        IReadOnlyList<Stroke> source,
        Rect sourceViewBox,
        Rect targetScreenRect,
        bool preserveAspectRatio = true);
}
```

### Svg

```csharp
namespace SyntheticPen.Svg;

public sealed record SvgDocument(IReadOnlyList<Stroke> Strokes, Rect SourceViewBox);

public sealed record FlattenOptions(double Tolerance = 0.25);  // SVG units

public sealed class SvgParseException : Exception
{
    public SvgParseException(string message, int? lineNumber = null) : base(message)
    {
        LineNumber = lineNumber;
    }
    public int? LineNumber { get; }
}

public interface ISvgPathLoader
{
    Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default);
}

public static class BezierFlattener
{
    public static IReadOnlyList<PointF> FlattenCubic(PointF p0, PointF c1, PointF c2, PointF p3, double tolerance);
    public static IReadOnlyList<PointF> FlattenQuadratic(PointF p0, PointF c, PointF p1, double tolerance);
}
```

### Motion

```csharp
namespace SyntheticPen.Motion;

public readonly record struct TimedPoint(PointF Point, TimeSpan Offset, bool PenDown);

public sealed record PlanOptions(double SpeedMultiplier = 1.0, double SampleHz = 200.0);

public interface IMotionPlanner
{
    IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> screenStrokes,
        PlanOptions opts,
        CancellationToken ct = default);
}
```

### Input

```csharp
namespace SyntheticPen.Input;

// InjectionMode lives in SyntheticPen.Core.Playback; this assembly references it
// from there. Don't redeclare.

public sealed class InjectionBlockedException(string reason) : Exception(reason);

public interface ICursorInjector
{
    Task MoveAsync(PointF screenPoint, CancellationToken ct = default);
    Task PenDownAsync(CancellationToken ct = default);
    Task PenUpAsync(CancellationToken ct = default);
}
```

### Hotkeys

```csharp
namespace SyntheticPen.Hotkeys;

public interface IGlobalHotkeyService : IDisposable
{
    bool IsInstalled { get; }
    event Action EmergencyStopRequested;
    void Install();
}
```

## 6. SVG parser

### Approach

`SkiaSvgPathLoader` uses **Svg.Skia 2.x** (already in CPM) to parse the SVG. We walk the parsed `SKPicture` / `SKSvg` model to extract per-`<path>` geometry, then iterate `SKPath` verbs to feed the flattener. Although Svg.Skia is primarily a renderer, its path traversal API exposes the same `Move/Line/Cubic/Quad/Close` verbs that map directly to `BezierFlattener`.

### Officially supported path commands

`M m L l C c Q q Z z S s T t` and their relative/shorthand variants. Svg.Skia will silently accept arcs (`A/a`) and we won't fail on them, but they are **not officially tested** in this slice. Add full arc coverage in Phase 2 alongside path optimization.

### Other supported elements

`<line>`, `<polyline>`, `<polygon>` are converted to single `<path>` equivalents internally.

### ViewBox & transforms

- `SourceViewBox` is read from the SVG root `viewBox` attribute. Fallback: bounding box of all parsed strokes.
- Nested `<g transform="…">` and `transform="…"` on path elements are applied during parse (matrix multiplication, recursive). Svg.Skia handles this.

### Errors

- Empty file / missing root element → `SvgParseException`.
- Malformed XML → `SvgParseException` (wraps `XmlException`).
- No path-bearing elements → `SvgDocument` with `Strokes.Count == 0` (not an error; UI disables Play).
- Tolerance ≤ 0 → `ArgumentOutOfRangeException`.

## 7. Bezier flattener

Standard recursive de Casteljau subdivision with flatness check. For each segment, compute the maximum perpendicular distance from the chord to the control points; if it's ≤ `tolerance`, emit the endpoints; otherwise subdivide. Tolerance is in SVG user units. Default 0.25.

Tests verify that every produced segment's midpoint lies within `tolerance` of the true Bezier at parameter `t = 0.5` of that subsegment — a necessary but stricter-than-sufficient property check. Hand-picked control-point cases (collinear, cusp, S-shape) round out the suite.

## 8. Coordinate mapping (`StrokeTransform.FitToScreen`)

Compute scale = `min(targetW/sourceW, targetH/sourceH)` when `preserveAspectRatio=true`, else two independent axis scales. Center the scaled bounds within the target rect (when preserving aspect). Translate each point: `screenPt = (svgPt - sourceTopLeft) * scale + targetTopLeft + centeringOffset`. SVG y-axis is the same direction as screen y-axis (top-down), so no flip.

## 9. Motion planner

`DefaultMotionPlanner.Plan` consumes screen-pixel strokes and emits `TimedPoint` events at `SampleHz` rate (default 200 Hz → 5 ms between samples).

Per-stroke math:
- Let `velocity = 600 px/s × opts.SpeedMultiplier` (constant, applies during the stroke).
- Let `L` = arc length of the stroke = sum of inter-point distances.
- Stroke duration `T = L / velocity` seconds.
- Sample count `N = ceil(T × SampleHz) + 1` (so we always cover [0, T] inclusive).
- For each sample `i = 0..N-1`:
  - `u = i / (N - 1)` ∈ [0, 1] — linear time.
  - `s = ease(u)` where `ease(u) = u < 0.5 ? 4u³ : 1 − (−2u + 2)³ / 2` (cubic in/out).
  - Position = point at arc-length `s × L` along the stroke (linear interpolation between input points).
  - `Offset = strokeStartTime + u × T`.
  - `PenDown = true`.

Easing modulates **position along the curve**, not the time grid. Time samples remain evenly spaced; the cursor moves slowly at stroke start/end and faster in the middle — natural motion.

Between strokes:
- Emit one travel point at the START of the next stroke (its first input point) with `PenDown = false`.
- Travel time = `travelDistance / (velocity × 2)` (move twice as fast in the air).
- Travel time accumulates into the running offset before the next stroke's samples begin.

`Offset` is cumulative from t=0 of `Play()`. The injector schedules each emission via `await Task.Delay(point.Offset - elapsed, ct)`.

The first emitted point of each stroke after the first carries `PenDown=false` (a "pen up, move, then pen down at first stroke point next tick" sequence). The very first point of the very first stroke carries `PenDown=true`. The synthetic final point at the end of playback is implicit — the controller, not the planner, emits the final `PenUpAsync` after the stream ends.

## 10. Input injection

### `MouseSendInputInjector`

`SendInput` with `INPUT_MOUSE` and the appropriate flags:
- Move: `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK`. Coordinates are normalized to `0..65535` over the virtual desktop.
- Down: `MOUSEEVENTF_LEFTDOWN`.
- Up: `MOUSEEVENTF_LEFTUP`.

After each `SendInput` call, if it returns 0, throw `InjectionBlockedException("SendInput refused — UIPI / UAC integrity mismatch?")`.

### `SyntheticPointerInjector` (Mode = SyntheticPointer)

Uses `InitializeSyntheticPointerDevice(POINTER_INPUT_TYPE_PEN, 1, …)` + `InjectSyntheticPointerInput` (Windows 10 1809+). Pressure is a flat constant (0.5) until Phase 2 adds curves.

If `InitializeSyntheticPointerDevice` is unavailable (Win 10 < 1809), `SyntheticPointerInjector` throws on construction and the controller transparently downgrades to `MouseSendInputInjector` with a logged warning.

### Foreground-window deny list

Before every batch of points, check `GetForegroundWindow` → class name via `GetClassName`. If matches one of:
- `Credential Dialog Xaml Host`
- `LockScreen`
- `LogonUI`
- `ConsentUI`
- `UACBlackScreen`

…raise `InjectionBlockedException("foreground window is a credential surface")`. Controller transitions to `Cancelling`.

## 11. Playback controller

Pseudocode:

```text
PlayAsync(strokes, opts, ct):
  state = CountingDown
  for sec = ceil(opts.Countdown.TotalSeconds) .. 1:
      raise CountdownTick(seconds remaining)
      await Task.Delay(1s, ct)
  state = Playing
  show "PLOTTING" indicator
  await foreach (tp in planner.Plan(strokes, planOpts, ct)):
      enforce deny list
      await Task.Delay(tp.Offset - elapsed, ct)
      if tp.PenDown && !penIsDown: await injector.PenDownAsync(ct); penIsDown = true
      if !tp.PenDown && penIsDown: await injector.PenUpAsync(ct);   penIsDown = false
      await injector.MoveAsync(tp.Point, ct)
  if penIsDown: await injector.PenUpAsync(ct)
  state = Idle

on cancel (ct fires OR RequestStop):
  state = Cancelling
  if penIsDown: best-effort injector.PenUpAsync()
  state = Idle
```

Cancellation tokens are linked: the caller's `ct`, an internal `CancellationTokenSource` driven by `RequestStop`, and the hotkey service's `EmergencyStopRequested` event.

## 12. UI

### MainWindow layout

Two-column split as in the scaffold. Left: preview Canvas with grid backdrop. Right: controls — file label, target-region label + Calibrate button, speed slider, mode combo, Play/Stop buttons, state label.

### Always-on-top

The MainWindow has `Topmost = true` for the entire app session. This lets the user keep SyntheticPen visible over the target app while they line things up. Exceptions:
- **Minimized during calibration drag** — restored automatically on mouse-up or Esc. Otherwise the always-on-top window would block the very region the user is trying to select.
- A `View → Always on top` menu item exposes a toggle for users who'd prefer it off; default ON.
- The countdown overlay and the PLOTTING indicator are *also* topmost but separate Avalonia windows; they sit above the MainWindow's topmost.

### Calibration overlay (Snip & Sketch-style region select)

When the user clicks **Calibrate**:

1. MainWindow minimizes (so its own topmost surface doesn't occlude the target region).
2. A borderless, frameless, always-on-top `CalibrationOverlay` window opens spanning the entire virtual desktop (`Screens.All` bounds aggregated). Window flags: `TransparencyLevelHint=Transparent`, `SystemDecorations=None`, `Topmost=true`, `ShowInTaskbar=false`, captures pointer + keyboard.
3. **Initial state**: the entire overlay is a translucent dim — `#80000000` (50% black). A faint crosshair tracks the cursor with subpixel-aligned 0.5px gridlines fading toward the edges. A floating instruction chip reads `Drag to select target region · Esc to cancel`.
4. **During drag** (`PointerPressed` → `PointerMoved`):
   - The selected rectangle is *cut out* of the dim: inside is fully transparent (the actual screen content visible), outside stays dim. Implemented as four `Rectangle`s composed around the selection, OR a `Path` with even-odd fill rule.
   - A 1px Electric Ink Blue (`#4DA3FF`) border on the selection rect, with a 6px outer glow (`#4DA3FF` 40% alpha) so it reads against any background.
   - A small live readout chip pinned just outside the rectangle's bottom-right corner shows `WIDTH × HEIGHT` in monospace (e.g. `1280 × 720`) and the top-left coordinate `(x, y)` in dim text below.
   - Selection snaps to integer pixels (no sub-pixel rect).
5. **On `PointerReleased`**: if the rect has non-zero area, commit to `ITargetRegionProvider.Set(...)` in absolute screen coordinates (DPI-corrected via `Screen.Scaling` per monitor — recompute per pointer position since cursors crossing monitor boundaries may switch DPI mid-drag), close the overlay, restore MainWindow. If zero-area, fall through to step 6.
6. **Esc** at any point: close overlay without setting a region, restore MainWindow.
7. **Right-click during drag**: cancel current selection, return to initial state (matches Snipping Tool's "restart" gesture).

Multi-monitor: the overlay covers all monitors as one logical surface. The chosen rectangle may straddle monitors; that's allowed and stored as a single rectangle in virtual-desktop coordinates.

DPI: Avalonia 11 reports per-window DPI; we set `UseLayoutRounding=true` on the overlay so visuals stay crisp. Coordinates returned to `ITargetRegionProvider` are in **physical device pixels** of the virtual desktop, which is what `SendInput` expects when normalizing to `0..65535`.

### Countdown overlay

Centered borderless window 320×220 showing `3` / `2` / `1` in 160px Space Grotesk-equivalent (system font fallback). Updates on each `CountdownTick`. Closes when state transitions out of `CountingDown`.

### PLOTTING indicator

220×40 borderless click-through window pinned top-right of the primary monitor while state ∈ {`CountingDown`, `Playing`}. Cyan dot + monospace "PLOTTING" text.

### File / Help menus

`File → Open SVG…` opens a system file dialog filtered to `*.svg`. `File → Exit` closes the app. `Help → About` opens a 480×320 modal with version, GitHub link, and a paragraph explaining the Esc hotkey.

## 13. Testing

| Layer | Approach | Project |
|---|---|---|
| `SkiaSvgPathLoader` | 6–8 golden-file fixture SVGs in `tests/fixtures/` with hand-computed expected stroke count and bounding box | `SyntheticPen.Svg.Tests` |
| `BezierFlattener` | Property: every flattened segment's midpoint within tolerance of true curve; cases for collinear, cusp, S-shape | `SyntheticPen.Svg.Tests` |
| `StrokeTransform.FitToScreen` | Pure-function tests for aspect preservation, bounds matching, off-center sources, both axes-scale mode | `SyntheticPen.Core.Tests` |
| `DefaultMotionPlanner` | Total path length, timing monotonicity, PenDown edges, SpeedMultiplier scaling | `SyntheticPen.Motion.Tests` |
| `PlaybackController` | FakeInjector + FakeHotkeyService. Verify countdown → playing → idle, mid-stroke cancel emits final LEFTUP, deny-list refusal | `SyntheticPen.Core.Tests` |
| `MouseSendInputInjector`, `SyntheticPointerInjector` | No automated tests — manual smoke in Notepad/Paint/Whiteboard | — |
| `GlobalHotkeyService` | Integration test: install, synthesize Esc via `keybd_event`, assert event raised. `[Trait("Category", "Integration")]`, skipped in CI | `SyntheticPen.Hotkeys.Tests` (new) |
| Calibration / countdown / indicator windows | Manual smoke | — |

CI runs unit tests only. Integration trait is excluded by default.

## 14. Error handling matrix

| Failure | Behavior |
|---|---|
| SVG with no path-bearing elements | `SvgDocument.Strokes.Count == 0`; UI: "0 strokes"; Play disabled |
| Invalid XML / malformed path data | `SvgParseException` (with line if available); UI toast |
| Target rect off-screen, zero-size, or negative | Calibration rejects; cannot set rect |
| `SendInput` returns 0 | `InjectionBlockedException`; controller cancels; UI toast |
| Synthetic pointer device init fails | Construction throws; controller falls back to mouse with logged warning |
| Hotkey hook install fails | App runs; Play disabled; orange "Esc-stop unavailable" badge in UI |
| Cancellation during stroke | Final `LEFTUP` emitted; state → Cancelling → Idle |
| Foreground window matches deny list | `InjectionBlockedException("…credential surface")`; cancel |

## 15. Risks

- **Big slice, lots of moving parts**: the user opted not to decompose. Mitigation: the writing-plans skill should break this into ≥ 10 small, individually committable tasks ordered Parser → Flattener → Transform → Planner → Mouse Injector → Pointer Injector → Hotkeys → Controller → Calibration UI → Countdown/Indicator → wiring. Each task ends with green tests and a commit; the demo lights up only on the last task.
- **Avalonia + `SetWindowsHookEx` lifetime**: low-level hooks need a message pump on the registering thread. Run on a dedicated thread with `Application.Run`-equivalent and marshal events out via a `ConcurrentQueue` or `SynchronizationContext.Post`.
- **DPI / virtual-desktop coords for `SendInput`**: ABSOLUTE flag uses normalized `(0..65535)` over the *virtual desktop*, not the primary monitor. Multi-monitor users will hit this immediately if we get it wrong. Test on a multi-monitor setup before declaring done.
- **Svg.Skia path-iteration ergonomics**: if its API turns out painful for our extraction needs, fall back to a hand-rolled `d=` parser (~400 LOC, well-bounded). Decide during implementation; flag in plan as a known fork point.
- **Synthetic pointer support range**: Win 10 1809 is our floor (matches MSIX manifest). Real-world deployment on 1809–1909 has been spotty for `InjectSyntheticPointerInput`; expect to fall back to mouse mode on those builds.

## 16. Acceptance criteria

The slice ships when:
1. The MainWindow stays on top of MS Paint when launched alongside it (and the `View → Always on top` toggle flips this).
2. Loading `tests/fixtures/cursive_signature.svg` shows correct strokes in the preview pane.
3. Clicking **Calibrate** dims the screen Snipping-Tool-style, lets the user drag a rectangle in MS Paint with a live `W × H` readout, and on release returns to the main window with the rectangle stored. Right-click during drag resets the selection. Esc cancels without setting a region. MainWindow restores from minimized.
4. Clicking **Play** shows the 3-second countdown, then visibly draws the strokes inside the Paint canvas using mouse input.
5. Pressing **Esc** during any state above interrupts cleanly with the cursor's left button released.
6. Switching the injection-mode combo to "SyntheticPointer" and replaying produces pen events visible in OneNote / Whiteboard (manual verification — no automated test).
7. CI is green; manual smoke checklist is documented in the implementation plan's final task.
