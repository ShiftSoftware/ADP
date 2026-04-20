import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

/** Alias the workspace packages to their `src/` entry points so the preview hot-
 *  reloads when we edit renderer code. The published package still resolves
 *  via `dist/`; this override only exists for dev ergonomics. */
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@shiftsoftware/survey-renderer/styles.css': fileURLToPath(
        new URL('../../packages/survey-renderer/src/styles.css', import.meta.url),
      ),
      '@shiftsoftware/survey-renderer': fileURLToPath(
        new URL('../../packages/survey-renderer/src/index.ts', import.meta.url),
      ),
      '@shiftsoftware/survey-sdk': fileURLToPath(
        new URL('../../packages/survey-sdk/src/index.ts', import.meta.url),
      ),
      '@shiftsoftware/survey-web-component': fileURLToPath(
        new URL('../../packages/survey-web-component/src/index.ts', import.meta.url),
      ),
    },
  },
  server: {
    port: 5180,
    strictPort: true,
    host: '127.0.0.1',
  },
});
