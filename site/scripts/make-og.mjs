// Regenerates site/public/og.png — the 1200x630 social/Open Graph card.
// On-brand dark card: pen logomark + wordmark + accurate tagline.
// Run from site/:  node scripts/make-og.mjs
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const out = join(dirname(fileURLToPath(import.meta.url)), '..', 'public', 'og.png');

// Pen logomark (favicon paths, 32x32 viewBox) scaled 3.4x and centered.
const S = 3.4;
const markW = 32 * S;
const markX = 600 - markW / 2;
const markY = 120;

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
  <defs>
    <radialGradient id="glow" cx="50%" cy="100%" r="80%">
      <stop offset="0%" stop-color="#4DA3FF" stop-opacity="0.22"/>
      <stop offset="55%" stop-color="#4DA3FF" stop-opacity="0.05"/>
      <stop offset="100%" stop-color="#4DA3FF" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect width="1200" height="630" fill="#0A0A0A"/>
  <rect width="1200" height="630" fill="url(#glow)"/>
  <rect x="0" y="0" width="1200" height="630" fill="none" stroke="#1c1c1c" stroke-width="2"/>

  <g transform="translate(${markX} ${markY}) scale(${S})">
    <path d="M6 26 L20 6 L26 12 L12 26 L6 26 Z" fill="#4DA3FF"/>
    <path d="M9 23 L20 12 L24 14 L13 25 Z" fill="#6BE6FF"/>
  </g>

  <text x="600" y="335" text-anchor="middle"
        font-family="Segoe UI, Arial, Helvetica, sans-serif"
        font-size="86" font-weight="800" letter-spacing="-1">
    <tspan fill="#FFFFFF">Synthetic</tspan><tspan fill="#8A8A8A">Pen</tspan>
  </text>

  <text x="600" y="395" text-anchor="middle"
        font-family="Segoe UI, Arial, Helvetica, sans-serif"
        font-size="29" fill="#B8B8B8">
    Replay SVG paths, signatures &amp; text as real Windows pen input.
  </text>

  <text x="80" y="565" text-anchor="start"
        font-family="Consolas, monospace" font-size="22" fill="#6E6E6E"
        letter-spacing="2">syntheticpen.com</text>

  <text x="1120" y="565" text-anchor="end"
        font-family="Consolas, monospace" font-size="22" fill="#9A9A9A"
        letter-spacing="2">FREE · WINDOWS 10/11</text>
</svg>`;

await sharp(Buffer.from(svg)).png().toFile(out);
console.log('wrote', out);
