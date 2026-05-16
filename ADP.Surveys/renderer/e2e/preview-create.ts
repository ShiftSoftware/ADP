/**
 * ADP Dynamic Surveys — Create-mode preview reproducer.
 *
 * Drives Sample.API's BUILDER (Blazor WASM at :5134, not the standalone renderer)
 * to reproduce the live-preview-not-updating bug reported in Create mode:
 *
 *   1. Log in as SuperUser
 *   2. Navigate to the survey form in Create mode
 *   3. Switch to the Visual tab
 *   4. Click "Add screen"
 *   5. Probe the <shift-survey> element's `.schema` property + the preview placeholder
 *   6. Screenshot before + after for visual diff
 *   7. Dump all console / pageerror messages
 *
 * Pure repro — no assertions yet. Once we understand the actual failure mode
 * this script becomes the canonical assertion-based harness for builder previews.
 *
 * Run with `npm run preview-create` from this directory. Headed by default so
 * an operator can watch.
 */

import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
// eslint-disable-next-line @typescript-eslint/no-var-requires
const puppeteer = require('C:/tmp/screenshot-tool/node_modules/puppeteer-core');

const BUILDER_BASE = 'http://localhost:5134';
const SHOT_DIR = 'C:/tmp/survey-renderer-shots/builder';
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';

const USERNAME = 'SuperUser';
const PASSWORD = 'OneTwo';

async function ensureShotDir() {
  const fs = await import('node:fs');
  fs.mkdirSync(SHOT_DIR, { recursive: true });
}

async function waitForBuilder() {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    try {
      const r = await fetch(BUILDER_BASE);
      if (r.ok) return;
    } catch {
      /* retry */
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Builder not reachable at ${BUILDER_BASE}. Start with \`dotnet run\` in samples/ADP.Surveys.Sample.API.`);
}

async function main() {
  console.log('ADP Surveys preview-create repro');
  await ensureShotDir();
  await waitForBuilder();

  const browser = await puppeteer.launch({
    executablePath: CHROME,
    headless: false,
    args: ['--no-sandbox', '--disable-gpu', '--window-size=1400,1000'],
    defaultViewport: { width: 1400, height: 1000 },
  });

  try {
    const page = await browser.newPage();

    const consoleLog: string[] = [];
    page.on('pageerror', (e: Error) => consoleLog.push(`pageerror: ${e.message}`));
    page.on('console', (msg: { type: () => string; text: () => string }) => {
      consoleLog.push(`console.${msg.type()}: ${msg.text()}`);
    });
    page.on('requestfailed', (req: { url: () => string; failure: () => { errorText: string } | null }) => {
      consoleLog.push(`requestfailed: ${req.url()} - ${req.failure()?.errorText ?? 'unknown'}`);
    });

    const shot = async (name: string) => {
      const path = `${SHOT_DIR}/${name}.png`;
      await page.screenshot({ path, fullPage: false });
      console.log(`  screenshot → ${path}`);
    };

    const probe = async (label: string) => {
      const result = await page.evaluate(() => {
        const el = document.querySelector('shift-survey') as HTMLElement & { schema?: unknown };
        const placeholder = document.querySelector('.survey-preview__placeholder');
        const surveyRoot = el?.shadowRoot?.querySelector('.survey-root');
        return {
          schemaSet: el ? !!el.schema : false,
          screenCount: el && el.schema && typeof el.schema === 'object'
            ? Array.isArray((el.schema as { screens?: unknown[] }).screens)
              ? (el.schema as { screens: unknown[] }).screens.length
              : 'no-screens'
            : 'no-schema',
          placeholderShown: !!placeholder,
          surveyRootText: (surveyRoot?.textContent || '').replace(/\s+/g, ' ').trim().slice(0, 200),
          surveyRootClass: surveyRoot?.className || '',
        };
      });
      console.log(`  [${label}]`, JSON.stringify(result));
      return result;
    };

    const dumpDomSummary = async (label: string) => {
      const summary = await page.evaluate(() => {
        const all = Array.from(document.querySelectorAll('input, button, h1, h2, h3'));
        return all.slice(0, 25).map((el) => {
          const tag = el.tagName.toLowerCase();
          const id = el.id ? `#${el.id}` : '';
          const cls = (typeof el.className === 'string' && el.className)
            ? '.' + el.className.split(/\s+/).filter(Boolean).slice(0, 2).join('.')
            : '';
          const text = (el.textContent || '').trim().slice(0, 40);
          const type = el.tagName === 'INPUT' ? ` [type=${(el as HTMLInputElement).type}]` : '';
          return `${tag}${id}${cls}${type}${text ? ` "${text}"` : ''}`;
        }).join('\n');
      });
      console.log(`\n  DOM summary [${label}]:\n${summary}\n`);
    };

    // -------- Step 1: open builder, capture initial state --------
    console.log('\n[1] navigate to builder root');
    // Clear cookies + storage so a stale refresh token from a previous run
    // doesn't suppress the fresh-login claims (e.g. IsSuperAdmin updates).
    const client = await page.target().createCDPSession();
    await client.send('Network.clearBrowserCookies');
    await client.send('Network.clearBrowserCache');
    await page.goto(BUILDER_BASE, { waitUntil: 'networkidle2', timeout: 30_000 });
    await page.evaluate(() => {
      try { localStorage.clear(); sessionStorage.clear(); } catch { /* about:blank */ }
    });
    await page.goto(BUILDER_BASE, { waitUntil: 'networkidle2', timeout: 30_000 });
    // Blazor WASM boot is async; wait for the loading shell to clear.
    await page.waitForFunction(
      () => !document.querySelector('#app')?.textContent?.trim()?.startsWith('Loading'),
      { timeout: 30_000 }
    );
    await new Promise((r) => setTimeout(r, 1500));
    await shot('00-after-boot');
    await dumpDomSummary('after boot');

    // -------- Step 2: log in --------
    console.log('\n[2] attempt login');
    // ShiftIdentity dashboard usually exposes Username / Password inputs.
    // Try common selectors; fall back to "first two inputs in the form".
    // Type via Puppeteer directly — MudBlazor wires its bindings through React-style
    // input events that the native setter trick is fiddly with, and `page.type`
    // dispatches the same keystroke events the real user would.
    const userInputHandle = await page.$('input[type="text"]');
    const passInputHandle = await page.$('input[type="password"]');
    if (!userInputHandle || !passInputHandle) {
      throw new Error('login: text/password input not found');
    }
    await userInputHandle.click({ clickCount: 3 });
    await userInputHandle.type(USERNAME, { delay: 30 });
    await passInputHandle.click({ clickCount: 3 });
    await passInputHandle.type(PASSWORD, { delay: 30 });

    // Capture login network response so we know whether the credentials were
    // actually accepted. ShiftIdentity dashboard POSTs to /Account/Login or similar.
    const loginResponsePromise = page.waitForResponse(
      (resp: { url: () => string; status: () => number; request: () => { method: () => string } }) =>
        /Account|Identity|[Ll]ogin|[Tt]oken/.test(resp.url()) && resp.request().method() === 'POST',
      { timeout: 10_000 },
    ).catch(() => null);

    // Press Enter — Blazor MudForms typically submit on Enter; the button click
    // sometimes loses focus before the bound-value change commits.
    await passInputHandle.press('Enter');
    const loginResponse = await loginResponsePromise;
    const loginResult = loginResponse
      ? {
          ok: loginResponse.status() < 400,
          status: loginResponse.status(),
          url: loginResponse.url(),
        }
      : { ok: false, reason: 'no auth POST captured in 10s' };
    console.log(`  login: ${JSON.stringify(loginResult)}`);
    await new Promise((r) => setTimeout(r, 3000));
    await shot('01-after-login');

    // Capture localStorage token for claim inspection.
    const storedToken = await page.evaluate(() => {
      const out: Record<string, string> = {};
      for (let i = 0; i < localStorage.length; i++) {
        const k = localStorage.key(i)!;
        const v = localStorage.getItem(k) ?? '';
        if (v.length > 50) out[k] = v.slice(0, 300) + (v.length > 300 ? '...' : '');
        else out[k] = v;
      }
      return out;
    });
    console.log('  localStorage keys:', Object.keys(storedToken).join(', '));
    for (const [k, v] of Object.entries(storedToken)) {
      const tokenMatch = v.match(/eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/);
      if (tokenMatch) {
        const middle = tokenMatch[0].split('.')[1];
        const padded = middle + '='.repeat((4 - middle.length % 4) % 4);
        try {
          const claims = JSON.parse(Buffer.from(padded, 'base64').toString('utf-8'));
          console.log(`  [${k}] decoded claims:`, JSON.stringify(claims, null, 2));
        } catch { /* not a real JWT */ }
      }
    }

    // -------- Step 3: navigate to Create form --------
    console.log('\n[3] navigate to /Surveys/SurveyForm (Create mode)');
    await page.goto(`${BUILDER_BASE}/Surveys/SurveyForm`, { waitUntil: 'networkidle2', timeout: 30_000 });
    await new Promise((r) => setTimeout(r, 2000));
    await shot('02-create-form-loaded');

    // -------- Step 4: probe preview state BEFORE adding screen --------
    console.log('\n[4] probe initial preview state');
    const beforeAdd = await page.evaluate(() => {
      const el = document.querySelector('shift-survey') as HTMLElement & { schema?: unknown };
      const placeholder = document.querySelector('.survey-preview__placeholder');
      return {
        shiftSurveyExists: !!el,
        schemaPropType: el ? typeof el.schema : 'no-element',
        schemaIsTruthy: el ? !!el.schema : false,
        placeholderText: placeholder?.textContent?.trim() ?? null,
        customElementDefined: !!customElements.get('shift-survey'),
      };
    });
    console.log('  before add:', JSON.stringify(beforeAdd, null, 2));

    // -------- Step 4b: set English locale on the General tab (reproduces user's path) --------
    console.log('\n[4b] set English locale on General tab');
    // Use real puppeteer mouse events — MudSelect's activator wants pointer events,
    // not synthetic .click() calls.
    const localesActivator = await page.$('div.mud-select .mud-input-control');
    if (!localesActivator) throw new Error('locales activator not found');
    await localesActivator.click();
    await new Promise((r) => setTimeout(r, 700));
    await shot('02ba-locales-open');

    // Pick the English option. Wait for the menu to render.
    const englishHandle = await page.evaluateHandle(() => {
      const items = Array.from(document.querySelectorAll('.mud-list-item, [role="option"]')) as HTMLElement[];
      return items.find((el) => /english\s*\(en\)/i.test(el.textContent || '')) ?? null;
    });
    const englishEl = englishHandle.asElement();
    if (!englishEl) {
      const itemDump = await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.mud-list-item, [role="option"]')) as HTMLElement[];
        return items.map((e) => (e.textContent || '').trim().slice(0, 40));
      });
      console.log('  ⚠ English option not found. Visible options:', JSON.stringify(itemDump));
      throw new Error('English locale option not found in dropdown');
    }
    await englishEl.click();
    console.log('  picked English');
    await new Promise((r) => setTimeout(r, 600));
    // Close the dropdown via Escape.
    await page.keyboard.press('Escape');
    await new Promise((r) => setTimeout(r, 400));
    await shot('02b-after-locale-set');

    // Probe preview AFTER locale set (no screens yet).
    await probe('after locale set, before visual');

    // -------- Step 5: switch to Visual tab + add a screen --------
    console.log('\n[5] click Visual toggle + Add screen');
    await page.evaluate(() => {
      // Match the toggle button by visible text.
      const items = Array.from(document.querySelectorAll('button, [role="button"]')) as HTMLElement[];
      const visual = items.find((b) => /^\s*visual\s*$/i.test(b.textContent?.replace(/\s+/g, ' ').trim() ?? ''));
      visual?.click();
    });
    await new Promise((r) => setTimeout(r, 800));
    await shot('03-visual-tab');

    // Per the DOM dump (see git history for the discovery iteration), the Add-screen
    // activator is the .mud-icon-button inside the .screen-list panel.
    const openMenuResult = await page.evaluate(() => {
      const screensPanel = document.querySelector('.screen-list, [class*="screen-list"]') as HTMLElement
        ?? Array.from(document.querySelectorAll('*'))
             .find((el) => /^\s*Screens\s*$/i.test(el.textContent || ''))
             ?.closest('.mud-paper, .screen-list') as HTMLElement;
      const iconBtn = screensPanel
        ? screensPanel.querySelector('button.mud-icon-button') as HTMLElement | null
        : null;
      if (!iconBtn) return { clicked: false, foundPanel: !!screensPanel };
      // Click via PointerEvent so MudMenu's activator handler picks it up.
      iconBtn.click();
      iconBtn.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
      iconBtn.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
      return {
        clicked: true,
        cls: iconBtn.className,
        innerHTML: (iconBtn.innerHTML || '').slice(0, 80),
      };
    });
    console.log('  open add-screen menu:', JSON.stringify(openMenuResult));
    await new Promise((r) => setTimeout(r, 500));

    const pickInlineResult = await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('.mud-menu-item, [role="menuitem"], button')) as HTMLElement[];
      const target = items.find((el) => /new\s*inline\s*screen/i.test(el.textContent ?? ''));
      if (!target) return { clicked: false, itemCount: items.length };
      target.click();
      return { clicked: true, label: target.textContent?.trim() ?? '' };
    });
    console.log('  pick inline screen:', JSON.stringify(pickInlineResult));
    await new Promise((r) => setTimeout(r, 1500));
    await shot('04-after-add-screen');

    // -------- Step 6a: probe after empty screen, repeatedly --------
    console.log('\n[6a] probe preview state after empty screen (over time)');
    // Poll the preview state for 5 seconds to catch transitions the user saw
    // ("Thank you" briefly → "No screens in this survey").
    for (let t = 0; t <= 5000; t += 500) {
      const r = await probe(`t+${t}ms after empty screen`);
      if (r.surveyRootText.includes('No screens in this survey')) {
        console.log(`  ⚠ caught "No screens in this survey" at t+${t}ms`);
      }
      await new Promise((r) => setTimeout(r, 500));
    }

    // -------- Step 6b: add a question to the screen --------
    console.log('\n[6b] add question to screen');
    const QUESTION_TEXT = 'TEST_QUESTION_XYZ_' + Date.now();
    // Find the "+ Add question" button. From screenshot it's text "+ ADD QUESTION".
    const addQuestionResult = await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('button')) as HTMLButtonElement[];
      const target = btns.find((b) => /add\s*question/i.test(b.textContent || ''));
      if (!target) return { clicked: false, btnCount: btns.length };
      target.click();
      return { clicked: true };
    });
    console.log('  open add-question menu:', JSON.stringify(addQuestionResult));
    await new Promise((r) => setTimeout(r, 500));

    // Pick a question type — try Text or any first menu item.
    const pickQuestionType = await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('.mud-menu-item, [role="menuitem"]')) as HTMLElement[];
      if (items.length === 0) return { clicked: false, reason: 'no menu items' };
      // Prefer a "Text" / "Single line" / "Short" item; else first.
      const target = items.find((el) => /text|short|single\s*line|paragraph/i.test(el.textContent || ''))
        ?? items[0];
      target.click();
      return { clicked: true, label: target.textContent?.trim() ?? '' };
    });
    console.log('  pick question type:', JSON.stringify(pickQuestionType));
    await new Promise((r) => setTimeout(r, 1000));
    await shot('05-after-add-question');
    await probe('after add question');

    // -------- Step 6c: type distinctive text into the QUESTION TITLE field --------
    console.log('\n[6c] type into question Title (EN) — the field that drives the prompt');
    // From the rendered form, the inputs in order are:
    //   1. Survey name (toolbar)
    //   2. Screen ID  ← previously typed here by mistake
    //   3. Screen Title (EN)
    //   4. Screen Description (EN)
    //   5. Question ID
    //   6. BI column
    //   7. Question Title (EN)  ← this is what we want
    // The Title (EN) fields share the "Title (EN)" floating label. The QUESTION's
    // Title (EN) is the SECOND such field (first is the screen's).
    const typeResult = await page.evaluate((text: string) => {
      // Find the question card by its "TEXT" badge + delete icon.
      const inputs = Array.from(document.querySelectorAll('input[type="text"]')) as HTMLInputElement[];
      // Skip Survey-name field, then take the inputs whose surrounding label says "Title (EN)".
      const titleEnInputs = inputs.filter((i) => {
        const labelEl = i.closest('.mud-input-control')?.querySelector('label, .mud-input-label');
        return /title\s*\(en\)/i.test(labelEl?.textContent || '');
      });
      // Second Title (EN) belongs to the question.
      const target = titleEnInputs[1] ?? titleEnInputs[0];
      if (!target) return { typed: false, candidateCount: titleEnInputs.length, totalInputs: inputs.length };
      const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!;
      setter.call(target, text);
      target.dispatchEvent(new Event('input', { bubbles: true }));
      target.dispatchEvent(new Event('change', { bubbles: true }));
      target.dispatchEvent(new Event('blur', { bubbles: true }));
      return { typed: true, candidateCount: titleEnInputs.length, totalInputs: inputs.length };
    }, QUESTION_TEXT);
    console.log('  typed question title:', JSON.stringify(typeResult));
    await new Promise((r) => setTimeout(r, 1500));
    await shot('06-after-type-prompt');

    const finalProbe = await probe('after type question title');
    const previewShowsQuestion = finalProbe.surveyRootText.includes(QUESTION_TEXT);
    console.log(`\n  ⇒ preview shows question text "${QUESTION_TEXT}"? ${previewShowsQuestion}`);
    const afterAdd = finalProbe;

    // -------- Step 7: dump captured console / network output --------
    console.log('\n[7] captured browser events:');
    const filtered = consoleLog.filter((l) =>
      l.includes('preview') ||
      l.includes('shift-survey') ||
      l.includes('setSchema') ||
      l.includes('pageerror') ||
      l.includes('requestfailed') ||
      l.includes('console.error') ||
      l.includes('console.warn')
    );
    if (filtered.length === 0) {
      console.log('  (no preview/shift-survey/error events captured)');
    } else {
      filtered.forEach((l) => console.log(`  ${l}`));
    }

    console.log('\nDone. Browser left open so you can inspect — close it manually.');
    // Hold for 60s so the operator can poke at DevTools if needed.
    await new Promise((r) => setTimeout(r, 60_000));
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error('\nPREVIEW-CREATE REPRO FAILED:', (err as Error).message ?? err);
  process.exit(1);
});
