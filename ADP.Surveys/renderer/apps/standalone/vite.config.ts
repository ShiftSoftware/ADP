import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

/** Customer-facing standalone app. Binds to `0.0.0.0:5190` in dev so the E2E
 *  harness can reach it from puppeteer. Workspace packages are aliased to `src/`
 *  for dev so hot-reload works; published builds still resolve through `dist/`. */
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
    },
  },
  server: {
    port: 5190,
    strictPort: true,
    host: '127.0.0.1',
  },
});
