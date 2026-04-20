/**
 * ADP Dynamic Surveys — API-layer end-to-end smoke harness.
 *
 * Exercises the full authenticated-admin + anonymous-public paths against a running
 * Sample.API, then drives the resulting schema through `@shiftsoftware/survey-sdk`
 * to assert the SDK + server agree on schema shape, logic routing, and response
 * persistence. Pair with `browser.ts` for the in-browser renderer smoke.
 *
 * Prerequisites
 *   - Sample.API on http://localhost:5134 (ASPNETCORE_ENVIRONMENT=Development)
 *   - SQL Express at `localhost\sqlexpress`, database `SurveysSample`
 *   - `sqlcmd` on PATH
 */

import {
  SurveyClient,
  SurveyClientError,
  computeNext,
  resolveNavigationListTarget,
  type NavigationOption,
  type Survey,
} from '@shiftsoftware/survey-sdk';
import { API_SURVEYS, assert, assertEq, sql, step } from './lib/util.js';
import { cleanupSeeded, seedSurvey, type SeededState } from './lib/seed.js';

let state: SeededState | undefined;

async function main() {
  console.log(`ADP Surveys E2E (API layer)`);
  state = await seedSurvey();

  const sdk = new SurveyClient({ baseUrl: API_SURVEYS });
  let fetchedSchema: Survey | undefined;

  await step('SDK.fetchSchema via anonymous public endpoint', async () => {
    fetchedSchema = await sdk.fetchSchema(state!.publicId);
    assertEq(fetchedSchema.version, state!.publishedVersion, 'pinned version matches publish result');
    assert(fetchedSchema.screens.length === 5, `5 screens on the wire (got ${fetchedSchema.screens.length})`);
    const feedback = fetchedSchema.screens.find((s) => s.id === 'feedback');
    assert(feedback, 'feedback screen exists');
    assert(Array.isArray(feedback!.questions) && feedback!.questions.length === 1, 'feedback has one question');
    const q0 = feedback!.questions[0] as Record<string, unknown>;
    assertEq(q0['type'], 'nps', 'banked entry resolved to inline NPS question');
    assertEq(q0['id'], state!.bankKey, 'resolved question id preserves the bank key (Decision #11)');
  })();

  await step('SDK.fetchSchema maps 404 to SurveyClientError(code=notFound)', async () => {
    try {
      await sdk.fetchSchema('00000000-0000-0000-0000-000000000000');
      throw new Error('expected SurveyClientError, got success');
    } catch (e) {
      assert(e instanceof SurveyClientError, `error is SurveyClientError (got ${(e as Error).constructor?.name})`);
      assertEq((e as SurveyClientError).code, 'notFound', '404 → code=notFound');
      assertEq((e as SurveyClientError).status, 404, 'status preserved');
    }
  })();

  await step('SDK.computeNext — detractor routes to thanks-default via logic rule', () => {
    const next = computeNext(fetchedSchema!, 'feedback', { [state!.bankKey]: 3 });
    assertEq(next.kind, 'screen', 'returns a screen step');
    assertEq((next as { screenId: string }).screenId, 'thanks-default', 'nps=3 → thanks-default');
  })();

  await step('SDK.computeNext — promoter falls back to screen.nextScreen (brand)', () => {
    const next = computeNext(fetchedSchema!, 'feedback', { [state!.bankKey]: 9 });
    assertEq(next.kind, 'screen', 'returns a screen step');
    assertEq((next as { screenId: string }).screenId, 'brand', 'nps=9 → brand via screen.nextScreen fallback');
  })();

  await step('SDK.resolveNavigationListTarget — toyota option routes to thanks-toyota', () => {
    const brandScreen = fetchedSchema!.screens.find((s) => s.id === 'brand');
    assert(brandScreen && Array.isArray(brandScreen.questions), 'brand screen has questions');
    const navQ = brandScreen!.questions![0] as { options?: NavigationOption[] };
    const toyota = navQ.options?.find((o) => o.id === 'toyota');
    assert(toyota, 'toyota option exists in fetched schema');
    const next = resolveNavigationListTarget(toyota!, fetchedSchema!, 'brand', {});
    assertEq((next as { screenId: string }).screenId, 'thanks-toyota', 'toyota option routes via per-option nextScreen');
  })();

  await step('SDK.submitResponse (promoter → toyota)', async () => {
    await sdk.submitResponse(state!.publicId, {
      schemaVersion: state!.publishedVersion,
      answers: {
        name: 'E2E Tester',
        [state!.bankKey]: 10,
        'brand-choice': 'toyota',
      },
      meta: {
        startedAt: new Date(Date.now() - 60_000).toISOString(),
        completedAt: new Date().toISOString(),
      },
    });
  })();

  await step('verify response persisted + banked answer stamped with BankEntryID', () => {
    const countStr = sql(`
      SELECT COUNT(1) FROM [Surveys].[SurveyResponse] r
      JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
      WHERE i.PublicID = '${state!.publicId}';
    `);
    assertEq(Number(countStr.trim()), 1, 'exactly one response row for this instance');

    const bankedRow = sql(`
      SELECT a.KeyAtSubmission, CASE WHEN a.BankEntryID IS NULL THEN 'NULL' ELSE 'NOTNULL' END
      FROM [Surveys].[SurveyAnswer] a
      JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
      JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
      WHERE i.PublicID = '${state!.publicId}' AND a.KeyAtSubmission = '${state!.bankKey}';
    `);
    const [key, bankState] = bankedRow.split('\t');
    assertEq(key?.trim(), state!.bankKey, 'banked answer persisted under its key');
    assertEq(bankState?.trim(), 'NOTNULL', 'banked answer carries the Decision #11 BankEntryID anchor');

    const inlineRow = sql(`
      SELECT CASE WHEN a.BankEntryID IS NULL THEN 'NULL' ELSE 'NOTNULL' END
      FROM [Surveys].[SurveyAnswer] a
      JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
      JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
      WHERE i.PublicID = '${state!.publicId}' AND a.KeyAtSubmission = 'name';
    `);
    assertEq(inlineRow.trim(), 'NULL', 'inline (non-banked) answer stays null on BankEntryID');
  })();

  await step('verify instance flipped to Completed', () => {
    const statusStr = sql(`
      SELECT Status FROM [Surveys].[SurveyInstance] WHERE PublicID = '${state!.publicId}';
    `);
    assertEq(Number(statusStr.trim()), 3, 'instance Status=Completed (3)');
  })();

  console.log('\nAll assertions passed.');
}

main()
  .then(() => cleanupSeeded(state!))
  .catch(async (err) => {
    console.error('\nE2E FAILED:', (err as Error).message ?? err);
    if (state) await cleanupSeeded(state);
    process.exit(1);
  });
