// @ts-check
import { defineConfig } from 'astro/config';
import react from '@astrojs/react';

export default defineConfig({
  site: 'https://kurtnelle.github.io',
  base: '/SyntheticPen/',
  trailingSlash: 'ignore',
  integrations: [react()],
  build: { format: 'directory' }
});
