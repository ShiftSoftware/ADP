import type { Survey } from '@shiftsoftware/survey-sdk';

/** Flow fixture used by the preview app — identical in shape to what the E2E
 *  harness exercises against the real API, so the two channels stay aligned.
 *  Localized to English + Arabic to exercise RTL during development. */
export const fixtureSurvey: Survey = {
  id: 'preview-survey',
  version: 1,
  defaultLocale: 'en',
  locales: ['en', 'ar'],
  screens: [
    {
      id: 'welcome',
      title: { en: 'Welcome', ar: 'أهلاً بك' },
      description: {
        en: 'This is a preview of the ADP Dynamic Surveys renderer.',
        ar: 'هذه معاينة لعارض الاستبيانات الديناميكية من ADP.',
      },
      questions: [
        {
          type: 'text',
          id: 'name',
          title: { en: 'Your name', ar: 'اسمك' },
          required: false,
        },
      ],
      nextScreen: 'feedback',
    },
    {
      id: 'feedback',
      title: { en: 'How was it?', ar: 'ما رأيك؟' },
      description: {
        en: 'Tap Next after scoring — detractors skip straight to the thank-you screen.',
        ar: 'اضغط على التالي بعد التقييم — المنتقدون ينتقلون مباشرة إلى شاشة الشكر.',
      },
      questions: [
        {
          type: 'nps',
          id: 'nps',
          title: {
            en: 'How likely are you to recommend us?',
            ar: 'ما مدى احتمالية توصيتك بنا؟',
          },
          min: 0,
          max: 10,
          required: true,
          lowLabel: { en: 'Not likely', ar: 'غير محتمل' },
          highLabel: { en: 'Very likely', ar: 'محتمل جداً' },
        },
      ],
      nextScreen: 'brand',
    },
    {
      id: 'brand',
      title: { en: 'Which brand?', ar: 'أي علامة تجارية؟' },
      description: {
        en: 'Tap a row — the selection IS the transition.',
        ar: 'اضغط على صف — الاختيار نفسه هو الانتقال.',
      },
      questions: [
        {
          type: 'navigationList',
          id: 'brand-choice',
          title: { en: 'Pick one', ar: 'اختر واحدة' },
          options: [
            {
              id: 'toyota',
              label: { en: 'Toyota', ar: 'تويوتا' },
              nextScreen: 'thanks-toyota',
            },
            {
              id: 'other',
              label: { en: 'Other brand', ar: 'علامة تجارية أخرى' },
              nextScreen: 'thanks-default',
            },
          ],
        },
      ],
    },
    {
      id: 'thanks-toyota',
      title: { en: 'Thanks — Toyota fan!', ar: 'شكراً — من محبي تويوتا!' },
      description: { en: 'We’ll be in touch.', ar: 'سنتواصل معك قريباً.' },
      questions: [],
    },
    {
      id: 'thanks-default',
      title: { en: 'Thanks for your feedback!', ar: 'شكراً على ملاحظاتك!' },
      description: {
        en: 'We appreciate you taking the time.',
        ar: 'نقدّر وقتك الذي منحتنا إياه.',
      },
      questions: [],
    },
  ],
  logic: [
    { if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'thanks-default' } },
  ],
};
