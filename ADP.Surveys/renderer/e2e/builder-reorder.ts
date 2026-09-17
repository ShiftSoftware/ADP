/**
 * ADP Dynamic Surveys — builder reorder regression.
 *
 * Seeds a survey through the API, opens it in the Blazor builder under headless
 * Chromium, reorders options / questions / screens and switches the selected
 * screen, then reads back what the editors SHOW against what the draft HOLDS.
 *
 * Guards the 2026-09-17 bug: ShiftBlazor's `LocalizedInput` reads its `Value`
 * only in `OnInitialized`, and the builder's lists were rendered without `@key`.
 * After "Move down" on option 1 the ID fields showed the new order while the
 * label fields kept the old one, and the author's next keystroke committed the
 * stale label onto the wrong option — one label silently lost. Same mechanism
 * stranded the screen inspector on the first screen's title/description no
 * matter which screen was selected.
 *
 * Prerequisites (same as browser.ts): Sample.API on :5134, puppeteer-core at
 * `C:/tmp/screenshot-tool/node_modules/puppeteer-core`, Chrome installed.
 */

import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
// eslint-disable-next-line @typescript-eslint/no-var-requires
const puppeteer = require('C:/tmp/screenshot-tool/node_modules/puppeteer-core');

import { API, PASSWORD, USERNAME, apiJson, assert, assertEq, step } from './lib/util.js';

const SHOT_DIR = 'C:/tmp/survey-renderer-shots/builder-reorder';
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';

type Page = {
  goto(url: string, opts?: unknown): Promise<unknown>;
  waitForSelector(sel: string, opts?: unknown): Promise<unknown>;
  waitForFunction(fn: () => unknown, opts?: unknown): Promise<unknown>;
  $(sel: string): Promise<Handle | null>;
  $$(sel: string): Promise<Handle[]>;
  $eval<T>(sel: string, fn: (el: Element) => T): Promise<T>;
  $$eval<T>(sel: string, fn: (els: Element[]) => T): Promise<T>;
  evaluate<T>(fn: () => T): Promise<T>;
  screenshot(opts: { path: string; fullPage?: boolean }): Promise<unknown>;
  setViewport(v: { width: number; height: number }): Promise<void>;
  keyboard: { type(text: string, opts?: unknown): Promise<void>; press(key: string): Promise<void> };
  on(event: string, handler: (arg: { message: string }) => void): void;
};
type Handle = { click(): Promise<void>; focus(): Promise<void>; $$(sel: string): Promise<Handle[]> };

const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

function draft(surveyId: string) {
  return {
    surveyId,
    version: 0,
    title: { en: 'Builder reorder regression' },
    locales: ['en'],
    defaultLocale: 'en',
    screens: [
      {
        id: 's1',
        title: { en: 'Screen One' },
        description: { en: 'Description one' },
        questions: [
          {
            type: 'singleChoice',
            id: 'color',
            title: { en: 'Favourite colour' },
            options: [
              { id: 'red', label: { en: 'Red' } },
              { id: 'green', label: { en: 'Green' } },
              { id: 'blue', label: { en: 'Blue' } },
            ],
          },
          { type: 'text', id: 'q1', title: { en: 'First question' } },
          { type: 'text', id: 'q2', title: { en: 'Second question' } },
        ],
        nextScreen: 's2',
      },
      {
        id: 's2',
        title: { en: 'Screen Two' },
        description: { en: 'Description two' },
        questions: [{ type: 'text', id: 'q3', title: { en: 'Third question' } }],
      },
    ],
    logic: [],
  };
}

/** Every option row as the editor shows it: first input = ID, third = EN label. */
function readOptionRows(page: Page) {
  return page.evaluate(() =>
    Array.from(document.querySelectorAll('.ole__row')).map((row) => {
      const values = Array.from(row.querySelectorAll('input')).map((i) => (i as HTMLInputElement).value);
      return { id: values[0] ?? '', label: values.slice(2).join('|') };
    }),
  );
}

/** ID + localized title inputs of the expanded question panel. */
function readExpandedQuestion(page: Page) {
  return page.evaluate(() => {
    const panel = document.querySelector('.mud-expand-panel.mud-panel-expanded');
    if (!panel) return null;
    const header = panel.querySelector('.ql__row-title')?.textContent?.trim() ?? '';
    const id = (panel.querySelector('.mud-expand-panel-content input') as HTMLInputElement | null)?.value ?? '';
    const title = (panel.querySelector('.mud-expand-panel-content .lsf input') as HTMLInputElement | null)?.value ?? '';
    return { header, id, title };
  });
}

/** ID / title / description inputs at the top of the screen inspector. */
function readInspector(page: Page) {
  return page.evaluate(() => {
    const root = document.querySelector('.screen-inspector');
    if (!root) return [];
    return Array.from(root.querySelectorAll('input, textarea'))
      .slice(0, 3)
      .map((el) => (el as HTMLInputElement).value);
  });
}

async function clickAction(page: Page, scope: string, title: string, nth: number) {
  const handles = await page.$$(`${scope} [title="${title}"]`);
  const handle = handles[nth];
  assert(handle, `"${title}" #${nth} under ${scope} (found ${handles.length})`);
  await handle.click();
  await sleep(400);
}

async function main() {
  console.log('ADP Surveys E2E (builder reorder)');
  const fs = await import('node:fs');
  fs.mkdirSync(SHOT_DIR, { recursive: true });

  let token = '';
  let hashId = '';
  const name = `e2e-builder-reorder-${Date.now()}`;

  await step('login + seed survey', async () => {
    const login = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: USERNAME, Password: PASSWORD },
    });
    token = login.Entity.Token;
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token,
      body: { name, draft: draft('pending') },
    });
    hashId = created.Entity.ID;
    const full = await apiJson<{ Entity: Record<string, unknown> }>('GET', `/api/Surveys/Survey/${hashId}`, { token });
    await apiJson('PUT', `/api/Surveys/Survey/${hashId}`, { token, body: { ...full.Entity, name, draft: draft(hashId) } });
  })();

  const browser = await puppeteer.launch({
    executablePath: CHROME,
    headless: 'new',
    args: ['--no-sandbox', '--disable-gpu'],
  });
  try {
    const page: Page = await browser.newPage();
    await page.setViewport({ width: 1500, height: 1000 });
    const pageErrors: string[] = [];
    page.on('pageerror', (e) => pageErrors.push(e.message));
    let shotN = 0;
    const shot = async (label: string) => {
      shotN++;
      const path = `${SHOT_DIR}/${String(shotN).padStart(2, '0')}-${label}.png`;
      await page.screenshot({ path });
      console.log(`    screenshot → ${path}`);
    };

    await step('log in to the dashboard', async () => {
      await page.goto(`${API}/`, { waitUntil: 'networkidle2' });
      await page.waitForSelector('input', { timeout: 30000 });
      const inputs = await page.$$('input');
      // keyboard.type + Tab commits MudTextField binds; input.type() does not.
      await inputs[0]!.focus();
      await page.keyboard.type(USERNAME, { delay: 30 });
      await page.keyboard.press('Tab');
      await inputs[1]!.focus();
      await page.keyboard.type(PASSWORD, { delay: 30 });
      await page.keyboard.press('Enter');
      await page.waitForFunction(() => !!localStorage.getItem('token'), { timeout: 20000 });
    })();

    await step('open the survey in Edit mode, select screen 1', async () => {
      await page.goto(`${API}/Surveys/SurveyForm/${hashId}`, { waitUntil: 'networkidle2' });
      await page.waitForSelector('.screen-list__row', { timeout: 30000 });
      await sleep(600);
      const edit = await page.$('button[aria-label="Edit"]');
      assert(edit, 'Edit pencil present (existing records open in View mode)');
      await edit.click();
      await sleep(600);
      await (await page.$$('.screen-list__row'))[0]!.click();
      await page.waitForSelector('.ql__row', { timeout: 15000 });
      await shot('edit-mode');
    })();

    await step('option reorder: labels follow their IDs', async () => {
      await (await page.$$('.ql__row'))[0]!.click(); // expand "color"
      await page.waitForSelector('.ole__row', { timeout: 10000 });
      // Let the expansion panel finish sliding open — a click that lands while the
      // rows are still moving hits whatever is under the cursor by then.
      await sleep(600);
      assertEq(
        JSON.stringify(await readOptionRows(page)),
        JSON.stringify([
          { id: 'red', label: 'Red' },
          { id: 'green', label: 'Green' },
          { id: 'blue', label: 'Blue' },
        ]),
        'options render in authored order',
      );
      await clickAction(page, '.ole__row-actions', 'Move down', 0);
      await shot('options-after-move');
      assertEq(
        JSON.stringify(await readOptionRows(page)),
        JSON.stringify([
          { id: 'green', label: 'Green' },
          { id: 'red', label: 'Red' },
          { id: 'blue', label: 'Blue' },
        ]),
        'after Move down, every row still shows its own label',
      );
    })();

    await step('option reorder: the next edit lands on the right option', async () => {
      const row1 = (await page.$$('.ole__row'))[0]!;
      const label = (await row1.$$('input'))[2]!;
      await label.focus();
      await page.keyboard.press('End');
      await page.keyboard.type(' X', { delay: 30 });
      await page.keyboard.press('Tab');
      await sleep(400);
      // The header's view switch is icon-only; the accessible name is the handle.
      await (await page.$('button[aria-label="Raw JSON"]'))!.click();
      await sleep(600);
      const rawJson = await page.$eval('.survey-form__editor-textarea', (t) => (t as HTMLTextAreaElement).value);
      const parsed = JSON.parse(rawJson) as { screens: Array<{ questions: Array<{ id: string; options?: Array<{ id: string; label: { en: string } }> }> }> };
      const options = parsed.screens[0]!.questions.find((q) => q.id === 'color')!.options!;
      assertEq(
        options.map((o) => `${o.id}=${o.label.en}`).join(','),
        'green=Green X,red=Red,blue=Blue',
        'draft holds the edit on "green" and every other label intact',
      );
      await (await page.$('button[aria-label="Visual editor"]'))!.click();
      await sleep(600);
      await page.waitForSelector('.ql__row', { timeout: 10000 });
    })();

    await step('question reorder: the expanded inspector follows its question', async () => {
      await (await page.$$('.ql__row'))[1]!.click(); // expand q1
      await sleep(500);
      const before = await readExpandedQuestion(page);
      assertEq(before?.id, 'q1', 'q1 expanded');
      assertEq(before?.title, 'First question', 'q1 title field');
      await clickAction(page, '.ql__row-actions', 'Move down', 1);
      await shot('questions-after-move');
      const headers = await page.$$eval('.ql__row-title', (els) => els.map((e) => e.textContent?.trim()));
      assertEq(headers.join(','), 'Favourite colour,Second question,First question', 'list order updated');
      const after = await readExpandedQuestion(page);
      assertEq(after?.id, 'q1', 'the same question is still the expanded one');
      assertEq(after?.title, 'First question', 'its title field still shows its own title');
    })();

    await step('screen selection: inspector shows the selected screen', async () => {
      await (await page.$$('.screen-list__row'))[1]!.click();
      await sleep(600);
      assertEq(JSON.stringify(await readInspector(page)), JSON.stringify(['s2', 'Screen Two', 'Description two']), 'screen 2 fields');
      await shot('screen-two-selected');
    })();

    await step('screen reorder: selection follows the moved screen', async () => {
      await clickAction(page, '.screen-list', 'Move up', 1);
      const labels = await page.$$eval('.screen-list__row', (els) =>
        els.map((e) => e.textContent?.replace(/\s+/g, ' ').trim()),
      );
      assert(labels[0]!.includes('s2') && labels[1]!.includes('s1'), `screens reordered (${labels.join(' | ')})`);
      assertEq(JSON.stringify(await readInspector(page)), JSON.stringify(['s2', 'Screen Two', 'Description two']), 'inspector still on s2');
      await shot('screen-after-move');
    })();

    await step('no page errors', () => {
      assertEq(pageErrors.length, 0, `page errors: ${pageErrors.join('; ')}`);
    })();
  } finally {
    await browser.close();
    console.log('\nCleanup:');
    try {
      await apiJson('DELETE', `/api/Surveys/Survey/${hashId}`, { token });
      console.log(`  • deleted survey ${hashId}`);
    } catch (e) {
      console.log(`  ! could not delete survey: ${(e as Error).message}`);
    }
  }
  console.log('\nAll builder reorder checks passed.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
