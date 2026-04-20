import { api, apiJson, assert, assertEq, sql, step, USERNAME, PASSWORD } from './util.js';

export interface SeededState {
  readonly tag: string;
  readonly bankKey: string;
  token: string;
  surveyHashId: string;
  publishedVersion: number;
  publicId: string;
}

/** Canonical test fixture: inline text + banked NPS + navigationList + cross-screen
 *  logic (`nps<=6 → thanks-default`). Matches the preview fixture one-to-one. */
export function buildSurveyDraft(surveyId: string, bankKey: string) {
  return {
    surveyId,
    version: 0,
    title: { en: `E2E Survey ${bankKey}` },
    locales: ['en'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'welcome',
        title: { en: 'Welcome' },
        questions: [{ type: 'text', id: 'name', title: { en: 'Your name' }, required: false }],
        nextScreen: 'feedback',
      },
      {
        id: 'feedback',
        title: { en: 'How was it?' },
        questions: [{ bankRef: bankKey }],
        nextScreen: 'brand',
      },
      {
        id: 'brand',
        title: { en: 'Which brand?' },
        questions: [
          {
            type: 'navigationList',
            id: 'brand-choice',
            title: { en: 'Pick one' },
            required: true,
            options: [
              { id: 'toyota', label: { en: 'Toyota' }, nextScreen: 'thanks-toyota' },
              { id: 'other', label: { en: 'Other' }, nextScreen: 'thanks-default' },
            ],
          },
        ],
      },
      { id: 'thanks-toyota', title: { en: 'Thanks — Toyota fan!' }, questions: [] },
      { id: 'thanks-default', title: { en: 'Thanks!' }, questions: [] },
    ],
    logic: [{ if: { questionId: bankKey, op: '<=', value: 6 }, then: { goto: 'thanks-default' } }],
  };
}

/** Runs login → seed bank → create survey → patch draft → publish → insert
 *  SurveyInstance via sqlcmd. Returns the populated state so the caller can
 *  pass it to `cleanupSeeded` afterwards. */
export async function seedSurvey(): Promise<SeededState> {
  const tag = `e2e-${Date.now()}`;
  const bankKey = `${tag}-nps`;
  const state: SeededState = {
    tag,
    bankKey,
    token: '',
    surveyHashId: '',
    publishedVersion: 0,
    publicId: '',
  };

  await step('login', async () => {
    const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: USERNAME, Password: PASSWORD },
    });
    state.token = body.Entity.Token;
    assert(state.token.length > 50, 'got a token');
  })();

  await step(`seed banked NPS question (${bankKey})`, async () => {
    const created = await apiJson<{ Entity: { Key: string } }>('POST', '/api/Surveys/BankQuestion', {
      token: state.token,
      body: {
        key: bankKey,
        biColumn: 'nps_e2e',
        retired: false,
        question: {
          type: 'nps',
          id: bankKey,
          title: { en: 'How likely would you recommend us? (0–10)' },
          required: true,
          min: 0,
          max: 10,
        },
      },
    });
    assertEq(created.Entity.Key, bankKey, 'banked Key round-trips');
  })();

  await step('create survey (placeholder)', async () => {
    const draft = buildSurveyDraft('pending', bankKey);
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token: state.token,
      body: { name: `E2E Survey ${tag}`, draft },
    });
    state.surveyHashId = created.Entity.ID;
    assert(state.surveyHashId && state.surveyHashId.length > 0, 'survey got a HashId');
  })();

  await step('update survey draft with real surveyId', async () => {
    const draft = buildSurveyDraft(state.surveyHashId, bankKey);
    const full = await apiJson<{ Entity: Record<string, unknown> }>(
      'GET',
      `/api/Surveys/Survey/${state.surveyHashId}`,
      { token: state.token },
    );
    const body = { ...full.Entity, name: `E2E Survey ${tag}`, draft };
    await apiJson('PUT', `/api/Surveys/Survey/${state.surveyHashId}`, {
      token: state.token,
      body,
    });
  })();

  await step('publish', async () => {
    const r = await api('POST', `/api/Surveys/Publish/${state.surveyHashId}`, { token: state.token });
    const text = await r.text();
    if (!r.ok) throw new Error(`publish failed ${r.status}: ${text}`);
    const body = JSON.parse(text) as { Version?: number; version?: number };
    state.publishedVersion = body.Version ?? body.version ?? 0;
    assert(state.publishedVersion >= 1, `published version ≥ 1 (got ${state.publishedVersion})`);
  })();

  await step('insert SurveyInstance via sqlcmd', async () => {
    const row = sql(`
      DECLARE @name NVARCHAR(200) = N'E2E Survey ${tag}';
      SELECT TOP 1 s.ID, v.ID
      FROM [Surveys].[Survey] s
      JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
      WHERE s.Name = @name AND v.Version = ${state.publishedVersion};
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
    state.publicId = inserted.trim();
    assert(/^[0-9a-fA-F-]{36}$/.test(state.publicId), `publicId looks like a GUID ("${state.publicId}")`);
  })();

  return state;
}

/** Reverse of `seedSurvey`. Hard-deletes the instance + response rows, soft-
 *  deletes the survey and bank entry via their CRUD endpoints. */
export async function cleanupSeeded(state: SeededState) {
  console.log('\nCleanup:');
  if (state.publicId) {
    try {
      sql(`
        DELETE a FROM [Surveys].[SurveyAnswer] a
        JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${state.publicId}';
        DELETE r FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        WHERE i.PublicID = '${state.publicId}';
        DELETE FROM [Surveys].[SurveyInstance] WHERE PublicID = '${state.publicId}';
      `);
      console.log(`  • removed instance + response + answers for ${state.publicId}`);
    } catch (e) {
      console.log(`  ! could not remove instance rows: ${(e as Error).message}`);
    }
  }
  if (state.token && state.surveyHashId) {
    try {
      await api('DELETE', `/api/Surveys/Survey/${state.surveyHashId}`, { token: state.token });
      console.log(`  • survey soft-deleted (${state.surveyHashId})`);
    } catch (e) {
      console.log(`  ! survey soft-delete failed: ${(e as Error).message}`);
    }
  }
  if (state.token && state.bankKey) {
    try {
      const list = await apiJson<{ Value: { ID: string; Key: string }[] }>(
        'GET',
        `/api/Surveys/BankQuestion?$filter=Key eq '${state.bankKey}'`,
        { token: state.token },
      );
      const row = list.Value?.find((x) => x.Key === state.bankKey);
      if (row) await api('DELETE', `/api/Surveys/BankQuestion/${row.ID}`, { token: state.token });
      console.log(`  • bank entry soft-deleted (${state.bankKey})`);
    } catch (e) {
      console.log(`  ! bank delete failed: ${(e as Error).message}`);
    }
  }
}
