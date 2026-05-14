# SyntheticPen — Scaffold Design

**Date:** 2026-05-14
**Status:** Approved (pending user spec review)
**Scope:** Initial repository scaffold for the SyntheticPen Windows desktop app and its companion GitHub Pages website. Skeleton only — no runtime drawing yet.

## 1. Goals

1. Stand up a monorepo that builds end-to-end on a clean Windows machine and on GitHub Actions.
2. Establish module boundaries that match the architecture in `syntheticpen_claude_code_brief.md` so Phase 1 (SVG → cursor replay) can drop into the existing seams without restructuring.
3. Produce an MSIX-packageable Avalonia 11 app with placeholder Store identity, ready to be signed and submitted once a Partner Center publisher identity exists.
4. Stand up an Astro static site that publishes to GitHub Pages on push to `main`.

## 2. Non-Goals

- SVG parsing, motion planning, and input injection logic. All interfaces exist but throw `NotImplementedException`.
- Humanization, recording, scripting, AI stroke synthesis (brief Phase 4).
- Code signing infrastructure or Partner Center automation.
- Cross-platform support. Avalonia is chosen for future flexibility, but the only supported target for now is Windows 10 1809+.

## 3. Technology Decisions

| Concern | Choice | Reason |
| --- | --- | --- |
| App framework | Avalonia 11 + .NET 8 | User selection during brainstorming; cross-platform XAML retains future portability while still MSIX-publishable. |
| MVVM | CommunityToolkit.Mvvm | Source-generator-based, minimal ceremony, plays well with Avalonia. |
| DI | `Microsoft.Extensions.Hosting` | Standard generic host; gives logging + DI + config out of the box. |
| Solution file | `SyntheticPen.slnx` | User preference — modern XML solution format. |
| SVG (later) | Svg.Skia | Listed in brief; mature; Skia renderer compatible with Avalonia. |
| Site | Astro 5 + React islands | User selection; static, GitHub-Pages friendly. React islands let us reuse the high-fidelity JSX prototype from `design_handoff/` for the interactive signature/motion components. |
| CSS | Hand-written, scoped + global tokens | Design handoff specifies exact CSS tokens, gradients, and timing; a utility framework like Tailwind would add friction. Tokens live in `src/styles/global.css` as CSS variables. |
| Animation | Hand-rolled `requestAnimationFrame` (port from handoff) | Handoff already provides exact timing curves and `getPointAtLength` logic; introducing GSAP/Framer adds a dependency for no gain. Can swap later if needed. |
| Fonts | Google Fonts (self-hosted via `@fontsource`) | Space Grotesk, Inter, JetBrains Mono — exact families specified by handoff. Self-host for performance + privacy (no Google Fonts beacon, important for Store privacy story). |
| CI | GitHub Actions | Required for GitHub Pages deploy; also handles app build/test. |

## 4. Repository Layout

```
SyntheticPen/
├─ app/
│  ├─ SyntheticPen.slnx
│  ├─ Directory.Build.props          # central versions, nullable, langversion
│  ├─ Directory.Packages.props       # CPM (Central Package Management)
│  ├─ src/
│  │  ├─ SyntheticPen.Core/
│  │  ├─ SyntheticPen.Svg/
│  │  ├─ SyntheticPen.Motion/
│  │  ├─ SyntheticPen.Input/
│  │  ├─ SyntheticPen.Rendering/
│  │  ├─ SyntheticPen.App/           # Avalonia entry point
│  │  └─ SyntheticPen.Package/       # Windows Application Packaging (MSIX)
│  └─ tests/
│     ├─ SyntheticPen.Core.Tests/
│     ├─ SyntheticPen.Svg.Tests/
│     └─ SyntheticPen.Motion.Tests/
├─ site/                              # Astro 5 + React islands → GitHub Pages
│  ├─ astro.config.mjs
│  ├─ package.json
│  ├─ tsconfig.json
│  ├─ src/
│  │  ├─ pages/
│  │  │  ├─ index.astro              # the single-page marketing site
│  │  │  ├─ privacy.astro            # required for Store listing
│  │  │  ├─ 404.astro
│  │  │  └─ docs/[...slug].astro     # rendered from content collection
│  │  ├─ layouts/BaseLayout.astro
│  │  ├─ components/                 # React islands ported from design_handoff/
│  │  │  ├─ Header.tsx, Hero.tsx, SignatureCanvas.tsx
│  │  │  ├─ WhatItIs.tsx, HowItWorks.tsx, UseCases.tsx
│  │  │  ├─ Technology.tsx, MotionProfile.tsx, CTA.tsx, Footer.tsx
│  │  │  └─ icons/{LogoMark,CursorArrow,BackgroundSplines}.tsx
│  │  ├─ content/docs/*.md           # getting-started, faq, safety
│  │  └─ styles/global.css           # design tokens + base layer
│  └─ public/                        # favicon, og image
├─ design_handoff/                    # FROZEN reference — do not edit
│  ├─ README.md                       # the handoff document
│  ├─ index.html, app.jsx, components.jsx, sections.jsx, sections2.jsx
│  └─ syntheticpen_website_style_guide.md
├─ docs/
│  ├─ superpowers/specs/             # design docs (this file)
│  └─ design/                         # mockups + style guide
│     ├─ application_concept.png
│     ├─ website_mockup.png
│     └─ website_style_guide.md      # copy of top-level guide
├─ .github/workflows/
│  ├─ app-ci.yml
│  ├─ app-package.yml
│  └─ site-deploy.yml
├─ .editorconfig
├─ .gitignore
├─ LICENSE                            # placeholder MIT, user can change
├─ README.md
└─ syntheticpen_claude_code_brief.md  # existing
```

## 5. Module Boundaries

Each project compiles independently and only references projects below it in the list.

### 5.1 `SyntheticPen.Core`
Domain primitives and orchestration. No UI, no Win32.

Key types:
- `record struct PointF(double X, double Y)`
- `sealed class Stroke(IReadOnlyList<PointF> Points)`
- `enum PlaybackState { Idle, CountingDown, Playing, Paused, Stopping }`
- `interface IPlaybackController` — `Task PlayAsync(...)`, `Task StopAsync()`, `event Action<PlaybackState> StateChanged`.
- `class PlaybackController` — composes `ISvgPathLoader`, `IMotionPlanner`, `ICursorInjector`. Skeleton body only.

### 5.2 `SyntheticPen.Svg`
- `interface ISvgPathLoader { Task<IReadOnlyList<Stroke>> LoadAsync(Stream svg, CancellationToken ct); }`
- `class SkiaSvgPathLoader : ISvgPathLoader` — throws `NotImplementedException`.
- `static class BezierFlattener` — placeholder static class.

### 5.3 `SyntheticPen.Motion`
- `record PlanOptions(double SpeedMultiplier, bool Humanize)`
- `record TimedPoint(PointF Point, TimeSpan Offset)`
- `interface IMotionPlanner { IAsyncEnumerable<TimedPoint> Plan(IReadOnlyList<Stroke> strokes, PlanOptions opts, CancellationToken ct); }`
- `class DefaultMotionPlanner` — stub.

### 5.4 `SyntheticPen.Input`
- `enum InjectionMode { Mouse, SyntheticPointer, VirtualHid }`
- `interface ICursorInjector { Task MoveAsync(PointF p, CancellationToken ct); Task PenDownAsync(); Task PenUpAsync(); }`
- `class MouseSendInputInjector : ICursorInjector` — Win32 P/Invoke signatures declared but method bodies throw. Future `SyntheticPointerInjector` and `HidInjector` not added yet — interface is enough.

### 5.5 `SyntheticPen.Rendering`
- `interface IStrokePreviewRenderer` — produces an Avalonia `Geometry` from a list of strokes.
- `class StrokePreviewRenderer` — stub.

### 5.6 `SyntheticPen.App` (Avalonia)
- `App.axaml` + `Program.cs` (Avalonia + generic host)
- `MainWindow.axaml`:
  - Left: stroke preview surface (`Canvas` placeholder)
  - Right: controls panel (open file, speed slider, smoothing toggle, injection-mode combo, Start/Stop)
  - Top: menu (File ▸ Open SVG; Help ▸ About)
- `MainWindowViewModel : ObservableObject` with relay commands wired to `IPlaybackController` (all no-ops for now).
- DI registration in `Program.cs` (`AddSingleton<IPlaybackController, PlaybackController>()` etc.).
- Fluent theme.

### 5.7 `SyntheticPen.Package`
A Windows Application Packaging Project targeting Windows 10 1809+ (`10.0.17763.0`). `Package.appxmanifest` with:
- `Identity Name="SyntheticPen" Publisher="CN=SyntheticPen-Dev" Version="0.1.0.0"`
- `DisplayName="SyntheticPen"`, `PublisherDisplayName="SyntheticPen"`
- No restricted capabilities (input injection is in-process).
- Placeholder PNG icons (Square44, Square150, Wide310, StoreLogo) at correct sizes.

Note: WAP projects build on Windows MSBuild only. CI handles this with `windows-latest`. Developer can build the unpackaged Avalonia app on any platform if needed.

### 5.8 Tests
xUnit + FluentAssertions. Each test project pins its target project. Initial content: one passing smoke test per project (`true.Should().BeTrue()`) to prove CI wiring; real tests arrive with Phase 1.

## 6. Website

The website is a **single-page marketing site** built to the high-fidelity design handoff in `design_handoff/`. The handoff is treated as the source of truth for visual + interactive design — colors, type scale, spacing, copy, animation timing, and section structure are all specified there and must be matched.

### 6.1 Approach
- Astro 5 page shell + React islands (`@astrojs/react`) for the interactive components. Astro emits static HTML; React hydrates only the islands that animate.
- Port the JSX prototype in `design_handoff/` into TypeScript React components under `site/src/components/`. Replace `unpkg` Babel-in-browser with the real Astro/Vite build pipeline; replace `Object.assign(window, …)` exports with ES module exports.
- Hand-rolled `requestAnimationFrame` for the signature draw, use-case demos, motion-profile scanline, and tag-dot pulse — exact timings from the handoff (5400ms draw + 1600ms hold; 4200ms use-case loop; 4000ms velocity loop; 2s pulse).
- Self-host the three Google Fonts (`@fontsource/space-grotesk`, `@fontsource/inter`, `@fontsource/jetbrains-mono`) with only the specified weights.

### 6.2 Single page structure (`index.astro`)
Order matches handoff §Page Structure:
1. Sticky **Header** (blurred glass, nav links → smooth-scroll anchors).
2. **Hero** with live `SignatureCanvas` (cursive "Alistair Finch" animation, telemetry strip, dual CTAs).
3. **What It Is (01)** — definition + 4-cell spec strip.
4. **How It Works (02)** — 3 pipeline step cards, each with its own animation.
5. **Use Cases (03)** — left list switcher / right animated preview, 6 cases.
6. **Technology (04)** — spec table + animated S-curve velocity profile.
7. **CTA** — Download Beta panel.
8. **Footer**.

### 6.3 Design tokens (handoff §Design Tokens)
Implemented as CSS custom properties in `src/styles/global.css`:

```css
:root {
  --bg-0: #0A0A0A; --bg-1: #121212; --bg-2: #1A1A1A;
  --ink: #F5F5F5; --ink-dim: #8a8d92; --silver: #A6A6A6;
  --blue: #4DA3FF; --cyan: #6BE6FF;
  --grid: rgba(255,255,255,0.04);
  --grid-strong: rgba(77,163,255,0.08);
  --border: rgba(255,255,255,0.08);
  --border-strong: rgba(255,255,255,0.14);
}
```

Type scale, radii (2–4px max), shadow/glow values, and the dual-grid background layer are all defined in `global.css` exactly per handoff.

### 6.4 Other pages
- `privacy.astro` — required for Store listing. Content: "no telemetry, no network calls, what data is stored locally" — written to match the brief's safety constraints.
- `docs/[...slug].astro` — renders the `content/docs` collection (`getting-started.md`, `faq.md`, `safety.md`). Scaffold seeds them with `# Title` + one paragraph stubs.
- `404.astro` — minimal styled 404.

### 6.5 Build config
- `site: 'https://<github-user>.github.io'`, `base: '/SyntheticPen/'` — README flags this as a first-commit fill-in.
- Strict TypeScript.
- `astro check` runs in CI.
- Image optimization via Astro's built-in `<Image>` for any future raster assets.
- Lighthouse target: Performance ≥ 95, Accessibility ≥ 95.

### 6.6 Design assets in the repo
- `design_handoff/` — committed verbatim, treated as a frozen reference. CI should not depend on it; it's documentation.
- `docs/design/application_concept.png`, `website_mockup.png` — committed as reference imagery. Large PNGs (~6 MB each) — acceptable for a public repo but flagged as a one-time cost; future binary assets should consider Git LFS if they accumulate.

## 7. CI / CD

### 7.1 `app-ci.yml`
- Triggers: `push`, `pull_request` touching `app/**` or this workflow.
- Runs on `windows-latest`.
- Steps: checkout, setup-dotnet 8.x, `dotnet restore app/SyntheticPen.slnx`, `dotnet build -c Release --no-restore`, `dotnet test -c Release --no-build`.

### 7.2 `app-package.yml`
- Triggers: tag matching `v*`, plus `workflow_dispatch`.
- Runs on `windows-latest`.
- Steps: build the WAP project in Release, upload the resulting `.msix` (unsigned) as a workflow artifact. Signing is a manual follow-up step until a cert is provisioned.

### 7.3 `site-deploy.yml`
- Triggers: `push` to `main` touching `site/**` or this workflow; plus `workflow_dispatch`.
- Uses `actions/configure-pages`, `actions/upload-pages-artifact`, `actions/deploy-pages`.
- Steps: setup Node 20, `npm ci` in `site/`, `npm run build`, upload `site/dist`, deploy.
- Repo Pages source set to "GitHub Actions" (documented in README; one-time manual setting in repo settings).

## 8. Cross-Cutting

### 8.1 Coding standards
- `.editorconfig`: 4-space C#, 2-space everything else, UTF-8, LF, `dotnet_diagnostic.*` rules turned to warning for nullability and async.
- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`, deterministic builds.
- `Directory.Packages.props`: CPM enabled; package versions centralized.

### 8.2 README
Top-level README covers: what SyntheticPen is (one paragraph), repo layout, how to build the app, how to run the site locally, link to brief and design doc.

### 8.3 Safety scaffolding
Even though no real injection runs yet, the `IPlaybackController` skeleton reserves `event Action<PlaybackState> StateChanged` so the future "visible replay indicator" and "emergency stop hotkey" features (brief §Safety Features) have a clean attachment point.

## 9. Out of Scope / Deferred

- Phase 1 MVP implementation (separate spec).
- Code signing, Partner Center publisher identity, MSIX upload automation.
- Localization, theming beyond Fluent default.
- Crash reporting / telemetry (will need a separate privacy/consent design before introduction).
- **Full website fidelity is a stretch goal for the scaffold.** The scaffold's website target is: page shell + tokens + working Hero + Header + Footer islands + placeholder sections for 01–04 + CTA. The four remaining animated sections (signature with full path data, 3 step illustrations, 6 use-case demos, velocity profile) are pixel-perfect ports of the handoff and will be tracked under the scaffold's implementation plan but may slip to a follow-up. The spec sections (§6.2) define the *end state*, not the day-one PR.

## 10. Risks

- **Avalonia + MSIX**: Avalonia ships as a regular .NET app; packaging it via WAP is supported but less documented than WinUI 3. Mitigation: keep `SyntheticPen.App` runnable standalone (`dotnet run`); the WAP project is purely a packaging wrapper.
- **Solution file format**: `.slnx` requires recent `dotnet` SDK / VS 17.10+. Mitigation: document minimum SDK in README; CI uses `setup-dotnet@v4` pinned to 8.0.x latest, which supports it.
- **GitHub Pages base path**: site won't render correctly until `<github-user>` is filled into `astro.config.mjs`. Mitigation: README has a "first-time setup" section flagging this.
- **Handoff porting fidelity**: the design handoff is pixel-perfect; mis-ported animation timing or color values are easy to introduce and hard to spot. Mitigation: keep `design_handoff/` next to the live components in the repo so a side-by-side comparison is always one local-server tab away; lock the timing constants (5400, 1600, 4200, 4000, 2000 ms) into a single `src/lib/timing.ts` so they can't drift across components.
- **Repo size from PNG mockups**: two ~6 MB PNGs at the root inflate `git clone` time. Mitigation: move them under `docs/design/` (done in §4 layout) and accept the one-time cost; add Git LFS only if the design asset folder grows.
