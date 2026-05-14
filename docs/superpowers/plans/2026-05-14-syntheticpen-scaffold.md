# SyntheticPen Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the SyntheticPen monorepo: an Avalonia 11 / .NET 8 desktop app with MSIX packaging plus an Astro 5 / React-islands marketing site, both buildable on a clean machine and on GitHub Actions, with the design handoff ported into the site as far as Header + Hero + Footer + section shells.

**Architecture:** Monorepo. `app/` holds a `.slnx` solution with six library/app projects + one packaging project + three xUnit test projects. `site/` is an Astro 5 project that uses React islands (`@astrojs/react`) to host ports of the JSX prototype in `design_handoff/`. CI lives in `.github/workflows/`.

**Tech Stack:** .NET 8, Avalonia 11, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, xUnit, FluentAssertions, MSIX (Windows Application Packaging), Astro 5, React 18, TypeScript, `@fontsource/*`, GitHub Pages.

**Spec:** [docs/superpowers/specs/2026-05-14-syntheticpen-scaffold-design.md](../specs/2026-05-14-syntheticpen-scaffold-design.md)

**Note on TDD:** This scaffold is mostly boilerplate and interface declarations with `NotImplementedException` bodies — there is no meaningful logic to test. Each test project gets exactly one smoke test (`true.Should().BeTrue()`) to prove the test runner and CI wiring work. Phase 1 (the next plan) is where real TDD begins.

---

## Conventions

- **Working directory** is the repo root (`I:\Source\repos\SyntheticPen`) throughout.
- **Shell** is PowerShell. Use `;` not `&&` if chaining across steps that must run sequentially within one Bash invocation; otherwise run each command on its own.
- **Commits**: one logical change per commit, conventional-commit prefix (`chore:`, `feat:`, `docs:`, `ci:`). Co-author trailer is not required.
- **Verification**: every section ends with a build/test command and an explicit expected outcome.

---

## Task 1: Repository Hygiene Files

**Files:**
- Create: `.gitignore`
- Create: `.editorconfig`
- Create: `LICENSE`
- Create: `README.md`

- [ ] **Step 1: Write `.gitignore`**

```gitignore
# .NET
bin/
obj/
*.user
*.suo
.vs/
.idea/
*.userprefs
artifacts/
TestResults/
coverage/
*.coverage
*.coveragexml

# MSIX
AppPackages/
BundleArtifacts/
*.appx
*.appxbundle
*.msix
*.msixbundle
GeneratedArtifacts/

# Node / Astro
node_modules/
dist/
.astro/
.cache/
.turbo/

# OS
Thumbs.db
.DS_Store
desktop.ini

# Editors
*.swp
*~

# Env
.env
.env.local
.env.*.local

# Logs
*.log
npm-debug.log*
```

- [ ] **Step 2: Write `.editorconfig`**

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 2

[*.{cs,csproj,props,targets,slnx}]
indent_size = 4

[*.md]
trim_trailing_whitespace = false
```

- [ ] **Step 3: Write `LICENSE` (MIT)**

```
MIT License

Copyright (c) 2026 Shawn Lewis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

- [ ] **Step 4: Write `README.md`**

```markdown
# SyntheticPen

A virtual pen plotter for Windows — converts SVG paths and other vector geometry into synthetic cursor / pen input. Like a CNC plotter for your handwriting.

> **Status:** Scaffold. No drawing yet. See [the brief](syntheticpen_claude_code_brief.md) and the [scaffold design spec](docs/superpowers/specs/2026-05-14-syntheticpen-scaffold-design.md).

## Repo layout

| Path | What |
|---|---|
| `app/` | Avalonia 11 / .NET 8 desktop app + MSIX packaging |
| `site/` | Astro 5 marketing site (deploys to GitHub Pages) |
| `design_handoff/` | Frozen design reference — JSX prototype + style guide |
| `docs/design/` | Mockups and the long-form style guide |
| `docs/superpowers/` | Specs and implementation plans |

## Build

### Prerequisites
- Windows 10 1809+ (for MSIX packaging)
- .NET 8 SDK (8.0.400 or newer for `.slnx` support)
- Node.js 20+
- (optional) Visual Studio 2022 17.10+ for IDE solution support

### Desktop app

```pwsh
dotnet build app/SyntheticPen.slnx
dotnet test app/SyntheticPen.slnx
dotnet run --project app/src/SyntheticPen.App
```

### Website

```pwsh
cd site
npm ci
npm run dev      # http://localhost:4321
npm run build    # outputs to site/dist
```

## First-time setup for GitHub Pages

Before the first site deploy, set `site` and `base` in [`site/astro.config.mjs`](site/astro.config.mjs) to match your GitHub username and repo name, and in your repo settings set **Pages → Build and deployment → Source → GitHub Actions**.

## License

MIT. See [LICENSE](LICENSE).
```

- [ ] **Step 5: Verify and commit**

```pwsh
git add .gitignore .editorconfig LICENSE README.md
git status
git commit -m "chore: add repo hygiene files (gitignore, editorconfig, license, readme)"
```

Expected: clean working tree minus untracked files added later.

---

## Task 2: Stage Existing Reference Files

The brief, style guide, design handoff, and mockups are already in the working tree. Bring them under version control.

- [ ] **Step 1: Verify the layout matches the spec**

```pwsh
ls
ls design_handoff
ls docs/design
ls docs/superpowers/specs
```

Expected:
- Root contains `syntheticpen_claude_code_brief.md`, `syntheticpen_website_style_guide.md`.
- `design_handoff/` contains the 7 files from the zip.
- `docs/design/` contains `application_concept.png`, `website_mockup.png`, `website_style_guide.md`.
- `docs/superpowers/specs/` contains the scaffold design doc.
- `docs/superpowers/plans/` contains this plan.

- [ ] **Step 2: Commit reference docs**

```pwsh
git add syntheticpen_claude_code_brief.md syntheticpen_website_style_guide.md docs/
git commit -m "docs: add product brief, website style guide, mockups, and scaffold spec/plan"
```

- [ ] **Step 3: Commit the design handoff separately**

```pwsh
git add design_handoff/
git commit -m "docs: freeze design handoff JSX prototype as visual reference"
```

---

## Task 3: .NET Solution Skeleton

**Files:**
- Create: `app/SyntheticPen.slnx`
- Create: `app/Directory.Build.props`
- Create: `app/Directory.Packages.props`
- Create: `app/global.json`

- [ ] **Step 1: Write `app/global.json`**

```json
{
  "sdk": {
    "version": "8.0.400",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 2: Write `app/Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <NeutralLanguage>en-US</NeutralLanguage>
    <RootNamespace>$(MSBuildProjectName)</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Write `app/Directory.Packages.props` (CPM)**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Avalonia" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Desktop" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Themes.Fluent" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="11.2.1" />
    <PackageVersion Include="Avalonia.Diagnostics" Version="11.2.1" />
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageVersion Include="Svg.Skia" Version="2.0.0.4" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create the empty solution**

```pwsh
cd app
dotnet new sln -n SyntheticPen --format slnx
cd ..
```

Expected: `app/SyntheticPen.slnx` exists.

- [ ] **Step 5: Verify SDK accepts `.slnx`**

```pwsh
dotnet sln app/SyntheticPen.slnx list
```

Expected: prints "No projects found in the solution." (or equivalent — exit code 0).

- [ ] **Step 6: Commit**

```pwsh
git add app/
git commit -m "feat(app): add slnx solution shell with CPM and build props"
```

---

## Task 4: Core Library Projects

Create five class library projects and wire them into the solution. All target `net8.0` and have no UI dependency.

**Files (per project, replace `<Name>` with each):**
- Create: `app/src/<Name>/<Name>.csproj`
- Create: source files listed per project below

- [ ] **Step 1: Create `SyntheticPen.Core`**

```pwsh
dotnet new classlib -n SyntheticPen.Core -o app/src/SyntheticPen.Core -f net8.0
rm app/src/SyntheticPen.Core/Class1.cs
```

Write `app/src/SyntheticPen.Core/Models/PointF.cs`:

```csharp
namespace SyntheticPen.Core.Models;

public readonly record struct PointF(double X, double Y);
```

Write `app/src/SyntheticPen.Core/Models/Stroke.cs`:

```csharp
namespace SyntheticPen.Core.Models;

public sealed class Stroke
{
    public Stroke(IReadOnlyList<PointF> points)
    {
        Points = points;
    }

    public IReadOnlyList<PointF> Points { get; }
}
```

Write `app/src/SyntheticPen.Core/Playback/PlaybackState.cs`:

```csharp
namespace SyntheticPen.Core.Playback;

public enum PlaybackState
{
    Idle,
    CountingDown,
    Playing,
    Paused,
    Stopping
}
```

Write `app/src/SyntheticPen.Core/Playback/IPlaybackController.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface IPlaybackController
{
    PlaybackState State { get; }
    event Action<PlaybackState>? StateChanged;
    Task PlayAsync(IReadOnlyList<Stroke> strokes, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
```

Write `app/src/SyntheticPen.Core/Playback/PlaybackController.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public sealed class PlaybackController : IPlaybackController
{
    public PlaybackState State { get; private set; } = PlaybackState.Idle;
    public event Action<PlaybackState>? StateChanged;

    public Task PlayAsync(IReadOnlyList<Stroke> strokes, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task StopAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
```

- [ ] **Step 2: Create `SyntheticPen.Svg`** (depends on Core)

```pwsh
dotnet new classlib -n SyntheticPen.Svg -o app/src/SyntheticPen.Svg -f net8.0
rm app/src/SyntheticPen.Svg/Class1.cs
dotnet add app/src/SyntheticPen.Svg reference app/src/SyntheticPen.Core
```

Edit `app/src/SyntheticPen.Svg/SyntheticPen.Svg.csproj` to add the Svg.Skia package reference (CPM-aware, no version):

```xml
<ItemGroup>
  <PackageReference Include="Svg.Skia" />
</ItemGroup>
```

Write `app/src/SyntheticPen.Svg/ISvgPathLoader.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public interface ISvgPathLoader
{
    Task<IReadOnlyList<Stroke>> LoadAsync(Stream svgStream, CancellationToken ct = default);
}
```

Write `app/src/SyntheticPen.Svg/SkiaSvgPathLoader.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<IReadOnlyList<Stroke>> LoadAsync(Stream svgStream, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
```

Write `app/src/SyntheticPen.Svg/BezierFlattener.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public static class BezierFlattener
{
    public static IReadOnlyList<PointF> Flatten(IReadOnlyList<PointF> controlPoints, double tolerance)
        => throw new NotImplementedException("Phase 1");
}
```

- [ ] **Step 3: Create `SyntheticPen.Motion`** (depends on Core)

```pwsh
dotnet new classlib -n SyntheticPen.Motion -o app/src/SyntheticPen.Motion -f net8.0
rm app/src/SyntheticPen.Motion/Class1.cs
dotnet add app/src/SyntheticPen.Motion reference app/src/SyntheticPen.Core
```

Write `app/src/SyntheticPen.Motion/PlanOptions.cs`:

```csharp
namespace SyntheticPen.Motion;

public sealed record PlanOptions(double SpeedMultiplier = 1.0, bool Humanize = false);
```

Write `app/src/SyntheticPen.Motion/TimedPoint.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public readonly record struct TimedPoint(PointF Point, TimeSpan Offset);
```

Write `app/src/SyntheticPen.Motion/IMotionPlanner.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public interface IMotionPlanner
{
    IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> strokes,
        PlanOptions options,
        CancellationToken ct = default);
}
```

Write `app/src/SyntheticPen.Motion/DefaultMotionPlanner.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Motion;

public sealed class DefaultMotionPlanner : IMotionPlanner
{
    public IAsyncEnumerable<TimedPoint> Plan(
        IReadOnlyList<Stroke> strokes,
        PlanOptions options,
        CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
```

- [ ] **Step 4: Create `SyntheticPen.Input`** (depends on Core)

```pwsh
dotnet new classlib -n SyntheticPen.Input -o app/src/SyntheticPen.Input -f net8.0
rm app/src/SyntheticPen.Input/Class1.cs
dotnet add app/src/SyntheticPen.Input reference app/src/SyntheticPen.Core
```

Write `app/src/SyntheticPen.Input/InjectionMode.cs`:

```csharp
namespace SyntheticPen.Input;

public enum InjectionMode
{
    Mouse,
    SyntheticPointer,
    VirtualHid
}
```

Write `app/src/SyntheticPen.Input/ICursorInjector.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Input;

public interface ICursorInjector
{
    Task MoveAsync(PointF point, CancellationToken ct = default);
    Task PenDownAsync(CancellationToken ct = default);
    Task PenUpAsync(CancellationToken ct = default);
}
```

Write `app/src/SyntheticPen.Input/MouseSendInputInjector.cs`:

```csharp
using System.Runtime.InteropServices;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Input;

public sealed class MouseSendInputInjector : ICursorInjector
{
    public Task MoveAsync(PointF point, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task PenDownAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task PenUpAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    // P/Invoke signatures reserved for Phase 1.
#pragma warning disable IDE0051, CA1812
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
#pragma warning restore IDE0051, CA1812
}
```

- [ ] **Step 5: Create `SyntheticPen.Rendering`** (depends on Core)

```pwsh
dotnet new classlib -n SyntheticPen.Rendering -o app/src/SyntheticPen.Rendering -f net8.0
rm app/src/SyntheticPen.Rendering/Class1.cs
dotnet add app/src/SyntheticPen.Rendering reference app/src/SyntheticPen.Core
```

Write `app/src/SyntheticPen.Rendering/IStrokePreviewRenderer.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public interface IStrokePreviewRenderer
{
    object BuildGeometry(IReadOnlyList<Stroke> strokes);
}
```

Write `app/src/SyntheticPen.Rendering/StrokePreviewRenderer.cs`:

```csharp
using SyntheticPen.Core.Models;

namespace SyntheticPen.Rendering;

public sealed class StrokePreviewRenderer : IStrokePreviewRenderer
{
    public object BuildGeometry(IReadOnlyList<Stroke> strokes)
        => throw new NotImplementedException("Phase 1");
}
```

- [ ] **Step 6: Add all five libraries to the solution and build**

```pwsh
dotnet sln app/SyntheticPen.slnx add `
  app/src/SyntheticPen.Core/SyntheticPen.Core.csproj `
  app/src/SyntheticPen.Svg/SyntheticPen.Svg.csproj `
  app/src/SyntheticPen.Motion/SyntheticPen.Motion.csproj `
  app/src/SyntheticPen.Input/SyntheticPen.Input.csproj `
  app/src/SyntheticPen.Rendering/SyntheticPen.Rendering.csproj
dotnet build app/SyntheticPen.slnx -c Release
```

Expected: build succeeds with 0 errors. Warnings allowed only from third-party packages.

- [ ] **Step 7: Commit**

```pwsh
git add app/src
git commit -m "feat(app): scaffold Core, Svg, Motion, Input, Rendering libraries"
```

---

## Task 5: Avalonia UI Project

**Files:**
- Create: `app/src/SyntheticPen.App/` (full Avalonia project)

- [ ] **Step 1: Generate the Avalonia app from the template**

```pwsh
dotnet new install Avalonia.Templates::11.2.1
dotnet new avalonia.app -n SyntheticPen.App -o app/src/SyntheticPen.App -f net8.0
```

Expected: `App.axaml`, `Program.cs`, `MainWindow.axaml`, `app.manifest` created.

- [ ] **Step 2: Add references and packages**

```pwsh
dotnet add app/src/SyntheticPen.App reference `
  app/src/SyntheticPen.Core `
  app/src/SyntheticPen.Svg `
  app/src/SyntheticPen.Motion `
  app/src/SyntheticPen.Input `
  app/src/SyntheticPen.Rendering
```

Edit `app/src/SyntheticPen.App/SyntheticPen.App.csproj` — replace its `<ItemGroup>` package references with (versions are managed centrally by CPM):

```xml
<ItemGroup>
  <PackageReference Include="Avalonia" />
  <PackageReference Include="Avalonia.Desktop" />
  <PackageReference Include="Avalonia.Themes.Fluent" />
  <PackageReference Include="Avalonia.Fonts.Inter" />
  <PackageReference Include="CommunityToolkit.Mvvm" />
  <PackageReference Include="Microsoft.Extensions.Hosting" />
  <PackageReference Condition="'$(Configuration)' == 'Debug'" Include="Avalonia.Diagnostics" />
</ItemGroup>
```

Confirm the csproj has:

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net8.0</TargetFramework>
<ApplicationManifest>app.manifest</ApplicationManifest>
<BuiltInComInteropSupport>true</BuiltInComInteropSupport>
```

- [ ] **Step 3: Replace `Program.cs` with hosted-startup wiring**

Write `app/src/SyntheticPen.App/Program.cs`:

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;
using SyntheticPen.Svg;
using SyntheticPen.App.ViewModels;

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
        host.Services.AddSingleton<ICursorInjector, MouseSendInputInjector>();
        host.Services.AddSingleton<IStrokePreviewRenderer, StrokePreviewRenderer>();
        host.Services.AddSingleton<IPlaybackController, PlaybackController>();
        host.Services.AddSingleton<MainWindowViewModel>();

        Services = host.Build().Services;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

- [ ] **Step 4: Replace `App.axaml.cs` to inject the viewmodel**

Write `app/src/SyntheticPen.App/App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;

namespace SyntheticPen.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Program.Services.GetRequiredService<MainWindowViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 5: Write the MainWindow XAML**

Replace `app/src/SyntheticPen.App/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:SyntheticPen.App.ViewModels"
        x:Class="SyntheticPen.App.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="SyntheticPen"
        Width="1100" Height="720"
        MinWidth="900" MinHeight="600">
    <Grid RowDefinitions="Auto,*">
        <Menu Grid.Row="0">
            <MenuItem Header="_File">
                <MenuItem Header="_Open SVG..." Command="{Binding OpenSvgCommand}" />
                <Separator/>
                <MenuItem Header="E_xit" Command="{Binding ExitCommand}" />
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
                <TextBlock Text="Speed"/>
                <Slider Minimum="0.25" Maximum="4.0" Value="{Binding SpeedMultiplier}"/>
                <CheckBox Content="Humanize" IsChecked="{Binding Humanize}"/>
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

Write `app/src/SyntheticPen.App/MainWindow.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace SyntheticPen.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}
```

- [ ] **Step 6: Write the MainWindowViewModel**

Write `app/src/SyntheticPen.App/ViewModels/MainWindowViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input;

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

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

    [RelayCommand] private Task OpenSvgAsync() => Task.CompletedTask;
    [RelayCommand] private void Exit() { /* TODO: graceful shutdown */ }
    [RelayCommand] private void About() { /* TODO: about dialog */ }
    [RelayCommand] private Task StartAsync() => Task.CompletedTask;
    [RelayCommand] private Task StopAsync() => Task.CompletedTask;
}
```

- [ ] **Step 7: Replace `App.axaml`**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="SyntheticPen.App.App"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- [ ] **Step 8: Add to solution and build**

```pwsh
dotnet sln app/SyntheticPen.slnx add app/src/SyntheticPen.App/SyntheticPen.App.csproj
dotnet build app/SyntheticPen.slnx -c Release
```

Expected: build succeeds.

- [ ] **Step 9: Smoke-run the window (manual, optional)**

```pwsh
dotnet run --project app/src/SyntheticPen.App
```

Expected: a dark window titled "SyntheticPen" appears with the left preview panel, right controls panel, and File/Help menus. Close it.

- [ ] **Step 10: Commit**

```pwsh
git add app/src/SyntheticPen.App
git commit -m "feat(app): scaffold Avalonia desktop window with MVVM + DI host"
```

---

## Task 6: MSIX Packaging Project

**Files:**
- Create: `app/src/SyntheticPen.Package/SyntheticPen.Package.wapproj`
- Create: `app/src/SyntheticPen.Package/Package.appxmanifest`
- Create: `app/src/SyntheticPen.Package/Images/*.png` (placeholders)

WAP projects aren't generated by `dotnet new`; the file must be authored by hand.

- [ ] **Step 1: Write `app/src/SyntheticPen.Package/SyntheticPen.Package.wapproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)..\..\'))" />
  <PropertyGroup>
    <ProjectGuid>{4F2D7B53-9F50-4D2A-9F26-2A1F0E5F1B41}</ProjectGuid>
    <TargetPlatformVersion>10.0.22621.0</TargetPlatformVersion>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <DefaultLanguage>en-US</DefaultLanguage>
    <EntryPointProjectUniqueName>..\SyntheticPen.App\SyntheticPen.App.csproj</EntryPointProjectUniqueName>
    <AppxPackageSigningEnabled>False</AppxPackageSigningEnabled>
    <GenerateAppInstallerFile>False</GenerateAppInstallerFile>
    <AppxAutoIncrementPackageRevision>True</AppxAutoIncrementPackageRevision>
    <AppxBundle>Always</AppxBundle>
    <AppxBundlePlatforms>x64</AppxBundlePlatforms>
    <UapAppxPackageBuildMode>SideloadOnly</UapAppxPackageBuildMode>
  </PropertyGroup>
  <ItemGroup>
    <AppxManifest Include="Package.appxmanifest">
      <SubType>Designer</SubType>
    </AppxManifest>
  </ItemGroup>
  <ItemGroup>
    <Content Include="Images\*.png" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SyntheticPen.App\SyntheticPen.App.csproj" />
  </ItemGroup>
  <Import Project="$(WapProjPath)\Microsoft.DesktopBridge.targets" />
</Project>
```

- [ ] **Step 2: Write `app/src/SyntheticPen.Package/Package.appxmanifest`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity Name="SyntheticPen"
            Publisher="CN=SyntheticPen-Dev"
            Version="0.1.0.0" />

  <Properties>
    <DisplayName>SyntheticPen</DisplayName>
    <PublisherDisplayName>SyntheticPen</PublisherDisplayName>
    <Logo>Images\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-US" />
  </Resources>

  <Applications>
    <Application Id="App"
                 Executable="SyntheticPen.App.exe"
                 EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="SyntheticPen"
        Description="A virtual pen plotter for Windows."
        Square150x150Logo="Images\Square150x150Logo.png"
        Square44x44Logo="Images\Square44x44Logo.png"
        BackgroundColor="#0A0A0A">
        <uap:DefaultTile Wide310x150Logo="Images\Wide310x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

- [ ] **Step 3: Generate placeholder icon PNGs**

Use PowerShell with `System.Drawing` to emit four solid-color PNGs:

```pwsh
Add-Type -AssemblyName System.Drawing
$bg = [System.Drawing.Color]::FromArgb(10, 10, 10)
$accent = [System.Drawing.Color]::FromArgb(77, 163, 255)
$dir = "app/src/SyntheticPen.Package/Images"
New-Item -ItemType Directory -Force $dir | Out-Null

function New-Logo($path, $w, $h) {
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.Clear($bg)
  $brush = New-Object System.Drawing.SolidBrush $accent
  $g.FillEllipse($brush, [int]($w*0.25), [int]($h*0.25), [int]($w*0.5), [int]($h*0.5))
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose(); $g.Dispose()
}

New-Logo "$dir/Square44x44Logo.png" 44 44
New-Logo "$dir/Square150x150Logo.png" 150 150
New-Logo "$dir/Wide310x150Logo.png" 310 150
New-Logo "$dir/StoreLogo.png" 50 50
```

Expected: four PNGs in `app/src/SyntheticPen.Package/Images/`.

- [ ] **Step 4: Add to solution**

```pwsh
dotnet sln app/SyntheticPen.slnx add app/src/SyntheticPen.Package/SyntheticPen.Package.wapproj
```

**Note:** `dotnet build` on a `.wapproj` is unreliable across platforms; this project only builds reliably with full MSBuild on Windows (`msbuild app/src/SyntheticPen.Package/SyntheticPen.Package.wapproj /restore /p:Configuration=Release /p:Platform=x64`). CI runs it; local devs aren't required to build it.

- [ ] **Step 5: Commit**

```pwsh
git add app/src/SyntheticPen.Package
git commit -m "feat(app): add MSIX packaging project with placeholder identity and icons"
```

---

## Task 7: Test Projects

**Files (per project):**
- Create: `app/tests/<Name>.Tests/<Name>.Tests.csproj`
- Create: `app/tests/<Name>.Tests/SmokeTests.cs`

- [ ] **Step 1: Create the three xUnit projects**

```pwsh
dotnet new xunit -n SyntheticPen.Core.Tests -o app/tests/SyntheticPen.Core.Tests -f net8.0
dotnet new xunit -n SyntheticPen.Svg.Tests  -o app/tests/SyntheticPen.Svg.Tests  -f net8.0
dotnet new xunit -n SyntheticPen.Motion.Tests -o app/tests/SyntheticPen.Motion.Tests -f net8.0

dotnet add app/tests/SyntheticPen.Core.Tests   reference app/src/SyntheticPen.Core
dotnet add app/tests/SyntheticPen.Svg.Tests    reference app/src/SyntheticPen.Svg
dotnet add app/tests/SyntheticPen.Motion.Tests reference app/src/SyntheticPen.Motion
```

- [ ] **Step 2: Add FluentAssertions + remove default UnitTest1.cs**

For each test project, edit `<Name>.Tests.csproj` so the `<ItemGroup>` package references are exactly:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="xunit" />
  <PackageReference Include="xunit.runner.visualstudio" />
  <PackageReference Include="FluentAssertions" />
</ItemGroup>
```

```pwsh
rm app/tests/SyntheticPen.Core.Tests/UnitTest1.cs
rm app/tests/SyntheticPen.Svg.Tests/UnitTest1.cs
rm app/tests/SyntheticPen.Motion.Tests/UnitTest1.cs
```

- [ ] **Step 3: Write one smoke test per project**

Write `app/tests/SyntheticPen.Core.Tests/SmokeTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Core.Models;
using Xunit;

namespace SyntheticPen.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Stroke_carries_its_points()
    {
        var stroke = new Stroke(new[] { new PointF(0, 0), new PointF(1, 1) });
        stroke.Points.Should().HaveCount(2);
    }
}
```

Write `app/tests/SyntheticPen.Svg.Tests/SmokeTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Svg;
using Xunit;

namespace SyntheticPen.Svg.Tests;

public class SmokeTests
{
    [Fact]
    public void Loader_type_is_resolvable()
    {
        typeof(SkiaSvgPathLoader).Should().Implement<ISvgPathLoader>();
    }
}
```

Write `app/tests/SyntheticPen.Motion.Tests/SmokeTests.cs`:

```csharp
using FluentAssertions;
using SyntheticPen.Motion;
using Xunit;

namespace SyntheticPen.Motion.Tests;

public class SmokeTests
{
    [Fact]
    public void Planner_type_is_resolvable()
    {
        typeof(DefaultMotionPlanner).Should().Implement<IMotionPlanner>();
    }
}
```

- [ ] **Step 4: Add to solution and run tests**

```pwsh
dotnet sln app/SyntheticPen.slnx add `
  app/tests/SyntheticPen.Core.Tests/SyntheticPen.Core.Tests.csproj `
  app/tests/SyntheticPen.Svg.Tests/SyntheticPen.Svg.Tests.csproj `
  app/tests/SyntheticPen.Motion.Tests/SyntheticPen.Motion.Tests.csproj
dotnet test app/SyntheticPen.slnx -c Release
```

Expected: "Passed: 3, Failed: 0". The `.wapproj` may show "skipped" or a warning on non-Windows hosts; that's fine.

- [ ] **Step 5: Commit**

```pwsh
git add app/tests
git commit -m "test(app): add smoke tests for Core, Svg, Motion"
```

---

## Task 8: Astro Site Bootstrap

**Files:**
- Create: `site/package.json`, `site/astro.config.mjs`, `site/tsconfig.json`
- Create: `site/src/styles/global.css`
- Create: `site/src/layouts/BaseLayout.astro`
- Create: `site/src/pages/index.astro`, `privacy.astro`, `404.astro`
- Create: `site/src/lib/timing.ts`
- Create: `site/public/favicon.svg`

- [ ] **Step 1: Initialize the project files manually (skip `npm create astro` to keep it deterministic)**

Create `site/package.json`:

```json
{
  "name": "syntheticpen-site",
  "private": true,
  "type": "module",
  "version": "0.1.0",
  "scripts": {
    "dev": "astro dev",
    "build": "astro check && astro build",
    "preview": "astro preview"
  },
  "dependencies": {
    "astro": "^5.1.0",
    "@astrojs/react": "^4.1.0",
    "@astrojs/check": "^0.9.4",
    "@fontsource/space-grotesk": "^5.1.0",
    "@fontsource/inter": "^5.1.0",
    "@fontsource/jetbrains-mono": "^5.1.0",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "typescript": "^5.7.2"
  },
  "devDependencies": {
    "@types/react": "^18.3.13",
    "@types/react-dom": "^18.3.1"
  }
}
```

Create `site/astro.config.mjs`:

```js
// @ts-check
import { defineConfig } from 'astro/config';
import react from '@astrojs/react';

// TODO before first deploy: replace <github-user> with your GitHub username.
export default defineConfig({
  site: 'https://<github-user>.github.io',
  base: '/SyntheticPen/',
  trailingSlash: 'ignore',
  integrations: [react()],
  build: { format: 'directory' }
});
```

Create `site/tsconfig.json`:

```json
{
  "extends": "astro/tsconfigs/strict",
  "compilerOptions": {
    "jsx": "react-jsx",
    "jsxImportSource": "react",
    "baseUrl": "."
  },
  "include": ["src/**/*"]
}
```

Create `site/src/env.d.ts`:

```ts
/// <reference path="../.astro/types.d.ts" />
/// <reference types="astro/client" />
```

- [ ] **Step 2: Write the design-token stylesheet**

Create `site/src/styles/global.css`:

```css
@import '@fontsource/space-grotesk/400.css';
@import '@fontsource/space-grotesk/500.css';
@import '@fontsource/space-grotesk/600.css';
@import '@fontsource/space-grotesk/700.css';
@import '@fontsource/inter/300.css';
@import '@fontsource/inter/400.css';
@import '@fontsource/inter/500.css';
@import '@fontsource/inter/600.css';
@import '@fontsource/jetbrains-mono/400.css';
@import '@fontsource/jetbrains-mono/500.css';

:root {
  --bg-0: #0A0A0A;
  --bg-1: #121212;
  --bg-2: #1A1A1A;
  --ink: #F5F5F5;
  --ink-dim: #8a8d92;
  --silver: #A6A6A6;
  --blue: #4DA3FF;
  --cyan: #6BE6FF;
  --grid: rgba(255, 255, 255, 0.04);
  --grid-strong: rgba(77, 163, 255, 0.08);
  --border: rgba(255, 255, 255, 0.08);
  --border-strong: rgba(255, 255, 255, 0.14);

  --font-display: 'Space Grotesk', system-ui, sans-serif;
  --font-body: 'Inter', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, monospace;
}

*, *::before, *::after { box-sizing: border-box; }

html, body {
  margin: 0;
  padding: 0;
  background: var(--bg-0);
  color: var(--ink);
  font-family: var(--font-body);
  font-size: 16px;
  line-height: 1.55;
  -webkit-font-smoothing: antialiased;
}

a { color: inherit; text-decoration: none; }
h1, h2, h3 { font-family: var(--font-display); margin: 0; }
code, kbd, .mono { font-family: var(--font-mono); }

.bg-grid {
  position: fixed;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(to right, var(--grid) 1px, transparent 1px),
    linear-gradient(to bottom, var(--grid) 1px, transparent 1px);
  background-size: 64px 64px;
  -webkit-mask-image: radial-gradient(ellipse 90% 70% at 50% 30%, #000 30%, transparent 100%);
          mask-image: radial-gradient(ellipse 90% 70% at 50% 30%, #000 30%, transparent 100%);
  z-index: 0;
}

.bg-grid::after {
  content: '';
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(to right, var(--grid-strong) 1px, transparent 1px),
    linear-gradient(to bottom, var(--grid-strong) 1px, transparent 1px);
  background-size: 320px 320px;
  -webkit-mask-image: radial-gradient(ellipse 40% 40% at 50% 20%, #000 0%, transparent 70%);
          mask-image: radial-gradient(ellipse 40% 40% at 50% 20%, #000 0%, transparent 70%);
}

.container {
  position: relative;
  max-width: 1280px;
  margin: 0 auto;
  padding: 0 32px;
  z-index: 1;
}
```

- [ ] **Step 3: Write the central animation timing constants**

Create `site/src/lib/timing.ts`:

```ts
/** Centralized animation timing constants (handoff §Interactions & Behavior). */
export const HERO_DRAW_MS = 5400;
export const HERO_HOLD_MS = 1600;
export const USECASE_LOOP_MS = 4200;
export const MOTION_PROFILE_MS = 4000;
export const TAG_PULSE_MS = 2000;
export const REVEAL_MS = 800;
```

- [ ] **Step 4: Write `BaseLayout.astro`**

Create `site/src/layouts/BaseLayout.astro`:

```astro
---
import '../styles/global.css';
interface Props {
  title?: string;
  description?: string;
}
const { title = 'SyntheticPen', description = 'A virtual pen plotter for Windows.' } = Astro.props;
---
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="theme-color" content="#0A0A0A" />
    <meta name="description" content={description} />
    <title>{title}</title>
    <link rel="icon" type="image/svg+xml" href={`${import.meta.env.BASE_URL}favicon.svg`} />
  </head>
  <body>
    <div class="bg-grid" aria-hidden="true"></div>
    <slot />
  </body>
</html>
```

- [ ] **Step 5: Write the favicon**

Create `site/public/favicon.svg`:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
  <rect width="32" height="32" fill="#0A0A0A"/>
  <path d="M6 26 L20 6 L26 12 L12 26 L6 26 Z" fill="#4DA3FF"/>
  <path d="M9 23 L20 12 L24 14 L13 25 Z" fill="#6BE6FF"/>
</svg>
```

- [ ] **Step 6: Write `index.astro` shell**

Create `site/src/pages/index.astro`:

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import Header from '../components/Header.tsx';
import Hero from '../components/Hero.tsx';
import Footer from '../components/Footer.tsx';
---
<BaseLayout title="SyntheticPen — Virtual Pen Plotter for Windows">
  <Header client:load />
  <main>
    <Hero client:load />

    <section id="what" class="container" style="padding: 80px 32px;">
      <p class="mono" style="color: var(--silver); text-transform: uppercase; letter-spacing: 0.18em; font-size: 11px;">01 / What it is</p>
      <h2 style="font-size: clamp(32px, 4vw, 52px); margin-top: 16px;">A virtual pen plotter that lives inside Windows.</h2>
      <p style="color: var(--silver); max-width: 720px; margin-top: 16px;">SyntheticPen reads vector geometry — SVG paths, glyph outlines, hand-drawn signatures — and replays it as synthetic mouse and pen input.</p>
    </section>

    <section id="how" class="container" style="padding: 80px 32px;">
      <p class="mono" style="color: var(--silver); text-transform: uppercase; letter-spacing: 0.18em; font-size: 11px;">02 / How it works</p>
      <h2 style="font-size: clamp(32px, 4vw, 52px); margin-top: 16px;">From vector to input in three stages.</h2>
      <p style="color: var(--silver); max-width: 720px; margin-top: 16px;">Parse → Plan → Inject. Each stage is deterministic, scriptable, and inspectable.</p>
    </section>

    <section id="use" class="container" style="padding: 80px 32px;">
      <p class="mono" style="color: var(--silver); text-transform: uppercase; letter-spacing: 0.18em; font-size: 11px;">03 / Use cases</p>
      <h2 style="font-size: clamp(32px, 4vw, 52px); margin-top: 16px;">Anywhere the OS accepts a pointer.</h2>
    </section>

    <section id="tech" class="container" style="padding: 80px 32px;">
      <p class="mono" style="color: var(--silver); text-transform: uppercase; letter-spacing: 0.18em; font-size: 11px;">04 / Technology</p>
      <h2 style="font-size: clamp(32px, 4vw, 52px); margin-top: 16px;">Built like motion control hardware.</h2>
    </section>

    <section id="cta" class="container" style="padding: 100px 32px;">
      <div style="border: 1px solid rgba(77,163,255,0.2); padding: 64px 56px; text-align: center;">
        <h2 style="font-size: clamp(36px, 5vw, 64px); text-transform: uppercase; letter-spacing: -0.02em;">Bring your geometry into motion.</h2>
        <p style="color: var(--silver); margin-top: 16px;">Free during beta. No telemetry. Single signed binary.</p>
      </div>
    </section>
  </main>
  <Footer />
</BaseLayout>
```

- [ ] **Step 7: Write `privacy.astro`**

Create `site/src/pages/privacy.astro`:

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
---
<BaseLayout title="Privacy — SyntheticPen" description="SyntheticPen privacy policy.">
  <main class="container" style="padding: 80px 32px;">
    <h1 style="font-size: clamp(32px, 4vw, 52px);">Privacy</h1>
    <p style="color: var(--silver); margin-top: 16px;">
      SyntheticPen does not collect, transmit, or store any personal data. The application runs entirely
      on your device. No telemetry, no analytics, no network calls.
    </p>
    <h2 style="margin-top: 32px;">Local files</h2>
    <p style="color: var(--silver);">
      Any SVGs or motion profiles you load remain on your machine. Settings are stored in your user
      profile under <code>%APPDATA%\SyntheticPen</code>.
    </p>
    <h2 style="margin-top: 32px;">Contact</h2>
    <p style="color: var(--silver);">
      Questions? Open an issue on the project's GitHub repository.
    </p>
  </main>
</BaseLayout>
```

- [ ] **Step 8: Write `404.astro`**

Create `site/src/pages/404.astro`:

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
---
<BaseLayout title="Not Found — SyntheticPen">
  <main class="container" style="padding: 120px 32px; text-align: center;">
    <p class="mono" style="color: var(--blue); letter-spacing: 0.2em;">404</p>
    <h1 style="font-size: clamp(36px, 5vw, 64px); margin-top: 16px;">Off the canvas.</h1>
    <p style="color: var(--silver); margin-top: 16px;">
      <a href={import.meta.env.BASE_URL} style="color: var(--blue);">Return home →</a>
    </p>
  </main>
</BaseLayout>
```

- [ ] **Step 9: Install and verify (build will fail until Task 9 adds the React components — that's expected)**

```pwsh
cd site
npm install
cd ..
```

Expected: `site/node_modules` exists, `package-lock.json` written.

- [ ] **Step 10: Commit**

```pwsh
git add site/package.json site/package-lock.json site/astro.config.mjs site/tsconfig.json site/src/env.d.ts site/src/styles site/src/layouts site/src/lib site/src/pages site/public
git commit -m "feat(site): scaffold Astro project with design tokens and page shells"
```

---

## Task 9: React Island Components

Port the minimum set of components from `design_handoff/` to TypeScript React. Day-one scope is Header, Hero (with a simplified `SignatureCanvas`), and Footer. Animated sections 01–04 remain shells.

**Files:**
- Create: `site/src/components/Header.tsx`
- Create: `site/src/components/Hero.tsx`
- Create: `site/src/components/SignatureCanvas.tsx`
- Create: `site/src/components/Footer.tsx`
- Create: `site/src/components/icons/LogoMark.tsx`
- Create: `site/src/components/icons/CursorArrow.tsx`

- [ ] **Step 1: Write `icons/LogoMark.tsx`**

```tsx
export function LogoMark({ size = 22 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24"
         style={{ filter: 'drop-shadow(0 0 6px rgba(77,163,255,0.4))' }}>
      <path d="M4 20 L14 4 L20 8 L10 24 Z" fill="none" stroke="#4DA3FF" strokeWidth="1.5" />
      <circle cx="14" cy="4" r="1.2" fill="#6BE6FF" />
    </svg>
  );
}
```

- [ ] **Step 2: Write `icons/CursorArrow.tsx`**

```tsx
export function CursorArrow({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x}, ${y})`}
       style={{ filter: 'drop-shadow(0 0 4px rgba(107,230,255,0.7))' }}>
      <path d="M0 0 L0 14 L4 10 L7 16 L9 15 L6 9 L11 9 Z"
            fill="#F5F5F5" stroke="#000" strokeWidth="0.5" />
    </g>
  );
}
```

- [ ] **Step 3: Write `Header.tsx`**

```tsx
import { LogoMark } from './icons/LogoMark';

const navItems = [
  { href: '#what', label: 'What it is' },
  { href: '#how', label: 'How it works' },
  { href: '#use', label: 'Use cases' },
  { href: '#tech', label: 'Technology' }
];

export default function Header() {
  return (
    <header style={{
      position: 'sticky', top: 0, zIndex: 10,
      backdropFilter: 'blur(12px)',
      background: 'rgba(10,10,10,0.6)',
      borderBottom: '1px solid var(--border)'
    }}>
      <div className="container" style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '18px 32px'
      }}>
        <a href="#" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <LogoMark />
          <span style={{ fontFamily: 'var(--font-display)', fontSize: 17, fontWeight: 600 }}>
            Synthetic<span style={{ color: 'var(--ink-dim)', fontWeight: 400 }}>Pen</span>
          </span>
        </a>
        <nav style={{ display: 'flex', alignItems: 'center', gap: 28 }}>
          {navItems.map(i => (
            <a key={i.href} href={i.href} className="mono" style={{
              fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
              color: 'var(--silver)'
            }}>{i.label}</a>
          ))}
          <a href="#cta" className="mono" style={{
            fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
            color: 'var(--blue)', border: '1px solid rgba(77,163,255,0.3)',
            padding: '7px 14px', borderRadius: 2
          }}>Download</a>
        </nav>
      </div>
    </header>
  );
}
```

- [ ] **Step 4: Write `SignatureCanvas.tsx`** (simplified — single sample path, exact handoff timing)

```tsx
import { useEffect, useRef, useState } from 'react';
import { HERO_DRAW_MS, HERO_HOLD_MS } from '../lib/timing';
import { CursorArrow } from './icons/CursorArrow';

// Single placeholder cursive path. Phase 1 replaces with full handoff geometry.
const SIG_PATH = 'M 120 160 C 140 100 200 100 220 160 C 240 220 300 220 320 160 C 340 100 400 100 420 160';

export default function SignatureCanvas() {
  const pathRef = useRef<SVGPathElement | null>(null);
  const [progress, setProgress] = useState(0);
  const [cursor, setCursor] = useState({ x: 120, y: 160 });
  const [length, setLength] = useState(0);

  useEffect(() => {
    if (pathRef.current) setLength(pathRef.current.getTotalLength());
  }, []);

  useEffect(() => {
    if (!length || !pathRef.current) return;
    let raf = 0;
    const start = performance.now();
    const total = HERO_DRAW_MS + HERO_HOLD_MS;
    const tick = (now: number) => {
      const t = ((now - start) % total) / total;
      const drawT = Math.min(1, t * (total / HERO_DRAW_MS));
      const eased = drawT < 0.5
        ? 4 * drawT * drawT * drawT
        : 1 - Math.pow(-2 * drawT + 2, 3) / 2;
      setProgress(eased);
      const p = pathRef.current!.getPointAtLength(eased * length);
      setCursor({ x: p.x, y: p.y });
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [length]);

  const dashOffset = length * (1 - progress);

  return (
    <svg viewBox="0 0 820 260" width="100%" style={{ maxWidth: 960, display: 'block', margin: '52px auto 0' }}>
      <defs>
        <linearGradient id="inkGrad" x1="0" x2="1">
          <stop offset="0%" stopColor="#4DA3FF" />
          <stop offset="100%" stopColor="#6BE6FF" />
        </linearGradient>
        <filter id="inkGlow">
          <feGaussianBlur stdDeviation="3.5" result="b1" />
          <feGaussianBlur stdDeviation="8" result="b2" />
          <feMerge>
            <feMergeNode in="b1" />
            <feMergeNode in="b2" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>
      <rect x="40" y="20" width="740" height="220"
            fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="0.6" />
      <path ref={pathRef} d={SIG_PATH}
            stroke="url(#inkGrad)" strokeWidth="3.2" fill="none"
            strokeLinecap="round" filter="url(#inkGlow)"
            strokeDasharray={length} strokeDashoffset={dashOffset} opacity={0.9} />
      <path d={SIG_PATH}
            stroke="#E8F4FF" strokeWidth="1.5" fill="none"
            strokeLinecap="round"
            strokeDasharray={length} strokeDashoffset={dashOffset} />
      {progress < 1 && (
        <circle cx={cursor.x} cy={cursor.y} r="5" fill="#6BE6FF"
                style={{ filter: 'drop-shadow(0 0 12px #6BE6FF)' }} />
      )}
      <CursorArrow x={cursor.x} y={cursor.y} />
    </svg>
  );
}
```

- [ ] **Step 5: Write `Hero.tsx`**

```tsx
import SignatureCanvas from './SignatureCanvas';

export default function Hero() {
  return (
    <section style={{ padding: '56px 0 80px', position: 'relative', overflow: 'hidden' }}>
      <div className="container" style={{ textAlign: 'center' }}>
        <span className="mono" style={{
          display: 'inline-flex', alignItems: 'center', gap: 8,
          fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
          color: 'var(--blue)', background: 'rgba(77,163,255,0.08)',
          border: '1px solid rgba(77,163,255,0.2)', borderRadius: 2, padding: '6px 12px'
        }}>
          <span style={{
            width: 6, height: 6, borderRadius: '50%', background: 'var(--blue)',
            boxShadow: '0 0 8px var(--blue)'
          }} />
          v0.4 BETA · Windows 10/11
        </span>

        <h1 style={{
          fontSize: 'clamp(48px, 7.5vw, 104px)', fontWeight: 700,
          lineHeight: 0.95, letterSpacing: '-0.025em', textTransform: 'uppercase',
          margin: '24px 0 0'
        }}>
          Vector Paths<br />Into Real Motion
        </h1>

        <p style={{ maxWidth: 640, margin: '24px auto 0', color: 'var(--silver)' }}>
          Synthetic cursor & pen motion for Windows. SyntheticPen replays SVG paths as native input — like a CNC plotter for your handwriting.
        </p>

        <SignatureCanvas />

        <div style={{ display: 'flex', gap: 16, justifyContent: 'center', marginTop: 44, flexWrap: 'wrap' }}>
          <a href="#cta" style={{
            fontFamily: 'var(--font-display)', fontWeight: 600,
            background: 'linear-gradient(135deg, #4DA3FF, #6BE6FF)',
            color: '#0A0A0A', padding: '14px 22px', borderRadius: 2,
            boxShadow: '0 0 0 1px rgba(107,230,255,0.4), 0 0 24px rgba(77,163,255,0.35), 0 0 60px rgba(77,163,255,0.15)',
            textTransform: 'uppercase', letterSpacing: '0.05em', fontSize: 14
          }}>Download Beta ↘</a>
          <a href="#how" style={{
            fontFamily: 'var(--font-display)', fontWeight: 500,
            border: '1px solid var(--border-strong)', padding: '14px 22px', borderRadius: 2,
            textTransform: 'uppercase', letterSpacing: '0.05em', fontSize: 14, color: 'var(--ink)'
          }}>See How It Works</a>
        </div>
      </div>
    </section>
  );
}
```

- [ ] **Step 6: Write `Footer.tsx`**

```tsx
import { LogoMark } from './icons/LogoMark';

export default function Footer() {
  return (
    <footer style={{
      borderTop: '1px solid var(--border)',
      padding: '40px 0',
      marginTop: 40
    }}>
      <div className="container" style={{
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        gap: 20, flexWrap: 'wrap'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <LogoMark size={18} />
          <span style={{ fontFamily: 'var(--font-display)', fontSize: 14 }}>SyntheticPen</span>
        </div>
        <span className="mono" style={{
          fontSize: 11, color: 'var(--ink-dim)', letterSpacing: '0.15em', textTransform: 'uppercase'
        }}>© 2026 · Built for precision</span>
        <nav style={{ display: 'flex', gap: 20 }}>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="/docs/getting-started">Docs</a>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="https://github.com">GitHub</a>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="/privacy">Privacy</a>
        </nav>
      </div>
    </footer>
  );
}
```

- [ ] **Step 7: Build and verify**

```pwsh
cd site
npm run build
cd ..
```

Expected: `npm run build` exits 0; `site/dist/index.html` and `site/dist/privacy/index.html` exist.

- [ ] **Step 8: Smoke-run the dev server (manual, optional)**

```pwsh
cd site
npm run dev
```

Browse to `http://localhost:4321/SyntheticPen/`. Confirm: dark background with grid, sticky header, hero with animated signature path, the four section shells, CTA panel, footer. Ctrl-C to stop.

- [ ] **Step 9: Commit**

```pwsh
git add site/src/components
git commit -m "feat(site): port Header, Hero (with SignatureCanvas), Footer from handoff"
```

---

## Task 10: Docs Content Collection

**Files:**
- Create: `site/src/content/config.ts`
- Create: `site/src/content/docs/getting-started.md`, `faq.md`, `safety.md`
- Create: `site/src/pages/docs/[...slug].astro`

- [ ] **Step 1: Define the content collection**

Create `site/src/content/config.ts`:

```ts
import { defineCollection, z } from 'astro:content';

const docs = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    order: z.number().default(100)
  })
});

export const collections = { docs };
```

- [ ] **Step 2: Seed three docs**

Create `site/src/content/docs/getting-started.md`:

```markdown
---
title: Getting Started
order: 1
---

# Getting Started

SyntheticPen is currently in beta. Download the latest signed `.msix` from the [GitHub releases page](https://github.com), double-click to install, and launch from the Start menu.

The Phase 1 build covers SVG loading, motion planning, and synthetic mouse input. Pen and HID modes ship later.
```

Create `site/src/content/docs/faq.md`:

```markdown
---
title: FAQ
order: 2
---

# FAQ

**Does SyntheticPen send any data over the network?**
No. See the [privacy page](/privacy).

**Will it work in games or anti-cheat-protected applications?**
No, and that's a design constraint, not a limitation.

**Can I script it?**
Scripting is on the Phase 4 roadmap.
```

Create `site/src/content/docs/safety.md`:

```markdown
---
title: Safety
order: 3
---

# Safety

SyntheticPen is transparent about what it does: every playback is initiated by you and displays a visible indicator while running. An emergency-stop hotkey (default `Esc`) cancels playback immediately.

The app refuses to operate against credential UI surfaces or system dialogs.
```

- [ ] **Step 3: Render docs**

Create `site/src/pages/docs/[...slug].astro`:

```astro
---
import { getCollection, render } from 'astro:content';
import BaseLayout from '../../layouts/BaseLayout.astro';

export async function getStaticPaths() {
  const docs = await getCollection('docs');
  return docs.map(entry => ({
    params: { slug: entry.id },
    props: { entry }
  }));
}

const { entry } = Astro.props;
const { Content } = await render(entry);
---
<BaseLayout title={`${entry.data.title} — SyntheticPen`}>
  <main class="container" style="padding: 80px 32px; max-width: 760px;">
    <article style="color: var(--silver); line-height: 1.7;">
      <Content />
    </article>
  </main>
</BaseLayout>
```

- [ ] **Step 4: Build and verify**

```pwsh
cd site
npm run build
cd ..
```

Expected: `site/dist/docs/getting-started/index.html`, `faq/index.html`, `safety/index.html` all exist.

- [ ] **Step 5: Commit**

```pwsh
git add site/src/content site/src/pages/docs
git commit -m "feat(site): add docs content collection (getting-started, faq, safety)"
```

---

## Task 11: GitHub Actions Workflows

**Files:**
- Create: `.github/workflows/app-ci.yml`
- Create: `.github/workflows/app-package.yml`
- Create: `.github/workflows/site-deploy.yml`

- [ ] **Step 1: Write `.github/workflows/app-ci.yml`**

```yaml
name: app-ci
on:
  push:
    branches: [main]
    paths:
      - 'app/**'
      - '.github/workflows/app-ci.yml'
  pull_request:
    paths:
      - 'app/**'
      - '.github/workflows/app-ci.yml'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore app/SyntheticPen.slnx
      - name: Build
        run: dotnet build app/SyntheticPen.slnx -c Release --no-restore
      - name: Test
        run: dotnet test app/SyntheticPen.slnx -c Release --no-build --verbosity normal
```

- [ ] **Step 2: Write `.github/workflows/app-package.yml`**

```yaml
name: app-package
on:
  push:
    tags: ['v*']
  workflow_dispatch:

jobs:
  package:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - uses: microsoft/setup-msbuild@v2
      - name: Restore
        run: dotnet restore app/SyntheticPen.slnx
      - name: Build MSIX (unsigned)
        run: msbuild app/src/SyntheticPen.Package/SyntheticPen.Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxPackageSigningEnabled=false /p:UapAppxPackageBuildMode=SideloadOnly
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: SyntheticPen-MSIX-${{ github.ref_name }}
          path: app/src/SyntheticPen.Package/AppPackages/**/*.msix*
```

- [ ] **Step 3: Write `.github/workflows/site-deploy.yml`**

```yaml
name: site-deploy
on:
  push:
    branches: [main]
    paths:
      - 'site/**'
      - '.github/workflows/site-deploy.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: site/package-lock.json
      - name: Install
        working-directory: site
        run: npm ci
      - name: Build
        working-directory: site
        run: npm run build
      - uses: actions/configure-pages@v5
      - uses: actions/upload-pages-artifact@v3
        with:
          path: site/dist
  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

- [ ] **Step 4: Commit**

```pwsh
git add .github
git commit -m "ci: add app build/test, MSIX packaging, and site Pages deploy workflows"
```

---

## Task 12: Final Verification

- [ ] **Step 1: Full local build and test sweep**

```pwsh
dotnet restore app/SyntheticPen.slnx
dotnet build app/SyntheticPen.slnx -c Release --no-restore
dotnet test app/SyntheticPen.slnx -c Release --no-build
cd site; npm run build; cd ..
```

Expected:
- .NET build: 0 errors, 0 warnings (third-party warnings allowed).
- Tests: 3 passed.
- Astro build: success, `site/dist/` populated with `index.html`, `privacy/index.html`, `docs/getting-started/index.html`, etc.

- [ ] **Step 2: Confirm git status is clean**

```pwsh
git status
git log --oneline
```

Expected: clean working tree; ~9 commits visible.

- [ ] **Step 3: Tag the scaffold (optional)**

```pwsh
git tag -a scaffold-complete -m "Scaffold complete — ready for Phase 1"
```

---

## Post-scaffold first-deploy checklist (for the human)

These steps require the human and cannot be automated:

1. Create the GitHub repository (public).
2. Add the remote: `git remote add origin git@github.com:<user>/SyntheticPen.git` then `git push -u origin main`.
3. Edit `site/astro.config.mjs` — replace `<github-user>` with the actual username, commit, push.
4. Repo settings → Pages → Build and deployment → Source: **GitHub Actions**.
5. Watch the `site-deploy` workflow finish and verify the live URL.
6. Plan: kick off the Phase 1 implementation plan (`docs/superpowers/specs/` will hold the Phase 1 spec when written).
