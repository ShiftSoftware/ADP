import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

/** Customer-facing standalone app. Binds to `127.0.0.1:5190` in dev so the E2E
 *  harness can reach it from puppeteer. Workspace packages are aliased to `src/`
 *  so the app compiles them directly — it never needs their built `dist/`, which
 *  is why CI can build this app without a prior workspace build pass. */
export default defineConfig(({ mode }) => {
  // A production bundle with no API address can only ever render "not configured"
  // to every respondent who opens their link. Refuse to produce one — failing the
  // build is recoverable, discovering it from a customer is not. Dev builds fall
  // through to the localhost default in `src/config.ts`.
  if (mode === 'production' && !process.env.VITE_API_BASE) {
    throw new Error(
      'VITE_API_BASE is required for a production build of the survey app. ' +
        "Set it to the deployment's API root, e.g. https://<host>/api/Surveys",
    );
  }

  return {
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
  };
});
