import { defineConfig } from 'vite';
import { fileURLToPath } from 'node:url';

/** Library build. We deliberately bundle React, the renderer, and the SDK INTO
 *  the web-component output so non-React hosts can drop in a single script and
 *  get `<shift-survey>` with zero setup. This is opposite to survey-renderer's
 *  build, which externalizes peers — different audience, different tradeoffs. */
export default defineConfig({
  // React 19 references `process.env.NODE_ENV` to pick its dev vs prod branch.
  // Vite's lib-mode build doesn't replace this automatically (that's an
  // app-level concern), so the bundle would throw `process is not defined` in
  // the browser host. Statically replace it with the production shim at build.
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  build: {
    lib: {
      entry: fileURLToPath(new URL('./src/index.ts', import.meta.url)),
      formats: ['es'],
      fileName: 'index',
    },
    sourcemap: true,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
  },
});
