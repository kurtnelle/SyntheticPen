# SyntheticPen Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take the scaffold from "builds and shows an empty window" to "load an SVG, calibrate a target region on screen, replay it as real Win32 cursor / pen input with a 3-second countdown and an Esc emergency stop."

**Architecture:** Pure-function pipeline (Svg parser → screen-space transform → motion planner → Win32 injector) orchestrated by `PlaybackController`. Avalonia UI minimized during Snip-style region select; always-on-top during playback. Global Esc hotkey via low-level keyboard hook in a separate assembly.

**Tech Stack:** .NET 10 (target), .NET 10 SDK, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4, Svg.Skia 2.0, xUnit 2.9, FluentAssertions 6.12, Win32 P/Invoke (`SendInput`, `InjectSyntheticPointerInput`, `SetWindowsHookEx`).

**Spec:** [docs/superpowers/specs/2026-05-14-syntheticpen-phase1-design.md](../specs/2026-05-14-syntheticpen-phase1-design.md)

---

## Conventions

- **Working directory** is the repo root (`I:\Source\repos\SyntheticPen`) throughout, unless a task says otherwise.
- **Shell** is PowerShell. Use `;` to chain in a single Bash invocation if needed.
- **Branch**: all Phase 1 work lands on a new branch `feat/phase-1` cut from `dev`. Final merge to `dev` is a manual user action after the smoke checklist passes.
- **Commit style**: conventional-commits prefix (`feat(svg):`, `test(motion):`, `chore:`). Co-author trailer not required.
- **TDD micro-cycle** is used for every pure-logic task: write failing test → run it red → write minimum code → run green → commit. UI-only tasks skip TDD where automated tests aren't sensible (called out per task).
- **Verification**: every task ends with a build + test pass and a single commit. If a task touches multiple projects, that's still one commit unless the task says otherwise.

---

## Task 0: Cut the feature branch

- [ ] **Step 1: Confirm starting state**

```pwsh
cd I:\Source\repos\SyntheticPen
git status                  # expect: clean
git branch --show-current   # expect: dev (or main; we'll start from dev)
git checkout dev
git pull
```

- [ ] **Step 2: Create and push the feature branch**

```pwsh
git checkout -b feat/phase-1
git push -u origin feat/phase-1
```

Expected: `feat/phase-1` exists locally and on origin.

---

## Task 1: Core types — Rect, InjectionMode, ITargetRegionProvider

**Files:**
- Create: `app/src/SyntheticPen.Core/Models/Rect.cs`
- Create: `app/src/SyntheticPen.Core/Playback/InjectionMode.cs`
- Create: `app/src/SyntheticPen.Core/Targeting/ITargetRegionProvider.cs`
- Create: `app/src/SyntheticPen.Core/Targeting/TargetRegionProvider.cs`
- Test: `app/tests/SyntheticPen.Core.Tests/Models/RectTests.cs`
- Test: `app/tests/SyntheticPen.Core.Tests/Targeting/TargetRegionProviderTests.cs`
- Delete: `app/src/SyntheticPen.Input/InjectionMode.cs` (moved to Core)

- [ ] **Step 1: Write the failing tests**

`app/tests/SyntheticPen.Core.Tests/Models/RectTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Core.Models;
using Xunit;

namespace SyntheticPen.Core.Tests.Models;

public class RectTests
{
    [Fact]
    public void IsEmpty_is_true_for_zero_or_negative_dimensions()
    {
        new Rect(0, 0, 0, 10).IsEmpty.Should().BeTrue();
        new Rect(0, 0, 10, 0).IsEmpty.Should().BeTrue();
        new Rect(0, 0, -1, 10).IsEmpty.Should().BeTrue();
        new Rect(0, 0, 10, 10).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Right_and_Bottom_compose_from_origin_and_size()
    {
        var r = new Rect(100, 50, 200, 30);
        r.Right.Should().Be(300);
        r.Bottom.Should().Be(80);
    }
}
```

`app/tests/SyntheticPen.Core.Tests/Targeting/TargetRegionProviderTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Targeting;
using Xunit;

namespace SyntheticPen.Core.Tests.Targeting;

public class TargetRegionProviderTests
{
    [Fact]
    public void Set_updates_Current_and_raises_Changed()
    {
        var p = new TargetRegionProvider();
        Rect? received = null;
        var count = 0;
        p.Changed += r => { received = r; count++; };

        var rect = new Rect(10, 20, 300, 200);
        p.Set(rect);

        p.Current.Should().Be(rect);
        received.Should().Be(rect);
        count.Should().Be(1);
    }

    [Fact]
    public void Set_to_null_clears_Current()
    {
        var p = new TargetRegionProvider();
        p.Set(new Rect(0, 0, 100, 100));
        p.Set(null);
        p.Current.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

```pwsh
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release
```

Expected: build FAIL (types don't exist).

- [ ] **Step 3: Write `Rect.cs`**

```csharp
namespace SyntheticPen.Core.Models;

public readonly record struct Rect(double X, double Y, double W, double H)
{
    public bool IsEmpty => W <= 0 || H <= 0;
    public double Right => X + W;
    public double Bottom => Y + H;
}
```

- [ ] **Step 4: Move InjectionMode from Input to Core.Playback**

Delete `app/src/SyntheticPen.Input/InjectionMode.cs`.

Create `app/src/SyntheticPen.Core/Playback/InjectionMode.cs`:

```csharp
namespace SyntheticPen.Core.Playback;

public enum InjectionMode
{
    Mouse,
    SyntheticPointer,
    VirtualHid
}
```

Add a project reference from `SyntheticPen.Input` to `SyntheticPen.Core` if not already present (it should be — the scaffold added it). Edit `app/src/SyntheticPen.Input/MouseSendInputInjector.cs` and add `using SyntheticPen.Core.Playback;` if it references `InjectionMode`. (Currently it doesn't, so no change needed in this task.)

Edit `app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs`: change the using from `using SyntheticPen.Input;` to `using SyntheticPen.Core.Playback;` next to the `InjectionMode` reference. (Keep the `using SyntheticPen.Input;` for `ICursorInjector` etc.)

- [ ] **Step 5: Write `ITargetRegionProvider.cs` and `TargetRegionProvider.cs`**

`app/src/SyntheticPen.Core/Targeting/ITargetRegionProvider.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Targeting;

public interface ITargetRegionProvider
{
    Rect? Current { get; }
    event Action<Rect?> Changed;
    void Set(Rect? region);
}
```

`app/src/SyntheticPen.Core/Targeting/TargetRegionProvider.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Targeting;

public sealed class TargetRegionProvider : ITargetRegionProvider
{
    private Rect? _current;

    public Rect? Current => _current;
    public event Action<Rect?>? Changed;

    public void Set(Rect? region)
    {
        _current = region;
        Changed?.Invoke(region);
    }
}
```

- [ ] **Step 6: Run tests green**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release --no-build
```

Expected: 5+ tests pass (2 new + 3 from scaffold smoke). 0 warnings, 0 errors.

- [ ] **Step 7: Commit**

```pwsh
git add app/
git commit -m "feat(core): add Rect, ITargetRegionProvider; move InjectionMode to Core"
```

---

## Task 2: StrokeTransform.FitToScreen

**Files:**
- Create: `app/src/SyntheticPen.Core/StrokeTransform.cs`
- Test: `app/tests/SyntheticPen.Core.Tests/StrokeTransformTests.cs`

- [ ] **Step 1: Write the failing tests**

`app/tests/SyntheticPen.Core.Tests/StrokeTransformTests.cs`:

```csharp
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

        // scale = min(400/100, 400/50) = 4; result is 400x200 centered → y offset = 100
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
```

- [ ] **Step 2: Run tests to verify failure**

```pwsh
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release
```

Expected: FAIL (class doesn't exist).

- [ ] **Step 3: Write `StrokeTransform.cs`**

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core;

public static class StrokeTransform
{
    public static IReadOnlyList<Stroke> FitToScreen(
        IReadOnlyList<Stroke> source,
        Rect sourceViewBox,
        Rect targetScreenRect,
        bool preserveAspectRatio = true)
    {
        if (source.Count == 0) return Array.Empty<Stroke>();

        double scaleX = targetScreenRect.W / sourceViewBox.W;
        double scaleY = targetScreenRect.H / sourceViewBox.H;
        double sx, sy, offsetX, offsetY;

        if (preserveAspectRatio)
        {
            double s = Math.Min(scaleX, scaleY);
            sx = sy = s;
            double fittedW = sourceViewBox.W * s;
            double fittedH = sourceViewBox.H * s;
            offsetX = targetScreenRect.X + (targetScreenRect.W - fittedW) / 2.0;
            offsetY = targetScreenRect.Y + (targetScreenRect.H - fittedH) / 2.0;
        }
        else
        {
            sx = scaleX;
            sy = scaleY;
            offsetX = targetScreenRect.X;
            offsetY = targetScreenRect.Y;
        }

        var result = new Stroke[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i].Points;
            var dst = new PointF[src.Count];
            for (int j = 0; j < src.Count; j++)
            {
                dst[j] = new PointF(
                    (src[j].X - sourceViewBox.X) * sx + offsetX,
                    (src[j].Y - sourceViewBox.Y) * sy + offsetY);
            }
            result[i] = new Stroke(dst);
        }
        return result;
    }
}
```

- [ ] **Step 4: Run green and commit**

```pwsh
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release
git add app/
git commit -m "feat(core): add StrokeTransform.FitToScreen with aspect-ratio + offset tests"
```

Expected: all Core.Tests pass.

---

## Task 3: BezierFlattener (cubic + quadratic)

**Files:**
- Modify: `app/src/SyntheticPen.Svg/BezierFlattener.cs` (replace stub body)
- Test: `app/tests/SyntheticPen.Svg.Tests/BezierFlattenerTests.cs`

- [ ] **Step 1: Write the failing tests**

`app/tests/SyntheticPen.Svg.Tests/BezierFlattenerTests.cs`:

```csharp
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

        // Each consecutive pair should be ≤ ~5x tolerance apart (sanity)
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
```

- [ ] **Step 2: Run tests to verify failure**

```pwsh
dotnet test app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj -c Release
```

Expected: FAIL (`FlattenCubic`/`FlattenQuadratic` throw `NotImplementedException`).

- [ ] **Step 3: Replace `BezierFlattener.cs` body**

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public static class BezierFlattener
{
    public static IReadOnlyList<PointF> FlattenCubic(PointF p0, PointF c1, PointF c2, PointF p3, double tolerance)
    {
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var result = new List<PointF> { p0 };
        SubdivideCubic(p0, c1, c2, p3, tolerance, result, depth: 0);
        result.Add(p3);
        return result;
    }

    public static IReadOnlyList<PointF> FlattenQuadratic(PointF p0, PointF c, PointF p1, double tolerance)
    {
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        // Promote to cubic: c1 = p0 + 2/3(c - p0), c2 = p1 + 2/3(c - p1)
        var c1 = new PointF(p0.X + 2.0 / 3.0 * (c.X - p0.X), p0.Y + 2.0 / 3.0 * (c.Y - p0.Y));
        var c2 = new PointF(p1.X + 2.0 / 3.0 * (c.X - p1.X), p1.Y + 2.0 / 3.0 * (c.Y - p1.Y));
        return FlattenCubic(p0, c1, c2, p1, tolerance);
    }

    private const int MaxDepth = 18;

    private static void SubdivideCubic(PointF p0, PointF c1, PointF c2, PointF p3, double tol, List<PointF> output, int depth)
    {
        // Flatness check: max perpendicular distance from chord p0..p3 of the two control points
        double d1 = PerpDistance(p0, p3, c1);
        double d2 = PerpDistance(p0, p3, c2);
        double maxD = Math.Max(d1, d2);

        if (maxD <= tol || depth >= MaxDepth) return;

        // De Casteljau subdivision at t=0.5
        var p01 = Mid(p0, c1);
        var p12 = Mid(c1, c2);
        var p23 = Mid(c2, p3);
        var p012 = Mid(p01, p12);
        var p123 = Mid(p12, p23);
        var p0123 = Mid(p012, p123);

        SubdivideCubic(p0, p01, p012, p0123, tol, output, depth + 1);
        output.Add(p0123);
        SubdivideCubic(p0123, p123, p23, p3, tol, output, depth + 1);
    }

    private static PointF Mid(PointF a, PointF b) => new((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);

    private static double PerpDistance(PointF lineA, PointF lineB, PointF p)
    {
        double dx = lineB.X - lineA.X;
        double dy = lineB.Y - lineA.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return Math.Sqrt((p.X - lineA.X) * (p.X - lineA.X) + (p.Y - lineA.Y) * (p.Y - lineA.Y));
        // |cross| / |line|
        double cross = Math.Abs(dx * (p.Y - lineA.Y) - dy * (p.X - lineA.X));
        return cross / len;
    }
}
```

- [ ] **Step 4: Run green and commit**

```pwsh
dotnet test app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj -c Release
git add app/
git commit -m "feat(svg): implement BezierFlattener (cubic + quadratic) with adaptive subdivision"
```

Expected: all tests pass.

---

## Task 4: SvgDocument, SvgParseException, FlattenOptions

**Files:**
- Create: `app/src/SyntheticPen.Svg/SvgDocument.cs`
- Create: `app/src/SyntheticPen.Svg/SvgParseException.cs`
- Create: `app/src/SyntheticPen.Svg/FlattenOptions.cs`
- Modify: `app/src/SyntheticPen.Svg/ISvgPathLoader.cs` (new signature)

No tests in this task — these are types consumed by Task 5. Tests of `ISvgPathLoader` live with the implementation.

- [ ] **Step 1: Write `SvgDocument.cs`**

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed record SvgDocument(IReadOnlyList<Stroke> Strokes, Rect SourceViewBox);
```

- [ ] **Step 2: Write `SvgParseException.cs`**

```csharp
namespace SyntheticPen.Svg;

public sealed class SvgParseException : Exception
{
    public SvgParseException(string message, int? lineNumber = null) : base(message)
    {
        LineNumber = lineNumber;
    }

    public SvgParseException(string message, Exception inner, int? lineNumber = null) : base(message, inner)
    {
        LineNumber = lineNumber;
    }

    public int? LineNumber { get; }
}
```

- [ ] **Step 3: Write `FlattenOptions.cs`**

```csharp
namespace SyntheticPen.Svg;

public sealed record FlattenOptions(double Tolerance = 0.25);
```

- [ ] **Step 4: Update `ISvgPathLoader.cs`**

```csharp
namespace SyntheticPen.Svg;

public interface ISvgPathLoader
{
    Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default);
}
```

- [ ] **Step 5: Update `SkiaSvgPathLoader.cs` stub signature (keep throwing for now)**

```csharp
namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in Task 5");
}
```

- [ ] **Step 6: Build green and commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
git add app/
git commit -m "feat(svg): add SvgDocument, SvgParseException, FlattenOptions; update loader signature"
```

---

## Task 5: SkiaSvgPathLoader + golden-file tests

**Files:**
- Modify: `app/src/SyntheticPen.Svg/SkiaSvgPathLoader.cs` (real implementation)
- Test: `app/tests/SyntheticPen.Svg.Tests/SkiaSvgPathLoaderTests.cs`
- Test fixtures: `app/tests/SyntheticPen.Svg.Tests/fixtures/{straight_line,square,cursive_signature,viewbox_offset,empty,malformed}.svg`
- Modify: `app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj` (copy fixtures to output)

- [ ] **Step 1: Create fixture SVGs**

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/straight_line.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><path d="M 10 50 L 90 50"/></svg>
```

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/square.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><path d="M 10 10 L 90 10 L 90 90 L 10 90 Z"/></svg>
```

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/cursive_signature.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 100">
  <path d="M 20 60 C 30 30, 70 30, 80 60 S 130 90, 140 60"/>
  <path d="M 50 40 L 60 40"/>
</svg>
```

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/viewbox_offset.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="50 25 100 50"><path d="M 50 25 L 150 25 L 150 75"/></svg>
```

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/empty.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"></svg>
```

Create `app/tests/SyntheticPen.Svg.Tests/fixtures/malformed.svg`:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><path d="M 10 10 L
```

- [ ] **Step 2: Configure test project to copy fixtures**

Edit `app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj`. Add inside an existing `<ItemGroup>` (or new):

```xml
<ItemGroup>
  <Content Include="fixtures\**\*.svg">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 3: Write the failing tests**

`app/tests/SyntheticPen.Svg.Tests/SkiaSvgPathLoaderTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Svg;
using Xunit;

namespace SyntheticPen.Svg.Tests;

public class SkiaSvgPathLoaderTests
{
    private static Stream Open(string name)
        => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    private readonly ISvgPathLoader _loader = new SkiaSvgPathLoader();
    private readonly FlattenOptions _opts = new(Tolerance: 0.25);

    [Fact]
    public async Task Straight_line_yields_one_stroke_with_two_points()
    {
        await using var s = Open("straight_line.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(1);
        doc.Strokes[0].Points.Should().HaveCount(2);
        doc.Strokes[0].Points[0].Should().Be(new PointF(10, 50));
        doc.Strokes[0].Points[1].Should().Be(new PointF(90, 50));
        doc.SourceViewBox.Should().Be(new Rect(0, 0, 100, 100));
    }

    [Fact]
    public async Task Square_yields_one_stroke_with_five_points()
    {
        await using var s = Open("square.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(1);
        doc.Strokes[0].Points.Should().HaveCount(5);     // M, L, L, L, Z back to start
        doc.Strokes[0].Points[0].Should().Be(new PointF(10, 10));
        doc.Strokes[0].Points[^1].Should().Be(new PointF(10, 10));
    }

    [Fact]
    public async Task Cursive_signature_yields_two_strokes()
    {
        await using var s = Open("cursive_signature.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(2);
        doc.Strokes[0].Points.Count.Should().BeGreaterThan(5);   // curve was flattened
    }

    [Fact]
    public async Task ViewBox_with_offset_is_reported()
    {
        await using var s = Open("viewbox_offset.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.SourceViewBox.Should().Be(new Rect(50, 25, 100, 50));
    }

    [Fact]
    public async Task Empty_svg_yields_zero_strokes()
    {
        await using var s = Open("empty.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().BeEmpty();
    }

    [Fact]
    public async Task Malformed_svg_throws_SvgParseException()
    {
        await using var s = Open("malformed.svg");
        var act = () => _loader.LoadAsync(s, _opts);
        await act.Should().ThrowAsync<SvgParseException>();
    }

    [Fact]
    public async Task Invalid_tolerance_throws()
    {
        await using var s = Open("straight_line.svg");
        var act = () => _loader.LoadAsync(s, new FlattenOptions(Tolerance: 0));
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 4: Run tests red**

```pwsh
dotnet test app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj -c Release
```

Expected: 7 new tests FAIL (loader not implemented).

- [ ] **Step 5: Implement SkiaSvgPathLoader**

Replace `app/src/SyntheticPen.Svg/SkiaSvgPathLoader.cs`:

```csharp
using System.Xml;
using SkiaSharp;
using Svg.Skia;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default)
    {
        if (opts.Tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(opts), "Tolerance must be > 0");

        return Task.Run<SvgDocument>(() =>
        {
            ct.ThrowIfCancellationRequested();

            // Buffer the stream so we can both parse XML (for viewBox + element iteration)
            // and feed Svg.Skia for path data parsing if we want it later.
            using var ms = new MemoryStream();
            svgStream.CopyTo(ms);
            ms.Position = 0;

            XmlDocument xml;
            try
            {
                xml = new XmlDocument { PreserveWhitespace = false };
                xml.Load(ms);
            }
            catch (XmlException ex)
            {
                throw new SvgParseException($"SVG XML is malformed: {ex.Message}", ex, ex.LineNumber);
            }

            var root = xml.DocumentElement;
            if (root == null || !string.Equals(root.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
                throw new SvgParseException("Document root is not <svg>.");

            var viewBox = ParseViewBox(root);

            var strokes = new List<Stroke>();
            CollectStrokes(root, Identity, strokes, opts.Tolerance, ct);

            // Fallback viewBox = bounding box of all points (used when SVG omits viewBox)
            if (viewBox is null)
                viewBox = StrokesBoundingBox(strokes) ?? new Rect(0, 0, 100, 100);

            return new SvgDocument(strokes, viewBox.Value);
        }, ct);
    }

    private static Rect? ParseViewBox(XmlElement root)
    {
        var attr = root.GetAttribute("viewBox");
        if (string.IsNullOrWhiteSpace(attr)) return null;
        var parts = attr.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) throw new SvgParseException($"Invalid viewBox: '{attr}'");
        var v = parts.Select(p => double.Parse(p, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return new Rect(v[0], v[1], v[2], v[3]);
    }

    private static readonly SKMatrix Identity = SKMatrix.CreateIdentity();

    private static void CollectStrokes(XmlElement element, SKMatrix transform, List<Stroke> strokes, double tolerance, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var localTransform = ApplyTransformAttr(transform, element.GetAttribute("transform"));

        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is not XmlElement child) continue;
            var name = child.LocalName.ToLowerInvariant();
            switch (name)
            {
                case "g":
                case "svg":
                    CollectStrokes(child, localTransform, strokes, tolerance, ct);
                    break;
                case "path":
                    AddPathStrokes(child.GetAttribute("d"), localTransform, strokes, tolerance);
                    break;
                case "line":
                    AddLine(child, localTransform, strokes);
                    break;
                case "polyline":
                case "polygon":
                    AddPoly(child, localTransform, strokes, closed: name == "polygon");
                    break;
            }
        }
    }

    private static SKMatrix ApplyTransformAttr(SKMatrix parent, string transformAttr)
    {
        if (string.IsNullOrWhiteSpace(transformAttr)) return parent;
        // Svg.Skia has a parser, but we keep this simple: support translate(x,y), scale(x[,y]), matrix(...)
        var m = parent;
        foreach (var t in TokenizeTransforms(transformAttr))
        {
            m = m.PreConcat(t);
        }
        return m;
    }

    private static IEnumerable<SKMatrix> TokenizeTransforms(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
            int nameStart = i;
            while (i < s.Length && (char.IsLetter(s[i]))) i++;
            if (i == nameStart) yield break;
            string name = s.Substring(nameStart, i - nameStart);
            while (i < s.Length && s[i] != '(') i++;
            if (i >= s.Length) yield break;
            i++; // skip '('
            int argsStart = i;
            while (i < s.Length && s[i] != ')') i++;
            string argsStr = s.Substring(argsStart, i - argsStart);
            if (i < s.Length) i++; // skip ')'
            var args = argsStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(a => float.Parse(a, System.Globalization.CultureInfo.InvariantCulture))
                              .ToArray();
            switch (name)
            {
                case "translate":
                    yield return SKMatrix.CreateTranslation(args[0], args.Length > 1 ? args[1] : 0);
                    break;
                case "scale":
                    yield return SKMatrix.CreateScale(args[0], args.Length > 1 ? args[1] : args[0]);
                    break;
                case "matrix":
                    yield return new SKMatrix(args[0], args[2], args[4], args[1], args[3], args[5], 0, 0, 1);
                    break;
                case "rotate":
                    yield return SKMatrix.CreateRotationDegrees(args[0],
                        args.Length > 1 ? args[1] : 0, args.Length > 2 ? args[2] : 0);
                    break;
            }
        }
    }

    private static void AddPathStrokes(string d, SKMatrix transform, List<Stroke> strokes, double tolerance)
    {
        if (string.IsNullOrWhiteSpace(d)) return;
        var skPath = SKPath.ParseSvgPathData(d);
        if (skPath is null) throw new SvgParseException($"Failed to parse path d='{d}'");

        var stroke = new List<PointF>();
        var iter = skPath.CreateIterator(forceClose: false);
        var pts = new SKPoint[4];
        SKPoint subpathStart = default;
        SKPoint last = default;
        SKPathVerb verb;

        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    if (stroke.Count > 0)
                    {
                        strokes.Add(new Stroke(stroke.ToArray()));
                        stroke.Clear();
                    }
                    last = pts[0];
                    subpathStart = pts[0];
                    stroke.Add(Transform(pts[0], transform));
                    break;
                case SKPathVerb.Line:
                    stroke.Add(Transform(pts[1], transform));
                    last = pts[1];
                    break;
                case SKPathVerb.Quad:
                {
                    var flat = BezierFlattener.FlattenQuadratic(
                        new PointF(pts[0].X, pts[0].Y),
                        new PointF(pts[1].X, pts[1].Y),
                        new PointF(pts[2].X, pts[2].Y),
                        tolerance);
                    for (int k = 1; k < flat.Count; k++) stroke.Add(Transform(flat[k], transform));
                    last = pts[2];
                    break;
                }
                case SKPathVerb.Cubic:
                {
                    var flat = BezierFlattener.FlattenCubic(
                        new PointF(pts[0].X, pts[0].Y),
                        new PointF(pts[1].X, pts[1].Y),
                        new PointF(pts[2].X, pts[2].Y),
                        new PointF(pts[3].X, pts[3].Y),
                        tolerance);
                    for (int k = 1; k < flat.Count; k++) stroke.Add(Transform(flat[k], transform));
                    last = pts[3];
                    break;
                }
                case SKPathVerb.Conic:
                {
                    // Convert conic to cubic approximation
                    var cubics = new SKPoint[10 * 3 + 1];
                    int count = SKPath.ConvertConicToQuads(pts[0], pts[1], pts[2],
                        iter.ConicWeight, cubics, pow2: 2);
                    for (int k = 0; k < count; k++)
                    {
                        int idx = k * 2;
                        var flat = BezierFlattener.FlattenQuadratic(
                            new PointF(cubics[idx].X, cubics[idx].Y),
                            new PointF(cubics[idx + 1].X, cubics[idx + 1].Y),
                            new PointF(cubics[idx + 2].X, cubics[idx + 2].Y),
                            tolerance);
                        for (int j = 1; j < flat.Count; j++) stroke.Add(Transform(flat[j], transform));
                    }
                    last = pts[2];
                    break;
                }
                case SKPathVerb.Close:
                    stroke.Add(Transform(subpathStart, transform));
                    last = subpathStart;
                    break;
            }
        }
        if (stroke.Count > 0) strokes.Add(new Stroke(stroke.ToArray()));
    }

    private static PointF Transform(SKPoint p, SKMatrix m)
    {
        var t = m.MapPoint(p);
        return new PointF(t.X, t.Y);
    }

    private static PointF Transform(PointF p, SKMatrix m)
        => Transform(new SKPoint((float)p.X, (float)p.Y), m);

    private static void AddLine(XmlElement el, SKMatrix t, List<Stroke> strokes)
    {
        double x1 = ParseAttr(el, "x1"), y1 = ParseAttr(el, "y1");
        double x2 = ParseAttr(el, "x2"), y2 = ParseAttr(el, "y2");
        strokes.Add(new Stroke(new[]
        {
            Transform(new PointF(x1, y1), t),
            Transform(new PointF(x2, y2), t)
        }));
    }

    private static void AddPoly(XmlElement el, SKMatrix t, List<Stroke> strokes, bool closed)
    {
        var attr = el.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(attr)) return;
        var nums = attr.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(p => double.Parse(p, System.Globalization.CultureInfo.InvariantCulture))
                       .ToArray();
        var pts = new List<PointF>();
        for (int i = 0; i + 1 < nums.Length; i += 2)
            pts.Add(Transform(new PointF(nums[i], nums[i + 1]), t));
        if (closed && pts.Count > 0) pts.Add(pts[0]);
        if (pts.Count >= 2) strokes.Add(new Stroke(pts.ToArray()));
    }

    private static double ParseAttr(XmlElement el, string name)
        => double.TryParse(el.GetAttribute(name), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.0;

    private static Rect? StrokesBoundingBox(IReadOnlyList<Stroke> strokes)
    {
        if (strokes.Count == 0) return null;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var s in strokes)
            foreach (var p in s.Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}
```

Add SkiaSharp to CPM if not already present. Edit `app/Directory.Packages.props`:

```xml
<PackageVersion Include="SkiaSharp" Version="3.116.1" />
```

Edit `app/src/SyntheticPen.Svg/SyntheticPen.Svg.csproj` to add the `<PackageReference Include="SkiaSharp" />` (versionless, CPM).

- [ ] **Step 6: Run green**

```pwsh
dotnet test app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj -c Release
```

Expected: all 7 new tests pass.

If `Cursive_signature_yields_two_strokes` fails because Svg.Skia parses the comma-separated `C 30 30, 70 30, 80 60` differently, simplify the fixture to use spaces only. Update the fixture and re-run.

- [ ] **Step 7: Commit**

```pwsh
git add app/
git commit -m "feat(svg): real SkiaSvgPathLoader with viewBox, transforms, line/poly/path support"
```

---

## Task 6: DefaultMotionPlanner

**Files:**
- Modify: `app/src/SyntheticPen.Motion/PlanOptions.cs`
- Modify: `app/src/SyntheticPen.Motion/TimedPoint.cs`
- Modify: `app/src/SyntheticPen.Motion/IMotionPlanner.cs`
- Modify: `app/src/SyntheticPen.Motion/DefaultMotionPlanner.cs`
- Test: `app/tests/SyntheticPen.Motion.Tests/DefaultMotionPlannerTests.cs`

- [ ] **Step 1: Update `TimedPoint.cs` to include PenDown**

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public readonly record struct TimedPoint(PointF Point, TimeSpan Offset, bool PenDown);
```

- [ ] **Step 2: Update `PlanOptions.cs`**

```csharp
namespace SyntheticPen.Motion;

public sealed record PlanOptions(
    double SpeedMultiplier = 1.0,
    double SampleHz = 200.0,
    double BaseVelocityPxPerSec = 600.0,
    double TravelSpeedFactor = 2.0);
```

- [ ] **Step 3: Update `IMotionPlanner.cs`** (signature unchanged)

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public interface IMotionPlanner
{
    IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> screenStrokes,
        PlanOptions opts,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Write failing tests**

`app/tests/SyntheticPen.Motion.Tests/DefaultMotionPlannerTests.cs`:

```csharp
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
```

- [ ] **Step 5: Run tests red**

```pwsh
dotnet test app/tests/SyntheticPen.Motion.Tests/SyntheticPen.Motion.Tests.csproj -c Release
```

Expected: FAIL (planner not implemented).

- [ ] **Step 6: Implement `DefaultMotionPlanner.cs`**

```csharp
using System.Runtime.CompilerServices;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public sealed class DefaultMotionPlanner : IMotionPlanner
{
    public async IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> screenStrokes,
        PlanOptions opts,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (screenStrokes.Count == 0) yield break;

        double velocity = opts.BaseVelocityPxPerSec * opts.SpeedMultiplier;
        double travelVelocity = velocity * opts.TravelSpeedFactor;
        TimeSpan offset = TimeSpan.Zero;

        for (int sIdx = 0; sIdx < screenStrokes.Count; sIdx++)
        {
            ct.ThrowIfCancellationRequested();

            var stroke = screenStrokes[sIdx].Points;
            if (stroke.Count < 2) continue;

            // Travel point at the start of every stroke after the first
            if (sIdx > 0)
            {
                var prevEnd = screenStrokes[sIdx - 1].Points[^1];
                double travelDist = Distance(prevEnd, stroke[0]);
                offset += TimeSpan.FromSeconds(travelDist / travelVelocity);
                yield return new TimedPoint(stroke[0], offset, PenDown: false);
            }

            // Stroke arc-length table
            var (cum, total) = BuildLengthTable(stroke);
            double T = total / velocity;
            int N = Math.Max(2, (int)Math.Ceiling(T * opts.SampleHz) + 1);
            var strokeStart = offset;

            for (int i = 0; i < N; i++)
            {
                ct.ThrowIfCancellationRequested();
                double u = (double)i / (N - 1);
                double s = Ease(u);
                var pt = PointAtArcLength(stroke, cum, s * total);
                var localOffset = TimeSpan.FromSeconds(u * T);
                yield return new TimedPoint(pt, strokeStart + localOffset, PenDown: true);
            }

            offset = strokeStart + TimeSpan.FromSeconds(T);
        }
    }

    private static double Distance(PointF a, PointF b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static (double[] cum, double total) BuildLengthTable(IReadOnlyList<PointF> pts)
    {
        var cum = new double[pts.Count];
        cum[0] = 0;
        for (int i = 1; i < pts.Count; i++)
            cum[i] = cum[i - 1] + Distance(pts[i - 1], pts[i]);
        return (cum, cum[^1]);
    }

    private static PointF PointAtArcLength(IReadOnlyList<PointF> pts, double[] cum, double dist)
    {
        if (dist <= 0) return pts[0];
        if (dist >= cum[^1]) return pts[^1];
        int lo = 0, hi = cum.Length - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (cum[mid] <= dist) lo = mid; else hi = mid;
        }
        double segLen = cum[hi] - cum[lo];
        double t = segLen < 1e-9 ? 0 : (dist - cum[lo]) / segLen;
        return new PointF(
            pts[lo].X + (pts[hi].X - pts[lo].X) * t,
            pts[lo].Y + (pts[hi].Y - pts[lo].Y) * t);
    }

    private static double Ease(double u)
    {
        if (u < 0.5) return 4 * u * u * u;
        double f = -2 * u + 2;
        return 1 - f * f * f / 2.0;
    }
}
```

- [ ] **Step 7: Run green and commit**

```pwsh
dotnet test app/tests/SyntheticPen.Motion.Tests/SyntheticPen.Motion.Tests.csproj -c Release
git add app/
git commit -m "feat(motion): DefaultMotionPlanner with eased per-stroke sampling and travel points"
```

Expected: 5 motion tests pass.

---

## Task 7: MouseSendInputInjector

**Files:**
- Modify: `app/src/SyntheticPen.Input/MouseSendInputInjector.cs`
- Create: `app/src/SyntheticPen.Input/InjectionBlockedException.cs`
- Create: `app/src/SyntheticPen.Input/Win32/SendInputNative.cs`
- Create: `app/src/SyntheticPen.Input/Win32/ForegroundClassName.cs`

No automated tests — this is OS-level P/Invoke. Manual smoke after Task 10 wiring.

- [ ] **Step 1: Write `InjectionBlockedException.cs`**

```csharp
namespace SyntheticPen.Input;

public sealed class InjectionBlockedException : Exception
{
    public InjectionBlockedException(string reason) : base(reason) { }
}
```

- [ ] **Step 2: Write Win32 native helpers**

`app/src/SyntheticPen.Input/Win32/SendInputNative.cs`:

```csharp
using System.Runtime.InteropServices;

namespace SyntheticPen.Input.Win32;

internal static class SendInputNative
{
    public const uint INPUT_MOUSE = 0;
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);
}
```

`app/src/SyntheticPen.Input/Win32/ForegroundClassName.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Text;

namespace SyntheticPen.Input.Win32;

internal static class ForegroundClassName
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private static readonly HashSet<string> DenyList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Credential Dialog Xaml Host",
        "LockScreen",
        "LogonUI",
        "ConsentUI",
        "UACBlackScreen"
    };

    public static bool IsDenied(out string? matched)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) { matched = null; return false; }
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        var name = sb.ToString();
        if (DenyList.Contains(name)) { matched = name; return true; }
        matched = null;
        return false;
    }
}
```

- [ ] **Step 3: Replace `MouseSendInputInjector.cs`**

```csharp
using SyntheticPen.Core.Models;
using SyntheticPen.Input.Win32;
using static SyntheticPen.Input.Win32.SendInputNative;

namespace SyntheticPen.Input;

public sealed class MouseSendInputInjector : ICursorInjector
{
    public Task MoveAsync(PointF screenPoint, CancellationToken ct = default)
    {
        EnforceDenyList();
        SendMouse((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y),
            MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK);
        return Task.CompletedTask;
    }

    public Task PenDownAsync(CancellationToken ct = default)
    {
        EnforceDenyList();
        Send(MOUSEEVENTF_LEFTDOWN);
        return Task.CompletedTask;
    }

    public Task PenUpAsync(CancellationToken ct = default)
    {
        // Pen up always allowed even if foreground became a credential surface mid-stroke —
        // we want to RELEASE the button, not leave it stuck.
        Send(MOUSEEVENTF_LEFTUP);
        return Task.CompletedTask;
    }

    private static void EnforceDenyList()
    {
        if (ForegroundClassName.IsDenied(out var name))
            throw new InjectionBlockedException($"foreground window class '{name}' is on the deny list");
    }

    private static void SendMouse(int physX, int physY, uint flags)
    {
        int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        int nx = (int)Math.Round((physX - vx) * 65535.0 / Math.Max(1, vw - 1));
        int ny = (int)Math.Round((physY - vy) * 65535.0 / Math.Max(1, vh - 1));

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dx = nx, dy = ny, dwFlags = flags } }
        };
        var sent = SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        if (sent == 0)
            throw new InjectionBlockedException("SendInput returned 0 (UIPI / integrity?)");
    }

    private static void Send(uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
        };
        var sent = SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        if (sent == 0)
            throw new InjectionBlockedException("SendInput returned 0 (UIPI / integrity?)");
    }
}
```

`using System.Runtime.InteropServices;` is needed at the top of `MouseSendInputInjector.cs` for `Marshal`.

- [ ] **Step 4: Build green and commit**

```pwsh
dotnet build app/src/SyntheticPen.Input/SyntheticPen.Input.csproj -c Release
git add app/
git commit -m "feat(input): real MouseSendInputInjector with virtual-desktop normalization and deny list"
```

---

## Task 8: SyntheticPointerInjector

**Files:**
- Create: `app/src/SyntheticPen.Input/SyntheticPointerInjector.cs`
- Create: `app/src/SyntheticPen.Input/Win32/SyntheticPointerNative.cs`

No automated tests.

- [ ] **Step 1: Write `SyntheticPointerNative.cs`**

```csharp
using System.Runtime.InteropServices;

namespace SyntheticPen.Input.Win32;

internal static class SyntheticPointerNative
{
    public const int POINTER_INPUT_TYPE_PEN = 0x00000003;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [Flags]
    public enum POINTER_FLAGS : uint
    {
        NONE = 0,
        NEW = 0x00000001,
        INRANGE = 0x00000002,
        INCONTACT = 0x00000004,
        FIRSTBUTTON = 0x00000010,
        DOWN = 0x00010000,
        UPDATE = 0x00020000,
        UP = 0x00040000,
        CAPTURECHANGED = 0x00200000,
    }

    [Flags]
    public enum POINTER_PEN_FLAGS : uint
    {
        NONE = 0,
        BARREL = 0x00000001,
        INVERTED = 0x00000002,
        ERASER = 0x00000004,
    }

    [Flags]
    public enum POINTER_PEN_MASK : uint
    {
        NONE = 0,
        PRESSURE = 0x00000001,
        ROTATION = 0x00000002,
        TILT_X = 0x00000004,
        TILT_Y = 0x00000008,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_INFO
    {
        public uint pointerType;
        public uint pointerId;
        public uint frameId;
        public POINTER_FLAGS pointerFlags;
        public IntPtr sourceDevice;
        public IntPtr hwndTarget;
        public POINT ptPixelLocation;
        public POINT ptHimetricLocation;
        public POINT ptPixelLocationRaw;
        public POINT ptHimetricLocationRaw;
        public uint dwTime;
        public uint historyCount;
        public int inputData;
        public uint dwKeyStates;
        public ulong PerformanceCount;
        public int ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_PEN_INFO
    {
        public POINTER_INFO pointerInfo;
        public POINTER_PEN_FLAGS penFlags;
        public POINTER_PEN_MASK penMask;
        public uint pressure;
        public uint rotation;
        public int tiltX;
        public int tiltY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_TYPE_INFO
    {
        public uint type;
        public POINTER_PEN_INFO penInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CreateSyntheticPointerDevice(uint pointerType, uint maxCount, uint mode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InjectSyntheticPointerInput(IntPtr device, ref POINTER_TYPE_INFO pointerInfo, uint count);

    [DllImport("user32.dll")]
    public static extern void DestroySyntheticPointerDevice(IntPtr device);
}
```

- [ ] **Step 2: Write `SyntheticPointerInjector.cs`**

```csharp
using SyntheticPen.Core.Models;
using SyntheticPen.Input.Win32;
using static SyntheticPen.Input.Win32.SyntheticPointerNative;

namespace SyntheticPen.Input;

public sealed class SyntheticPointerInjector : ICursorInjector, IDisposable
{
    private readonly IntPtr _device;
    private bool _disposed;

    public SyntheticPointerInjector()
    {
        // POINTER_FEEDBACK_DEFAULT = 1
        _device = CreateSyntheticPointerDevice((uint)POINTER_INPUT_TYPE_PEN, 1, 1);
        if (_device == IntPtr.Zero)
            throw new InjectionBlockedException("CreateSyntheticPointerDevice failed (Windows 10 1809+ required)");
    }

    public Task MoveAsync(PointF p, CancellationToken ct = default) => Inject(p, drag: true, down: false, up: false);
    public Task PenDownAsync(CancellationToken ct = default) => Inject(_lastPoint, drag: false, down: true, up: false);
    public Task PenUpAsync(CancellationToken ct = default) => Inject(_lastPoint, drag: false, down: false, up: true);

    private PointF _lastPoint;
    private bool _contact;

    private Task Inject(PointF p, bool drag, bool down, bool up)
    {
        if (ForegroundClassName.IsDenied(out var name) && !up)
            throw new InjectionBlockedException($"foreground window class '{name}' is on the deny list");

        _lastPoint = p;
        if (down) _contact = true;

        POINTER_FLAGS flags = POINTER_FLAGS.INRANGE;
        if (_contact) flags |= POINTER_FLAGS.INCONTACT | POINTER_FLAGS.FIRSTBUTTON;
        if (down) flags |= POINTER_FLAGS.DOWN | POINTER_FLAGS.NEW;
        else if (up) flags |= POINTER_FLAGS.UP;
        else if (drag) flags |= POINTER_FLAGS.UPDATE;

        var info = new POINTER_TYPE_INFO
        {
            type = POINTER_INPUT_TYPE_PEN,
            penInfo = new POINTER_PEN_INFO
            {
                pointerInfo = new POINTER_INFO
                {
                    pointerType = POINTER_INPUT_TYPE_PEN,
                    pointerId = 1,
                    pointerFlags = flags,
                    ptPixelLocation = new POINT { X = (int)Math.Round(p.X), Y = (int)Math.Round(p.Y) }
                },
                penMask = POINTER_PEN_MASK.PRESSURE,
                pressure = _contact ? 512u : 0u   // 512 of 1024 = mid pressure; constant for Phase 1
            }
        };

        if (!InjectSyntheticPointerInput(_device, ref info, 1))
            throw new InjectionBlockedException("InjectSyntheticPointerInput failed");

        if (up) _contact = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroySyntheticPointerDevice(_device);
    }
}
```

- [ ] **Step 3: Build green and commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
git add app/
git commit -m "feat(input): SyntheticPointerInjector via InjectSyntheticPointerInput (Win10 1809+)"
```

---

## Task 9: SyntheticPen.Hotkeys project + GlobalHotkeyService

**Files:**
- Create: `app/src/SyntheticPen.Hotkeys/SyntheticPen.Hotkeys.csproj`
- Create: `app/src/SyntheticPen.Hotkeys/IGlobalHotkeyService.cs`
- Create: `app/src/SyntheticPen.Hotkeys/GlobalHotkeyService.cs`
- Modify: `app/SyntheticPen.slnx` (add project)

No automated tests in CI (Esc hook needs a real window/message pump). Integration test added in a later phase.

- [ ] **Step 1: Create the project**

```pwsh
dotnet new classlib -n SyntheticPen.Hotkeys -o app/src/SyntheticPen.Hotkeys -f net10.0
Remove-Item app/src/SyntheticPen.Hotkeys/Class1.cs
dotnet add app/src/SyntheticPen.Hotkeys reference app/src/SyntheticPen.Core
dotnet sln app/SyntheticPen.slnx add app/src/SyntheticPen.Hotkeys/SyntheticPen.Hotkeys.csproj
```

- [ ] **Step 2: Write `IGlobalHotkeyService.cs`**

```csharp
namespace SyntheticPen.Hotkeys;

public interface IGlobalHotkeyService : IDisposable
{
    bool IsInstalled { get; }
    event Action? EmergencyStopRequested;
    void Install();
}
```

- [ ] **Step 3: Write `GlobalHotkeyService.cs`**

```csharp
using System.Runtime.InteropServices;

namespace SyntheticPen.Hotkeys;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_ESCAPE = 0x1B;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private readonly ManualResetEventSlim _installed = new(false);

    public bool IsInstalled => _hookId != IntPtr.Zero;
    public event Action? EmergencyStopRequested;

    public void Install()
    {
        if (IsInstalled) return;

        _hookThread = new Thread(HookThreadMain) { IsBackground = true, Name = "SyntheticPen.Hotkeys" };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();

        if (!_installed.Wait(TimeSpan.FromSeconds(2)))
            throw new InvalidOperationException("Hotkey hook failed to install within 2 seconds.");
    }

    private void HookThreadMain()
    {
        _hookThreadId = GetCurrentThreadId();
        _proc = HookCallback;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        if (_hookId == IntPtr.Zero)
        {
            _installed.Set();
            return;
        }
        _installed.Set();

        // Standard low-level hook message pump
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == VK_ESCAPE)
            {
                EmergencyStopRequested?.Invoke();
                // Don't swallow — let other apps see Esc too. Return CallNextHookEx.
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        if (_hookThreadId != 0)
        {
            PostThreadMessage(_hookThreadId, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
            _hookThread?.Join(TimeSpan.FromSeconds(1));
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
        public uint time; public int pt_x; public int pt_y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpmsg);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
```

- [ ] **Step 4: Build green and commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
git add app/
git commit -m "feat(hotkeys): add SyntheticPen.Hotkeys with global Esc hook via SetWindowsHookEx"
```

---

## Task 10: PlaybackController

**Files:**
- Modify: `app/src/SyntheticPen.Core/Playback/PlaybackState.cs` (replace Stopping/Paused with Cancelling)
- Modify: `app/src/SyntheticPen.Core/Playback/IPlaybackController.cs`
- Create: `app/src/SyntheticPen.Core/Playback/PlaybackOptions.cs`
- Replace: `app/src/SyntheticPen.Core/Playback/PlaybackController.cs`
- Test: `app/tests/SyntheticPen.Core.Tests/PlaybackControllerTests.cs`

This is the orchestrator. We test via a `FakeInjector` so we don't drive the real cursor.

- [ ] **Step 1: Replace `PlaybackState.cs`**

```csharp
namespace SyntheticPen.Core.Playback;

public enum PlaybackState
{
    Idle,
    CountingDown,
    Playing,
    Cancelling
}
```

- [ ] **Step 2: Create `PlaybackOptions.cs`**

```csharp
namespace SyntheticPen.Core.Playback;

public sealed record PlaybackOptions(
    double SpeedMultiplier = 1.0,
    InjectionMode Mode = InjectionMode.Mouse,
    TimeSpan Countdown = default,
    double SampleHz = 200.0,
    bool WaitForFocusRelease = true);
```

- [ ] **Step 3: Replace `IPlaybackController.cs`**

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface IPlaybackController
{
    PlaybackState State { get; }
    event Action<PlaybackState> StateChanged;
    event Action<TimeSpan> CountdownTick;
    Task PlayAsync(IReadOnlyList<Stroke> screenStrokes, PlaybackOptions opts, CancellationToken ct = default);
    void RequestStop();
}
```

- [ ] **Step 4: Write failing tests**

The Core test project doesn't reference Motion or Input today. We add references and a `FakeCursorInjector` test double. Edit `app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj` to add:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\SyntheticPen.Motion\SyntheticPen.Motion.csproj" />
  <ProjectReference Include="..\..\src\SyntheticPen.Input\SyntheticPen.Input.csproj" />
</ItemGroup>
```

`app/tests/SyntheticPen.Core.Tests/PlaybackControllerTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input;
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
    public async Task PlayAsync_emits_DOWN_then_moves_then_UP_for_single_stroke()
    {
        var injector = new FakeInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        var strokes = new[] { S((0, 0), (100, 0)) };

        await ctrl.PlayAsync(strokes, new PlaybackOptions(SampleHz: 100, Countdown: TimeSpan.Zero));

        injector.Events.First().Should().Be("DOWN");
        injector.Events.Last().Should().Be("UP");
        injector.Events.Count(e => e.StartsWith("M(")).Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task RequestStop_during_play_transitions_to_Idle_with_final_UP()
    {
        var injector = new FakeInjector();
        var ctrl = new PlaybackController(injector, new DefaultMotionPlanner());
        // Long stroke so we have time to cancel
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
```

- [ ] **Step 5: Run tests red**

```pwsh
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release
```

Expected: FAIL (new controller types don't compile yet).

- [ ] **Step 6: Replace `PlaybackController.cs`**

The Core project must reference Motion and Input to compose them. Add project references:

```pwsh
dotnet add app/src/SyntheticPen.Core reference app/src/SyntheticPen.Motion app/src/SyntheticPen.Input
```

Wait — Core was supposed to stay platform-agnostic. Reconsider: `PlaybackController` consumes `IMotionPlanner` and `ICursorInjector`, both interfaces. It does NOT depend on the concrete `MouseSendInputInjector`. So the references are *only to the contract types*. That's still a layering concern. Two options:

A. Keep the controller in Core and reference Motion + Input (as above).
B. Move the controller and `IPlaybackController` into a new `SyntheticPen.Playback` project that references all three.

Pick A for Phase 1 — fewer projects, accept that Core now has knowledge of injector + planner interfaces. Document the choice in the spec's future-work section.

Now write the controller:

```csharp
using SyntheticPen.Core.Models;
using SyntheticPen.Input;
using SyntheticPen.Motion;

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
        catch (OperationCanceledException) { /* stop normally */ }
        catch (InjectionBlockedException) { /* swallow, state goes to Idle below */ }
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
            // Wait until the scheduled offset
            var due = start + p.Offset;
            var delay = due - DateTime.UtcNow;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);

            // Edge transitions
            if (p.PenDown && !penDown) { await _injector.PenDownAsync(ct); penDown = true; }
            else if (!p.PenDown && penDown) { await _injector.PenUpAsync(ct); penDown = false; }

            await _injector.MoveAsync(p.Point, ct);
        }

        if (penDown) { await _injector.PenUpAsync(ct); }
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
```

- [ ] **Step 7: Run green and commit**

```pwsh
dotnet test app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj -c Release
git add app/
git commit -m "feat(core): real PlaybackController with countdown, edge-detected pen up/down, cancellation"
```

Expected: 4 new tests pass.

---

## Task 11: MainWindow always-on-top + View menu toggle

**Files:**
- Modify: `app/src/SyntheticPen.App/MainWindow.axaml`
- Modify: `app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Update XAML**

Replace `MainWindow.axaml` (full file):

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:SyntheticPen.App.ViewModels"
        x:Class="SyntheticPen.App.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="SyntheticPen"
        Width="1100" Height="720"
        MinWidth="900" MinHeight="600"
        Topmost="{Binding IsAlwaysOnTop}">
    <Grid RowDefinitions="Auto,*">
        <Menu Grid.Row="0">
            <MenuItem Header="_File">
                <MenuItem Header="_Open SVG..." Command="{Binding OpenSvgCommand}" />
                <Separator/>
                <MenuItem Header="E_xit" Command="{Binding ExitCommand}" />
            </MenuItem>
            <MenuItem Header="_View">
                <MenuItem Header="Always on _top"
                          ToggleType="CheckBox"
                          IsChecked="{Binding IsAlwaysOnTop}" />
            </MenuItem>
            <MenuItem Header="_Help">
                <MenuItem Header="_About" Command="{Binding AboutCommand}" />
            </MenuItem>
        </Menu>
        <Grid Grid.Row="1" ColumnDefinitions="*,360">
            <Border Grid.Column="0" Background="#101418" Padding="16">
                <TextBlock Text="Stroke preview"
                           Foreground="#A6A6A6"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"/>
            </Border>
            <StackPanel Grid.Column="1" Margin="16" Spacing="12">
                <TextBlock Text="Playback" FontWeight="SemiBold"/>
                <TextBlock Text="{Binding StateText}" Foreground="#4DA3FF"/>
                <Separator/>
                <TextBlock Text="File"/>
                <TextBlock Text="{Binding SvgFileLabel}" Foreground="#A6A6A6"/>
                <TextBlock Text="Target region" Margin="0,8,0,0"/>
                <TextBlock Text="{Binding TargetRegionLabel}" Foreground="#A6A6A6"/>
                <Button Content="Calibrate target..." Command="{Binding CalibrateCommand}"/>
                <Separator/>
                <TextBlock Text="Speed"/>
                <Slider Minimum="0.25" Maximum="4.0" Value="{Binding SpeedMultiplier}"/>
                <CheckBox Content="Humanize" IsChecked="{Binding Humanize}" IsEnabled="False"
                          ToolTip.Tip="Phase 2"/>
                <TextBlock Text="Injection mode" Margin="0,8,0,0"/>
                <ComboBox ItemsSource="{Binding InjectionModes}"
                          SelectedItem="{Binding SelectedInjectionMode}"/>
                <Button Content="Start" Command="{Binding StartCommand}"/>
                <Button Content="Stop"  Command="{Binding StopCommand}"/>
            </StackPanel>
        </Grid>
    </Grid>
</Window>
```

- [ ] **Step 2: Update the ViewModel**

`app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyntheticPen.Core.Playback;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;

    public MainWindowViewModel(IPlaybackController playback)
    {
        _playback = playback;
        _playback.StateChanged += s => StateText = s.ToString();
        StateText = _playback.State.ToString();
    }

    [ObservableProperty] private string _stateText = string.Empty;
    [ObservableProperty] private double _speedMultiplier = 1.0;
    [ObservableProperty] private bool _humanize;
    [ObservableProperty] private InjectionMode _selectedInjectionMode = InjectionMode.Mouse;
    [ObservableProperty] private bool _isAlwaysOnTop = true;
    [ObservableProperty] private string _svgFileLabel = "(no file)";
    [ObservableProperty] private string _targetRegionLabel = "(not set)";

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

    [RelayCommand] private Task OpenSvgAsync() => Task.CompletedTask;
    [RelayCommand] private void Exit() { /* implemented in Task 15 */ }
    [RelayCommand] private void About() { /* implemented in Task 16 */ }
    [RelayCommand] private Task CalibrateAsync() => Task.CompletedTask; // Task 12
    [RelayCommand] private Task StartAsync() => Task.CompletedTask;     // Task 15
    [RelayCommand] private Task StopAsync() => Task.CompletedTask;      // Task 15
}
```

- [ ] **Step 3: Build, smoke, commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
dotnet run --project app/src/SyntheticPen.App
```

Manual: confirm the window opens on top of other windows, and that toggling **View → Always on top** flips it. Close.

```pwsh
git add app/
git commit -m "feat(app): MainWindow Topmost binding + View menu toggle + new VM properties"
```

---

## Task 12: CalibrationOverlay (Snip-style)

**Files:**
- Create: `app/src/SyntheticPen.App/Views/CalibrationOverlay.axaml`
- Create: `app/src/SyntheticPen.App/Views/CalibrationOverlay.axaml.cs`
- Modify: `app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs` (wire CalibrateAsync)
- Modify: `app/src/SyntheticPen.App/Program.cs` (register ITargetRegionProvider)

- [ ] **Step 1: Write `CalibrationOverlay.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="SyntheticPen.App.Views.CalibrationOverlay"
        SystemDecorations="None"
        WindowStartupLocation="Manual"
        Topmost="True"
        ShowInTaskbar="False"
        Background="Transparent"
        TransparencyLevelHint="Transparent"
        CanResize="False"
        UseLayoutRounding="True">
    <Canvas Name="Root" Background="#80000000">
        <!-- Dim is the Canvas Background. The selected rect is "cut out" by drawing a black rect with PorterDuff DstOut — Avalonia lacks DstOut so we paint dim AROUND the selection via four rects in code-behind. -->
        <Rectangle Name="SelectionRect"
                   Stroke="#4DA3FF" StrokeThickness="1"
                   Fill="Transparent"
                   IsVisible="False"/>
        <Border Name="ReadoutChip"
                Background="#0A0A0AAA"
                BorderBrush="#4DA3FF"
                BorderThickness="1"
                Padding="6,3"
                IsVisible="False">
            <StackPanel>
                <TextBlock Name="DimLabel" Foreground="#6BE6FF" FontFamily="Consolas" FontSize="13"/>
                <TextBlock Name="OriginLabel" Foreground="#A6A6A6" FontFamily="Consolas" FontSize="11"/>
            </StackPanel>
        </Border>
        <TextBlock Name="Instructions"
                   Text="Drag to select target region · Esc to cancel · Right-click to restart"
                   Foreground="#F5F5F5"
                   FontFamily="Consolas" FontSize="13"
                   Canvas.Left="20" Canvas.Top="20"/>
    </Canvas>
</Window>
```

- [ ] **Step 2: Write `CalibrationOverlay.axaml.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SyntheticPen.Core.Models;

namespace SyntheticPen.App.Views;

public partial class CalibrationOverlay : Window
{
    private Point? _dragStart;
    private Rectangle _selRect = null!;
    private Border _readout = null!;
    private TextBlock _dimLabel = null!;
    private TextBlock _originLabel = null!;
    private Canvas _root = null!;

    public Rect? SelectedRect { get; private set; }

    public CalibrationOverlay()
    {
        InitializeComponent();
        _root = this.FindControl<Canvas>("Root")!;
        _selRect = this.FindControl<Rectangle>("SelectionRect")!;
        _readout = this.FindControl<Border>("ReadoutChip")!;
        _dimLabel = this.FindControl<TextBlock>("DimLabel")!;
        _originLabel = this.FindControl<TextBlock>("OriginLabel")!;

        // Span the entire virtual desktop
        var screens = Screens;
        var bounds = ComputeVirtualBounds(screens);
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private static PixelRect ComputeVirtualBounds(IScreens screens)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var s in screens.All)
        {
            var b = s.Bounds;
            if (b.X < minX) minX = b.X;
            if (b.Y < minY) minY = b.Y;
            if (b.X + b.Width > maxX) maxX = b.X + b.Width;
            if (b.Y + b.Height > maxY) maxY = b.Y + b.Height;
        }
        return new PixelRect(minX, minY, maxX - minX, maxY - minY);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsRightButtonPressed)
        {
            ClearSelection();
            return;
        }
        _dragStart = e.GetPosition(this);
        _selRect.IsVisible = true;
        _readout.IsVisible = true;
        UpdateRect(e.GetPosition(this));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is null) return;
        UpdateRect(e.GetPosition(this));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragStart is null) return;
        var rect = NormalizeRect(_dragStart.Value, e.GetPosition(this));
        _dragStart = null;
        if (rect.Width >= 4 && rect.Height >= 4)
        {
            // Convert window-local to virtual-desktop pixels
            var topLeft = Position;
            SelectedRect = new Rect(
                topLeft.X + rect.X,
                topLeft.Y + rect.Y,
                rect.Width,
                rect.Height);
            Close();
        }
        else
        {
            ClearSelection();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRect = null;
            Close();
        }
    }

    private void UpdateRect(Point current)
    {
        var rect = NormalizeRect(_dragStart!.Value, current);
        Canvas.SetLeft(_selRect, rect.X);
        Canvas.SetTop(_selRect, rect.Y);
        _selRect.Width = rect.Width;
        _selRect.Height = rect.Height;

        _dimLabel.Text = $"{(int)rect.Width} × {(int)rect.Height}";
        _originLabel.Text = $"({(int)rect.X}, {(int)rect.Y})";
        Canvas.SetLeft(_readout, rect.X + rect.Width + 8);
        Canvas.SetTop(_readout, rect.Y + rect.Height + 8);
    }

    private void ClearSelection()
    {
        _dragStart = null;
        _selRect.IsVisible = false;
        _readout.IsVisible = false;
    }

    private static Avalonia.Rect NormalizeRect(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(a.X - b.X);
        var h = Math.Abs(a.Y - b.Y);
        return new Avalonia.Rect(x, y, w, h);
    }
}
```

Note: The "dim cutout" effect — the spec describes punching the selection through the dim. The simplest cross-platform implementation is to paint dim *around* the selection (four rectangles). For Phase 1 we keep the simpler "dim background + bright selection border" form and document the cutout as a Phase 2 polish item. The selection rect's `Fill="Transparent"` already lets the screen show through where the rect is drawn.

- [ ] **Step 3: Register `ITargetRegionProvider` in `Program.cs`**

Edit `app/src/SyntheticPen.App/Program.cs`. Add `using SyntheticPen.Core.Targeting;` at top and add the registration in the DI block:

```csharp
host.Services.AddSingleton<ITargetRegionProvider, TargetRegionProvider>();
```

(Place it after the `MainWindowViewModel` registration but before `Services = host.Build().Services;`. Order doesn't matter functionally; group with other singletons.)

- [ ] **Step 4: Wire `CalibrateAsync` in the VM**

Replace `CalibrateAsync` in `MainWindowViewModel.cs`:

```csharp
private readonly ITargetRegionProvider _regions = Program.Services.GetRequiredService<ITargetRegionProvider>();

[RelayCommand]
private async Task CalibrateAsync()
{
    var owner = (Avalonia.Application.Current?.ApplicationLifetime
        as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    if (owner is null) return;

    owner.WindowState = Avalonia.Controls.WindowState.Minimized;
    var overlay = new Views.CalibrationOverlay();
    await overlay.ShowDialog(owner);
    owner.WindowState = Avalonia.Controls.WindowState.Normal;
    owner.Activate();

    if (overlay.SelectedRect is { } r)
    {
        _regions.Set(r);
        TargetRegionLabel = $"{(int)r.W}×{(int)r.H} at ({(int)r.X},{(int)r.Y})";
    }
}
```

You'll need these usings at the top of `MainWindowViewModel.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.Views;
using SyntheticPen.Core.Targeting;
```

- [ ] **Step 5: Build, smoke, commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
dotnet run --project app/src/SyntheticPen.App
```

Manual: open the app, click **Calibrate**, drag a rectangle, release. Confirm: main window minimized during drag, restored after, label shows the rectangle dimensions.

```pwsh
git add app/
git commit -m "feat(app): Snip-style calibration overlay with virtual-desktop span and Esc cancel"
```

---

## Task 13: Countdown overlay

**Files:**
- Create: `app/src/SyntheticPen.App/Views/CountdownOverlay.axaml`
- Create: `app/src/SyntheticPen.App/Views/CountdownOverlay.axaml.cs`

- [ ] **Step 1: Write `CountdownOverlay.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="SyntheticPen.App.Views.CountdownOverlay"
        Title="SyntheticPen — Countdown"
        SystemDecorations="None"
        WindowStartupLocation="CenterScreen"
        Width="320" Height="220"
        Topmost="True"
        ShowInTaskbar="False"
        Background="#0A0A0A"
        CanResize="False">
    <Border Background="#0A0A0A" BorderBrush="#4DA3FF" BorderThickness="1">
        <TextBlock Name="Number"
                   Text="3"
                   FontSize="160"
                   FontWeight="Bold"
                   Foreground="#4DA3FF"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"/>
    </Border>
</Window>
```

- [ ] **Step 2: Write `CountdownOverlay.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace SyntheticPen.App.Views;

public partial class CountdownOverlay : Window
{
    private TextBlock _number = null!;

    public CountdownOverlay()
    {
        InitializeComponent();
        _number = this.FindControl<TextBlock>("Number")!;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void SetRemaining(TimeSpan remaining)
    {
        var secs = (int)Math.Ceiling(remaining.TotalSeconds);
        Dispatcher.UIThread.Post(() => _number.Text = secs <= 0 ? "GO" : secs.ToString());
    }
}
```

- [ ] **Step 3: Build and commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
git add app/
git commit -m "feat(app): add CountdownOverlay window for pre-play 3-2-1 indicator"
```

---

## Task 14: PLOTTING indicator

**Files:**
- Create: `app/src/SyntheticPen.App/Views/PlottingIndicator.axaml`
- Create: `app/src/SyntheticPen.App/Views/PlottingIndicator.axaml.cs`

- [ ] **Step 1: Write `PlottingIndicator.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="SyntheticPen.App.Views.PlottingIndicator"
        SystemDecorations="None"
        WindowStartupLocation="Manual"
        Width="220" Height="40"
        Topmost="True"
        ShowInTaskbar="False"
        Background="Transparent"
        TransparencyLevelHint="Transparent"
        CanResize="False">
    <Border Background="#CC0A0A0A" BorderBrush="#4DA3FF" BorderThickness="1" CornerRadius="2">
        <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="12,0">
            <Ellipse Width="8" Height="8" Fill="#6BE6FF" Margin="0,0,10,0"/>
            <TextBlock Text="PLOTTING"
                       Foreground="#F5F5F5"
                       FontFamily="Consolas"
                       FontSize="12"
                       Letterspacing="2"
                       VerticalAlignment="Center"/>
        </StackPanel>
    </Border>
</Window>
```

- [ ] **Step 2: Write `PlottingIndicator.axaml.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SyntheticPen.App.Views;

public partial class PlottingIndicator : Window
{
    public PlottingIndicator()
    {
        InitializeComponent();
        var primary = Screens.Primary;
        if (primary is not null)
        {
            var area = primary.WorkingArea;
            Position = new PixelPoint(area.X + area.Width - 240, area.Y + 20);
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 3: Build and commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
git add app/
git commit -m "feat(app): add PlottingIndicator always-on-top status pill"
```

---

## Task 15: App-level wiring — SVG load, preview, Play/Stop, hotkey integration

**Files:**
- Modify: `app/src/SyntheticPen.App/Program.cs` — register hotkey service, injectors, planner, controller
- Modify: `app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs` — full implementation
- Modify: `app/src/SyntheticPen.App/MainWindow.axaml` — preview Path binding
- Create: `app/src/SyntheticPen.Rendering/StrokePreviewRenderer.cs` — real impl

- [ ] **Step 1: Implement `StrokePreviewRenderer.cs`**

```csharp
using Avalonia;
using Avalonia.Media;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public sealed class StrokePreviewRenderer : IStrokePreviewRenderer
{
    public object BuildGeometry(IReadOnlyList<Stroke> strokes)
    {
        var geo = new PathGeometry();
        foreach (var s in strokes)
        {
            if (s.Points.Count < 2) continue;
            var fig = new PathFigure
            {
                StartPoint = new Point(s.Points[0].X, s.Points[0].Y),
                IsClosed = false,
                IsFilled = false
            };
            for (int i = 1; i < s.Points.Count; i++)
                fig.Segments.Add(new LineSegment { Point = new Point(s.Points[i].X, s.Points[i].Y) });
            geo.Figures.Add(fig);
        }
        return geo;
    }
}
```

Edit `app/src/SyntheticPen.Rendering/SyntheticPen.Rendering.csproj` to reference Avalonia (versionless CPM):

```xml
<ItemGroup>
  <PackageReference Include="Avalonia" />
</ItemGroup>
```

- [ ] **Step 2: Update `Program.cs`**

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyntheticPen.App.ViewModels;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Hotkeys;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;
using SyntheticPen.Svg;

namespace SyntheticPen.App;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args);
        host.Services.AddSingleton<ISvgPathLoader, SkiaSvgPathLoader>();
        host.Services.AddSingleton<IMotionPlanner, DefaultMotionPlanner>();
        host.Services.AddSingleton<IStrokePreviewRenderer, StrokePreviewRenderer>();
        host.Services.AddSingleton<ITargetRegionProvider, TargetRegionProvider>();
        host.Services.AddSingleton<MouseSendInputInjector>();
        host.Services.AddSingleton<InjectorFactory>();
        host.Services.AddSingleton<IPlaybackController>(sp =>
        {
            var factory = sp.GetRequiredService<InjectorFactory>();
            return new PlaybackController(factory.Create(InjectionMode.Mouse), sp.GetRequiredService<IMotionPlanner>());
        });
        host.Services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        host.Services.AddSingleton<MainWindowViewModel>();

        Services = host.Build().Services;
        Services.GetRequiredService<IGlobalHotkeyService>().Install();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

internal sealed class InjectorFactory
{
    public ICursorInjector Create(InjectionMode mode) => mode switch
    {
        InjectionMode.SyntheticPointer => new SyntheticPointerInjector(),
        _ => new MouseSendInputInjector()
    };
}
```

- [ ] **Step 3: Update `MainWindowViewModel.cs` — full implementation**

```csharp
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.Views;
using SyntheticPen.Core;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Hotkeys;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;
using SyntheticPen.Svg;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;
    private readonly ISvgPathLoader _loader;
    private readonly IStrokePreviewRenderer _previewRenderer;
    private readonly ITargetRegionProvider _regions;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly InjectorFactory _injectorFactory;
    private readonly IMotionPlanner _planner;

    private SvgDocument? _doc;
    private CountdownOverlay? _countdown;
    private PlottingIndicator? _indicator;

    public MainWindowViewModel(
        IPlaybackController playback,
        ISvgPathLoader loader,
        IStrokePreviewRenderer previewRenderer,
        ITargetRegionProvider regions,
        IGlobalHotkeyService hotkeys,
        InjectorFactory injectorFactory,
        IMotionPlanner planner)
    {
        _playback = playback;
        _loader = loader;
        _previewRenderer = previewRenderer;
        _regions = regions;
        _hotkeys = hotkeys;
        _injectorFactory = injectorFactory;
        _planner = planner;

        _playback.StateChanged += OnStateChanged;
        _playback.CountdownTick += OnCountdownTick;
        _hotkeys.EmergencyStopRequested += () => _playback.RequestStop();
        StateText = _playback.State.ToString();
    }

    [ObservableProperty] private string _stateText = string.Empty;
    [ObservableProperty] private double _speedMultiplier = 1.0;
    [ObservableProperty] private bool _humanize;
    [ObservableProperty] private InjectionMode _selectedInjectionMode = InjectionMode.Mouse;
    [ObservableProperty] private bool _isAlwaysOnTop = true;
    [ObservableProperty] private string _svgFileLabel = "(no file)";
    [ObservableProperty] private string _targetRegionLabel = "(not set)";
    [ObservableProperty] private Geometry? _previewGeometry;

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

    [RelayCommand]
    private async Task OpenSvgAsync()
    {
        var owner = MainWindow();
        if (owner is null) return;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open SVG",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SVG files") { Patterns = new[] { "*.svg" } }
            }
        });
        if (files.Count == 0) return;

        await using var s = await files[0].OpenReadAsync();
        _doc = await _loader.LoadAsync(s, new FlattenOptions(0.25));
        SvgFileLabel = files[0].Name;
        PreviewGeometry = (Geometry)_previewRenderer.BuildGeometry(_doc.Strokes);
    }

    [RelayCommand]
    private async Task CalibrateAsync()
    {
        var owner = MainWindow();
        if (owner is null) return;
        owner.WindowState = WindowState.Minimized;
        var overlay = new CalibrationOverlay();
        await overlay.ShowDialog(owner);
        owner.WindowState = WindowState.Normal;
        owner.Activate();
        if (overlay.SelectedRect is { } r)
        {
            _regions.Set(r);
            TargetRegionLabel = $"{(int)r.W}×{(int)r.H} at ({(int)r.X},{(int)r.Y})";
        }
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_doc is null || _regions.Current is null) return;

        var screenStrokes = StrokeTransform.FitToScreen(_doc.Strokes, _doc.SourceViewBox, _regions.Current.Value);

        // Rebuild controller with the injector for current mode (simpler than reflecting into Program's singleton)
        var injector = _injectorFactory.Create(SelectedInjectionMode);
        var ctrl = new PlaybackController(injector, _planner);
        ctrl.StateChanged += OnStateChanged;
        ctrl.CountdownTick += OnCountdownTick;

        try
        {
            await ctrl.PlayAsync(screenStrokes,
                new PlaybackOptions(
                    SpeedMultiplier: SpeedMultiplier,
                    Mode: SelectedInjectionMode,
                    Countdown: TimeSpan.FromSeconds(3)));
        }
        finally
        {
            if (injector is IDisposable d) d.Dispose();
        }
    }

    [RelayCommand]
    private void Stop() => _playback.RequestStop();

    [RelayCommand]
    private void Exit() => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    [RelayCommand]
    private void About() { /* implemented in Task 16 */ }

    private void OnStateChanged(PlaybackState s)
    {
        StateText = s.ToString();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (s == PlaybackState.CountingDown)
            {
                _countdown ??= new CountdownOverlay();
                _countdown.Show();
            }
            else if (s == PlaybackState.Playing)
            {
                _countdown?.Close(); _countdown = null;
                _indicator ??= new PlottingIndicator();
                _indicator.Show();
            }
            else // Idle, Cancelling end
            {
                _countdown?.Close(); _countdown = null;
                _indicator?.Close(); _indicator = null;
            }
        });
    }

    private void OnCountdownTick(TimeSpan remaining)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _countdown?.SetRemaining(remaining));
    }

    private static Window? MainWindow()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
```

- [ ] **Step 4: Bind preview in `MainWindow.axaml`**

Replace the preview Border block in the existing XAML:

```xml
<Border Grid.Column="0" Background="#101418" Padding="16">
    <Viewbox Stretch="Uniform" StretchDirection="Both">
        <Path Stroke="#4DA3FF" StrokeThickness="1.5"
              Data="{Binding PreviewGeometry}" />
    </Viewbox>
</Border>
```

- [ ] **Step 5: Build + smoke + commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
dotnet run --project app/src/SyntheticPen.App
```

Manual:
1. **File → Open SVG…** pick `app/tests/SyntheticPen.Svg.Tests/fixtures/cursive_signature.svg`. Confirm strokes appear in the preview pane.
2. **Calibrate**, drag a rectangle in a visible MS Paint canvas, release.
3. **Start**. Confirm: 3-2-1 countdown overlay, then "PLOTTING" pill appears top-right, cursor draws the strokes in Paint.
4. **Esc** during playback: confirm cursor releases (left button up) and indicator/countdown closes.
5. Switch combo to **SyntheticPointer**, open OneNote / Whiteboard, calibrate, play. Confirm pen marks (where supported).

```pwsh
git add app/
git commit -m "feat(app): wire SVG load → preview, target region, countdown/indicator, hotkey-driven stop"
```

---

## Task 16: About dialog

**Files:**
- Create: `app/src/SyntheticPen.App/Views/AboutDialog.axaml`
- Create: `app/src/SyntheticPen.App/Views/AboutDialog.axaml.cs`
- Modify: `MainWindowViewModel.cs` (wire About command)

- [ ] **Step 1: Write `AboutDialog.axaml`**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="SyntheticPen.App.Views.AboutDialog"
        Title="About SyntheticPen"
        Width="480" Height="320"
        CanResize="False"
        WindowStartupLocation="CenterOwner"
        Background="#0A0A0A">
    <StackPanel Margin="24" Spacing="14">
        <TextBlock Text="SyntheticPen" FontSize="24" FontWeight="SemiBold" Foreground="#F5F5F5"/>
        <TextBlock Text="A virtual pen plotter for Windows."
                   Foreground="#A6A6A6" FontSize="14"/>
        <Separator/>
        <TextBlock Text="Version 0.1.0 — Phase 1" Foreground="#A6A6A6" FontSize="12"/>
        <TextBlock Foreground="#6BE6FF" FontSize="12">
            <Run Text="github.com/kurtnelle/SyntheticPen"/>
        </TextBlock>
        <Separator/>
        <TextBlock TextWrapping="Wrap" Foreground="#A6A6A6" FontSize="12">
            Safety: press <Bold>Esc</Bold> at any time to cancel playback. SyntheticPen never collects or transmits data.
        </TextBlock>
        <Button Content="OK" HorizontalAlignment="Right" Click="OnClose"/>
    </StackPanel>
</Window>
```

- [ ] **Step 2: Write `AboutDialog.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SyntheticPen.App.Views;

public partial class AboutDialog : Window
{
    public AboutDialog() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 3: Wire About in VM**

Replace the `[RelayCommand] private void About()` line in `MainWindowViewModel.cs`:

```csharp
[RelayCommand]
private async Task AboutAsync()
{
    var owner = MainWindow();
    if (owner is null) return;
    await new Views.AboutDialog().ShowDialog(owner);
}
```

- [ ] **Step 4: Build, smoke, commit**

```pwsh
dotnet build app/SyntheticPen.slnx -c Release
dotnet run --project app/src/SyntheticPen.App
```

Manual: Help → About shows the dialog. Close.

```pwsh
git add app/
git commit -m "feat(app): About dialog with version, repo link, safety note"
```

---

## Task 17: Final smoke checklist + merge prep

No code. This task documents the manual smoke test, verifies CI, and prepares the PR back to `dev`.

- [ ] **Step 1: Full local sweep**

```pwsh
cd I:\Source\repos\SyntheticPen
dotnet test app/SyntheticPen.slnx -c Release
cd site; npm run build; cd ..
```

Expected: all unit tests pass (Core, Svg, Motion, plus the scaffold smokes). Site builds clean.

- [ ] **Step 2: Manual smoke test on Windows**

Run through the spec's §16 acceptance criteria one by one. For each, note pass/fail.

1. MainWindow stays on top of MS Paint when launched alongside it. Toggle off via View → Always on top.
2. Load `app/tests/SyntheticPen.Svg.Tests/fixtures/cursive_signature.svg`. Preview shows two strokes.
3. Click Calibrate, drag a rectangle in Paint. Esc cancels cleanly. Right-click resets. Mouse-up commits.
4. Click Start with a valid file + region. 3-2-1 countdown overlay appears. PLOTTING pill in top-right.
5. Cursor draws the strokes in Paint at SpeedMultiplier ≈ 1.0.
6. Esc during play: left button releases, indicator closes, state goes to Idle.
7. Switch mode to SyntheticPointer, open OneNote, calibrate over its canvas, play. Pen marks appear.

If any step fails, fix and re-test before merging.

- [ ] **Step 3: Push the branch + open PR**

```pwsh
git push
gh pr create --base dev --head feat/phase-1 `
  --title "Phase 1: SVG load → calibrate → playback" `
  --body "Implements docs/superpowers/specs/2026-05-14-syntheticpen-phase1-design.md. See plan: docs/superpowers/plans/2026-05-14-syntheticpen-phase1.md. Manual smoke checklist passed locally."
```

- [ ] **Step 4: Wait for CI**

```pwsh
gh pr checks --watch
```

Expected: `app-ci` green. (`site-deploy` not triggered by app-only changes.)

- [ ] **Step 5: Merge after review**

```pwsh
gh pr merge --merge   # or --squash to your preference
git checkout dev && git pull
```

Phase 1 complete.

---

## Post-Phase-1 follow-ups (for the human / next plan)

- Phase 2: persistence (recent files, saved target regions, profiles).
- Phase 2: humanization, jitter, variable pressure curves.
- Phase 2: integration test for `GlobalHotkeyService` in `SyntheticPen.Hotkeys.Tests` (excluded from CI by trait).
- Phase 2: cutout dim in `CalibrationOverlay` (four-rect compose instead of single dim background).
- Phase 3: virtual HID driver mode.
- Phase 4: G-code import (matches user's CNC instincts), Lua scripting, recording.
