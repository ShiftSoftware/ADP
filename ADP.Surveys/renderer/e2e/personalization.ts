/**
 * E2E — serve-time personalization tokens ({{recipient.*}} / {{candidate.*}}).
 *
 * Walks the full loop: publish a survey whose LocalizedStrings carry tokens →
 * insert one instance WITH recipient + candidate snapshot and one WITHOUT →
 * GET /schema anonymously for both → assert substitution on the first,
 * verbatim tokens on the second, and that ids/expressions are never touched.
 *
 * Requires Sample.API running on http://localhost:5134 (Development env).
 * Run: npx tsx personalization.ts
 */

import { API_SURVEYS, api, apiJson, assert, assertEq, sql, step, USERNAME, PASSWORD } from './lib/util.js';

interface State {
  tag: string;
  token: string;
  surveyHashId: string;
  publishedVersion: number;
  publicIdWithMeta: string;
  publicIdBare: string;
}

const state: State = {
  tag: `e2e-pers-${Date.now()}`,
  token: '',
  surveyHashId: '',
  publishedVersion: 0,
  publicIdWithMeta: '',
  publicIdBare: '',
};

function buildDraft(surveyId: string) {
  return {
    surveyId,
    title: { en: 'Survey for {{candidate.customerName}}' },
    locales: ['en'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'welcome',
        title: { en: 'Hello {{candidate.customerName}}!' },
        description: { en: 'About your {{ candidate.vehicleModel }} — ref {{recipient.customerRef}}.' },
        questions: [
          {
            type: 'singleChoice',
            id: 'q1',
            title: { en: 'Is the {{candidate.vehicleModel}} treating you well, {{candidate.missing}}?' },
            required: false,
            options: [
              { id: 'yes', label: { en: 'My {{candidate.vehicleModel}} is great' } },
              { id: 'no', label: { en: 'Not really' } },
            ],
          },
        ],
      },
      { id: 'thanks', title: { en: 'Thanks {{candidate.customerName}}' }, questions: [] },
    ],
    logic: [],
  };
}

async function insertInstance(withMeta: boolean): Promise<string> {
  const row = sql(`
    DECLARE @name NVARCHAR(200) = N'Personalization ${state.tag}';
    SELECT TOP 1 s.ID, v.ID
    FROM [Surveys].[Survey] s
    JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
    WHERE s.Name = @name AND v.Version = ${state.publishedVersion};
  `);
  const [surveyIdStr, versionIdStr] = row.split('\t');
  assert(surveyIdStr && versionIdStr, `sqlcmd returned survey/version ids (got "${row}")`);

  const extraCols = withMeta ? ', CustomerRef, RecipientAddress, RecipientLocale, MetaDataJson' : '';
  // The sql() helper hands the query to sqlcmd as a CLI argument, so literal
  // double-quotes inside the JSON break argument parsing. Build the JSON with
  // ^ placeholders and swap them for CHAR(34) server-side.
  const extraVals = withMeta
    ? `, N'CUST-42', N'+9647701112233', N'en', REPLACE(N'{^customerName^:^Aza^,^vehicleModel^:^Land Cruiser^,^dealerId^:7}', N'^', CHAR(34))`
    : '';

  const inserted = sql(`
    DECLARE @out TABLE (PublicID UNIQUEIDENTIFIER);
    INSERT INTO [Surveys].[SurveyInstance]
      (PublicID, SurveyID, SurveyVersionID, TriggeredAt, Status, CreateDate, LastSaveDate, IsDeleted${extraCols})
    OUTPUT inserted.PublicID INTO @out
    VALUES (NEWID(), ${surveyIdStr}, ${versionIdStr}, SYSDATETIMEOFFSET(), 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0${extraVals});
    SELECT CONVERT(NVARCHAR(36), PublicID) FROM @out;
  `);
  const publicId = inserted.trim();
  assert(/^[0-9a-fA-F-]{36}$/.test(publicId), `publicId looks like a GUID ("${publicId}")`);
  return publicId;
}

async function fetchSchema(publicId: string): Promise<any> {
  const r = await fetch(`${API_SURVEYS}/SurveyInstances/${publicId}/schema`);
  assert(r.ok, `GET /schema ${publicId} → ${r.status}`);
  return r.json();
}

async function main() {
  await step('login', async () => {
    const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: USERNAME, Password: PASSWORD },
    });
    state.token = body.Entity.Token;
    assert(state.token.length > 50, 'got a token');
  })();

  await step('create + publish survey with tokens in every LocalizedString slot', async () => {
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token: state.token,
      body: { name: `Personalization ${state.tag}`, draft: buildDraft('pending') },
    });
    state.surveyHashId = created.Entity.ID;

    const full = await apiJson<{ Entity: Record<string, unknown> }>(
      'GET',
      `/api/Surveys/Survey/${state.surveyHashId}`,
      { token: state.token },
    );
    await apiJson('PUT', `/api/Surveys/Survey/${state.surveyHashId}`, {
      token: state.token,
      body: { ...full.Entity, name: `Personalization ${state.tag}`, draft: buildDraft(state.surveyHashId) },
    });

    const r = await api('POST', `/api/Surveys/Publish/${state.surveyHashId}`, { token: state.token });
    const text = await r.text();
    if (!r.ok) throw new Error(`publish failed ${r.status}: ${text}`);
    const body = JSON.parse(text) as { Version?: number; version?: number };
    state.publishedVersion = body.Version ?? body.version ?? 0;
    assert(state.publishedVersion >= 1, `published version ≥ 1 (got ${state.publishedVersion})`);
  })();

  await step('insert instance WITH recipient + candidate snapshot', async () => {
    state.publicIdWithMeta = await insertInstance(true);
  })();

  await step('insert instance WITHOUT snapshot (manual/bare)', async () => {
    state.publicIdBare = await insertInstance(false);
  })();

  await step('GET /schema (with snapshot) → tokens substituted', async () => {
    const schema = await fetchSchema(state.publicIdWithMeta);
    assertEq(schema.title.en, 'Survey for Aza', 'survey title substituted');
    const welcome = schema.screens[0];
    assertEq(welcome.title.en, 'Hello Aza!', 'screen title substituted');
    assertEq(
      welcome.description.en,
      'About your Land Cruiser — ref CUST-42.',
      'whitespace-tolerant token + recipient.customerRef substituted',
    );
    const q1 = welcome.questions[0];
    assertEq(
      q1.title.en,
      'Is the Land Cruiser treating you well, {{candidate.missing}}?',
      'known token substituted, unknown token verbatim',
    );
    assertEq(q1.options[0].label.en, 'My Land Cruiser is great', 'option label substituted');
    assertEq(q1.id, 'q1', 'question id untouched');
    assertEq(schema.screens[1].title.en, 'Thanks Aza', 'terminal screen title substituted');
  })();

  await step('GET /schema (bare instance) → tokens stay verbatim', async () => {
    const schema = await fetchSchema(state.publicIdBare);
    assertEq(schema.title.en, 'Survey for {{candidate.customerName}}', 'no context → verbatim');
    assertEq(
      schema.screens[0].questions[0].options[0].label.en,
      'My {{candidate.vehicleModel}} is great',
      'option label verbatim without context',
    );
  })();

  console.log('\nAll assertions passed.');

  console.log('\nCleanup:');
  for (const publicId of [state.publicIdWithMeta, state.publicIdBare]) {
    if (!publicId) continue;
    sql(`DELETE FROM [Surveys].[SurveyInstance] WHERE PublicID = '${publicId}';`);
  }
  console.log(`  • removed both instances`);
  if (state.surveyHashId) {
    await api('DELETE', `/api/Surveys/Survey/${state.surveyHashId}`, { token: state.token });
    console.log(`  • survey soft-deleted (${state.surveyHashId})`);
  }
}

main().catch((e) => {
  console.error('\nFAILED:', e);
  process.exit(1);
});
