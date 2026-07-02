/**
 * E2E for external option sources (`optionsSource`), server-side half:
 *
 *   1. Seeds two sourced bank questions (TIQ-style dropdown) + a survey mixing
 *      an inline sourced navigationList with bankRef usages, one of which
 *      narrows the bank's endpoint via `overrides.sourceParams`.
 *   2. Publishes and asserts the resolver merged sourceParams over the bank's
 *      queryParams in the served public schema (override wins, bank params
 *      retained, url locked) — and that the source block ships to the client.
 *   3. Submits answers with ids the server has never seen: sourced questions
 *      must pass shape-only validation (the option list lives client-side),
 *      required-on-path must still 400 when the sourced navigationList's
 *      nextScreen routes the replay onto a screen with a missing required
 *      answer, and non-string ids must still 400.
 *
 * The client-side half (fetch, loading/error states, Accept-Language) is
 * covered by renderer unit tests + the browser verification script. The
 * server never calls the source URL, so this harness runs offline.
 *
 * Requires Sample.API on http://localhost:5134 (Development env) and the
 * SurveysSample DB reachable via sqlcmd.
 */

import { api, apiJson, assert, assertEq, sql, step } from './lib/util.js';

const TAG = `e2e-src-${Date.now()}`;
const CITY_BANK = `${TAG}-city`;
const BRANCH_BANK = `${TAG}-branch`;
const TIQ = 'https://tiq-identity-server.azurewebsites.net/api/public';

let token = '';
let surveyHashId = '';
let publishedVersion = 0;
let publicId = '';

function draft(surveyId: string) {
  return {
    surveyId,
    version: 0,
    title: { en: `E2E OptionsSource ${TAG}` },
    locales: ['en'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'pick',
        title: { en: 'Choose a bodywork branch' },
        questions: [
          {
            type: 'navigationList',
            id: 'pick-branch',
            title: { en: 'Branches' },
            required: true,
            optionsSource: {
              url: `${TIQ}/company-branch`,
              queryParams: { services: 'body-and-paint' },
              nextScreen: 'branch-banked',
            },
          },
        ],
      },
      {
        id: 'branch-banked',
        title: { en: 'Service center (banked + override)' },
        questions: [
          {
            bankRef: BRANCH_BANK,
            overrides: { sourceParams: { services: 'auto-repair-and-maintenance' } },
          },
        ],
        nextScreen: 'city',
      },
      {
        id: 'city',
        title: { en: 'Your city' },
        questions: [{ bankRef: CITY_BANK }],
        nextScreen: 'done',
      },
      { id: 'done', title: { en: 'Thank you!' }, questions: [] },
    ],
    logic: [],
  };
}

async function seed() {
  await step('login', async () => {
    const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: 'SuperUser', Password: 'OneTwo' },
    });
    token = body.Entity.Token;
    assert(token.length > 50, 'got a token');
  })();

  await step('seed sourced bank questions', async () => {
    await apiJson('POST', '/api/Surveys/BankQuestion', {
      token,
      body: {
        key: CITY_BANK,
        biColumn: 'city_e2e',
        retired: false,
        question: {
          type: 'dropdown',
          id: CITY_BANK,
          title: { en: 'Which city?' },
          required: true,
          optionsSource: { url: `${TIQ}/city` },
        },
      },
    });
    await apiJson('POST', '/api/Surveys/BankQuestion', {
      token,
      body: {
        key: BRANCH_BANK,
        biColumn: 'branch_e2e',
        retired: false,
        question: {
          type: 'dropdown',
          id: BRANCH_BANK,
          title: { en: 'Preferred branch' },
          required: false,
          optionsSource: {
            url: `${TIQ}/company-branch`,
            queryParams: { services: 'new-vehicle-sale', top: '50' },
          },
        },
      },
    });
  })();

  await step('create + update survey', async () => {
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token,
      body: { name: `E2E OptionsSource ${TAG}`, draft: draft('pending') },
    });
    surveyHashId = created.Entity.ID;
    const full = await apiJson<{ Entity: Record<string, unknown> }>(
      'GET',
      `/api/Surveys/Survey/${surveyHashId}`,
      { token },
    );
    await apiJson('PUT', `/api/Surveys/Survey/${surveyHashId}`, {
      token,
      body: { ...full.Entity, name: `E2E OptionsSource ${TAG}`, draft: draft(surveyHashId) },
    });
  })();

  await step('publish', async () => {
    const r = await api('POST', `/api/Surveys/Publish/${surveyHashId}`, { token });
    const text = await r.text();
    if (!r.ok) throw new Error(`publish failed ${r.status}: ${text}`);
    const body = JSON.parse(text) as { Version?: number; version?: number };
    publishedVersion = body.Version ?? body.version ?? 0;
    assert(publishedVersion >= 1, `published version ≥ 1 (got ${publishedVersion})`);
  })();

  await step('insert SurveyInstance via sqlcmd', async () => {
    const row = sql(`
      DECLARE @name NVARCHAR(200) = N'E2E OptionsSource ${TAG}';
      SELECT TOP 1 s.ID, v.ID
      FROM [Surveys].[Survey] s
      JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
      WHERE s.Name = @name AND v.Version = ${publishedVersion};
    `);
    const [surveyIdStr, versionIdStr] = row.split('\t');
    assert(surveyIdStr && versionIdStr, `sqlcmd returned a row (got "${row}")`);
    const inserted = sql(`
      DECLARE @out TABLE (PublicID UNIQUEIDENTIFIER);
      INSERT INTO [Surveys].[SurveyInstance]
        (PublicID, SurveyID, SurveyVersionID, TriggeredAt, Status, CreateDate, LastSaveDate, IsDeleted)
      OUTPUT inserted.PublicID INTO @out
      VALUES (NEWID(), ${surveyIdStr}, ${versionIdStr}, SYSDATETIMEOFFSET(), 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0);
      SELECT CONVERT(NVARCHAR(36), PublicID) FROM @out;
    `);
    publicId = inserted.trim();
    assert(/^[0-9a-fA-F-]{36}$/.test(publicId), `publicId looks like a GUID ("${publicId}")`);
  })();
}

interface WireQuestion {
  type?: string;
  id?: string;
  bankRef?: string;
  optionsSource?: {
    url: string;
    queryParams?: Record<string, string>;
    nextScreen?: string;
  };
}
interface WireScreen {
  id: string;
  questions?: WireQuestion[];
}

async function verifySchema() {
  await step('served schema: resolver merged sourceParams; source block ships', async () => {
    const schema = await apiJson<{ screens: WireScreen[] }>(
      'GET',
      `/api/Surveys/SurveyInstances/${publicId}/schema`,
    );
    const byId = new Map(schema.screens.map((s) => [s.id, s]));

    const nav = byId.get('pick')?.questions?.[0];
    assert(nav?.optionsSource, 'inline navigationList kept its optionsSource');
    assertEq(nav!.optionsSource!.queryParams?.['services'], 'body-and-paint', 'inline params intact');
    assertEq(nav!.optionsSource!.nextScreen, 'branch-banked', 'inline source nextScreen intact');

    const banked = byId.get('branch-banked')?.questions?.[0];
    assert(banked?.optionsSource, 'banked dropdown resolved with its optionsSource');
    assertEq(banked!.id, BRANCH_BANK, 'banked id preserved');
    assertEq(
      banked!.optionsSource!.queryParams?.['services'],
      'auto-repair-and-maintenance',
      'sourceParams override won over the bank param',
    );
    assertEq(banked!.optionsSource!.queryParams?.['top'], '50', 'bank param without override retained');
    assertEq(banked!.optionsSource!.url, `${TIQ}/company-branch`, 'url stays bank-locked');

    const city = byId.get('city')?.questions?.[0];
    assert(city?.optionsSource, 'city bank resolved with its optionsSource');
  })();
}

async function verifySubmission() {
  const submit = (answers: Record<string, unknown>) =>
    api('POST', `/api/Surveys/SurveyInstances/${publicId}/responses`, {
      body: { schemaVersion: publishedVersion, answers, meta: {} },
    });

  // Rejections first — a successful submit completes the instance (one
  // response per instance), after which everything else would 409.
  await step('required-on-path still enforced across the sourced navlist route', async () => {
    // pick-branch answered → replay routes via source.nextScreen onto
    // branch-banked → city, where the required city answer is missing.
    const r = await submit({ 'pick-branch': 'MJLGr' });
    const text = await r.text();
    assertEq(r.status, 400, `missing required sourced answer → 400 (got ${r.status}: ${text.slice(0, 200)})`);
    assert(text.includes(CITY_BANK), 'error names the missing city question');
  })();

  await step('shape checks still apply to sourced questions', async () => {
    const r = await submit({
      'pick-branch': 42,
      [CITY_BANK]: 'L0VEX',
    });
    assertEq(r.status, 400, `non-string choice answer → 400 (got ${r.status})`);
  })();

  await step('sourced answers the server never saw are accepted (shape-only)', async () => {
    const r = await submit({
      'pick-branch': 'MJLGr',
      [BRANCH_BANK]: 'some-fetched-branch',
      [CITY_BANK]: 'L0VEX',
    });
    const text = await r.text();
    assert(r.ok, `submit accepted (${r.status}): ${text.slice(0, 300)}`);
  })();
}

async function cleanup() {
  console.log('\nCleanup:');
  if (publicId) {
    try {
      sql(`
        DELETE a FROM [Surveys].[SurveyAnswer] a
        JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${publicId}';
        DELETE r FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${publicId}';
        DELETE FROM [Surveys].[SurveyInstance] WHERE PublicID = '${publicId}';
      `);
      console.log(`  • removed instance + response rows for ${publicId}`);
    } catch (e) {
      console.log(`  ! could not remove instance rows: ${(e as Error).message}`);
    }
  }
  if (token && surveyHashId) {
    try {
      await api('DELETE', `/api/Surveys/Survey/${surveyHashId}`, { token });
      console.log(`  • survey soft-deleted (${surveyHashId})`);
    } catch (e) {
      console.log(`  ! survey soft-delete failed: ${(e as Error).message}`);
    }
  }
  for (const key of [CITY_BANK, BRANCH_BANK]) {
    try {
      const list = await apiJson<{ Value: { ID: string; Key: string }[] }>(
        'GET',
        `/api/Surveys/BankQuestion?$filter=Key eq '${key}'`,
        { token },
      );
      const row = list.Value?.find((x) => x.Key === key);
      if (row) await api('DELETE', `/api/Surveys/BankQuestion/${row.ID}`, { token });
      console.log(`  • bank entry soft-deleted (${key})`);
    } catch (e) {
      console.log(`  ! bank delete failed: ${(e as Error).message}`);
    }
  }
}

async function main() {
  console.log(`OptionsSource E2E — tag ${TAG}\n`);
  try {
    await seed();
    await verifySchema();
    await verifySubmission();
    console.log('\nAll options-source e2e steps passed ✔');
  } finally {
    await cleanup();
  }
}

main().catch((e) => {
  console.error('\nFAILED:', e);
  process.exit(1);
});
