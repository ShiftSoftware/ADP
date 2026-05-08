import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: fileURLToPath(new URL('./src/index.ts', import.meta.url)),
      formats: ['es'],
      fileName: 'index',
    },
    rollupOptions: {
      external: ['react', 'react-dom', 'react/jsx-runtime', '@shiftsoftware/survey-sdk'],
    },
    sourcemap: true,
  },
  test: {
    environment: 'jsdom',
    // React Testing Library relies on vitest globals so it can register
    // its automatic afterEach cleanup — without them, renders leak across tests.
    globals: true,
    setupFiles: ['./tests/setup.ts'],
  },
});
