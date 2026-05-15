# GitHub MSI Release Channel — Design

**Date:** 2026-05-15
**Status:** Proposed (awaiting user review)

## Goal

Provide a parallel, no-cost distribution channel for SyntheticPen so users who
cannot or will not install from the Microsoft Store can still install it: a
single Windows Installer (`.msi`) per architecture, published as a GitHub
Release. The Microsoft Store remains the primary channel.

## Context

- App: `app/src/SyntheticPen.App` — Avalonia, .NET 10, Windows desktop.
- `Directory.Build.props` already sets `RuntimeIdentifiers=win-x64;win-arm64`.
- `app-ci.yml` already proves `dotnet build/test` of the app works on a
  hosted `windows-latest` runner. The wapproj/MSIX path is fragile (only the
  maintainer's VS 2026 box builds it reliably) and is intentionally avoided
  here.
- `app-package.yml` exists but only uploads an unsigned x64 MSIX as an
  ephemeral workflow artifact (never run, no Release). It will be removed.
- The marketing site's `DOWNLOAD_URL` already points at
  `https://github.com/kurtnelle/SyntheticPen/releases`. No site change needed.

## Decisions (locked with user)

1. Artifact: unsigned `.msi` installer, one per architecture, attached to a
   GitHub Release. Not zip, not MSIX.
2. Build tooling: WiX Toolset v5 via the `wix` .NET CLI tool.
3. Automation: GitHub Actions, recommended approach (CI-built, reproducible).
4. Architectures: **x64 and arm64** (two MSIs per release).
5. Trigger: push of a `v*` tag **and** manual `workflow_dispatch`.

## Non-goals / out of scope

- Code signing. MSIs ship unsigned; first launch shows SmartScreen + a UAC
  elevation prompt. Documented in release notes, not engineered around.
- Auto-update. The MSI has a stable UpgradeCode so a newer MSI cleanly
  upgrades an older install, but there is no in-app updater.
- Microsoft Store packaging. Untouched; remains the local VS 2026 build.
- Site changes. `DOWNLOAD_URL` already targets the releases page.

## Architecture / components

Three units, each independently understandable:

### 1. Publish step (input producer)

`dotnet publish app/src/SyntheticPen.App/SyntheticPen.App.csproj`
`-c Release -r <rid> --self-contained true` for `rid ∈ {win-x64, win-arm64}`.

- Self-contained (no .NET prerequisite for end users).
- **Not** single-file (`PublishSingleFile` left off) — a plain folder of
  files is the most robust input for WiX harvesting and avoids Avalonia/Skia
  native-extraction edge cases. The MSI installs the whole publish folder.
- Output: `app/src/SyntheticPen.App/bin/Release/net10.0/<rid>/publish/`.
- Main executable: `SyntheticPen.App.exe`.

### 2. WiX authoring (`installer/SyntheticPen.wxs` + `installer/SyntheticPen.wixproj`)

One MSI per architecture. Authoring is parameterized by `-arch` and by
`ProductVersion` / `PublishDir` build properties.

- **Package:** Name `SyntheticPen`, Manufacturer `kurtnelle`,
  `Version=$(ProductVersion)`, `Scope=perMachine`.
- **UpgradeCode (stable, per arch — never change):**
  - x64: `3F7C1A92-8D4E-4B6A-9E21-5C0FA7B3D284`
  - arm64: `A1E9D60B-2C7F-4538-8B14-6D3A0F9C7E52`
  - Separate codes per arch so cross-arch installs don't fight; same code
    across versions so `0.1.0 → 0.1.1` is a `MajorUpgrade` (downgrade
    blocked with a clear message).
- **Install dir:** `[ProgramFiles64Folder]\SyntheticPen` (both x64 and
  native arm64 are 64-bit).
- **Files:** WiX v5 auto-harvest of the publish folder
  (`<Files Include="$(PublishDir)\**" />`).
- **Shortcut:** Start-menu shortcut "SyntheticPen" → `SyntheticPen.App.exe`.
- **ARP/uninstall:** standard Add/Remove Programs entry; icon from the app
  `.ico`; `ARPNOMODIFY=yes`.
- UI: WixUI minimal (license-less) or no UI (silent-capable). Default:
  `WixUI_Minimal` so a user sees a normal install wizard.

### 3. Release workflow (`.github/workflows/app-release.yml`)

Replaces the removed `app-package.yml`.

```
on:
  push: { tags: ['v*'] }
  workflow_dispatch:
    inputs:
      version: { description: 'Version for manual builds (e.g. 0.1.0)', required: false }
permissions:
  contents: write          # needed to create the Release
jobs:
  release (runs-on: windows-latest):
    - actions/checkout@v5
    - actions/setup-dotnet@v5 (global-json-file: app/global.json)
    - Resolve VERSION:
        tag build  -> github.ref_name without leading 'v'
        manual     -> inputs.version, else fallback '0.0.0'
    - dotnet tool install --global wix (pin major v5)
    - For rid in win-x64, win-arm64:
        dotnet publish ... -r $rid --self-contained
        wix build installer/SyntheticPen.wxs
          -arch (x64|arm64)
          -d ProductVersion=$VERSION
          -d PublishDir=<publish path for $rid>
          -o SyntheticPen-$VERSION-<x64|arm64>.msi
    - softprops/action-gh-release (or `gh release create`):
        tag = github.ref_name (tag build) or v$VERSION (manual)
        files = both .msi
        body = release-notes template (see below)
        prerelease = false
```

Release-notes body template (committed in the workflow):

> SyntheticPen <version> — free.
> Primary install is the Microsoft Store. These MSIs are an unsigned
> fallback: on first run Windows shows "Windows protected your PC"
> (click **More info → Run anyway**) and a **UAC** prompt to install.
> `SyntheticPen-<v>-x64.msi` for Intel/AMD, `-arm64.msi` for Arm64 PCs.

## Versioning

- Tag `vX.Y.Z` → MSI `ProductVersion = X.Y.Z`.
- Keep tag versions aligned with the app/wapproj version (`0.1.0.0` today →
  first tag `v0.1.0`). Bumping the app version and tagging is a manual,
  separate step (not in scope to automate here).
- Manual `workflow_dispatch` without a version input → `0.0.0` and a
  prerelease-style draft is acceptable for testing (workflow still attaches
  artifacts; tag `v0.0.0` is overwritten/cleaned by maintainer if needed).

## Files added / changed / removed

- **Add:** `installer/SyntheticPen.wxs` only. Built directly with the
  `wix build` CLI (chosen path) — **no `.wixproj`**.
- **Add:** `.github/workflows/app-release.yml`.
- **Remove:** `.github/workflows/app-package.yml` (dead MSIX-artifact
  workflow).
- **Unchanged:** `app-ci.yml`, `site-deploy.yml`, the wapproj/Store path,
  the site.

## Testing / verification

- Workflow dry-run via `workflow_dispatch` with `version=0.0.0` on a branch;
  confirm both MSIs build and a (draft/test) Release is produced.
- Install verification on a clean Windows VM/host: `msiexec /i` the x64 MSI →
  app launches, Start-menu shortcut works, ARP entry present, uninstall
  removes it. Repeat conceptually for arm64 if hardware available (else rely
  on identical authoring).
- Upgrade verification: install `0.0.0`, then install a higher version →
  in-place upgrade, single ARP entry, no duplicate install.
- `gh release view <tag>` shows both `.msi` assets.

## Risks / mitigations

- **WiX v5 auto-harvest path correctness** — publish output path differs by
  RID; mitigated by passing `PublishDir` explicitly per arch.
- **arm64 WiX support** — WiX v5 supports `-arch arm64`; if a runner toolset
  gap appears, x64 still ships and arm64 is iterated separately (does not
  block the channel).
- **Unsigned SmartScreen friction** — accepted and documented; out of scope
  to solve without a signing identity (Azure Trusted Signing was ruled out;
  no other cert available).
- **`contents: write` permission** — scoped to this workflow only; uses the
  built-in `GITHUB_TOKEN`, no PAT.
