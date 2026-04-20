/**
 * ADP Dynamic Surveys — browser end-to-end smoke.
 *
 * Seeds a real survey + instance through the same helpers as `run.ts`, then
 * drives the standalone React app in a headless Chromium to walk the promoter
 * path screen-by-screen. Verifies that the renderer, SDK, API, and DB agree —
 * the canary for anything that only breaks in a real browser (CSS missing,
 * fetch binding, postMessage, React strict-mode side-effects).
 *
 * Prerequisites (same as run.ts, plus):
 *   - Standalone app running on http://127.0.0.1:5190 (started separately via
 *     `npx vite dev` in `apps/standalone`, or started by you before invocation).
 *   - puppeteer-core available at `C:/tmp/screenshot-tool/node_modules/puppeteer-core`
 *     (same harness the web-components work uses).
 */

// Resolve puppeteer-core through an absolute path rather than a workspace dep so
// this script has zero install cost; harness is shared with the web-components work.
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
// eslint-disable-next-line @typescript-eslint/no-var-requires
const puppeteer = require('C:/tmp/screenshot-tool/node_modules/puppeteer-core');

import { assert, assertEq, sql, step } from './lib/util.js';
import { cleanupSeeded, seedSurvey, type SeededState } from './lib/seed.js';

const STANDALONE_BASE = 'http://127.0.0.1:5190';
const SHOT_DIR = 'C:/tmp/survey-renderer-shots/browser';
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';

let state: SeededState | undefined;

async function waitForApp() {
  // The standalone dev server must already be up. We don't start it here to keep
  // the harness's process lifetime clear — the caller owns Vite.
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
  throw new Error(`Standalone app not reachable at ${STANDALONE_BASE}. Start it with \`npx vite dev\` in apps/standalone.`);
}

async function ensureShotDir() {
  const fs = await import('node:fs');
  fs.mkdirSync(SHOT_DIR, { recursive: true });
}

async function main() {
  console.log(`ADP Surveys E2E (browser)`);
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
    const consoleErrors: string[] = [];
    page.on('pageerror', (e: Error) => consoleErrors.push(`pageerror: ${e.message}`));
    page.on('console', (msg: { type: () => string; text: () => string }) => {
      if (msg.type() !== 'error') return;
      const text = msg.text();
      // Ignore favicon / other auto-requested-resource 404s — noisy and not a signal.
      if (text.includes('404') && text.toLowerCase().includes('resource')) return;
      consoleErrors.push(`console.error: ${text}`);
    });

    const shot = async (name: string) => {
      const path = `${SHOT_DIR}/${name}.png`;
      await page.screenshot({ path, fullPage: true });
      console.log(`    screenshot → ${path}`);
    };

    await step('open survey URL', async () => {
      await page.goto(url, { waitUntil: 'networkidle0' });
      await new Promise((r) => setTimeout(r, 400));
      // Welcome is the first screen.
      await page.waitForSelector('h2', { timeout: 5000 });
      const heading = await page.$eval('h2', (el: Element) => el.textContent ?? '');
      assertEq(heading, 'Welcome', 'first screen is Welcome');
      await shot('01-welcome');
    })();

    await step('fill name, click Next → feedback', async () => {
      await page.type('#q-name', 'Browser E2E');
      await page.click('.survey-button--primary');
      await new Promise((r) => setTimeout(r, 300));
      const heading = await page.$eval('h2', (el: Element) => el.textContent ?? '');
      assertEq(heading, 'How was it?', 'advanced to feedback screen');
      await shot('02-feedback');
    })();

    await step('pick NPS 10 → promoter', async () => {
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('.survey-question__nps-step')) as HTMLElement[];
        const match = btns.find((b) => b.textContent?.trim() === '10');
        match?.click();
      });
      await new Promise((r) => setTimeout(r, 150));
      await shot('03-feedback-nps-10');
    })();

    await step('Next → brand (promoter falls through screen.nextScreen)', async () => {
      await page.click('.survey-button--primary');
      await new Promise((r) => setTimeout(r, 300));
      const heading = await page.$eval('h2', (el: Element) => el.textContent ?? '');
      assertEq(heading, 'Which brand?', 'landed on brand screen');
      // navigationList-terminal screen hides the Next button per Phase 3 Part B.1.
      const nextCount = await page.$$eval('.survey-button--primary', (els: Element[]) => els.length);
      assertEq(nextCount, 0, 'Next button is hidden on navigationList-terminal screens');
      await shot('04-brand');
    })();

    await step('tap Toyota → thanks-toyota → auto-submit', async () => {
      await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.survey-navlist__button')) as HTMLElement[];
        const toyota = rows.find((r) => (r.textContent ?? '').toLowerCase().includes('toyota'));
        toyota?.click();
      });
      // Zero-question terminal screen auto-submits on arrival. Wait for the
      // submit fetch to resolve and the `done` state to render.
      await page.waitForSelector('.survey-root--done', { timeout: 5000 });
      const heading = await page.$eval('h2', (el: Element) => el.textContent ?? '');
      assertEq(heading, 'Thanks — Toyota fan!', 'done state renders thanks-toyota');
      // Auto-submit means no more Next button on the done screen.
      const nextCount = await page.$$eval('.survey-button--primary', (els: Element[]) => els.length);
      assertEq(nextCount, 0, 'no Next button in the done state');
      await shot('05-thanks-toyota');
    })();

    await step('verify response row in DB', () => {
      const countStr = sql(`
        SELECT COUNT(1) FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${state!.publicId}';
      `);
      assertEq(Number(countStr.trim()), 1, 'exactly one response row');
    })();

    await step('verify banked answer carries BankEntryID', () => {
      const bankedRow = sql(`
        SELECT a.KeyAtSubmission, CASE WHEN a.BankEntryID IS NULL THEN 'NULL' ELSE 'NOTNULL' END
        FROM [Surveys].[SurveyAnswer] a
        JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${state!.publicId}' AND a.KeyAtSubmission = '${state!.bankKey}';
      `);
      const [key, bankState] = bankedRow.split('\t');
      assertEq(key?.trim(), state!.bankKey, 'banked answer key round-trips');
      assertEq(bankState?.trim(), 'NOTNULL', 'Decision #11 BankEntryID present');
    })();

    await step('no console errors during the walk', () => {
      if (consoleErrors.length > 0) {
        throw new Error(`console errors during walk:\n${consoleErrors.join('\n')}`);
      }
    })();

    console.log('\nAll assertions passed.');
  } finally {
    await browser.close();
  }
}

main()
  .then(() => cleanupSeeded(state!))
  .catch(async (err) => {
    console.error('\nBROWSER E2E FAILED:', (err as Error).message ?? err);
    if (state) await cleanupSeeded(state);
    process.exit(1);
  });
