# Handoff: SyntheticPen Marketing Site

## Overview
A single-page marketing site for **SyntheticPen** — Windows software that converts SVG paths (and other vector geometry) into synthetic cursor / pen motion. The product is positioned as "a virtual CNC plotter for human handwriting." The site sells the precision-engineering story with a dark, technical aesthetic, an animated signature centerpiece, and step-by-step explanations of the pipeline.

## About the Design Files
The HTML / JSX in this bundle is a **design reference**, not production code. It is a React-via-Babel prototype optimized for a single-file preview environment. The implementation task is to **recreate this design inside your target codebase** (Next.js / Astro / SvelteKit / plain Vite — whatever fits your stack), using your codebase's component conventions, build pipeline, font loading, and animation library. Where this prototype hand-rolls things with `requestAnimationFrame` and inline SVG, you may prefer Framer Motion, GSAP, or Lottie — the **visual + interactive outcome** is what matters, not the technique.

If the project has no frontend codebase yet, pick a static-site / SSG framework with first-class React support (Next.js App Router, Astro with React islands, or Remix) and recreate the design there.

## Fidelity
**High-fidelity.** Final colors, typography, spacing, motion timing, copy, and interactions are all defined. The developer should match this design pixel-perfectly. Substitute equivalents are only acceptable for animation primitives (use your library of choice as long as the timing/easing matches) and for fonts (use the same family names — they are all on Google Fonts).

---

## Design Tokens

### Colors
| Token | Value | Use |
|---|---|---|
| `--bg-0` | `#0A0A0A` | Page background (deep matte black) |
| `--bg-1` | `#121212` | Secondary dark surface (panel fill) |
| `--bg-2` | `#1A1A1A` | Tertiary panel surface (gradient top stop) |
| `--ink` | `#F5F5F5` | Primary text / White Ink |
| `--ink-dim` | `#8a8d92` | Secondary text |
| `--silver` | `#A6A6A6` | Outlines, separators, mono labels |
| `--blue` | `#4DA3FF` | **Electric Ink Blue** — active traces, accents |
| `--cyan` | `#6BE6FF` | **Precision Cyan** — glow highlights, animated paths |
| `--grid` | `rgba(255,255,255,0.04)` | Background grid lines (micro) |
| `--grid-strong` | `rgba(77,163,255,0.08)` | Background grid lines (macro / accent) |
| `--border` | `rgba(255,255,255,0.08)` | Panel borders |
| `--border-strong` | `rgba(255,255,255,0.14)` | Hovered / emphasized borders |

Primary gradient: `linear-gradient(135deg, #4DA3FF 0%, #6BE6FF 100%)`

### Typography
All from Google Fonts. Load weights: Space Grotesk 400/500/600/700, Inter 300/400/500/600, JetBrains Mono 400/500.

| Family | Use | CSS |
|---|---|---|
| **Space Grotesk** | Headings, display, button labels | `font-family: 'Space Grotesk', sans-serif;` |
| **Inter** | Body copy, paragraph text | `font-family: 'Inter', system-ui, sans-serif;` |
| **JetBrains Mono** | Eyebrows, code-style readouts, telemetry, captions | `font-family: 'JetBrains Mono', monospace;` |

**Scales**
- Display hero (`h1.display`): `clamp(48px, 7.5vw, 104px)` / weight 700 / line-height 0.95 / tracking -0.025em / **uppercase**
- Section title (`h2.section-title`): `clamp(32px, 4vw, 52px)` / weight 600 / line-height 1.05 / tracking -0.02em
- Subhead: 18px / weight 400 / color `--silver`
- Body: 13–16px / weight 400 / line-height 1.5–1.6 / color `--silver`
- Eyebrow (mono): 10–11px / uppercase / tracking 0.18–0.20em / color `--silver` or `--ink-dim`

### Spacing
- Section vertical padding: 80px top + 80px bottom (CTA section uses 100/100)
- Container max-width: **1280px**, horizontal padding 32px
- Card inner padding: 18–24px
- Grid gaps: 20–24px between cards; 1px (with `--border` background) for cell-divider grids

### Borders & Radii
- **Radius is minimal**: 2px on buttons and tags, 4px on panels. Avoid rounded blobs — this is engineering software, not consumer fluff.
- Panel border: `1px solid var(--border)`
- Hover border on ghost button: `1px solid var(--blue)`

### Shadows & Glow
- Primary button glow (rest): `0 0 0 1px rgba(107,230,255,0.4), 0 0 24px rgba(77,163,255,0.35), 0 0 60px rgba(77,163,255,0.15)`
- Primary button glow (hover): `0 0 0 1px rgba(107,230,255,0.7), 0 0 32px rgba(77,163,255,0.55), 0 0 90px rgba(77,163,255,0.3)`
- SVG-element glow effect: `filter: drop-shadow(0 0 6px rgba(107,230,255,0.6))` (for cursors, dots)
- Path glow: SVG `<filter id="inkGlow">` with two `feGaussianBlur` (stdDeviation 3.5 + 8) merged with `SourceGraphic`. See `components.jsx` for the exact filter.

### Site Background
Two stacked CSS grids on a fixed full-viewport layer:
```css
background-image:
  linear-gradient(to right, var(--grid) 1px, transparent 1px),
  linear-gradient(to bottom, var(--grid) 1px, transparent 1px);
background-size: 64px 64px;
mask-image: radial-gradient(ellipse 90% 70% at 50% 30%, #000 30%, transparent 100%);
```
Then a `::after` pseudo with a 320px stronger grid masked to the top center for the "engineering blueprint" feel.

---

## Page Structure

The page is a single scrollable column. Sections are stacked top-to-bottom:

1. **Header** (sticky)
2. **Hero** (with live signature)
3. **What It Is** (01) — definition + spec strip
4. **How It Works** (02) — 3-step pipeline cards
5. **Use Cases** (03) — interactive switcher + preview
6. **Technology** (04) — spec table + velocity profile chart
7. **CTA** — Download Beta panel
8. **Footer**

Every numbered section uses the same `SectionHeader` pattern: a 120px left column with mono section number + eyebrow, top border `1px solid var(--border)`, title and body in the right column.

---

## Screens / Components

### 1. Header (sticky)
- **Layout**: full-width sticky bar, `backdrop-filter: blur(12px)`, `background: rgba(10,10,10,0.6)`, bottom border `1px solid var(--border)`.
- **Inner**: max-width 1280px, padding 18px 32px, flex space-between.
- **Left**: Logo mark (22px pen-nib SVG, color `--blue`, `drop-shadow(0 0 6px rgba(77,163,255,0.4))`) + wordmark "Synthetic" + "Pen" (Pen dimmed to `--ink-dim`, weight 400). Space Grotesk 17px / weight 600.
- **Right nav links**: JetBrains Mono 11px / uppercase / tracking 0.15em / color `--silver`. Items: "What it is", "How it works", "Use cases", "Technology". Hover → `--ink`.
- **CTA nav link** "Download": same type, color `--blue`, 1px border `rgba(77,163,255,0.3)`, padding 7px 14px, radius 2px. Hover → fill `rgba(77,163,255,0.08)`, border `--blue`.
- **Behavior**: Each link smooth-scrolls to its section by id.

### 2. Hero
- **Container**: padding-top 56px, padding-bottom 80px, overflow hidden, position relative.
- **Background**: a `<BackgroundSplines>` SVG layer (decorative bezier curves with control-point dots) at opacity 0.5, behind content.
- **Eyebrow tag**: centered pill — `<span class="tag">` containing a pulsing dot + text "v0.4 BETA · WINDOWS 10/11". Tag style: 11px JetBrains Mono / uppercase / tracking 0.15em / color `--blue` / background `rgba(77,163,255,0.08)` / 1px border `rgba(77,163,255,0.2)` / radius 2px / 6×6px dot with `box-shadow: 0 0 8px var(--blue)` and 2s pulse animation.
- **Headline**: `h1.display` text-align center, two lines: "VECTOR PATHS" / "INTO REAL MOTION". Uppercase.
- **Subhead**: max-width 640px, centered, top-margin 24px. Copy: *"Synthetic cursor & pen motion for Windows. SyntheticPen replays SVG paths as native input — like a CNC plotter for your handwriting."*
- **Signature canvas** (centerpiece): max-width 960px, top-margin 52px. See §**Signature Canvas** below.
- **Buttons row**: top-margin 44px, centered, gap 16px, wrap.
  - Primary: "DOWNLOAD BETA" + down-right arrow icon.
  - Ghost: "SEE HOW IT WORKS".
- **Trust strip**: top-margin 48px, centered, JetBrains Mono 11px uppercase tracking 0.12em / color `--ink-dim`, items separated by slash dim dividers: "Native Win32 input / ~0.1 px precision / SVG · DXF · TTF".

#### Signature Canvas (the live demo)
Animated SVG on a coordinate grid showing a cursor drawing a cursive signature.
- **viewBox**: `0 0 820 260`. SVG width 100%, height auto.
- **Backplate**: 740×220 rectangle at (40,20). Two pattern fills stacked: a 20px micro-grid (`rgba(77,163,255,0.08)` lines, stroke 0.5) and a 100px macro-grid (`rgba(77,163,255,0.18)` lines, stroke 0.6). Wrapped in a 0.6px white-6%-alpha outline.
- **Axes**: y axis at x=40, x axis at y=240. JetBrains Mono 9px tick labels in `rgba(166,166,166,0.55)`: "200" / "0" / "800". Tick marks every 100 units along the x axis.
- **Signature strokes**: 3 sub-paths defining the cursive word *Alistair Finch* (first name, t-crossbar, last name). Each path is rendered twice:
  - **Glow underlay**: `stroke="url(#inkGrad)"` (gradient #4DA3FF → #6BE6FF), `stroke-width="3.2"`, with `<filter id="inkGlow">` applied, opacity 0.9.
  - **Sharp top**: `stroke="#E8F4FF"`, `stroke-width="1.5"`.
  - Both share the same animated `stroke-dasharray` / `stroke-dashoffset` so the line appears to be drawn over time.
- **Live coordinate readout**: top-right of the canvas. A 116×44 box at (660, 34) filled `rgba(10,10,10,0.7)` with 0.6px `rgba(107,230,255,0.3)` border. Inside: label "CURSOR" (8px mono, dim grey) and live `X: ___` / `Y: ___` lines (11px JetBrains Mono, color `--cyan`), updated every frame.
- **Cursor arrow**: a small Windows-style pointer SVG rendered as inline SVG at the current draw position. White fill, 0.5px black stroke, with a cyan glow via `drop-shadow`.
- **Trail dot**: a 5px cyan circle at the cursor head (only when progress < 1) with `drop-shadow(0 0 12px #6BE6FF)`.
- **Bottom telemetry strip**: directly below the SVG, a 10px-14px padded row with 1px border `--border`, background `rgba(18,18,18,0.6)`, JetBrains Mono 11px. Items:
  - `● PLOTTING` (color `--blue`)
  - 2px-tall progress bar filled by `linear-gradient(90deg, #4DA3FF, #6BE6FF)` with cyan glow, width driven by progress
  - `XX.X%` numeric
  - `FEED 4800 mm/min` (dim)
  - `STROKES 3` (dim)

#### Signature Animation Logic
- Measure `getTotalLength()` on each stroke after mount.
- Animate a single `progress` value 0→1 over **5400ms** via an ease-in-out cubic, with a **1600ms hold** at the end before looping. Total cycle 7000ms.
- Compute `targetLen = progress * sumOfLengths`. Walk strokes cumulatively to find the current stroke + local offset.
- Update each stroke's `strokeDashoffset` so only the drawn portion is visible.
- For the cursor: `path.getPointAtLength(localLen)` on the current stroke gives `{x, y}` for the cursor arrow and the readout.

If recreating in Framer Motion / GSAP, the same math applies — just animate `progress` instead of hand-rolling `requestAnimationFrame`.

### 3. What It Is (Section 01)
- **SectionHeader**: number "01 / WHAT IT IS", eyebrow "DEFINITION", title *"A virtual pen plotter that lives inside Windows."*, body: *"SyntheticPen reads vector geometry — SVG paths, glyph outlines, hand-drawn signatures — and replays it as synthetic mouse and pen input. The system cursor becomes a plotter head, tracing your geometry with sub-pixel precision into any application that accepts input."*
- **Spec strip**: 4-column grid (auto-fit, minmax 280px), gap 1px on a `var(--border)` background to create hairline dividers between cells. Each cell:
  - Padding 28px 24px, bg `--bg-1`.
  - Top label (mono 10px, tracking 0.2em, color `--ink-dim`).
  - Big value (mono 18px, color `--blue`, weight 500).
  - Description (13px Inter, color `--silver`, line-height 1.5).
- **Cells**:
  - INPUT — `SVG · DXF · TTF · CSV` — Vector geometry from any source
  - OUTPUT — `SendInput · WM_POINTER` — Native Win32 cursor / pen events
  - PRECISION — `0.1 px @ 1000 Hz` — Sub-pixel interpolation
  - LATENCY — `< 2 ms` — From queue to dispatch

### 4. How It Works (Section 02)
- **SectionHeader**: "02 / HOW IT WORKS" / "PIPELINE" / title *"From vector to input in three stages."* / body about deterministic, scriptable, inspectable pipeline.
- **3 step cards**, auto-fit grid minmax(280px, 1fr), gap 20px. Each card:
  - Panel surface with header strip: 14×18px padding, bottom border `--border`. Header contains mono label "STEP 0X" (color `--blue`) on the left and a small mono code snippet on the right (color `--ink-dim`).
  - Illustration area: 18px padded, bg `rgba(10,10,10,0.4)`. Hosts an animated SVG specific to each step.
  - Footer area: 20–24px padded. Title (Space Grotesk 19px / weight 600 / `--ink`), body 13.5px Inter / color `--silver`.
- **Step 01 — SVG Path Parsing** — `commands = parsePath(svg)`. Illustration: a small "SVG file" card behind a "parsed" card showing the path being drawn with control-point dots appearing as progress crosses each.
- **Step 02 — Motion Planning** — `plan = jerkLimited(curve)`. Illustration: a coordinate axis with a Bezier path through 5 points, dashed handle lines, a vertical scanline plotter sweeping left-to-right with a glowing cyan plot head.
- **Step 03 — Synthetic Input** — `SendInput(plan.next)`. Illustration: an isometric-style perspective grid (skewed vertical + horizontal lines) with a curved path being traced; a cursor arrow follows the path.
- **Flow strip below** (margin-top 32px, centered, mono 11px / tracking 0.18em / color `--ink-dim`): `SVG PATH → MOTION PLANNER → SYNTHETIC INPUT` with arrows colored `--blue`.

### 5. Use Cases (Section 03)
- **SectionHeader**: "03 / USE CASES" / "APPLICATIONS" / *"Anywhere the OS accepts a pointer."*
- **Two-column layout** (380px / 1fr, gap 24px):
  - **Left**: vertical list of 6 cases, each a full-width clickable row inside a panel. Active row has: bg `rgba(77,163,255,0.06)`, 2px left border `--blue`. Inactive rows: transparent bg, 2px transparent left border, 1px bottom `--border` between rows. Each row shows: mono "0X" number (blue when active, dim when not), Space Grotesk 17px label, and a right-aligned arrow icon (opacity 0.3 inactive / 1 active).
  - **Right**: preview panel, min-height 460px. Header strip with mono label "PREVIEW · {CASE NAME}" and 3 colored dots (two grey + one cyan/glowing). Big SVG demo area showing the active case's vector geometry being plotted onto a faux target app (with `target_app.exe` text top-left and `feed 4800 mm/min · NN%` text bottom-left, both mono 9px in dim grey). Bottom strip: 18–22px padded description (14px Inter, color `--silver`).
- **Cases**:
  - Signatures — *Replay a stored signature into any form, PDF, or signing surface that accepts pen input.* Geometry: cursive signature path.
  - Presentations — *Annotate slides with pre-authored ink. Pre-scripted handwriting on Whiteboard, OneNote, Concepts.* Geometry: 4 horizontal underlines.
  - Accessibility — *Plot vector glyphs as handwriting for users who cannot hold a stylus. Voice → vector → motion.* Geometry: simple letter strokes.
  - SVG Replay — *Drop an .svg onto SyntheticPen and watch the pointer trace it across any canvas surface.* Geometry: smooth multi-S curve.
  - Annotation Automation — *Drive QA tooling and design reviews. Repeatable marks for screen capture, demos, and regression.* Geometry: a grid of straight lines.
  - Virtual CNC — *Pipe G-code through the planner. Visualize tool paths against any 2D canvas before machining.* Geometry: a zigzag tool-path.
- **Demo animation**: same `getPointAtLength` cursor-following technique as the hero, loop every 4200ms. Path drawn twice (glow + sharp), with a cyan plot dot + cursor arrow at the head.

### 6. Technology (Section 04)
- **SectionHeader**: "04 / TECHNOLOGY" / "UNDER THE HOOD" / *"Built like motion control hardware."*
- **Two-column layout** (1.1fr / 1fr, gap 24px):
  - **Left — Spec table** in a panel. Header strip: "SPECIFICATIONS" left, `v0.4.2-beta` right (both mono 10px, tracking 0.2em, color `--blue` for the version). 8 rows, each 14×20px padded, 130px key column + value column, hairline dividers.
    - Parser — `SVG 1.1 · DXF R12 · TTF glyph outlines · CSV stroke logs`
    - Resampler — `Adaptive arc-length, max chord error 0.05 px`
    - Planner — `Jerk-limited S-curve, look-ahead 64 nodes, 1 kHz`
    - Dispatcher — `SendInput · WM_POINTER · Wacom WinTab · UIA`
    - Sandbox — `Optional driver mode for kernel-level input (signed)`
    - Scripting — `JavaScript hooks, CLI, named pipe IPC`
    - Telemetry — `Per-stroke timing, coordinate trace, motion log`
    - Footprint — `14 MB installer · 38 MB working set`
  - **Right — Motion profile chart** (`<MotionProfile>`): a panel with header "VELOCITY PROFILE · S-CURVE" / "jerk-limited" (color `--blue`). Inside: a 360×240 SVG showing an animated S-curve velocity profile against a faint grid. Y axis labels `v` / `vmax` / `0`. X axis labels `t` and phase labels `accel`, `cruise`, `decel` below. Accel and decel regions get a subtle `rgba(77,163,255,0.05)` shaded background. A vertical dashed scanline + cyan dot + readout `v=0.XX` sweeps across the chart on a 4000ms loop.

  Curve definition (replicate exactly):
  - 0 ≤ t < 0.25: `v(t) = 0.5 * (1 - cos(π * (t/0.25)))` (smooth accel)
  - 0.25 ≤ t < 0.75: `v(t) = 1` (cruise)
  - 0.75 ≤ t ≤ 1: `v(t) = 0.5 * (1 + cos(π * ((t - 0.75)/0.25)))` (smooth decel)

### 7. CTA
- **Big panel** centered in container, padding 64×56px, slightly tinted border `rgba(77,163,255,0.2)`.
- Inside, layered behind content: a `radial-gradient(ellipse 60% 80% at 50% 100%, rgba(77,163,255,0.12), transparent 60%)` glow.
- **Tag**: "● BETA · WINDOWS 10/11 (X64)" (same tag style as hero, with pulsing dot).
- **Headline** (Space Grotesk uppercase, clamp 36–64px, weight 700, tracking -0.02em, max-width 760px): *"Bring your geometry into motion."*
- **Subhead** (max 520px, centered): *"Free during beta. No telemetry. Single signed binary."*
- **Buttons**: Primary "Download SyntheticPen" + mono badge "14 MB". Ghost "Read the docs".
- **Fine print** (mono 11px, tracking 0.15em, color `--ink-dim`, uppercase): `SHA256 · 4DA3FF6BE6FF · Signed · MIT-licensed core`.

### 8. Footer
- Top border `1px solid var(--border)`, padding 40×0 32×0.
- Flex space-between (wrap, gap 20px): logo on the left, copyright `© 2026 · BUILT FOR PRECISION` (mono 11px, tracking 0.15em, color `--ink-dim`) in the middle, and 4 nav links on the right: Docs, Changelog, GitHub, Contact (mono 11px, color `--silver`).

---

## Interactions & Behavior

| Element | Behavior |
|---|---|
| Nav links | Smooth-scroll to `#what` / `#how` / `#use` / `#tech` / `#cta` by id. |
| Section reveal on scroll | IntersectionObserver at threshold 0.15. On intersect, add `.in` class which transitions `opacity` 0→1 and `translateY(20px)→0` over 800ms with cubic-bezier(.2,.7,.2,1) easing. |
| Primary button hover | `transform: translateY(-1px) scale(1.02)` + stronger glow shadow. 250ms ease. |
| Ghost button hover | Border + text color shift to `--blue`, background `rgba(77,163,255,0.04)`. |
| Hero signature loop | 5400ms ease-in-out cubic draw + 1600ms hold, then snap back to start (no fade-out reverse). |
| How It Works cards | Each card has its own independent 3000ms looping animation. |
| Use Cases switcher | Click row → set `active` index. Right preview swaps geometry and restarts its 4200ms animation. The path-length measurement reruns when geometry changes. |
| Velocity profile | 4000ms loop. Scanline + readout track current `t`. |
| Tag dot | 2s ease-in-out pulse, opacity 1 ↔ 0.3. |
| Hero ink gradient | The two gradient stops cross-animate hue between `#4DA3FF` and `#6BE6FF` on a 4s loop, giving a subtle shimmer. |

---

## State Management
The prototype is self-contained. Required state per component:
- `Hero / SignatureCanvas`: `progress: number`, `coords: {x, y}`, `lengths: number[]`. Drive via `requestAnimationFrame` or your animation library.
- `HowItWorks` illustrations: each owns a `t: number` 0..1 loop.
- `UseCases`: `activeIndex: number`; preview component owns its own animation `t`.
- `Technology / MotionProfile`: `t: number` 0..1 loop.
- Reveal observer: optional, just adds a class.

No data fetching, no router state. The whole page is static + animated.

---

## Responsive Behavior
The current prototype is **desktop-first** and largely fluid via `clamp()`-typed headlines and `auto-fit minmax(...)` grids. Recommended breakpoints for the implementation:
- ≥ 1100px: as designed.
- 768–1099px: stack the Use Cases two-column into single column (list above preview). Reduce nav-links gap. Consider hiding 1–2 nav links on narrower screens.
- < 768px: collapse nav links into a single Download button. Stack everything. Hero signature SVG will scale via viewBox; cap its width to the available width. Section headers can collapse from 120px + 1fr to single column.

The hero signature is large; on narrow viewports it should keep aspect ratio (it already uses `preserveAspectRatio="xMidYMid meet"`).

---

## Assets
- **Fonts**: Google Fonts — Space Grotesk, Inter, JetBrains Mono.
- **Logo**: inline SVG (pen-nib drawing a vector node). Recreate or replace with your final brand mark.
- **Cursor arrow**: inline SVG (Windows-style pointer). Used in the hero signature, in step 3 illustration, and in the use-cases preview.
- **Background splines**: decorative inline SVG with two curves + control-point dots. Keep them subtle (opacity 0.5).
- **No external images** are used anywhere on the page. Every visual is SVG or CSS.

---

## Files in This Bundle
- `index.html` — page shell, fonts, base styles, root mount.
- `components.jsx` — `LogoMark`, `CursorArrow`, `Header`, `BackgroundSplines`, `SignatureCanvas`, `Hero`.
- `sections.jsx` — `useReveal`, `SectionHeader`, `WhatItIs`, `HowItWorks` + step illustrations (`IllParsing`, `IllPlanning`, `IllSyntheticInput`).
- `sections2.jsx` — `UseCases` + `UseCaseDemo`, `Technology` + `MotionProfile`, `CTA`, `Footer`.
- `app.jsx` — top-level composition.

To run the prototype locally: open `index.html` in a browser (it loads React 18.3.1 + Babel from unpkg). To recreate in your codebase: split each component into its own file, swap Babel-inline JSX for your build pipeline, and replace the global `Object.assign(window, …)` exports with real ES module exports.
