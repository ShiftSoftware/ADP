/**
 * E2E — serve-time personalization tokens ({{recipient.*}} / {{candidate.*}}).
 *
 * Walks the full loop: publish a survey whose LocalizedStrings carry tokens →
 * insert three instances → GET /schema anonymously for each → assert that the
 * whole precedence chain behaves, and that ids/expressions are never touched.
 *
 * The three instances exist to pin the three-way split that unit tests can only
 * assert in isolation:
 *
 *   1. WITH snapshot   — a real instance carrying an ingested event. Snapshot wins.
 *   2. BARE            — a real instance whose event lacked the fields. Falls to the
 *                        token's own |fallback, then the survey's declared variable.
 *                        Nothing customer-facing should show raw braces here.
 *   3. TEST            — TriggeredBy = 'dashboard-test'. No event at all, so the
 *                        declared variables STAND IN for one, ahead of |fallback.
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
  publicIdTest: string;
}

const state: State = {
  tag: `e2e-pers-${Date.now()}`,
  token: '',
  surveyHashId: '',
  publishedVersion: 0,
  publicIdWithMeta: '',
  publicIdBare: '',
  publicIdTest: '',
};

function buildDraft(surveyId: string) {
  return {
    surveyId,
    title: { en: 'Survey for {{candidate.customerName}}' },
    locales: ['en'],
    defaultLocale: 'en',
    // Declared for customerName only. vehicleModel is deliberately left
    // undeclared so the "still verbatim when nothing can fill it" rule stays
    // covered on the same survey.
    //
    // The example and the fallback are deliberately DIFFERENT strings: the whole
    // point of splitting them is that the test-run value must never appear on a
    // live instance, and a shared string could not tell those two apart.
    variables: [
      {
        name: 'candidate.customerName',
        example: { en: 'Camry Driver' },
        fallback: { en: 'our valued customer' },
        description: 'Set by the post-service event.',
      },
    ],
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
      {
        id: 'thanks',
        title: { en: 'Thanks {{candidate.customerName}}' },
        // One string that separates all three modes: the first token has both an
        // inline fallback AND a declared variable, the second has only an inline
        // fallback.
        description: {
          en: 'Goodbye {{candidate.customerName|dear customer}} and {{candidate.missing|friend}}',
        },
        questions: [],
      },
    ],
    logic: [],
  };
}

async function insertInstance(withMeta: boolean, asTest = false): Promise<string> {
  const row = sql(`
    DECLARE @name NVARCHAR(200) = N'Personalization ${state.tag}';
    SELECT TOP 1 s.ID, v.ID
    FROM [Surveys].[Survey] s
    JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
    WHERE s.Name = @name AND v.Version = ${state.publishedVersion};
  `);
  const [surveyIdStr, versionIdStr] = row.split('\t');
  assert(surveyIdStr && versionIdStr, `sqlcmd returned survey/version ids (got "${row}")`);

  let extraCols = withMeta ? ', CustomerRef, RecipientAddress, RecipientLocale, MetaDataJson' : '';
  // The sql() helper hands the query to sqlcmd as a CLI argument, so literal
  // double-quotes inside the JSON break argument parsing. Build the JSON with
  // ^ placeholders and swap them for CHAR(34) server-side.
  let extraVals = withMeta
    ? `, N'CUST-42', N'+9647701112233', N'en', REPLACE(N'{^customerName^:^Aza^,^vehicleModel^:^Land Cruiser^,^dealerId^:7}', N'^', CHAR(34))`
    : '';

  // Matches SurveysConstants.DashboardTestTriggerSource — the flag GetSchema reads
  // to decide that declared variables should stand in for a missing event.
  if (asTest) {
    extraCols += ', TriggeredBy';
    extraVals += `, N'dashboard-test'`;
  }

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

  await step('insert dashboard TEST instance (no snapshot)', async () => {
    state.publicIdTest = await insertInstance(false, true);
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
    assertEq(
      schema.screens[1].description.en,
      'Goodbye Aza and friend',
      'snapshot beats both the inline fallback and the declared variable; a token with only an inline fallback still uses it',
    );
  })();

  await step('GET /schema (bare instance) → inline fallback, then declared fallback, never the example', async () => {
    const schema = await fetchSchema(state.publicIdBare);

    // No snapshot and no inline fallback → the declared FALLBACK is the safety net
    // that keeps braces off a customer-facing screen.
    assertEq(schema.title.en, 'Survey for our valued customer', 'declared fallback fills a bare instance');
    assertEq(
      schema.screens[1].description.en,
      'Goodbye dear customer and friend',
      'inline fallback is more specific than the survey-wide one, so it wins on a live instance',
    );
    // The safety property: test data must have no path to a recipient.
    assert(
      !JSON.stringify(schema).includes('Camry Driver'),
      'the example value appears NOWHERE in a live instance schema',
    );
    // Undeclared and unfilled — still verbatim, so an author typo stays visible.
    assertEq(
      schema.screens[0].questions[0].options[0].label.en,
      'My {{candidate.vehicleModel}} is great',
      'token with no snapshot, no inline fallback and no declared fallback stays verbatim',
    );
  })();

  await step('GET /schema (test instance) → the example stands in for the event', async () => {
    const schema = await fetchSchema(state.publicIdTest);

    assertEq(schema.title.en, 'Survey for Camry Driver', 'example value renders in a test run');
    assertEq(
      schema.screens[1].description.en,
      'Goodbye Camry Driver and friend',
      'in sample mode the example leads, ahead of both the inline fallback and the declared fallback',
    );
    assertEq(
      schema.screens[0].questions[0].options[0].label.en,
      'My {{candidate.vehicleModel}} is great',
      'an undeclared token still shows itself in a test run — that is the prompt to declare it',
    );
  })();

  await step('published version is untouched by any of it', async () => {
    // Substitution happens on the served copy. If it ever leaked into the frozen
    // version, the bare instance above would have started serving "Example
    // Customer" from storage rather than from the overlay — assert the stored
    // bytes directly so that failure mode can't hide.
    const stored = sql(`
      SELECT TOP 1 CAST(v.ResolvedJson AS NVARCHAR(MAX))
      FROM [Surveys].[SurveyVersion] v
      JOIN [Surveys].[Survey] s ON s.ID = v.SurveyID
      WHERE s.Name = N'Personalization ${state.tag}' AND v.Version = ${state.publishedVersion};
    `);
    assert(
      stored.includes('{{candidate.customerName}}'),
      'frozen ResolvedJson still carries the raw tokens',
    );
  })();

  console.log('\nAll assertions passed.');

  console.log('\nCleanup:');
  for (const publicId of [state.publicIdWithMeta, state.publicIdBare, state.publicIdTest]) {
    if (!publicId) continue;
    sql(`DELETE FROM [Surveys].[SurveyInstance] WHERE PublicID = '${publicId}';`);
  }
  console.log(`  • removed all three instances`);
  if (state.surveyHashId) {
    await api('DELETE', `/api/Surveys/Survey/${state.surveyHashId}`, { token: state.token });
    console.log(`  • survey soft-deleted (${state.surveyHashId})`);
  }
}

main().catch((e) => {
  console.error('\nFAILED:', e);
  process.exit(1);
});
