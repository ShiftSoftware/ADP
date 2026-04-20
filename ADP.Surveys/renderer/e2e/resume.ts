/**
 * Browser E2E — localStorage resume flow.
 *
 * Seeds a real survey + instance, opens the standalone app in a headless browser,
 * fills the first screen, advances halfway, reloads the page, and asserts that
 * the renderer restores both the current screen AND the accumulated answers from
 * localStorage. Then completes the survey and verifies the response row lands in
 * the DB (which also confirms resume state is cleared on successful submission).
 *
 * Pairs with the API-layer `run.ts` and the full-path `browser.ts` — this one
 * focuses specifically on the resume contract.
 */

import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
// eslint-disable-next-line @typescript-eslint/no-var-requires
const puppeteer = require('C:/tmp/screenshot-tool/node_modules/puppeteer-core');

import { assert, assertEq, sql, step } from './lib/util.js';
import { cleanupSeeded, seedSurvey, type SeededState } from './lib/seed.js';

const STANDALONE_BASE = 'http://127.0.0.1:5190';
const SHOT_DIR = 'C:/tmp/survey-renderer-shots/resume';
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';

let state: SeededState | undefined;

async function waitForApp() {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    try {
      const r = await fetch(STANDALONE_BASE);
      if (r.ok) return;
    } catch {
      // retry
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Standalone app not reachable at ${STANDALONE_BASE}. Start it via \`npx vite dev\` in apps/standalone.`);
}

async function ensureShotDir() {
  const fs = await import('node:fs');
  fs.mkdirSync(SHOT_DIR, { recursive: true });
}

async function main() {
  console.log('ADP Surveys E2E (resume)');
  await ensureShotDir();
  await step('standalone app is reachable', waitForApp)();
  state = await seedSurvey();

  const url = `${STANDALONE_BASE}/s/${state.publicId}`;
  console.log(`  → ${url}`);

  const browser = await puppeteer.launch({
    executablePath: CHROME,
    headless: 'new',
    args: ['--no-sandbox', '--disable-gpu'],
  });
  try {
    const page = await browser.newPage();
    await page.setViewport({ width: 900, height: 1000 });

    await step('open survey + fill partial answer set', async () => {
      await page.goto(url, { waitUntil: 'networkidle0' });
      await page.waitForSelector('h2');
      await page.type('#q-name', 'Partial E2E');
      await page.click('.survey-button--primary'); // welcome → feedback
      await new Promise((r) => setTimeout(r, 300));
      // Pick NPS 9 — promoter, no logic rule match.
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.survey-question__nps-step')) as HTMLElement[];
        const match = btns.find((b) => b.textContent?.trim() === '9');
        match?.click();
      });
      await new Promise((r) => setTimeout(r, 150));
      await page.screenshot({ path: `${SHOT_DIR}/01-before-reload.png`, fullPage: true });
    })();

    await step('localStorage now has resume state with the typed name + screen', async () => {
      const raw = await page.evaluate((k: string) => window.localStorage.getItem(k), `adp-surveys:resume:${state!.publicId}`);
      assert(raw, 'resume state exists in localStorage');
      const parsed = JSON.parse(raw!);
      assertEq(parsed.answers.name, 'Partial E2E', 'name answer persisted');
      assertEq(parsed.currentScreenId, 'feedback', 'currentScreenId is feedback');
    })();

    await step('reload preserves current screen and answers', async () => {
      await page.reload({ waitUntil: 'networkidle0' });
      await page.waitForSelector('h2');
      // Heading should still be the feedback screen, not welcome.
      const heading = await page.$eval('h2', (el: Element) => el.textContent ?? '');
      assertEq(heading, 'How was it?', 'reload landed on the feedback screen');
      await page.screenshot({ path: `${SHOT_DIR}/02-after-reload.png`, fullPage: true });
    })();

    await step('advance → brand → tap Other → auto-submit → DB row exists', async () => {
      await page.click('.survey-button--primary'); // feedback → brand (nps=9, no rule match)
      await page.waitForFunction(() => {
        const h = document.querySelector('h2');
        return h?.textContent === 'Which brand?';
      }, { timeout: 5000 });
      await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.survey-navlist__button')) as HTMLElement[];
        const other = rows.find((r) => (r.textContent ?? '').toLowerCase().includes('other'));
        other?.click();
      });
      await page.waitForSelector('.survey-root--done', { timeout: 5000 });
      await page.screenshot({ path: `${SHOT_DIR}/03-done.png`, fullPage: true });

      const countStr = sql(`
        SELECT COUNT(1) FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${state!.publicId}';
      `);
      assertEq(Number(countStr.trim()), 1, 'exactly one response row');
    })();

    await step('localStorage resume state cleared on successful submission', async () => {
      const raw = await page.evaluate((k: string) => window.localStorage.getItem(k), `adp-surveys:resume:${state!.publicId}`);
      assertEq(raw, null, 'resume key removed after done');
    })();

    console.log('\nAll assertions passed.');
  } finally {
    await browser.close();
  }
}

main()
  .then(() => cleanupSeeded(state!))
  .catch(async (err) => {
    console.error('\nRESUME E2E FAILED:', (err as Error).message ?? err);
    if (state) await cleanupSeeded(state);
    process.exit(1);
  });
