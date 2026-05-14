/**
 * Slices 2 + 3 e2e: trigger ingest creates a SurveyInstance with the right snapshot,
 * and a re-POST of the same event is correctly de-duplicated at the DB level.
 *
 * Walk:
 *   1. login as SuperUser
 *   2. create + publish a CSI-GR-style survey carrying one `service-visit-closed` trigger
 *   3. POST a candidate event to /api/Surveys/Triggers/ingest
 *   4. assert response shape (created=1, item.outcome=Created)
 *   5. query the resulting SurveyInstance row via sqlcmd and verify all snapshot fields
 *   6. POST the same candidate again → assert response.skipped=1, item.outcome=Skipped,
 *      items[0].instances[0].publicId == originally created publicId, DB still has 1 row
 *   7. POST a candidate that the filter rejects → assert NoMatch (created=0)
 *   8. POST a candidate with unknown eventKind → assert NoMatch
 *   9. cleanup: hard-delete the instance, soft-delete the survey
 */

import { api, apiJson, assert, assertEq, sql, step, USERNAME, PASSWORD } from './lib/util.js';

interface TriggerState {
  tag: string;
  token: string;
  surveyHashId: string;
  surveyName: string;
  publishedVersion: number;
}

const TRIGGER_ID = 'csi-gr-trigger';
const EVENT_KIND = 'service-visit-closed';
const RECIPIENT_ADDRESS = '+964 770 000 0001';
const RECIPIENT_LOCALE = 'ar';
const RECIPIENT_CUSTOMER_REF = 'cust-e2e-1';

function buildSurveyDraft(surveyId: string, tag: string) {
  return {
    surveyId,
    version: 0,
    title: { en: `E2E CSI-GR ${tag}` },
    locales: ['en'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'q1',
        title: { en: 'Rate' },
        questions: [{ type: 'text', id: 'q1-text', title: { en: 'Comments' }, required: false }],
      },
    ],
    logic: [],
    triggers: [
      {
        id: TRIGGER_ID,
        enabled: true,
        eventKind: EVENT_KIND,
        filter: { questionId: 'candidate.jobType', op: '==', value: 'GR' },
        dedupRecipe: ['templateId', 'recipient.address', 'candidate.dealerId', 'candidate.wip'],
        schedule: { initialDelay: '0d', reminders: ['1d'] },
        channel: 'memory:default',
      },
    ],
  };
}

function buildCandidate(overrides: Partial<{ wip: string; dealerId: string; jobType: string }> = {}) {
  return {
    payload: {
      wip: overrides.wip ?? '40956',
      dealerId: overrides.dealerId ?? '1',
      jobType: overrides.jobType ?? 'GR',
      VIN: 'JTMABBBJ2N4024400',
      CustomerName: 'Kadhem Owaid',
    },
    recipient: {
      address: RECIPIENT_ADDRESS,
      locale: RECIPIENT_LOCALE,
      customerRef: RECIPIENT_CUSTOMER_REF,
    },
  };
}

let state: TriggerState | undefined;

async function main() {
  console.log(`ADP Surveys E2E (slice 2 — trigger ingest)`);

  const tag = `e2e-trig-${Date.now()}`;
  state = { tag, token: '', surveyHashId: '', surveyName: `E2E CSI-GR ${tag}`, publishedVersion: 0 };

  await step('login', async () => {
    const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
      body: { Username: USERNAME, Password: PASSWORD },
    });
    state!.token = body.Entity.Token;
    assert(state!.token.length > 50, 'got a token');
  })();

  await step('create survey (placeholder)', async () => {
    const draft = buildSurveyDraft('pending', tag);
    const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
      token: state!.token,
      body: { name: state!.surveyName, draft },
    });
    state!.surveyHashId = created.Entity.ID;
    assert(state!.surveyHashId && state!.surveyHashId.length > 0, 'survey got a HashId');
  })();

  await step('update survey draft with real surveyId', async () => {
    const full = await apiJson<{ Entity: Record<string, unknown> }>(
      'GET',
      `/api/Surveys/Survey/${state!.surveyHashId}`,
      { token: state!.token },
    );
    const draft = buildSurveyDraft(state!.surveyHashId, tag);
    const body = { ...full.Entity, name: state!.surveyName, draft };
    await apiJson('PUT', `/api/Surveys/Survey/${state!.surveyHashId}`, { token: state!.token, body });
  })();

  await step('publish', async () => {
    const r = await api('POST', `/api/Surveys/Publish/${state!.surveyHashId}`, { token: state!.token });
    const text = await r.text();
    if (!r.ok) throw new Error(`publish failed ${r.status}: ${text}`);
    const body = JSON.parse(text) as { Version?: number; version?: number };
    state!.publishedVersion = body.Version ?? body.version ?? 0;
    assert(state!.publishedVersion >= 1, `published version ≥ 1 (got ${state!.publishedVersion})`);
  })();

  type IngestResult = {
    created: number;
    skipped: number;
    failed: number;
    items: Array<{
      outcome: string;
      instances: Array<{ surveyId: number; triggerId: string; publicId: string }>;
      error?: string;
    }>;
  };

  let createdInstancePublicId = '';

  await step('POST matching candidate to /api/Surveys/Triggers/ingest', async () => {
    const result = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate()] },
    });

    assertEq(result.created, 1, 'response.created === 1');
    assertEq(result.skipped, 0, 'response.skipped === 0');
    assertEq(result.failed, 0, 'response.failed === 0');
    assertEq(result.items.length, 1, 'one item in response');
    assertEq(result.items[0].outcome, 'Created', 'item.outcome === Created');
    assertEq(result.items[0].instances.length, 1, 'one instance produced');
    assertEq(result.items[0].instances[0].triggerId, TRIGGER_ID, 'instance carries TriggerId');
    createdInstancePublicId = result.items[0].instances[0].publicId;
    assert(/^[0-9a-fA-F-]{36}$/.test(createdInstancePublicId), `publicId looks like a GUID ("${createdInstancePublicId}")`);
  })();

  await step('verify SurveyInstance row snapshot in DB', () => {
    const row = sql(`
      SELECT TriggerId, Channel, RecipientAddress, RecipientLocale, RemindersRemaining,
             CASE WHEN UniqueHash IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             CASE WHEN NextSendAt IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             Status, CustomerRef
      FROM [Surveys].[SurveyInstance]
      WHERE PublicID = '${createdInstancePublicId}';
    `);
    const [triggerId, channel, address, locale, remindersStr, hashState, nextSendState, statusStr, customerRef] = row.split('\t');

    assertEq(triggerId?.trim(), TRIGGER_ID, 'TriggerId column');
    assertEq(channel?.trim(), 'memory:default', 'Channel column');
    assertEq(address?.trim(), RECIPIENT_ADDRESS, 'RecipientAddress column');
    assertEq(locale?.trim(), RECIPIENT_LOCALE, 'RecipientLocale column');
    assertEq(Number(remindersStr?.trim()), 1, 'RemindersRemaining = 1 (one reminder configured)');
    assertEq(hashState?.trim(), 'NOTNULL', 'UniqueHash populated by ingest service');
    assertEq(nextSendState?.trim(), 'NOTNULL', 'NextSendAt computed at creation');
    assertEq(Number(statusStr?.trim()), 0, 'Status = Pending (0)');
    assertEq(customerRef?.trim(), RECIPIENT_CUSTOMER_REF, 'CustomerRef snapshotted from recipient');
  })();

  await step('verify MetaDataJson captured the candidate payload', () => {
    const metaRaw = sql(`
      SELECT MetaDataJson FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `);
    assert(metaRaw.includes('"wip":"40956"'), `MetaDataJson contains wip ("${metaRaw.slice(0, 200)}")`);
    assert(metaRaw.includes('"dealerId":"1"'), 'MetaDataJson contains dealerId');
    assert(metaRaw.includes('"jobType":"GR"'), 'MetaDataJson contains jobType');
  })();

  await step('POST same candidate again → Skipped (DB-level dedup via UniqueHash)', async () => {
    const result = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate()] },
    });

    assertEq(result.created, 0, 'response.created === 0');
    assertEq(result.skipped, 1, 'response.skipped === 1');
    assertEq(result.failed, 0, 'response.failed === 0');
    assertEq(result.items.length, 1, 'one item in response');
    assertEq(result.items[0].outcome, 'Skipped', 'item.outcome === Skipped');
    assertEq(result.items[0].instances.length, 1, 'one matched instance reported');
    assertEq(
      result.items[0].instances[0].publicId,
      createdInstancePublicId,
      'skipped item carries the EXISTING instance publicId (caller can correlate)',
    );

    const countStr = sql(`
      SELECT COUNT(1) FROM [Surveys].[SurveyInstance]
      WHERE SurveyID IN (SELECT ID FROM [Surveys].[Survey] WHERE Name = N'${state!.surveyName}');
    `);
    assertEq(Number(countStr.trim()), 1, 'DB still has exactly one instance row (dedup enforced)');
  })();

  await step('POST same candidate twice in one batch → first Created, second Skipped', async () => {
    // Use a different WIP so the within-batch dedup doesn't collide with the row from earlier steps.
    const wip = '99999';
    const result = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate({ wip }), buildCandidate({ wip })] },
    });

    assertEq(result.created, 1, 'first item creates a new instance');
    assertEq(result.skipped, 1, 'second item dedups against the first');
    assertEq(result.items[0].outcome, 'Created', 'items[0].outcome === Created');
    assertEq(result.items[1].outcome, 'Skipped', 'items[1].outcome === Skipped');
    assertEq(
      result.items[1].instances[0].publicId,
      result.items[0].instances[0].publicId,
      'within-batch dedup: items[1] points back to items[0] publicId',
    );
  })();

  await step('POST non-matching candidate (jobType=PM) → NoMatch, no instance created', async () => {
    const countBefore = Number(
      sql(`
        SELECT COUNT(1) FROM [Surveys].[SurveyInstance]
        WHERE SurveyID IN (SELECT ID FROM [Surveys].[Survey] WHERE Name = N'${state!.surveyName}');
      `).trim(),
    );

    const result = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate({ jobType: 'PM' })] },
    });

    assertEq(result.created, 0, 'response.created === 0');
    assertEq(result.items[0].outcome, 'NoMatch', 'item.outcome === NoMatch');
    assertEq(result.items[0].instances.length, 0, 'no instances produced');

    const countAfter = Number(
      sql(`
        SELECT COUNT(1) FROM [Surveys].[SurveyInstance]
        WHERE SurveyID IN (SELECT ID FROM [Surveys].[Survey] WHERE Name = N'${state!.surveyName}');
      `).trim(),
    );
    assertEq(countAfter, countBefore, 'no new instance rows after filter rejection');
  })();

  await step('POST candidate with unknown eventKind → no triggers matched', async () => {
    const result = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: 'no-such-event', items: [buildCandidate()] },
    });
    assertEq(result.created, 0, 'response.created === 0 for unknown eventKind');
    assertEq(result.items[0].outcome, 'NoMatch', 'item.outcome === NoMatch');
  })();

  await step('POST /scheduler/tick → instance routed to memory channel, schedule advanced', async () => {
    const result = await apiJson<{ Processed: number }>('POST', '/api/Surveys/Triggers/scheduler/tick', {
      token: state!.token,
    });
    assert(result.Processed >= 1, `tick processed at least our row (got ${result.Processed})`);

    const row = sql(`
      SELECT Status,
             CASE WHEN LastSentAt IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             CASE WHEN NextSendAt IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             CASE WHEN NextSendAt > LastSentAt THEN 'FUTURE' ELSE 'NOT_FUTURE' END,
             RemindersRemaining,
             CASE WHEN DeliveryLogJson LIKE '%delivered%' THEN 'DELIVERED' ELSE 'NOPE' END
      FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `);
    const [statusStr, lastSentState, nextSendState, nextSendVsLast, remindersStr, logState] = row.split('\t');

    assertEq(Number(statusStr?.trim()), 1, 'Status flipped Pending→Sent (1)');
    assertEq(lastSentState?.trim(), 'NOTNULL', 'LastSentAt populated');
    assertEq(nextSendState?.trim(), 'NOTNULL', 'NextSendAt advanced (not nulled — fixture has a 1d reminder)');
    assertEq(nextSendVsLast?.trim(), 'FUTURE', 'NextSendAt is after LastSentAt (advanced by reminder delay)');
    assertEq(Number(remindersStr?.trim()), 0, 'RemindersRemaining decremented 1→0');
    assertEq(logState?.trim(), 'DELIVERED', 'DeliveryLogJson contains a delivered entry');
  })();

  await step('POST /scheduler/tick again → row not re-sent (NextSendAt is in the future)', async () => {
    const beforeLastSent = sql(`
      SELECT CONVERT(NVARCHAR(50), LastSentAt) FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `).trim();

    await apiJson('POST', '/api/Surveys/Triggers/scheduler/tick', { token: state!.token });

    const afterLastSent = sql(`
      SELECT CONVERT(NVARCHAR(50), LastSentAt) FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `).trim();

    assertEq(afterLastSent, beforeLastSent, 'LastSentAt unchanged — second tick did not re-process this row');
  })();

  await step('Customer submits response → instance Completed + schedule cleared', async () => {
    await apiJson('POST', `/api/Surveys/SurveyInstances/${createdInstancePublicId}/responses`, {
      body: {
        schemaVersion: state!.publishedVersion,
        answers: { 'q1-text': 'great service' },
        meta: {
          startedAt: new Date(Date.now() - 30_000).toISOString(),
          completedAt: new Date().toISOString(),
        },
      },
    });

    const row = sql(`
      SELECT Status,
             CASE WHEN NextSendAt IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             RemindersRemaining
      FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `);
    const [statusStr, nextSendState, remindersStr] = row.split('\t');

    assertEq(Number(statusStr?.trim()), 3, 'Status flipped Sent → Completed (3)');
    assertEq(nextSendState?.trim(), 'NULL', 'NextSendAt cleared on submit');
    assertEq(Number(remindersStr?.trim()), 0, 'RemindersRemaining zeroed on submit');
  })();

  await step('Submit also wrote a Pending outbox event in the same transaction', async () => {
    const row = sql(`
      SELECT TOP 1 Status, EventType,
             CASE WHEN PayloadJson IS NULL THEN 'NULL' ELSE 'POPULATED' END,
             CASE WHEN PayloadJson LIKE '%great service%' THEN 'HAS_ANSWER' ELSE 'NOPE' END
      FROM [Surveys].[SurveyOutboxEvent]
      WHERE SurveyInstanceID IN (
        SELECT ID FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}'
      )
      ORDER BY ID DESC;
    `);
    const [statusStr, eventType, payloadState, hasAnswer] = row.split('\t');
    assertEq(Number(statusStr?.trim()), 0, 'outbox Status = Pending (0)');
    assertEq(eventType?.trim(), 'response-completed', 'EventType set');
    assertEq(payloadState?.trim(), 'POPULATED', 'PayloadJson populated');
    assertEq(hasAnswer?.trim(), 'HAS_ANSWER', 'PayloadJson contains the submitted answer text');
  })();

  await step('POST /scheduler/tick after submit → completed row not processed, outbox event dispatched', async () => {
    const beforeLog = sql(`
      SELECT ISNULL(LEN(DeliveryLogJson), 0) FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `).trim();

    const tick = await apiJson<{ Processed: number; Expired: number; Dispatched: number }>(
      'POST', '/api/Surveys/Triggers/scheduler/tick', { token: state!.token },
    );
    assert(tick.Dispatched >= 1, `tick.Dispatched >= 1 (got ${tick.Dispatched})`);

    const afterLog = sql(`
      SELECT ISNULL(LEN(DeliveryLogJson), 0) FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}';
    `).trim();
    assertEq(afterLog, beforeLog, 'DeliveryLogJson length unchanged — completed row was skipped by the active-status filter');
  })();

  await step('Outbox event is now Dispatched, subscriber log captured', () => {
    // Avoiding embedded double-quotes in the LIKE pattern — Windows arg parsing in
    // execFileSync(sqlcmd, …) breaks on them. Status=1 already implies every subscriber
    // returned success (the service's failure-path sets Status=Failed instead).
    const row = sql(`
      SELECT TOP 1 Status,
             CASE WHEN DispatchedAt IS NULL THEN 'NULL' ELSE 'NOTNULL' END,
             CASE WHEN DispatchLogJson LIKE '%memory:default%' THEN 'HAS_MEMORY' ELSE 'NOPE' END,
             Attempts
      FROM [Surveys].[SurveyOutboxEvent]
      WHERE SurveyInstanceID IN (
        SELECT ID FROM [Surveys].[SurveyInstance] WHERE PublicID = '${createdInstancePublicId}'
      )
      ORDER BY ID DESC;
    `);
    const [statusStr, dispatchedAt, hasMemoryKey, attemptsStr] = row.split('\t');
    assertEq(Number(statusStr?.trim()), 1, 'outbox Status = Dispatched (1) — implies every subscriber succeeded');
    assertEq(dispatchedAt?.trim(), 'NOTNULL', 'DispatchedAt populated');
    assertEq(hasMemoryKey?.trim(), 'HAS_MEMORY', 'DispatchLogJson contains the memory subscriber key');
    assertEq(Number(attemptsStr?.trim()), 1, 'Attempts = 1 (first try succeeded)');
  })();

  await step('Backdated row (LastSentAt 31d ago) flips to Expired on tick', async () => {
    // Fresh ingest with a unique WIP so we don't collide with anything earlier in the run.
    const ingest = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate({ wip: '88888' })] },
    });
    assertEq(ingest.created, 1, 'fresh instance created');
    const expiringPublicId = ingest.items[0].instances[0].publicId;

    // Simulate "all reminders sent 31 days ago, no submission" without waiting 31 days.
    // The required SET block satisfies SQL Server's filtered-index update preconditions —
    // sqlcmd's default ARITHABORT is OFF, which makes UPDATEs that touch the
    // IX_SurveyInstance_Status_NextSendAt_Active filtered index (Status + NextSendAt are
    // both key + filter columns here) fail with msg 1934.
    sql(`
      SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
      SET NUMERIC_ROUNDABORT OFF;
      UPDATE [Surveys].[SurveyInstance]
      SET Status = 1,
          LastSentAt = DATEADD(day, -31, SYSDATETIMEOFFSET()),
          NextSendAt = NULL,
          RemindersRemaining = 0
      WHERE PublicID = '${expiringPublicId}';
    `);

    const tick = await apiJson<{ Processed: number; Expired: number }>(
      'POST', '/api/Surveys/Triggers/scheduler/tick',
      { token: state!.token },
    );
    assert(tick.Expired >= 1, `tick.Expired >= 1 (got ${tick.Expired})`);

    const status = sql(`
      SELECT Status FROM [Surveys].[SurveyInstance] WHERE PublicID = '${expiringPublicId}';
    `).trim();
    assertEq(Number(status), 4, 'backdated row flipped to Expired (4)');
  })();

  await step('Recent row (LastSentAt 5m ago) stays Sent — within grace window', async () => {
    const ingest = await apiJson<IngestResult>('POST', '/api/Surveys/Triggers/ingest', {
      token: state!.token,
      body: { eventKind: EVENT_KIND, items: [buildCandidate({ wip: '77777' })] },
    });
    assertEq(ingest.created, 1, 'fresh instance created');
    const recentPublicId = ingest.items[0].instances[0].publicId;

    sql(`
      SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
      SET NUMERIC_ROUNDABORT OFF;
      UPDATE [Surveys].[SurveyInstance]
      SET Status = 1,
          LastSentAt = DATEADD(minute, -5, SYSDATETIMEOFFSET()),
          NextSendAt = NULL,
          RemindersRemaining = 0
      WHERE PublicID = '${recentPublicId}';
    `);

    await apiJson('POST', '/api/Surveys/Triggers/scheduler/tick', { token: state!.token });

    const status = sql(`
      SELECT Status FROM [Surveys].[SurveyInstance] WHERE PublicID = '${recentPublicId}';
    `).trim();
    assertEq(Number(status), 1, 'recent row stays Sent (1) — within ExpiryGracePeriod');
  })();

  console.log('\nAll assertions passed.');
}

async function cleanup() {
  console.log('\nCleanup:');
  if (state?.surveyName) {
    try {
      // Order matters: outbox + answers + responses before instances, because of FK
      // restrict relationships (SurveyOutboxEvent → SurveyInstance is OnDelete=Restrict).
      sql(`
        DELETE FROM [Surveys].[SurveyOutboxEvent]
        WHERE SurveyInstanceID IN (
          SELECT i.ID FROM [Surveys].[SurveyInstance] i
          JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
          WHERE s.Name = N'${state.surveyName}'
        );

        DELETE a FROM [Surveys].[SurveyAnswer] a
        JOIN [Surveys].[SurveyResponse] r ON r.ID = a.SurveyResponseID
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
        WHERE s.Name = N'${state.surveyName}';

        DELETE r FROM [Surveys].[SurveyResponse] r
        JOIN [Surveys].[SurveyInstance] i ON i.ID = r.SurveyInstanceID
        JOIN [Surveys].[Survey] s ON s.ID = i.SurveyID
        WHERE s.Name = N'${state.surveyName}';

        DELETE FROM [Surveys].[SurveyInstance]
        WHERE SurveyID IN (SELECT ID FROM [Surveys].[Survey] WHERE Name = N'${state.surveyName}');
      `);
      console.log(`  • removed outbox + answers + responses + instances for ${state.surveyName}`);
    } catch (e) {
      console.log(`  ! could not remove instances: ${(e as Error).message}`);
    }
  }
  if (state?.token && state.surveyHashId) {
    try {
      await api('DELETE', `/api/Surveys/Survey/${state.surveyHashId}`, { token: state.token });
      console.log(`  • survey soft-deleted (${state.surveyHashId})`);
    } catch (e) {
      console.log(`  ! survey soft-delete failed: ${(e as Error).message}`);
    }
  }
}

main()
  .then(cleanup)
  .catch(async (err) => {
    console.error('\nE2E FAILED:', (err as Error).message ?? err);
    await cleanup();
    process.exit(1);
  });
