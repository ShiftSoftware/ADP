/**
 * Teardown for `seed-demo.ts`. Removes every `demo-` tagged record:
 *   - SurveyAnswer / SurveyResponse / SurveyInstance rows for `demo:` surveys
 *   - `demo:` surveys (soft-delete via API — versions stay for audit)
 *   - `demo-` bank questions (soft-delete via API)
 *
 * Safe to run even when nothing is seeded. Idempotent.
 */

import { api, apiJson, sql, USERNAME, PASSWORD } from './lib/util.js';

const TAG = 'demo';

async function main() {
  console.log('Tearing down demo surveys…\n');
  const loginBody = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
    body: { Username: USERNAME, Password: PASSWORD },
  });
  const token = loginBody.Entity.Token;

  // 1. Instance / response / answer rows keyed by the `demo:` survey name.
  try {
    sql(`
      DELETE a FROM [Surveys].[SurveyAnswer] a
      JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
      JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
      JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
      WHERE s.Name LIKE N'${TAG}:%';
      DELETE r FROM [Surveys].[SurveyResponse] r
      JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
      JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
      WHERE s.Name LIKE N'${TAG}:%';
      DELETE i FROM [Surveys].[SurveyInstance] i
      JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
      WHERE s.Name LIKE N'${TAG}:%';
    `);
    console.log('• instance / response / answer rows cleared');
  } catch (e) {
    console.log(`  ! sqlcmd delete failed: ${(e as Error).message}`);
  }

  // 2. Surveys — soft-delete via CRUD endpoint.
  try {
    const list = await apiJson<{ Value: { ID: string; Name: string }[] }>(
      'GET',
      `/api/Surveys/Survey?$filter=startswith(Name, '${TAG}:')&top=100`,
      { token },
    );
    for (const row of list.Value ?? []) {
      if (!row.Name.startsWith(`${TAG}:`)) continue;
      await api('DELETE', `/api/Surveys/Survey/${row.ID}`, { token });
      console.log(`• survey deleted: ${row.Name}`);
    }
  } catch (e) {
    console.log(`  ! survey delete failed: ${(e as Error).message}`);
  }

  // 3. Bank questions — soft-delete via CRUD.
  try {
    const list = await apiJson<{ Value: { ID: string; Key: string }[] }>(
      'GET',
      `/api/Surveys/BankQuestion?$filter=startswith(Key, '${TAG}-')&top=100`,
      { token },
    );
    for (const row of list.Value ?? []) {
      if (!row.Key.startsWith(`${TAG}-`)) continue;
      await api('DELETE', `/api/Surveys/BankQuestion/${row.ID}`, { token });
      console.log(`• bank deleted: ${row.Key}`);
    }
  } catch (e) {
    console.log(`  ! bank delete failed: ${(e as Error).message}`);
  }

  console.log('\nDone.');
}

main().catch((err) => {
  console.error('\nUNSEED FAILED:', (err as Error).message ?? err);
  process.exit(1);
});
