/**
 * SF4 demo runner — prepares a live instance of the seeded "SF4 — Six-Month
 * Purchase Follow-up" survey so it can be walked in the standalone renderer.
 *
 * Prereqs: Sample.API on :5134 (its startup seeder inserts the SF4 draft),
 * standalone renderer dev server on :5190.
 *
 *   cd renderer/e2e && npx tsx sf4-demo.ts
 *
 * Steps: login → find the survey by IntegrationId → publish (no-op when the
 * draft hash is unchanged) → ingest a sample `vehicle-purchased` candidate →
 * fast-forward the 180-day initial delay → scheduler tick (records the channel
 * send) → print the public URL.
 *
 * Each run ingests a fresh VIN, so every demo gets its own untouched instance
 * (dedup recipe is dealerId+vin). Re-running is always safe.
 */

import { api, apiJson, sql, USERNAME, PASSWORD } from './lib/util.js';

const INTEGRATION_ID = 'sf4-purchase-followup';
const EVENT_KIND = 'vehicle-purchased';
const STANDALONE_BASE = 'http://localhost:5190';

interface IngestResult {
  created: number;
  items: { outcome: string; instances: { publicId: string; triggerId: string }[] }[];
}

async function main() {
  console.log('SF4 demo runner');

  const auth = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
    body: { Username: USERNAME, Password: PASSWORD },
  });
  const token = auth.Entity.Token;
  console.log('  ✔ logged in');

  const list = await apiJson<{ Value: { ID: string; Name: string }[] }>(
    'GET',
    `/api/Surveys/Survey?$filter=IntegrationId eq '${INTEGRATION_ID}'`,
    { token },
  );
  const survey = list.Value?.[0];
  if (!survey) {
    throw new Error(
      `No survey with IntegrationId '${INTEGRATION_ID}' — is Sample.API running? Its startup seeder inserts the draft.`,
    );
  }
  console.log(`  ✔ found "${survey.Name}" (${survey.ID})`);

  const publishResp = await api('POST', `/api/Surveys/Publish/${survey.ID}`, { token });
  const publishText = await publishResp.text();
  if (!publishResp.ok) throw new Error(`publish failed: ${publishResp.status} ${publishText}`);
  const version = (JSON.parse(publishText) as { Version?: number }).Version ?? 0;
  console.log(`  ✔ published (version ${version})`);

  // Fresh VIN per run → fresh instance (dedup is dealerId+vin).
  const vin = `DEMO${Date.now()}`;
  const ingest = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
    token,
    body: {
      eventKind: EVENT_KIND,
      items: [
        {
          payload: { dealerId: '1', vin, vehicleModel: 'Corolla', purchasedAt: '2025-12-04' },
          recipient: { address: '+964 770 000 0000', locale: 'en', customerRef: 'demo-customer-1' },
        },
      ],
    },
  });
  const publicId = ingest.items[0]?.instances?.[0]?.publicId;
  if (ingest.created !== 1 || !publicId) {
    throw new Error(`ingest did not create an instance: ${JSON.stringify(ingest)}`);
  }
  console.log(`  ✔ ingested ${EVENT_KIND} (vin ${vin}) → instance ${publicId}`);

  // The trigger's initial delay is 180d (the six-month mark), so NextSendAt is
  // half a year out. Pretend the time has passed — demo-only; production relies
  // on the periodic scheduler reaching the real send time.
  sql(`UPDATE [Surveys].[SurveyInstance] SET NextSendAt = SYSUTCDATETIME() WHERE PublicID = '${publicId}';`);
  const tick = await apiJson<{ Processed: number }>('POST', '/api/Surveys/Triggers/scheduler/tick', { token });
  console.log(`  ✔ fast-forwarded 180d + scheduler tick (processed ${tick.Processed}) — instance is now Sent`);

  const log = sql(
    `SELECT Status, RemindersRemaining FROM [Surveys].[SurveyInstance] WHERE PublicID = '${publicId}';`,
  );
  console.log(`  · instance state: [Status, RemindersRemaining] = [${log.replace('\t', ', ')}]`);

  console.log('\n  Survey URL (open as the customer):');
  console.log(`    ${STANDALONE_BASE}/s/${publicId}\n`);
}

main().catch((e) => {
  console.error(`\nSF4 demo runner FAILED: ${(e as Error).message}`);
  process.exit(1);
});
