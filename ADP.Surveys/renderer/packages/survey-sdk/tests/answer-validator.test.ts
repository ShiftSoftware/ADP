import { describe, expect, it } from 'vitest';
import { validateAnswerValue, validatePresentAnswers } from '../src/answer-validator.js';

/**
 * Mirrors the shape-check facts in `AnswerValidatorTests.cs` (the path-replay
 * facts — required-on-unvisited-branch etc. — are server-scope: this module
 * deliberately excludes presence/required, which the renderer's required gate
 * owns client-side).
 */

const codes = (question: Record<string, unknown>, value: unknown) =>
  validateAnswerValue(question, value).map((e) => e.code);

describe('validateAnswerValue', () => {
  // ── text ──────────────────────────────────────────────────────────────────
  it('text too short reports minLength (C#: Validate_TextTooShort_ReportsError)', () => {
    const q = { type: 'text', id: 't', minLength: 5 };
    expect(codes(q, 'abc')).toEqual(['minLength']);
  });

  it('text inside bounds passes (C#: Validate_TextInsideBounds_Passes)', () => {
    const q = { type: 'text', id: 't', minLength: 2, maxLength: 10 };
    expect(codes(q, 'hello')).toEqual([]);
  });

  it('text over maxLength reports maxLength', () => {
    const q = { type: 'text', id: 't', maxLength: 3 };
    expect(codes(q, 'toolong')).toEqual(['maxLength']);
  });

  it('text pattern mismatch reports pattern', () => {
    const q = { type: 'text', id: 't', pattern: '^[0-9]+$' };
    expect(codes(q, 'abc')).toEqual(['pattern']);
    expect(codes(q, '123')).toEqual([]);
  });

  it('an invalid regex in the schema fails open', () => {
    const q = { type: 'text', id: 't', pattern: '([' };
    expect(codes(q, 'anything')).toEqual([]);
  });

  it('non-string text answer reports type', () => {
    expect(codes({ type: 'text', id: 't' }, 42)).toEqual(['type']);
  });

  // ── paragraph ─────────────────────────────────────────────────────────────
  it('paragraph length bounds mirror text', () => {
    const q = { type: 'paragraph', id: 'p', minLength: 4, maxLength: 6 };
    expect(codes(q, 'ab')).toEqual(['minLength']);
    expect(codes(q, 'toolongtext')).toEqual(['maxLength']);
    expect(codes(q, 'right')).toEqual([]);
  });

  // ── number ────────────────────────────────────────────────────────────────
  it('number below min / above max', () => {
    const q = { type: 'number', id: 'n', min: 1, max: 10 };
    expect(codes(q, 0)).toEqual(['min']);
    expect(codes(q, 11)).toEqual(['max']);
    expect(codes(q, 5)).toEqual([]);
  });

  it('non-number number answer reports type', () => {
    expect(codes({ type: 'number', id: 'n' }, '5')).toEqual(['type']);
  });

  // ── rating ────────────────────────────────────────────────────────────────
  it('rating outside 0..max reports range', () => {
    const q = { type: 'rating', id: 'r', max: 5 };
    expect(codes(q, 6)).toEqual(['range']);
    expect(codes(q, 3)).toEqual([]);
  });

  it('half rating rejected unless allowHalf', () => {
    expect(codes({ type: 'rating', id: 'r', max: 5 }, 3.5)).toEqual(['halfNotAllowed']);
    expect(codes({ type: 'rating', id: 'r', max: 5, allowHalf: true }, 3.5)).toEqual([]);
  });

  // ── nps ───────────────────────────────────────────────────────────────────
  it('nps out of range reports range (C#: Validate_NpsOutOfRange_ReportsError)', () => {
    const q = { type: 'nps', id: 'nps', min: 0, max: 10 };
    expect(codes(q, 11)).toEqual(['range']);
    expect(codes(q, 7)).toEqual([]);
  });

  it('non-integer nps reports type (C# GetInt32 rejects non-integers)', () => {
    expect(codes({ type: 'nps', id: 'nps', min: 0, max: 10 }, 7.5)).toEqual(['type']);
  });

  // ── choice types ──────────────────────────────────────────────────────────
  it('singleChoice unknown option reports invalidOption (C#: Validate_SingleChoiceWithUnknownOption_ReportsError)', () => {
    const q = { type: 'singleChoice', id: 'sc', options: [{ id: 'a' }, { id: 'b' }] };
    expect(codes(q, 'zz')).toEqual(['invalidOption']);
    expect(codes(q, 'a')).toEqual([]);
  });

  it('navigationList answer not in options reports invalidOption (C#: Validate_NavigationListAnswerNotInOptions_ReportsError)', () => {
    const q = { type: 'navigationList', id: 'nl', options: [{ id: 'sales' }, { id: 'service' }] };
    expect(codes(q, 'nope')).toEqual(['invalidOption']);
  });

  it('dropdown validates like singleChoice', () => {
    const q = { type: 'dropdown', id: 'dd', options: [{ id: 'x' }] };
    expect(codes(q, 'y')).toEqual(['invalidOption']);
    expect(codes(q, 'x')).toEqual([]);
  });

  // ── multiChoice ───────────────────────────────────────────────────────────
  it('multiChoice maxSelected exceeded reports maxSelected (C#: Validate_MultiChoiceMaxSelectedExceeded_ReportsError)', () => {
    const q = {
      type: 'multiChoice',
      id: 'mc',
      maxSelected: 2,
      options: [{ id: 'a' }, { id: 'b' }, { id: 'c' }],
    };
    expect(codes(q, ['a', 'b', 'c'])).toEqual(['maxSelected']);
    expect(codes(q, ['a', 'b'])).toEqual([]);
  });

  it('multiChoice minSelected enforced', () => {
    const q = { type: 'multiChoice', id: 'mc', minSelected: 2, options: [{ id: 'a' }, { id: 'b' }] };
    expect(codes(q, ['a'])).toEqual(['minSelected']);
  });

  it('multiChoice non-array reports type; non-string entry reports type and stops', () => {
    const q = { type: 'multiChoice', id: 'mc', options: [{ id: 'a' }] };
    expect(codes(q, 'a')).toEqual(['type']);
    expect(codes(q, ['a', 5])).toEqual(['type']);
  });

  it('multiChoice unknown entries each report invalidOption', () => {
    const q = { type: 'multiChoice', id: 'mc', options: [{ id: 'a' }] };
    expect(codes(q, ['a', 'zz'])).toEqual(['invalidOption']);
  });

  // ── yesNo ─────────────────────────────────────────────────────────────────
  it('yesNo with string reports type (C#: Validate_YesNoWithString_ReportsError)', () => {
    expect(codes({ type: 'yesNo', id: 'yn' }, 'yes')).toEqual(['type']);
    expect(codes({ type: 'yesNo', id: 'yn' }, true)).toEqual([]);
  });

  // ── date / dateTime ───────────────────────────────────────────────────────
  it('date out of bounds reports minDate/maxDate (C#: Validate_DateOutOfBounds_ReportsError)', () => {
    const q = { type: 'date', id: 'd', minDate: '2026-01-01', maxDate: '2026-12-31' };
    expect(codes(q, '2025-06-01')).toEqual(['minDate']);
    expect(codes(q, '2027-06-01')).toEqual(['maxDate']);
    expect(codes(q, '2026-06-01')).toEqual([]);
  });

  it('malformed date reports invalidDate', () => {
    const q = { type: 'date', id: 'd' };
    expect(codes(q, 'June 1st')).toEqual(['invalidDate']);
    expect(codes(q, '2026-13-40')).toEqual(['invalidDate']);
  });

  it('dateTime bounds enforced; malformed reports invalidDateTime', () => {
    const q = {
      type: 'dateTime',
      id: 'dt',
      minDateTime: '2026-01-01T00:00:00Z',
      maxDateTime: '2026-12-31T23:59:59Z',
    };
    expect(codes(q, '2025-06-01T10:00:00Z')).toEqual(['minDateTime']);
    expect(codes(q, '2027-06-01T10:00:00Z')).toEqual(['maxDateTime']);
    expect(codes(q, '2026-06-01T10:00:00Z')).toEqual([]);
    expect(codes(q, 'not-a-date')).toEqual(['invalidDateTime']);
  });

  // ── file / signature ──────────────────────────────────────────────────────
  it('file and signature must be non-empty strings', () => {
    expect(codes({ type: 'file', id: 'f' }, '')).toEqual(['empty']);
    expect(codes({ type: 'signature', id: 's' }, 42)).toEqual(['empty']);
    expect(codes({ type: 'file', id: 'f' }, 'blob.png')).toEqual([]);
  });

  // ── all-good composite (C#: Validate_AllGood_NoErrors) ────────────────────
  it('a fully valid answer set produces no errors', () => {
    const questions = [
      { type: 'text', id: 't', minLength: 2 },
      { type: 'nps', id: 'nps', min: 0, max: 10 },
      { type: 'singleChoice', id: 'sc', options: [{ id: 'a' }] },
      { type: 'yesNo', id: 'yn' },
    ];
    const answers = { t: 'hello', nps: 9, sc: 'a', yn: false };
    expect(validatePresentAnswers(questions, answers)).toEqual([]);
  });
});

describe('validatePresentAnswers', () => {
  it('skips absent and null answers (presence is the caller concern)', () => {
    const questions = [
      { type: 'text', id: 'a', minLength: 5 },
      { type: 'nps', id: 'b', min: 0, max: 10 },
    ];
    expect(validatePresentAnswers(questions, {})).toEqual([]);
    expect(validatePresentAnswers(questions, { a: null })).toEqual([]);
  });

  it('shape-checks every present answer (C#: Validate_AnswerOnUnvisitedScreen_ShapeStillChecked)', () => {
    const questions = [{ type: 'text', id: 'a', minLength: 5 }];
    const errors = validatePresentAnswers(questions, { a: 'ab' });
    expect(errors).toHaveLength(1);
    expect(errors[0]!.code).toBe('minLength');
    expect(errors[0]!.questionId).toBe('a');
  });
});
