// @ts-check
import { defineConfig } from 'astro/config';
import react from '@astrojs/react';

export default defineConfig({
  // Served from the apex custom domain (public/CNAME) so the site lives at
  // the root — no project-path base.
  site: 'https://syntheticpen.com',
  trailingSlash: 'ignore',
  integrations: [react()],
  build: { format: 'directory' }
});
