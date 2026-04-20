import type { Survey } from '@shiftsoftware/survey-sdk';

/** Showcase fixture — every question type on one screen. Used for visual
 *  verification during renderer development. Localized to English + Arabic
 *  so both LTR and RTL layouts can be screenshot side-by-side. */
export const showcaseSurvey: Survey = {
  id: 'showcase',
  version: 1,
  defaultLocale: 'en',
  locales: ['en', 'ar'],
  screens: [
    {
      id: 'showcase',
      title: { en: 'All question types', ar: 'كل أنواع الأسئلة' },
      description: {
        en: 'One of each — for visual verification.',
        ar: 'واحد من كل نوع — للتحقق البصري.',
      },
      questions: [
        {
          type: 'text',
          id: 'name',
          title: { en: 'Text — your name', ar: 'نص — اسمك' },
        },
        {
          type: 'paragraph',
          id: 'notes',
          title: { en: 'Paragraph — notes', ar: 'فقرة — ملاحظات' },
          placeholder: { en: 'Type something long…', ar: 'اكتب شيئاً طويلاً…' },
        },
        {
          type: 'number',
          id: 'age',
          title: { en: 'Number — age', ar: 'رقم — العمر' },
          min: 0,
          max: 120,
          unit: { en: 'years', ar: 'سنة' },
        },
        {
          type: 'rating',
          id: 'stars',
          title: { en: 'Rating — stars out of 5', ar: 'تقييم — نجوم من ٥' },
          max: 5,
        },
        {
          type: 'nps',
          id: 'nps',
          title: { en: 'NPS — 0 through 10', ar: 'NPS — من ٠ إلى ١٠' },
          min: 0,
          max: 10,
          lowLabel: { en: 'Not likely', ar: 'غير محتمل' },
          highLabel: { en: 'Very likely', ar: 'محتمل جداً' },
        },
        {
          type: 'singleChoice',
          id: 'fav',
          title: { en: 'SingleChoice — favourite', ar: 'اختيار واحد — المفضل' },
          options: [
            { id: 'cat', label: { en: 'Cat', ar: 'قطة' } },
            { id: 'dog', label: { en: 'Dog', ar: 'كلب' } },
            { id: 'fish', label: { en: 'Fish', ar: 'سمكة' } },
          ],
        },
        {
          type: 'multiChoice',
          id: 'toppings',
          title: {
            en: 'MultiChoice — toppings (max 2)',
            ar: 'اختيار متعدد — إضافات (حد أقصى ٢)',
          },
          maxSelected: 2,
          options: [
            { id: 'cheese', label: { en: 'Cheese', ar: 'جبنة' } },
            { id: 'olives', label: { en: 'Olives', ar: 'زيتون' } },
            { id: 'mushrooms', label: { en: 'Mushrooms', ar: 'فطر' } },
            { id: 'peppers', label: { en: 'Peppers', ar: 'فلفل' } },
          ],
        },
        {
          type: 'dropdown',
          id: 'country',
          title: { en: 'Dropdown — country', ar: 'قائمة منسدلة — البلد' },
          placeholder: { en: 'Pick a country', ar: 'اختر بلداً' },
          options: [
            { id: 'iq', label: { en: 'Iraq', ar: 'العراق' } },
            { id: 'jp', label: { en: 'Japan', ar: 'اليابان' } },
            { id: 'tr', label: { en: 'Türkiye', ar: 'تركيا' } },
          ],
        },
        {
          type: 'date',
          id: 'dob',
          title: { en: 'Date — birthday', ar: 'تاريخ — الميلاد' },
        },
        {
          type: 'dateTime',
          id: 'when',
          title: { en: 'DateTime — when', ar: 'تاريخ ووقت — متى' },
        },
        {
          type: 'yesNo',
          id: 'agree',
          title: { en: 'YesNo — do you agree?', ar: 'نعم/لا — هل توافق؟' },
        },
        {
          type: 'file',
          id: 'photo',
          title: { en: 'File — upload a photo', ar: 'ملف — ارفع صورة' },
        },
        {
          type: 'signature',
          id: 'sig',
          title: { en: 'Signature — sign here', ar: 'توقيع — وقّع هنا' },
        },
        {
          type: 'navigationList',
          id: 'next',
          title: { en: 'NavigationList — next step', ar: 'قائمة تنقل — الخطوة التالية' },
          options: [
            {
              id: 'done',
              label: { en: 'Done — finish', ar: 'تم — إنهاء' },
              nextScreen: 'done',
            },
          ],
        },
      ],
    },
    { id: 'done', title: { en: 'All done!', ar: 'تم بالكامل!' } },
  ],
};
