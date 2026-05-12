# ADP Surveys — End-to-End Harnesses

Live integration tests against the running Sample.API + DB. Each harness seeds a fresh survey, exercises the surface, asserts persistence, and cleans up after itself — zero residual rows on success.

## Prerequisites

1. **`dotnet build` once** at repo root.
2. **`npm install`** at `ADP.Surveys/renderer/` (sets up the npm workspace).
3. **`npm run build --workspaces --if-present`** at `ADP.Surveys/renderer/` — `run.ts` imports `@shiftsoftware/survey-sdk` from its built `dist/`. Skipping this gives `ERR_MODULE_NOT_FOUND` on `survey-sdk/dist/index.js`.
4. **Sample.API running** on `http://localhost:5134` — `cd samples/ADP.Surveys.Sample.API && dotnet run`. The harness logs in as `SuperUser` / `OneTwo` and writes via authenticated CRUD.
5. **`SurveysSample` DB present** on `localhost\sqlexpress` with EF migrations applied (`dotnet ef database update` from `samples/ADP.Surveys.Sample.API`). The harness uses `sqlcmd` to seed `SurveyInstance` rows and to verify persistence.
6. **For the browser harness only:**
   - Standalone app on `http://127.0.0.1:5190` — `cd renderer/apps/standalone && npx vite dev`.
   - `puppeteer-core` installed at `C:\tmp\screenshot-tool\node_modules\puppeteer-core` (path is hard-coded in `browser.ts`). One-time setup: `mkdir C:\tmp\screenshot-tool && cd C:\tmp\screenshot-tool && npm init -y && npm i puppeteer-core`.
   - Chrome at `C:\Program Files\Google\Chrome\Application\chrome.exe` (also hard-coded).

## Running

From `ADP.Surveys/renderer/e2e/`:

| Command | Harness | What it does |
|---------|---------|--------------|
| `npm start` | `run.ts` | API + SDK only. Login, seed banked NPS, create + publish survey, insert instance, then drive `SurveyClient.fetchSchema` / `computeNext` / `resolveNavigationListTarget` / `submitResponse`. Asserts response persisted with `BankEntryID` stamped, instance flipped to `Completed`. ~14 assertions. |
| `npm run browser` | `browser.ts` | Same seed, but drives the standalone React app in headless Chromium. Walks Welcome → Feedback (NPS) → Brand → Toyota → auto-submit, screenshots each screen to `C:/tmp/survey-renderer-shots/browser/`, verifies DB persistence + zero console errors. The canary for anything that only breaks in a real browser (CSS missing, fetch binding, postMessage, React strict-mode side-effects). |
| `npm run seed-demo` | `seed-demo.ts` | Inserts a `demo:`-prefixed survey + instance and prints the public URL. Persistent — survives runs. Use for manual exploration of the standalone app. |
| `npm run unseed-demo` | `unseed-demo.ts` | Removes everything `seed-demo.ts` created. |
| `npm run resume` | `resume.ts` | Walks the localStorage-resume flow: starts a survey, navigates partway, reloads the page, asserts mid-flow state is restored. |
| `npm run typecheck` | — | `tsc --noEmit` across the harness. |

## Adding a new harness

Follow the shape of `run.ts`:

1. `seedSurvey()` from `lib/seed.ts` returns a `SeededState` with `publicId`, the bank entry id, etc. — that helper handles login, banked-question creation, survey + draft + publish, and `SurveyInstance` insertion.
2. Wrap each assertion in `step('description', async () => …)` from `lib/util.ts` so failures print a readable trace.
3. End with `cleanupSeeded(state)` in a `finally` — soft-deletes the survey + bank entry and removes the instance + response + answers. Don't skip this; it's what keeps reruns idempotent.

Don't reach into the API with raw `fetch` for setup — use the seed helper. Use the SDK (`SurveyClient`) for the surface under test, never raw `fetch` to the public endpoints, so the harness exercises the same code paths a renderer client would.
