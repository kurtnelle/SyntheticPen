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
- .NET 10 SDK (10.0.300 or newer)
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
