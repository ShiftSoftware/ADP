/**
 * E2E for the dashboard answers/test loop (SurveyResponsesController), API half:
 *
 *   1. Creates + publishes a small survey, then creates a TEST instance through
 *      the new admin endpoint — asserts it pins the latest published version,
 *      carries the public link, and shows up in the instance list as
 *      TEST / Pending / no response.
 *   2. Answers it through the PUBLIC pipeline (same endpoints a real recipient
 *      hits) — asserts the list flips to Completed and the detail endpoint
 *      returns the answers + the pinned ResolvedJson.
 *   3. Outbox isolation: the test response must write NO SurveyOutboxEvent,
 *      while a regular (non-test) instance on the same version still writes
 *      exactly one — proves the skip keys off TriggeredBy='dashboard-test' and
 *      nothing else.
 *   4. Guards: unauthenticated list → 401; test instance on an unpublished
 *      survey → 400.
 *
 * The dashboard half (Responses page, embedded test-run dialog, answers view)
 * is covered by the browser verification script.
 *
 * Requires Sample.API on http://localhost:5134 (Development env) and the
 * SurveysSample DB reachable via sqlcmd.
 */

import { api, apiJson, assert, assertEq, sql, step } from './lib/util.js';

const TAG = `e2e-loop-${Date.now()}`;

let token = '';
let surveyHashId = '';
let unpublishedHashId = '';
let publishedVersion = 0;
let testPublicId = '';
let regularPublicId = '';

function draft(surveyId: string) {
  return {
    surveyId,
    version: 0,
    title: { en: `E2E Test Loop ${TAG}`, ar: `E2E حلقة الاختبار ${TAG}` },
    locales: ['en', 'ar'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'feedback',
        title: { en: 'Your feedback', ar: 'ملاحظاتك' },
        questions: [
          {
            type: 'rating',
            id: 'overall',
            title: { en: 'Overall experience', ar: 'التجربة العامة' },
            required: true,
            max: 5,
          },
          {
            type: 'yesNo',
            id: 'recommend',
            title: { en: 'Would you recommend us?', ar: 'هل توصي بنا؟' },
            required: true,
          },
          {
            type: 'singleChoice',
            id: 'visit-reason',
            title: { en: 'Reason for your visit', ar: 'سبب الزيارة' },
            required: false,
            options: [
              { id: 'service', label: { en: 'Service', ar: 'صيانة' } },
              { id: 'purchase', label: { en: 'Purchase', ar: 'شراء' } },
            ],
          },
          {
            type: 'paragraph',
            id: 'comments',
            title: { en: 'Anything else?', ar: 'أي شيء آخر؟' },
            required: false,
          },
        ],
        nextScreen: 'done',
      },
      { id: 'done', title: { en: 'Thank you!', ar: 'شكراً!' }, questions: [] },
    ],
    logic: [],
  };
}

interface CreateResult {
  PublicId: string;
  SchemaVersion: number;
  PublicUrl?: string | null;
}
/** Row shape of the read-only OData SurveyInstance resource (ShiftList's source). */
interface ListItem {
  PublicID: string;
  Status: number; // SurveyInstanceStatus ordinal: 0 Pending … 3 Completed
  TriggeredBy?: string | null;
  IsTest: boolean;
  SchemaVersion: number;
  ResponseCount: number;
  CompletedAt?: string | null;
}
interface ListResult {
  Count?: number | null;
  Value: ListItem[];
}

/** GET the OData instance list scoped to a survey (hashed id — the framework
 *  hashid-decodes DTO string filters server-side, same as ShiftList's Filter). */
const instanceList = (surveyHash: string, token: string) =>
  apiJson<ListResult>(
    'GET',
    `/api/Surveys/SurveyInstance?$count=true&$filter=${encodeURIComponent(`SurveyID eq '${surveyHash}'`)}`,
    { token },
  );
interface DetailResult {
  Instance: ListItem;
  ResolvedJson: string;
  Responses: {
    CompletedAt?: string | null;
    AgentId?: string | null;
    Answers: { Key: string; BankEntryId?: string | null; ValueJson: string }[];
  }[];
}

async function seed() {
  await step('login', async () => {
    const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: 'SuperUser', Password: 'OneTwo' },
    });
    token = body.Entity.Token;
    assert(token.length > 50, 'got a token');
  })();

  await step('create + publish survey', async () => {
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token,
      body: { name: `E2E Test Loop ${TAG}`, draft: draft('pending') },
    });
    surveyHashId = created.Entity.ID;
    const full = await apiJson<{ Entity: Record<string, unknown> }>(
      'GET',
      `/api/Surveys/Survey/${surveyHashId}`,
      { token },
    );
    await apiJson('PUT', `/api/Surveys/Survey/${surveyHashId}`, {
      token,
      body: { ...full.Entity, name: `E2E Test Loop ${TAG}`, draft: draft(surveyHashId) },
    });
    const r = await api('POST', `/api/Surveys/Publish/${surveyHashId}`, { token });
    const text = await r.text();
    if (!r.ok) throw new Error(`publish failed ${r.status}: ${text}`);
    const body = JSON.parse(text) as { Version?: number };
    publishedVersion = body.Version ?? 0;
    assert(publishedVersion >= 1, `published version ≥ 1 (got ${publishedVersion})`);
  })();

  await step('create second survey WITHOUT publishing (negative fixture)', async () => {
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token,
      body: { name: `E2E Test Loop Unpublished ${TAG}`, draft: draft('pending') },
    });
    unpublishedHashId = created.Entity.ID;
  })();
}

async function verifyCreateAndList() {
  await step('unauthenticated instance list → 401', async () => {
    const r = await api('GET', `/api/Surveys/SurveyInstance`);
    assertEq(r.status, 401, `list without token rejected (got ${r.status})`);
  })();

  await step('SurveyInstance resource is read-only (writes → 405)', async () => {
    const r = await api('POST', `/api/Surveys/SurveyInstance`, { token, body: {} });
    assertEq(r.status, 405, `POST rejected as method-not-allowed (got ${r.status})`);
  })();

  await step('test instance on unpublished survey → 400', async () => {
    const r = await api('POST', `/api/Surveys/SurveyResponses/${unpublishedHashId}/test-instances`, { token });
    const text = await r.text();
    assertEq(r.status, 400, `unpublished survey rejected (got ${r.status}: ${text.slice(0, 200)})`);
    assert(text.toLowerCase().includes('publish'), 'error tells the author to publish first');
  })();

  await step('create test instance via admin endpoint', async () => {
    const created = await apiJson<CreateResult>(
      'POST',
      `/api/Surveys/SurveyResponses/${surveyHashId}/test-instances`,
      { token },
    );
    testPublicId = created.PublicId;
    assert(/^[0-9a-fA-F-]{36}$/.test(testPublicId), `publicId looks like a GUID ("${testPublicId}")`);
    assertEq(created.SchemaVersion, publishedVersion, 'test instance pinned to the latest published version');
    assert(
      !!created.PublicUrl && created.PublicUrl.includes(testPublicId),
      `PublicUrl carries the publicId (got "${created.PublicUrl}")`,
    );
  })();

  await step('OData instance list shows TEST / Pending / no response', async () => {
    const list = await instanceList(surveyHashId, token);
    assertEq(list.Value.length, 1, 'one instance for this survey (hashed SurveyID filter decoded)');
    const row = list.Value[0]!;
    assertEq(row.PublicID.toLowerCase(), testPublicId.toLowerCase(), 'row is our instance');
    assertEq(row.IsTest, true, 'flagged as test');
    assertEq(row.TriggeredBy, 'dashboard-test', 'TriggeredBy stamped');
    assertEq(row.Status, 0, 'starts Pending (ordinal 0)');
    assertEq(row.ResponseCount, 0, 'no response yet');
    assertEq(row.SchemaVersion, publishedVersion, 'row shows pinned version');
  })();
}

async function verifyAnswerLoop() {
  await step('public schema serves for the test instance', async () => {
    const r = await api('GET', `/api/Surveys/SurveyInstances/${testPublicId}/schema`);
    assert(r.ok, `schema 200 (got ${r.status})`);
  })();

  await step('submit through the public pipeline', async () => {
    const r = await api('POST', `/api/Surveys/SurveyInstances/${testPublicId}/responses`, {
      body: {
        schemaVersion: publishedVersion,
        answers: {
          overall: 4,
          recommend: true,
          'visit-reason': 'service',
          comments: 'E2E test-loop response — please ignore.',
        },
        meta: { startedAt: new Date(Date.now() - 45_000).toISOString() },
      },
    });
    const text = await r.text();
    assert(r.ok, `submit accepted (${r.status}): ${text.slice(0, 300)}`);
  })();

  await step('list flips to Completed with 1 response', async () => {
    const list = await instanceList(surveyHashId, token);
    const row = list.Value.find((i) => i.PublicID.toLowerCase() === testPublicId.toLowerCase());
    assert(row, 'instance still listed');
    assertEq(row!.Status, 3, 'status flipped to Completed (ordinal 3)');
    assertEq(row!.ResponseCount, 1, 'one response recorded');
    assert(!!row!.CompletedAt, 'CompletedAt set');
  })();

  await step('detail returns answers + pinned ResolvedJson', async () => {
    const detail = await apiJson<DetailResult>(
      'GET',
      `/api/Surveys/SurveyResponses/instance/${testPublicId}`,
      { token },
    );
    assertEq(detail.Instance.IsTest, true, 'detail knows it is a test instance');
    assertEq(detail.Responses.length, 1, 'one response in detail');

    const answers = new Map(detail.Responses[0]!.Answers.map((a) => [a.Key, a]));
    assertEq(answers.get('overall')?.ValueJson, '4', 'rating stored as raw number');
    assertEq(answers.get('recommend')?.ValueJson, 'true', 'yesNo stored as raw boolean');
    assertEq(answers.get('visit-reason')?.ValueJson, '"service"', 'choice stored as option id');
    assertEq(answers.get('overall')?.BankEntryId ?? null, null, 'inline answer has no bank anchor');

    const schema = JSON.parse(detail.ResolvedJson) as { version?: number; screens?: unknown[] };
    assertEq(schema.version, publishedVersion, 'ResolvedJson is the pinned version');
    assert((schema.screens?.length ?? 0) >= 2, 'ResolvedJson has the screens');
  })();
}

async function verifyOutboxIsolation() {
  await step('test response wrote NO outbox event', async () => {
    const count = sql(`
      SELECT COUNT(*) FROM [Surveys].[SurveyOutboxEvent] o
      JOIN [Surveys].[SurveyInstance] i ON i.ID = o.SurveyInstanceID
      WHERE i.PublicID = '${testPublicId}';
    `);
    assertEq(count, '0', 'no SurveyOutboxEvent for the test instance');
  })();

  await step('regular instance on the same version STILL writes one', async () => {
    const row = sql(`
      SELECT TOP 1 s.ID, v.ID
      FROM [Surveys].[Survey] s
      JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
      WHERE s.Name = N'E2E Test Loop ${TAG}' AND v.Version = ${publishedVersion};
    `);
    const [surveyIdStr, versionIdStr] = row.split('\t');
    assert(surveyIdStr && versionIdStr, `sqlcmd returned survey+version ids (got "${row}")`);

    const inserted = sql(`
      DECLARE @out TABLE (PublicID UNIQUEIDENTIFIER);
      INSERT INTO [Surveys].[SurveyInstance]
        (PublicID, SurveyID, SurveyVersionID, TriggeredAt, TriggeredBy, Status, CreateDate, LastSaveDate, IsDeleted)
      OUTPUT inserted.PublicID INTO @out
      VALUES (NEWID(), ${surveyIdStr}, ${versionIdStr}, SYSDATETIMEOFFSET(), N'e2e-regular', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0);
      SELECT CONVERT(NVARCHAR(36), PublicID) FROM @out;
    `);
    regularPublicId = inserted.trim();

    const r = await api('POST', `/api/Surveys/SurveyInstances/${regularPublicId}/responses`, {
      body: {
        schemaVersion: publishedVersion,
        answers: { overall: 5, recommend: true },
        meta: {},
      },
    });
    assert(r.ok, `regular submit accepted (${r.status})`);

    const count = sql(`
      SELECT COUNT(*) FROM [Surveys].[SurveyOutboxEvent] o
      JOIN [Surveys].[SurveyInstance] i ON i.ID = o.SurveyInstanceID
      WHERE i.PublicID = '${regularPublicId}';
    `);
    assertEq(count, '1', 'exactly one SurveyOutboxEvent for the regular instance');
  })();
}

async function cleanup() {
  console.log('\nCleanup:');
  const ids = [testPublicId, regularPublicId].filter(Boolean);
  for (const pid of ids) {
    try {
      sql(`
        DELETE o FROM [Surveys].[SurveyOutboxEvent] o
        JOIN [Surveys].[SurveyInstance] i ON i.ID = o.SurveyInstanceID
        WHERE i.PublicID = '${pid}';
        DELETE a FROM [Surveys].[SurveyAnswer] a
        JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${pid}';
        DELETE r FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${pid}';
        DELETE FROM [Surveys].[SurveyInstance] WHERE PublicID = '${pid}';
      `);
      console.log(`  • removed instance + response + outbox rows for ${pid}`);
    } catch (e) {
      console.log(`  ! could not remove rows for ${pid}: ${(e as Error).message}`);
    }
  }
  for (const hash of [surveyHashId, unpublishedHashId].filter(Boolean)) {
    try {
      await api('DELETE', `/api/Surveys/Survey/${hash}`, { token });
      console.log(`  • deleted survey ${hash}`);
    } catch (e) {
      console.log(`  ! could not delete survey ${hash}: ${(e as Error).message}`);
    }
  }
}

async function main() {
  console.log(`Dashboard test-loop e2e (${TAG})\n`);
  try {
    await seed();
    await verifyCreateAndList();
    await verifyAnswerLoop();
    await verifyOutboxIsolation();
    console.log('\nAll test-loop e2e checks passed ✔');
  } finally {
    await cleanup();
  }
}

main().catch((e) => {
  console.error(`\nFAILED: ${(e as Error).message}`);
  process.exitCode = 1;
});
