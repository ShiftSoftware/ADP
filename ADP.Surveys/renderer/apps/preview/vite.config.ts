import { defineConfig, type Connect, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';

/** Stand-in for a branch calendar, so the showcase's booking question has times to show
 *  without an Identity instance. The shape of the public calendar endpoint: the next two
 *  weeks from `from`, starting two days out, no Fridays, a few hourly slots a day. */
const calendar: Connect.NextHandleFunction = (req, res) => {
  const requested = new URL(req.url ?? '', 'http://preview').searchParams.get('from') ?? '';
  const from = /^\d{4}-\d{2}-\d{2}$/.test(requested) ? new Date(`${requested}T00:00:00Z`) : new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  const days = [];
  for (let offset = 2; offset < 16; offset++) {
    const day = new Date(Date.UTC(from.getUTCFullYear(), from.getUTCMonth(), from.getUTCDate() + offset));
    if (day.getUTCDay() === 5) continue;
    const date = `${day.getUTCFullYear()}-${pad(day.getUTCMonth() + 1)}-${pad(day.getUTCDate())}`;
    const times = [8, 9, 10, 11, 13, 14, 15].map((h) => `${date} ${pad(h % 12 || 12)}:00 ${h < 12 ? 'AM' : 'PM'}`);
    days.push({ Date: date, Times: times });
  }
  res.setHeader('Content-Type', 'application/json');
  res.end(JSON.stringify(days));
};

const mockCalendar: Plugin = {
  name: 'mock-calendar',
  configureServer: (server) => void server.middlewares.use('/mock/calendar', calendar),
  configurePreviewServer: (server) => void server.middlewares.use('/mock/calendar', calendar),
};

/** Alias the workspace packages to their `src/` entry points so the preview hot-
 *  reloads when we edit renderer code. The published package still resolves
 *  via `dist/`; this override only exists for dev ergonomics. */
export default defineConfig({
  plugins: [react(), mockCalendar],
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
