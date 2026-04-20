/**
 * Seeds a realistic set of surveys + shared banked questions into the live
 * Sample.API database for manual inspection. Unlike `run.ts` this script does
 * **no cleanup** — the seeded records stay visible in the Surveys/SurveyList
 * page until the matching teardown script is run (`unseed-demo.ts`).
 *
 * Every record carries a `demo-` prefix so it's easy to spot + bulk-delete.
 *
 * Run: `npx tsx seed-demo.ts`
 * Undo: `npx tsx unseed-demo.ts`
 */

import { apiJson, sql } from './lib/util.js';
import { api, USERNAME, PASSWORD } from './lib/util.js';

const TAG = 'demo';

// ─── Shared banked questions ─────────────────────────────────────────────────
// These exist ONCE and are referenced by every survey — the whole point of the
// Question Bank per Decision #11. If you re-run this script the `demo-` keys
// are already there; we skip re-insert and reuse them.

interface BankEntry {
  key: string;
  biColumn: string;
  question: Record<string, unknown>;
}

const BANK: BankEntry[] = [
  {
    key: `${TAG}-overall-nps`,
    biColumn: 'overall_nps',
    question: {
      type: 'nps',
      id: `${TAG}-overall-nps`,
      title: {
        en: 'How likely are you to recommend us to a friend or colleague?',
        ar: 'ما مدى احتمالية توصيتك بنا لصديق أو زميل؟',
      },
      required: true,
      min: 0,
      max: 10,
      lowLabel: { en: 'Not likely', ar: 'غير محتمل' },
      highLabel: { en: 'Very likely', ar: 'محتمل جداً' },
    },
  },
  {
    key: `${TAG}-overall-rating`,
    biColumn: 'overall_rating',
    question: {
      type: 'rating',
      id: `${TAG}-overall-rating`,
      title: { en: 'Overall experience', ar: 'التجربة العامة' },
      required: true,
      max: 5,
    },
  },
  {
    key: `${TAG}-would-visit-again`,
    biColumn: 'would_visit_again',
    question: {
      type: 'yesNo',
      id: `${TAG}-would-visit-again`,
      title: { en: 'Would you visit us again?', ar: 'هل ستزورنا مرة أخرى؟' },
      required: true,
    },
  },
];

// ─── Survey definitions ──────────────────────────────────────────────────────

interface SurveyDef {
  name: string;
  draft: (surveyId: string) => Record<string, unknown>;
}

const serviceFeedback: SurveyDef = {
  name: `${TAG}: Service Visit Feedback`,
  draft: (surveyId) => ({
    surveyId,
    version: 0,
    title: { en: 'Service Visit Feedback', ar: 'تعليقات زيارة الصيانة' },
    description: {
      en: 'Tell us about your recent service visit — it takes about two minutes.',
      ar: 'أخبرنا عن زيارة الصيانة الأخيرة — تستغرق حوالي دقيقتين.',
    },
    locales: ['en', 'ar'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'welcome',
        title: { en: 'Thanks for visiting', ar: 'شكراً لزيارتك' },
        description: {
          en: 'Your feedback helps us serve you better next time.',
          ar: 'تساعدنا ملاحظاتك على خدمتك بشكل أفضل في المرة القادمة.',
        },
        questions: [
          {
            type: 'text',
            id: 'customer-name',
            title: { en: 'Your name (optional)', ar: 'اسمك (اختياري)' },
            required: false,
          },
        ],
        nextScreen: 'overall',
      },
      {
        id: 'overall',
        title: { en: 'Overall experience', ar: 'التجربة العامة' },
        questions: [{ bankRef: `${TAG}-overall-nps` }],
        nextScreen: 'details',
      },
      {
        id: 'details',
        title: { en: 'Rate these areas', ar: 'قيّم هذه الجوانب' },
        questions: [
          {
            type: 'rating',
            id: 'service-speed',
            title: { en: 'How fast was the service?', ar: 'ما مدى سرعة الخدمة؟' },
            required: true,
            max: 5,
          },
          {
            type: 'rating',
            id: 'staff-friendliness',
            title: { en: 'Staff friendliness', ar: 'ودّية الموظفين' },
            required: true,
            max: 5,
          },
          {
            type: 'rating',
            id: 'cleanliness',
            title: { en: 'Cleanliness of the facility', ar: 'نظافة المنشأة' },
            required: false,
            max: 5,
          },
        ],
        nextScreen: 'improvements',
      },
      {
        id: 'improvements',
        title: { en: 'Anything that needed improvement?', ar: 'هل هناك ما يحتاج إلى تحسين؟' },
        questions: [
          {
            type: 'multiChoice',
            id: 'improvements',
            title: { en: 'Pick all that apply', ar: 'اختر كل ما ينطبق' },
            required: false,
            options: [
              { id: 'wait-time', label: { en: 'Wait time', ar: 'وقت الانتظار' } },
              { id: 'parts-availability', label: { en: 'Parts availability', ar: 'توفر القطع' } },
              { id: 'pricing', label: { en: 'Pricing', ar: 'الأسعار' } },
              { id: 'communication', label: { en: 'Communication', ar: 'التواصل' } },
              { id: 'none', label: { en: 'Nothing — it was great', ar: 'لا شيء — كانت ممتازة' } },
            ],
          },
        ],
        nextScreen: 'loyalty',
      },
      {
        id: 'loyalty',
        title: { en: 'Coming back?', ar: 'ستعود إلينا؟' },
        questions: [{ bankRef: `${TAG}-would-visit-again` }],
        nextScreen: 'comments',
      },
      {
        id: 'comments',
        title: { en: 'Anything else you’d like to share?', ar: 'هل هناك شيء آخر تريد مشاركته؟' },
        questions: [
          {
            type: 'paragraph',
            id: 'open-comments',
            title: { en: 'Your comments', ar: 'تعليقاتك' },
            required: false,
            placeholder: {
              en: 'Optional — what would you like us to know?',
              ar: 'اختياري — ماذا تريدنا أن نعرف؟',
            },
          },
        ],
      },
      {
        id: 'recover',
        title: { en: 'We’d like to make it right', ar: 'نود أن نصحح الأمر' },
        description: {
          en: 'Sorry we let you down. Please share what happened and we’ll follow up.',
          ar: 'نأسف لخذلانك. شاركنا ما حدث وسنتابع معك.',
        },
        questions: [
          {
            type: 'paragraph',
            id: 'recover-details',
            title: { en: 'What went wrong?', ar: 'ما الذي حدث؟' },
            required: true,
          },
          {
            type: 'text',
            id: 'recover-phone',
            title: { en: 'Phone number to reach you', ar: 'رقم الهاتف للتواصل' },
            required: false,
          },
        ],
      },
      {
        id: 'thanks',
        title: { en: 'Thank you!', ar: 'شكراً لك!' },
        description: { en: 'Your feedback has been recorded.', ar: 'تم تسجيل ملاحظاتك.' },
        questions: [],
      },
    ],
    logic: [
      // Detractors (NPS <= 6) get a recovery prompt before the thanks screen.
      {
        if: { questionId: `${TAG}-overall-nps`, op: '<=', value: 6 },
        then: { goto: 'recover' },
      },
    ],
  }),
};

const testDrive: SurveyDef = {
  name: `${TAG}: Test Drive Follow-up`,
  draft: (surveyId) => ({
    surveyId,
    version: 0,
    title: { en: 'Test Drive Follow-up', ar: 'متابعة القيادة التجريبية' },
    locales: ['en', 'ar'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'intro',
        title: { en: 'How was your test drive?', ar: 'كيف كانت قيادتك التجريبية؟' },
        questions: [{ bankRef: `${TAG}-overall-rating` }],
        nextScreen: 'model',
      },
      {
        id: 'model',
        title: { en: 'Which model did you drive?', ar: 'أي طراز قدت؟' },
        description: {
          en: 'Tap one — we’ll follow up with model-specific info.',
          ar: 'اختر واحداً — سنتابع معك بمعلومات خاصة بالطراز.',
        },
        questions: [
          {
            type: 'navigationList',
            id: 'model-choice',
            title: { en: 'Pick one', ar: 'اختر واحداً' },
            required: true,
            options: [
              { id: 'rav4', label: { en: 'Toyota RAV4', ar: 'تويوتا راف 4' }, nextScreen: 'rav4-thanks' },
              { id: 'camry', label: { en: 'Toyota Camry', ar: 'تويوتا كامري' }, nextScreen: 'camry-thanks' },
              { id: 'other', label: { en: 'Another model', ar: 'طراز آخر' }, nextScreen: 'other-feedback' },
            ],
          },
        ],
      },
      {
        id: 'rav4-thanks',
        title: { en: 'Thanks — we’ll send RAV4 info', ar: 'شكراً — سنرسل معلومات راف 4' },
        description: {
          en: 'Expect an email with pricing + current offers within 24 hours.',
          ar: 'توقع بريداً إلكترونياً بالأسعار والعروض الحالية خلال 24 ساعة.',
        },
        questions: [],
      },
      {
        id: 'camry-thanks',
        title: { en: 'Thanks — we’ll send Camry info', ar: 'شكراً — سنرسل معلومات كامري' },
        description: {
          en: 'Expect an email with pricing + current offers within 24 hours.',
          ar: 'توقع بريداً إلكترونياً بالأسعار والعروض الحالية خلال 24 ساعة.',
        },
        questions: [],
      },
      {
        id: 'other-feedback',
        title: { en: 'Which model were you looking at?', ar: 'أي طراز كنت تنظر إليه؟' },
        questions: [
          {
            type: 'text',
            id: 'other-model',
            title: { en: 'Model name', ar: 'اسم الطراز' },
            required: true,
          },
        ],
      },
    ],
  }),
};

const deliveryExperience: SurveyDef = {
  name: `${TAG}: New Car Delivery Experience`,
  draft: (surveyId) => ({
    surveyId,
    version: 0,
    title: { en: 'New Car Delivery Experience', ar: 'تجربة تسليم السيارة الجديدة' },
    locales: ['en', 'ar'],
    defaultLocale: 'en',
    screens: [
      {
        id: 'main',
        title: {
          en: 'Congrats on your new car!',
          ar: 'تهانينا على سيارتك الجديدة!',
        },
        description: {
          en: 'A few quick questions about the handover.',
          ar: 'بضعة أسئلة سريعة حول التسليم.',
        },
        questions: [
          { bankRef: `${TAG}-overall-rating` },
          {
            type: 'dropdown',
            id: 'branch',
            title: { en: 'Which branch delivered your car?', ar: 'أي فرع سلّمك السيارة؟' },
            required: true,
            placeholder: { en: 'Pick a branch', ar: 'اختر فرعاً' },
            options: [
              { id: 'erbil-main', label: { en: 'Erbil — Main', ar: 'أربيل — الرئيسي' } },
              { id: 'erbil-airport', label: { en: 'Erbil — Airport Road', ar: 'أربيل — طريق المطار' } },
              { id: 'sulaimani', label: { en: 'Sulaimani', ar: 'السليمانية' } },
              { id: 'duhok', label: { en: 'Duhok', ar: 'دهوك' } },
              { id: 'baghdad', label: { en: 'Baghdad', ar: 'بغداد' } },
            ],
          },
          {
            type: 'yesNo',
            id: 'walkthrough-done',
            title: {
              en: 'Did the sales team walk you through the car’s features?',
              ar: 'هل شرح لك فريق المبيعات ميزات السيارة؟',
            },
            required: true,
          },
          {
            type: 'paragraph',
            id: 'delivery-notes',
            title: {
              en: 'Anything else you’d like to share about the delivery?',
              ar: 'هل هناك شيء آخر تريد مشاركته حول التسليم؟',
            },
            required: false,
            placeholder: {
              en: 'Optional — anything that delighted or disappointed you',
              ar: 'اختياري — أي شيء أسعدك أو خيب ظنك',
            },
          },
        ],
      },
      {
        id: 'thanks',
        title: { en: 'Thank you!', ar: 'شكراً لك!' },
        description: {
          en: 'Enjoy your new car. Service reminders will come via SMS.',
          ar: 'استمتع بسيارتك الجديدة. ستصلك تذكيرات الصيانة عبر الرسائل القصيرة.',
        },
        questions: [],
      },
    ],
  }),
};

const SURVEYS: SurveyDef[] = [serviceFeedback, testDrive, deliveryExperience];

// ─── Orchestration ───────────────────────────────────────────────────────────

interface Login { token: string; }

async function login(): Promise<Login> {
  const body = await apiJson<{ Entity: { Token: string } }>('POST', '/api/Auth/Login', {
    body: { Username: USERNAME, Password: PASSWORD },
  });
  return { token: body.Entity.Token };
}

async function ensureBank(token: string) {
  for (const entry of BANK) {
    const list = await apiJson<{ Value: { ID: string; Key: string }[] }>(
      'GET',
      `/api/Surveys/BankQuestion?$filter=Key eq '${entry.key}'`,
      { token },
    );
    if (list.Value?.some((x) => x.Key === entry.key)) {
      console.log(`  · bank entry already exists: ${entry.key}`);
      continue;
    }
    await apiJson('POST', '/api/Surveys/BankQuestion', {
      token,
      body: {
        key: entry.key,
        biColumn: entry.biColumn,
        retired: false,
        question: entry.question,
      },
    });
    console.log(`  + bank entry seeded: ${entry.key}`);
  }
}

async function seedSurveyDef(token: string, def: SurveyDef): Promise<{ hashId: string; publicId: string; version: number }> {
  // Create placeholder + patch with real surveyId, then publish + instance.
  const created = await apiJson<{ Entity: { ID: string } }>('POST', '/api/Surveys/Survey', {
    token,
    body: { name: def.name, draft: def.draft('pending') },
  });
  const hashId = created.Entity.ID;

  const full = await apiJson<{ Entity: Record<string, unknown> }>(
    'GET', `/api/Surveys/Survey/${hashId}`, { token },
  );
  await apiJson('PUT', `/api/Surveys/Survey/${hashId}`, {
    token,
    body: { ...full.Entity, name: def.name, draft: def.draft(hashId) },
  });

  const publishResp = await api('POST', `/api/Surveys/Publish/${hashId}`, { token });
  const publishText = await publishResp.text();
  if (!publishResp.ok) {
    throw new Error(`publish failed for ${def.name}: ${publishResp.status} ${publishText}`);
  }
  const publishBody = JSON.parse(publishText) as { Version?: number };
  const version = publishBody.Version ?? 0;

  // Insert instance via sqlcmd — no API endpoint creates instances yet (Phase 5).
  const row = sql(`
    SELECT TOP 1 s.ID, v.ID
    FROM [Surveys].[Survey] s
    JOIN [Surveys].[SurveyVersion] v ON v.SurveyID = s.ID
    WHERE s.Name = N'${def.name.replace(/'/g, "''")}' AND v.Version = ${version};
  `);
  const [surveyIdStr, versionIdStr] = row.split('\t');
  const inserted = sql(`
    DECLARE @out TABLE (PublicID UNIQUEIDENTIFIER);
    INSERT INTO [Surveys].[SurveyInstance]
      (PublicID, SurveyID, SurveyVersionID, TriggeredAt, Status, CreateDate, LastSaveDate, IsDeleted)
    OUTPUT inserted.PublicID INTO @out
    VALUES (NEWID(), ${surveyIdStr}, ${versionIdStr}, SYSDATETIMEOFFSET(), 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0);
    SELECT CONVERT(NVARCHAR(36), PublicID) FROM @out;
  `);
  const publicId = inserted.trim().toLowerCase();
  return { hashId, publicId, version };
}

async function main() {
  console.log('Seeding demo surveys (no cleanup)…\n');
  const { token } = await login();
  console.log('• logged in as SuperUser\n');

  console.log('Shared bank questions:');
  await ensureBank(token);

  console.log('\nSurveys:');
  const results: Array<{ def: SurveyDef; hashId: string; publicId: string; version: number }> = [];
  for (const def of SURVEYS) {
    const r = await seedSurveyDef(token, def);
    results.push({ def, ...r });
    console.log(`  + ${def.name}  (hash=${r.hashId}, version=${r.version}, publicId=${r.publicId})`);
  }

  // ─── Handoff banner ─────────────────────────────────────────────────────
  console.log('\n' + '─'.repeat(72));
  console.log('Done — records left in the database. To explore:');
  console.log('─'.repeat(72));
  console.log('');
  console.log('BUILDER (edit + live preview):');
  console.log('  1. Browse to  http://localhost:5134/');
  console.log('  2. Login      SuperUser / OneTwo');
  console.log('  3. Open       Surveys → Surveys');
  console.log('  4. Click any row to see the JSON editor + live preview pane');
  console.log('');
  for (const r of results) {
    console.log(`     ${r.def.name}`);
    console.log(`       → http://localhost:5134/Surveys/SurveyForm/${r.hashId}`);
  }
  console.log('');
  console.log('STANDALONE RENDERER (what the customer sees):');
  console.log('  Start the standalone dev server first:');
  console.log('    cd ADP.Surveys/renderer/apps/standalone && npx vite dev');
  console.log('  Then open any of these URLs:');
  console.log('');
  for (const r of results) {
    console.log(`     ${r.def.name}`);
    console.log(`       → http://127.0.0.1:5190/s/${r.publicId}`);
    console.log(`       → http://127.0.0.1:5190/s/${r.publicId}?locale=ar   (Arabic / RTL)`);
  }
  console.log('');
  console.log('PUBLIC SCHEMA (what the renderer fetches):');
  for (const r of results) {
    console.log(`       → http://localhost:5134/api/Surveys/SurveyInstances/${r.publicId}/schema`);
  }
  console.log('');
  console.log('Teardown when done:');
  console.log('  cd ADP.Surveys/renderer/e2e && npx tsx unseed-demo.ts');
  console.log('');
}

main().catch((err) => {
  console.error('\nSEED FAILED:', (err as Error).message ?? err);
  process.exit(1);
});
