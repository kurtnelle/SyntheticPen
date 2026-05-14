// @ts-check
import { defineConfig } from 'astro/config';
import react from '@astrojs/react';

// TODO before first deploy: replace `github-user-placeholder` with your GitHub username.
export default defineConfig({
  site: 'https://github-user-placeholder.github.io',
  base: '/SyntheticPen/',
  trailingSlash: 'ignore',
  integrations: [react()],
  build: { format: 'directory' }
});
