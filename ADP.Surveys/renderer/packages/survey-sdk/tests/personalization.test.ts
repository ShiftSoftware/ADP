import { describe, expect, it, vi } from 'vitest';
import {
  answerTokenQuestionId,
  bodySurface,
  collectTokenNames,
  createAnswerContext,
  encodeForSurface,
  formatAnswerLabel,
  formatAnswerValue,
  isAnswerToken,
  personalizeScreen,
  substituteRequestFields,
  substituteTokens,
  type TokenContext,
} from '../src/personalization.js';
import { fetchOptions, requestSignature } from '../src/options-source.js';
import type { Screen, Survey } from '../src/schema.js';

/**
 * Mirror of `PersonalizationAnswerTokenTests` on the C# side. The grammar, the
 * fallback chain and the per-surface escaping are asserted with the same inputs so
 * a drift between the two engines shows up as one of these failing.
 */

const survey: Survey = {
  id: 's',
  defaultLocale: 'en',
  locales: ['en', 'ar'],
  variables: [
    { name: 'answers.city', fallback: { en: 'your city', ar: 'مدينتك' } },
    { name: 'candidate.name', fallback: { en: 'valued customer' } },
  ],
  screens: [
    {
      id: 's1',
      title: { en: 'Welcome' },
      questions: [
        { id: 'name', type: 'text', title: { en: 'Your name' } },
        {
          id: 'brand',
          type: 'singleChoice',
          title: { en: 'Brand' },
          options: [
            { id: 'toyota', label: { en: 'Toyota', ar: 'تويوتا' } },
            { id: 'lexus', label: { en: 'Lexus' } },
          ],
        },
        {
          id: 'extras',
          type: 'multiChoice',
          title: { en: 'Extras' },
          options: [
            { id: 'wash', label: { en: 'Car wash' } },
            { id: 'pickup', label: { en: 'Pick-up' } },
          ],
        },
        { id: 'has-car', type: 'yesNo', title: { en: 'Car?' }, noLabel: { en: 'Not yet' } },
        { id: 'visit', type: 'date', title: { en: 'Visit' } },
        { id: 'nps', type: 'nps', title: { en: 'NPS' } },
        { id: 'city', type: 'dropdown', title: { en: 'City' }, optionsSource: { url: 'https://api.test/cities' } },
      ],
    },
  ],
};

const answers = {
  name: 'Aza "the" <tester>',
  brand: 'toyota',
  extras: ['wash', 'pickup'],
  'has-car': false,
  visit: '2026-09-17',
  nps: 9,
  city: 'L0VEX',
  blank: '',
  none: [],
};

const context = createAnswerContext({
  schema: survey,
  answers,
  lookupOptions: (id) => (id === 'city' ? [{ id: 'L0VEX', label: 'North City' }] : undefined),
  yesNoLabels: { yes: 'Yes', no: 'No' },
});

describe('token grammar helpers', () => {
  it.each([
    ['answers.nps', true, 'nps'],
    ['answers.brand.label', true, 'brand'],
    ['answers.has-car', true, 'has-car'],
    ['answers.', false, null],
    ['answers', false, null],
    ['candidate.answers.x', false, null],
    ['recipient.address', false, null],
  ])('%s → answer token %s, question %s', (name, isAnswer, questionId) => {
    expect(isAnswerToken(name)).toBe(isAnswer);
    expect(answerTokenQuestionId(name)).toBe(questionId);
  });

  it('collects distinct token names', () => {
    expect(collectTokenNames('{{answers.a}} and {{ answers.b | x }} and {{answers.a}}').sort()).toEqual([
      'answers.a',
      'answers.b',
    ]);
    expect(collectTokenNames('no tokens')).toEqual([]);
  });
});

describe('answer values and labels', () => {
  it('formats stored values as BI sees them', () => {
    expect(formatAnswerValue('x')).toBe('x');
    expect(formatAnswerValue(9)).toBe('9');
    expect(formatAnswerValue(false)).toBe('false');
    expect(formatAnswerValue(['a', 'b'])).toBe('a, b');
    expect(formatAnswerValue({ name: 'receipt.pdf', size: 1, type: 'application/pdf' })).toBe('receipt.pdf');
    // Empty answers count as unanswered, as on the server.
    expect(formatAnswerValue('')).toBeNull();
    expect(formatAnswerValue([])).toBeNull();
    expect(formatAnswerValue(null)).toBeNull();
    expect(formatAnswerValue(undefined)).toBeNull();
  });

  it('resolves {{answers.x}} to the value and {{answers.x.label}} to the display form', () => {
    expect(context.value('answers.brand', 'en')).toBe('toyota');
    expect(context.value('answers.brand.label', 'en')).toBe('Toyota');
    expect(context.value('answers.brand.label', 'ar')).toBe('تويوتا');
    expect(context.value('answers.lexus-not-a-question', 'en')).toBeNull();
    expect(context.value('answers.extras', 'en')).toBe('wash, pickup');
    expect(context.value('answers.extras.label', 'en')).toBe('Car wash, Pick-up');
    expect(context.value('answers.has-car', 'en')).toBe('false');
    expect(context.value('answers.has-car.label', 'en')).toBe('Not yet');
    expect(context.value('answers.nps', 'en')).toBe('9');
    expect(context.value('answers.nps.label', 'en')).toBe('9');
    expect(context.value('answers.name.label', 'en')).toBe('Aza "the" <tester>');
  });

  it('labels a sourced answer from the fetched options, or falls back to the id', () => {
    expect(context.value('answers.city', 'en')).toBe('L0VEX');
    expect(context.value('answers.city.label', 'en')).toBe('North City');
    const noLookup = createAnswerContext({ schema: survey, answers });
    expect(noLookup.value('answers.city.label', 'en')).toBe('L0VEX');
  });

  it('formats a date label for the locale and keeps an unparseable one verbatim', () => {
    const label = context.value('answers.visit.label', 'en');
    expect(label).toContain('2026');
    expect(label).not.toBe('2026-09-17');
    expect(formatAnswerLabel({ id: 'd', type: 'date' }, 'not a date', 'en', { schema: survey, answers })).toBe(
      'not a date',
    );
  });

  it('uses the caller-supplied yes/no words when the question declares none', () => {
    const yes = formatAnswerLabel({ id: 'y', type: 'yesNo' }, true, 'en', {
      schema: survey,
      answers,
      yesNoLabels: { yes: 'نعم', no: 'لا' },
    });
    expect(yes).toBe('نعم');
  });

  it('never resolves non-answer tokens, but does know every declared fallback', () => {
    expect(context.value('candidate.name', 'en')).toBeNull();
    expect(context.fallback('candidate.name', 'en')).toBe('valued customer');
    expect(context.fallback('answers.city', 'ar')).toBe('مدينتك');
    expect(context.fallback('answers.city', 'ku')).toBe('your city'); // default locale
    expect(context.fallback('answers.nothing', 'en')).toBeNull();
  });
});

describe('substituteTokens', () => {
  it('fills answers into copy and keeps unresolvable tokens verbatim', () => {
    expect(substituteTokens('Thanks {{answers.name}}, {{answers.brand.label}} it is.', context, 'en')).toBe(
      'Thanks Aza "the" <tester>, Toyota it is.',
    );
    expect(substituteTokens('Hi {{answers.unknown}}!', context, 'en')).toBe('Hi {{answers.unknown}}!');
    expect(substituteTokens('Hi {{candidate.missing}}!', context, 'en')).toBe('Hi {{candidate.missing}}!');
  });

  it('walks the fallback chain: answer, inline, declared', () => {
    const blankContext = createAnswerContext({ schema: survey, answers: {} });
    expect(substituteTokens('{{answers.city|any city}}', blankContext, 'en')).toBe('any city');
    expect(substituteTokens('{{answers.city}}', blankContext, 'en')).toBe('your city');
    expect(substituteTokens('{{answers.city}}', blankContext, 'ar')).toBe('مدينتك');
    // An empty inline fallback is no fallback.
    expect(substituteTokens('{{answers.name|}}', blankContext, 'en')).toBe('{{answers.name|}}');
    // The answer beats both when present.
    expect(substituteTokens('{{answers.city|any city}}', context, 'en')).toBe('L0VEX');
  });

  it('treats empty answers as unanswered', () => {
    expect(substituteTokens('[{{answers.blank|none}}][{{answers.none|none}}]', context, 'en')).toBe('[none][none]');
  });

  it('escapes per surface and blanks unresolvable tokens in request fields', () => {
    const tricky: TokenContext = {
      value: (name) => (name === 'answers.x' ? 'A "q" a/b & c\nline2' : null),
      fallback: () => null,
    };
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'url')).toBe('A%20%22q%22%20a%2Fb%20%26%20c%0Aline2');
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'queryValue')).toBe('A "q" a/b & c\nline2');
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'headerValue')).toBe('A "q" a/b & cline2');
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'jsonBody')).toBe('A \\"q\\" a/b & c\\nline2');
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'formBody')).toBe('A%20%22q%22%20a%2Fb%20%26%20c%0Aline2');
    expect(substituteTokens('{{answers.x}}', tricky, 'en', 'rawBody')).toBe('A "q" a/b & c\nline2');

    expect(substituteTokens('?x={{answers.nope}}&y=1', tricky, 'en', 'url')).toBe('?x=&y=1');
    expect(substituteTokens('{"a":"{{answers.nope}}"}', tricky, 'en', 'jsonBody')).toBe('{"a":""}');
    expect(substituteTokens('Hi {{answers.nope}}', tricky, 'en', 'copy')).toBe('Hi {{answers.nope}}');
  });

  it('json-escaped values stay valid inside a JSON string literal', () => {
    const ctx: TokenContext = { value: () => 'tab\there "q" back\\slash', fallback: () => null };
    const body = substituteTokens('{"note":"{{answers.note}}"}', ctx, 'en', 'jsonBody');
    expect(JSON.parse(body)).toEqual({ note: 'tab\there "q" back\\slash' });
  });

  it.each([
    ['application/json', 'jsonBody'],
    ['application/json; charset=utf-8', 'jsonBody'],
    ['application/vnd.api+json', 'jsonBody'],
    ['application/x-www-form-urlencoded', 'formBody'],
    ['text/plain', 'rawBody'],
    [undefined, 'rawBody'],
  ])('bodySurface(%s) → %s', (contentType, surface) => {
    expect(bodySurface(contentType)).toBe(surface);
  });

  it('encodeForSurface is the identity for copy and query values', () => {
    expect(encodeForSurface('a b', 'copy')).toBe('a b');
    expect(encodeForSurface('a b', 'queryValue')).toBe('a b');
  });
});

describe('personalizeScreen', () => {
  const screen: Screen = {
    id: 's2',
    title: { en: 'Thanks {{answers.name}}', ar: 'شكرا {{answers.name}}' },
    description: { en: 'You drive a {{answers.brand.label}}.' },
    questions: [
      {
        id: 'why',
        type: 'singleChoice',
        title: { en: 'Why {{answers.brand.label}}?' },
        help: { en: 'Score was {{answers.nps}}' },
        options: [
          { id: 'a', label: { en: 'Because {{answers.brand.label}} is reliable' } },
          { id: 'b', label: { en: 'Other' } },
        ],
      },
      { id: 'plain', type: 'text', title: { en: 'No tokens here' }, placeholder: { en: 'e.g. {{answers.name}}' } },
      {
        id: 'model',
        type: 'dropdown',
        title: { en: 'Model' },
        optionsSource: { url: 'https://api.test/models/{{answers.brand}}' },
      },
    ],
  };

  it('fills every localized field, per locale, and leaves request fields alone', () => {
    const out = personalizeScreen(screen, context);
    expect(out.title).toEqual({ en: 'Thanks Aza "the" <tester>', ar: 'شكرا Aza "the" <tester>' });
    expect(out.description).toEqual({ en: 'You drive a Toyota.' });
    const [why, plain, model] = out.questions as Array<Record<string, unknown>>;
    expect(why!['title']).toEqual({ en: 'Why Toyota?' });
    expect(why!['help']).toEqual({ en: 'Score was 9' });
    expect((why!['options'] as Array<{ label: unknown }>)[0]!.label).toEqual({ en: 'Because Toyota is reliable' });
    expect(plain!['placeholder']).toEqual({ en: 'e.g. Aza "the" <tester>' });
    // Request fields are substituted at fetch time, with request escaping.
    expect((model!['optionsSource'] as { url: string }).url).toBe('https://api.test/models/{{answers.brand}}');
  });

  it('is copy-on-write: untouched nodes keep their identity, a token-free screen is returned as-is', () => {
    const out = personalizeScreen(screen, context);
    expect(out).not.toBe(screen);
    expect(out.questions![1]).not.toBe(screen.questions![1]); // placeholder changed
    expect(out.questions![2]).toBe(screen.questions![2]); // nothing in copy changed
    expect((out.questions![0] as { options: unknown[] }).options[1]).toBe(
      (screen.questions![0] as { options: unknown[] }).options[1],
    );

    const untouched: Screen = { id: 'x', title: { en: 'Plain' }, questions: [{ id: 'q', type: 'text', title: { en: 'Q' } }] };
    expect(personalizeScreen(untouched, context)).toBe(untouched);
  });

  it('updates live as answers change (same-screen references)', () => {
    const before = personalizeScreen(screen, createAnswerContext({ schema: survey, answers: {} }));
    expect(before.title).toEqual({ en: 'Thanks {{answers.name}}', ar: 'شكرا {{answers.name}}' });
    const after = personalizeScreen(screen, createAnswerContext({ schema: survey, answers: { name: 'Sara' } }));
    expect(after.title).toEqual({ en: 'Thanks Sara', ar: 'شكرا Sara' });
  });
});

describe('substituteRequestFields + fetchOptions', () => {
  const source = {
    url: 'https://api.test/v1/{{answers.brand}}/models?q={{answers.name}}',
    queryParams: { city: '{{answers.city}}', fixed: 'x' },
    headers: { 'X-Pick': '{{answers.brand.label}}', 'X-Missing': '{{answers.nope}}' },
    method: 'POST',
    body: '{"name":"{{answers.name}}","score":{{answers.nps}},"missing":"{{answers.nope}}"}',
  };

  it('substitutes each field with its own escaping', () => {
    const out = substituteRequestFields(source, context, 'en');
    expect(out.url).toBe('https://api.test/v1/toyota/models?q=Aza%20%22the%22%20%3Ctester%3E');
    expect(out.queryParams).toEqual({ city: 'L0VEX', fixed: 'x' });
    expect(out.headers).toEqual({ 'X-Pick': 'Toyota', 'X-Missing': '' });
    expect(out.body).toBe('{"name":"Aza \\"the\\" <tester>","score":9,"missing":""}');
    expect(JSON.parse(out.body!)).toEqual({ name: 'Aza "the" <tester>', score: 9, missing: '' });
    // Untouched request keeps its identity.
    const plain = { url: 'https://api.test/cities', queryParams: { a: 'b' } };
    expect(substituteRequestFields(plain, context, 'en')).toBe(plain);
  });

  it('sends a POST with the body and a default JSON content type, source headers winning', async () => {
    const fetchImpl = vi.fn(async () => new Response(JSON.stringify([{ ID: '1', Name: 'One' }]), { status: 200 }));
    const request = substituteRequestFields(source, context, 'en');
    const options = await fetchOptions(request, { locale: 'ar', fetchImpl: fetchImpl as unknown as typeof fetch });
    expect(options).toEqual([{ id: '1', label: 'One' }]);
    const [url, init] = fetchImpl.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe('https://api.test/v1/toyota/models?q=Aza+%22the%22+%3Ctester%3E&city=L0VEX&fixed=x');
    expect(init.method).toBe('POST');
    expect(init.body).toBe(request.body);
    expect(init.headers).toEqual({
      'Accept-Language': 'ar',
      'Content-Type': 'application/json',
      'X-Pick': 'Toyota',
      'X-Missing': '',
    });
  });

  it('a GET never carries a body, and an explicit content-type header replaces the automatic one', async () => {
    const fetchImpl = vi.fn(async () => new Response('[]', { status: 200 }));
    await fetchOptions(
      { url: 'https://api.test/x', body: '{}', headers: { 'content-type': 'text/plain' } },
      { fetchImpl: fetchImpl as unknown as typeof fetch },
    );
    const [, getInit] = fetchImpl.mock.calls[0] as unknown as [string, RequestInit];
    expect(getInit.method).toBe('GET');
    expect(getInit.body).toBeUndefined();
    // No automatic Content-Type without a POST body; the source's own header still goes out.
    expect(getInit.headers).toEqual({ 'content-type': 'text/plain' });

    await fetchOptions(
      { url: 'https://api.test/x', method: 'post', body: 'a=b', headers: { 'content-type': 'text/plain' } },
      { fetchImpl: fetchImpl as unknown as typeof fetch },
    );
    const [, postInit] = fetchImpl.mock.calls[1] as unknown as [string, RequestInit];
    expect(postInit.method).toBe('POST');
    expect(postInit.headers).toEqual({ 'content-type': 'text/plain' });
  });

  it('requestSignature changes with any substituted field, not only the URL', () => {
    const a = substituteRequestFields(source, context, 'en');
    const b = substituteRequestFields(source, createAnswerContext({ schema: survey, answers: { ...answers, nps: 3 } }), 'en');
    expect(a.url).toBe(b.url);
    expect(requestSignature(a, 'en')).not.toBe(requestSignature(b, 'en'));
    expect(requestSignature(a, 'en')).toBe(requestSignature({ ...a }, 'en'));
    expect(requestSignature(a, 'en')).not.toBe(requestSignature(a, 'ar'));
  });
});
