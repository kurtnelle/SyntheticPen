# SyntheticPen — Claude Code Brief

## Product Overview

SyntheticPen is a Windows desktop application that acts as a **Virtual Pen Plotter**.

The software converts vector paths (SVG signatures, handwriting, strokes, curves, or motion paths) into synthetic mouse or pen input events that are replayed into arbitrary applications.

Conceptually, the software behaves like:

> A tiny 2-axis CNC machine controlling the Windows cursor.

Instead of moving motors in physical space, SyntheticPen drives:
- the mouse cursor
- Windows Ink
- synthetic pointer input
- or virtual HID devices.

The primary use case is:
- replaying signatures
- drawing SVG paths
- annotation automation
- accessibility tooling
- cursor plotting
- synthetic handwriting
- software demonstrations
- automated on-screen drawing.

---

# Core Product Goals

## MVP Goals

The first working version should:

1. Load an SVG file
2. Extract vector path data
3. Convert Bezier curves into interpolated point lists
4. Scale the path to the target drawing area
5. Move the cursor through the path
6. Hold left mouse button while moving
7. Replay the drawing visibly on screen

The application should be able to:
- draw into Epic Pen
- Microsoft Paint
- Whiteboard apps
- browser canvas controls
- PDF annotation applications
- OneNote
- generic drawing applications.

---

# High-Level Architecture

## Primary Components

### 1. UI Layer
Responsibilities:
- load SVG files
- preview paths
- configure scaling
- configure playback speed
- select injection mode
- test drawing area
- start/stop playback

Suggested technology:
- WPF
- .NET 8+
- MVVM architecture

---

### 2. SVG Parser
Responsibilities:
- parse SVG path data
- support:
  - M
  - L
  - C
  - Q
  - Z
- flatten curves into sampled points
- normalize coordinate space

Suggested libraries:
- Svg.Skia
- Svg.NET
- custom parser if necessary

Internal output:

```csharp
List<PointF>
```

---

### 3. Motion Planner
Responsibilities:
- interpolate movement
- smooth paths
- generate timing curves
- apply easing
- optional humanization/jitter
- optional acceleration/deceleration curves

Conceptually similar to CNC motion planning.

Potential future support:
- pressure simulation
- variable stroke velocity
- hesitation simulation
- hand tremor simulation
- stroke ordering optimization.

---

### 4. Input Injection Engine
Responsibilities:
- generate synthetic cursor movement
- issue mouse down/up events
- optionally inject pen input

Initial implementation:
- Win32 SendInput()

Future implementations:
- Windows Pointer Injection
- Windows Ink APIs
- virtual HID digitizer device

---

# MVP Technical Flow

```text
SVG File
    ↓
Parse Vector Paths
    ↓
Flatten Curves Into Points
    ↓
Scale Coordinates
    ↓
Motion Planner
    ↓
Synthetic Input Injection
    ↓
Target Drawing Application
```

---

# Initial Feature Set

## SVG Loading
- drag-and-drop support
- file browser support
- preview rendering

---

## Coordinate Mapping
Support:
- absolute positioning
- relative positioning
- drawing box selection
- fit-to-region scaling
- maintain aspect ratio

---

## Playback Controls
- play
- pause
- stop
- speed adjustment
- loop playback

---

## Cursor Injection
Initial implementation should support:
- left mouse button down
- cursor movement
- left mouse button up

Injection frequency target:
- 125–1000 Hz equivalent update rates.

---

## Safety Features
- emergency stop hotkey
- playback timeout
- visible countdown before drawing
- active window confirmation

---

# Humanization Features (Future)

SyntheticPen should eventually support:

## Stroke Humanization
- slight randomization
- imperfect curvature
- acceleration variance
- natural pauses
- stroke overshoot
- variable pressure

---

## Velocity Curves
Humans draw:
- fast on straights
- slow on curves
- pause at transitions

Motion planner should support:
- spline-based velocity modulation
- acceleration profiles
- easing functions.

---

# Input Modes

## Mode 1 — Mouse Injection
Simplest implementation.

Uses:
- SendInput()
- standard cursor APIs.

Advantages:
- easiest
- compatible with most applications.

---

## Mode 2 — Synthetic Pointer Injection
More advanced.

Uses:
- InjectSyntheticPointerInput
- Windows Pointer APIs.

Supports:
- pressure
- pen state
- stylus semantics.

---

## Mode 3 — Virtual HID Device
Future advanced mode.

System creates:
- virtual stylus
- virtual mouse
- virtual digitizer.

Most realistic implementation.

---

# UI Ideas

## Main Window
Sections:

### Left Panel
- SVG preview
- path visualization
- point rendering

### Right Panel
Controls:
- speed slider
- smoothing
- scaling
- injection mode
- start button
- stop button
- target calibration.

---

# Example Use Cases

## Signature Replay
Load:

```text
Shawn_K_Lewis.svg
```

Replay into:
- PDF software
- Epic Pen
- Whiteboard apps
- browser signature fields.

---

## Annotation Automation
Replay pre-recorded drawings:
- arrows
- circles
- highlights
- notes.

---

## Accessibility
Assist users with:
- motor impairments
- repetitive movement limitations
- precision drawing.

---

## Presentation Tools
Automated live drawing overlays during:
- demos
- presentations
- training.

---

# Security & Ethical Constraints

The application must:
- remain transparent to users
- avoid stealth behavior
- avoid anti-cheat scenarios
- avoid malicious automation
- avoid credential automation.

The application should:
- visibly indicate active replay
- require explicit user initiation
- support immediate cancellation.

---

# Distribution Strategy

## Initial Distribution
- local executable
- portable build
- internal testing.

---

## Future Distribution
Potential Microsoft Store deployment.

Potential packaging:
- MSIX
- Win32 desktop bridge.

Potential need for:
- code signing certificate
- Microsoft Partner Center account.

---

# Recommended Technology Stack

## Language
C#

## Framework
.NET 8+

## UI
WPF

## Architecture
MVVM

## SVG Parsing
Svg.Skia or equivalent.

## Injection APIs
- SendInput()
- Windows Pointer Injection APIs.

---

# Suggested Internal Namespaces

```text
SyntheticPen.Core
SyntheticPen.Motion
SyntheticPen.Input
SyntheticPen.UI
SyntheticPen.Svg
SyntheticPen.Rendering
```

---

# Example Internal Classes

```csharp
SvgPathLoader
MotionPlanner
BezierFlattener
CursorInjector
PointerInjector
StrokePlayer
HumanizationEngine
PlaybackController
```

---

# Future Possibilities

## AI Stroke Synthesis
Potential future capability:
- train motion patterns
- emulate handwriting styles
- realistic signature motion.

---

## Recording Mode
Allow users to:
- draw naturally
- record input
- export SVG/motion profiles.

---

## Scripting Engine
Potential support:
- JSON motion scripts
- Lua scripting
- automation workflows.

---

# Product Positioning

SyntheticPen is not:
- a drawing tablet
- a hardware stylus
- a PDF editor.

SyntheticPen is:

> A virtual pen plotter for Windows.

It converts vector paths into synthetic cursor or pen movement.

---

# Core Tagline Ideas

- SyntheticPen — Virtual Pen Plotter Software
- SyntheticPen — SVG Motion Replay
- SyntheticPen — Cursor Plotting for Windows
- SyntheticPen — Synthetic Pen Input
- SyntheticPen — Vector Paths to Real Motion

---

# Development Priority

## Phase 1
- SVG parsing
- cursor replay
- mouse injection
- basic UI.

## Phase 2
- smoothing
- timing curves
- scaling tools
- profile saving.

## Phase 3
- synthetic pointer injection
- pressure simulation
- HID virtualization.

## Phase 4
- AI humanization
- recording engine
- scripting system.

---

# Final Concept Summary

SyntheticPen is essentially:

> a software CNC pen plotter that controls the Windows cursor instead of physical motors.

The application bridges:
- vector graphics
- motion planning
- synthetic input
- cursor automation
- and virtual handwriting.

