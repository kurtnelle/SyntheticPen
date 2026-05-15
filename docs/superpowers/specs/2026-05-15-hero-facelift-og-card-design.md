# Hero Facelift + Social/OG Card — Design

**Date:** 2026-05-15
**Status:** Approved in conversation; implementing

## Goal

Two related marketing-site improvements for syntheticpen.com:

1. **Hero facelift** — replace the splines/animated-demo hero with a
   full-bleed background video, and finish retiring the remaining CNC
   fiction. Relocate the authentic `SignatureCanvas` demo to its own band
   directly after the hero.
2. **Social/OG + Twitter card** — the site currently has no Open Graph or
   Twitter meta, so link previews (WhatsApp, etc.) show only title + a thin
   generic description and no image. Add prop-driven OG/Twitter meta
   site-wide and a static branded preview image.

## Context

- Single layout `site/src/layouts/BaseLayout.astro` wraps every page
  (index, privacy, docs, 404). Head currently has only charset/viewport/
  theme-color/description/title/icon — no OG/Twitter tags.
- `site/src/pages/index.astro` → `BaseLayout` → `LandingPage.tsx`.
- Hero lives in `LandingPage.tsx` `Hero()`; uses `BackgroundSplines` and
  `SignatureCanvas` from `Signature.tsx`. `SignatureCanvas` is only used in
  the hero.
- Remaining CNC fiction (same class purged elsewhere earlier):
  - hero subhead: "like a CNC plotter for your handwriting"
  - `SignatureCanvas` HUD: `FEED 4800 mm/min`, `PLOTTING`, `STROKES 3`
- Source video: `~/Downloads/kurtnelle_humanoid_robotic_software_engineer_
  using_SyntheticP_1be3efac-...-_3.mp4` — H.264, 832×464, 24 fps, ~9 s,
  5.2 MB. AI-generated stylized clip (conscious creative choice by owner).
- Tooling available: ffmpeg 8.0.1; `sharp` 0.34.5 in `site/`
  (SVG→PNG rasterization). No ImageMagick/rsvg.
- Brand: bg `#0A0A0A`, blue `#4DA3FF`, accent cyan `#6BE6FF`; pen logomark
  (`site/public/favicon.svg`); wordmark "SyntheticPen".
- Deploy: `.github/workflows/site-deploy.yml` on push to `main` touching
  `site/**`. Release flow: commit dev → ff staging → ff main → push all
  (owner drives every commit).

## Decisions (locked with user)

1. Video = **full-bleed hero background** (muted autoplay loop, dark overlay
   for legibility, poster + reduced-motion fallback).
2. `SignatureCanvas` demo = **moved to its own band** immediately after the
   hero (not removed, not layered under the video).
3. Finish the CNC-fiction cleanup as part of this work.
4. OG image = **clean dark brand card** (logomark + wordmark + accurate
   tagline), not a robot video frame — renders crisp at thumbnail size and
   holds the honest-brand line. Swappable later.
5. Video is **re-encoded/compressed**, not shipped raw.

## Components / changes

### 1. Video assets (`site/public/`)

- `hero.mp4`: re-encode source — drop audio (`-an`), scale to ~1280 wide
  (`-vf scale=1280:-2`), H.264 high, `-crf 28`, `-preset slow`,
  `-movflags +faststart`. Target ≤ ~2 MB. (832→1280 upscale is acceptable
  for a dark, overlaid, slightly-scaled background.)
- `hero-poster.jpg`: a clean frame (≈ t=1s) via
  `ffmpeg -ss 00:00:01 -i src -frames:v 1 -q:v 3`. Used as `<video poster>`
  and the `prefers-reduced-motion` / no-autoplay still.

### 2. Hero (`LandingPage.tsx`)

- New full-bleed layer: `<video class="hero-video" autoplay muted loop
  playsinline preload="metadata" poster="/hero-poster.jpg">` with
  `<source src="/hero.mp4" type="video/mp4">`, absolutely positioned,
  `object-fit:cover`, `z-index:0`. Dark gradient overlay element above it
  (`z-index:1`) for text contrast. Content wrapper `z-index:2`.
- Remove `BackgroundSplines` from the hero.
- Keep: `Free · Windows 10/11` tag, `Vector Paths / Into Real Motion` H1,
  buttons, mono `Native Win32 input / SVG · TTF/OTF` row.
- **Rewrite subhead** (kills CNC line):
  > "Synthetic cursor & pen motion for Windows. SyntheticPen replays SVG
  > paths, signatures, and text as input the OS treats as a real pen."
- Reduced motion: CSS `@media (prefers-reduced-motion: reduce)` hides the
  video, shows the poster as a background image on the hero. Mobile/no
  autoplay is covered by the same poster.
- Styles added to `site/src/styles/global.css` (`.hero-video`,
  `.hero-overlay`, reduced-motion rule). Follow existing CSS conventions.

### 3. Demo band (`LandingPage.tsx` + `Signature.tsx`)

- New `<Demo>` section rendered immediately after `<Hero/>` in
  `LandingPage`'s `<main>`, containing the relocated `SignatureCanvas`
  inside the existing panel/container styling. `BackgroundSplines` may back
  this band.
- Clean `SignatureCanvas` HUD copy in `Signature.tsx` to match the earlier
  UseCaseDemo cleanup: `FEED 4800 mm/min` → `replay · NN%`,
  `PLOTTING` → `REPLAY`; drop the fabricated `STROKES 3` / `FEED` tokens.
  Keep the `CURSOR X/Y` readout (it is a real coordinate concept).

### 4. OG/Twitter meta (`BaseLayout.astro`)

- Add props: `image` (default `/og.png`), `path`/canonical URL.
- Emit in `<head>`: `og:type=website`, `og:site_name=SyntheticPen`,
  `og:title`, `og:description`, `og:url` (absolute, from
  `https://syntheticpen.com` + path), `og:image` (absolute),
  `og:image:width=1200`, `og:image:height=630`; `twitter:card=
  summary_large_image`, `twitter:title`, `twitter:description`,
  `twitter:image`; `<link rel="canonical">`.
- Improve the default `description` to an accurate, compelling line
  (replaces "A virtual pen plotter for Windows.").
- Per-page `title`/`description` continue to flow via existing props.

### 5. `site/public/og.png` (1200×630)

- Authored as an SVG (on-brand: `#0A0A0A` bg, subtle blue radial glow,
  pen logomark, "SyntheticPen" wordmark, one-line accurate tagline,
  small `syntheticpen.com` footer), rasterized to PNG with `sharp` via a
  one-off Node script run from `site/`. Output committed; the script is
  kept at `site/scripts/make-og.mjs` for regeneration.

## Out of scope

- Dynamic per-page OG image generation (YAGNI for ~4 pages).
- Hosting the video off-repo (a ≤2 MB asset on GitHub Pages is fine).
- Any app/installer changes.

## Verification

- `npm run build` in `site/` succeeds; `dist/` contains `hero.mp4`,
  `hero-poster.jpg`, `og.png`.
- Built `index.html` head contains `og:image`/`twitter:card` with the
  absolute `https://syntheticpen.com/og.png` URL.
- Grep built output: zero `CNC`, `4800 mm/min`, `PLOTTING`, `STROKES 3`.
- Manual: hero shows video with legible text; reduced-motion shows poster.
- Post-deploy: re-check a link unfurl shows image + improved copy.

## Risks / mitigations

- **Video too large / slow** — mitigated by re-encode + `crf 28` +
  faststart + `preload=metadata`; poster paints immediately.
- **Autoplay blocked (mobile/data-saver)** — poster image is the designed
  fallback; no functional content lives in the video.
- **OG image stale in caches** — first publish is fine; note that
  re-scrapes (LinkedIn/FB debuggers) may be needed if changed later.
- **Robot video vs honest positioning** — owner made this call explicitly;
  OG card stays brand-art to keep thumbnails credible.
