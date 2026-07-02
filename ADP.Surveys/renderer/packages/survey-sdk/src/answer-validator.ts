/**
 * Client-side mirror of `ADP.Surveys.Shared/Answers/AnswerValidator.cs` —
 * the per-type constraint checks (type shape, min/max, length, pattern,
 * option-validity). Parity discipline: if a check changes on one side it must
 * change on the other in the same commit; `tests/answer-validator.test.ts`
 * mirrors the shape-check facts in `AnswerValidatorTests.cs`.
 *
 * Deliberate deviations from the C# side, both documented in STATUS.md:
 *  - Errors carry a stable machine `code` + `params` in addition to the
 *    English `message`, so the renderer can localize per-locale (the C# side
 *    returns developer-facing English strings — fine for a 400 payload, not
 *    for inline UI in an Arabic survey).
 *  - Presence/required is NOT checked here. The server enforces required
 *    path-aware via ComputeVisitedScreens; the renderer's own required gate
 *    owns it client-side. This module only validates answers that are present.
 */

export type AnswerValidationCode =
  | 'type'
  | 'minLength'
  | 'maxLength'
  | 'pattern'
  | 'min'
  | 'max'
  | 'range'
  | 'halfNotAllowed'
  | 'invalidOption'
  | 'minSelected'
  | 'maxSelected'
  | 'invalidDate'
  | 'minDate'
  | 'maxDate'
  | 'invalidDateTime'
  | 'minDateTime'
  | 'maxDateTime'
  | 'empty';

export interface AnswerValidationError {
  questionId: string;
  code: AnswerValidationCode;
  /** Numeric/string parameters for message templating (`n`, `min`, `max`…). */
  params?: Record<string, string | number>;
  /** English fallback mirroring the C# AnswerValidator copy. */
  message: string;
}

/** Loosely-typed wire question — the SDK's `Screen.questions` is `unknown[]`;
 *  this module reads only the constraint fields it needs. */
type WireQuestion = Record<string, unknown>;

const err = (
  questionId: string,
  code: AnswerValidationCode,
  message: string,
  params?: Record<string, string | number>,
): AnswerValidationError => ({ questionId, code, message, ...(params ? { params } : {}) });

const isFiniteNumber = (v: unknown): v is number => typeof v === 'number' && Number.isFinite(v);

/** Strict `yyyy-MM-dd` — mirrors C# `DateOnly.TryParseExact`. */
function parseIsoDate(raw: string): number | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(raw)) return null;
  const [y, m, d] = raw.split('-').map((p) => Number.parseInt(p, 10)) as [number, number, number];
  const dt = new Date(Date.UTC(y, m - 1, d));
  // Round-trip check rejects out-of-range parts like 2026-13-40.
  if (dt.getUTCFullYear() !== y || dt.getUTCMonth() !== m - 1 || dt.getUTCDate() !== d) return null;
  return dt.getTime();
}

/** Lenient ISO 8601 — mirrors C# `DateTimeOffset.TryParse` closely enough. */
function parseIsoDateTime(raw: string): number | null {
  const t = Date.parse(raw);
  return Number.isNaN(t) ? null : t;
}

/**
 * Validates one present answer value against its question's type + constraints.
 * Mirrors `AnswerValidator.ValidateAnswer`'s per-type switch. Callers must not
 * pass absent answers — presence/required is the caller's concern.
 */
export function validateAnswerValue(question: WireQuestion, value: unknown): AnswerValidationError[] {
  const id = question['id'] as string;
  const errors: AnswerValidationError[] = [];

  switch (question['type'] as string | undefined) {
    case 'text': {
      if (typeof value !== 'string') {
        errors.push(err(id, 'type', 'Text answer must be a JSON string.'));
        break;
      }
      const minLength = question['minLength'];
      const maxLength = question['maxLength'];
      const pattern = question['pattern'];
      if (isFiniteNumber(minLength) && value.length < minLength)
        errors.push(err(id, 'minLength', `Answer length ${value.length} is less than minLength ${minLength}.`, { n: minLength, actual: value.length }));
      if (isFiniteNumber(maxLength) && value.length > maxLength)
        errors.push(err(id, 'maxLength', `Answer length ${value.length} exceeds maxLength ${maxLength}.`, { n: maxLength, actual: value.length }));
      if (typeof pattern === 'string' && pattern.length > 0) {
        // An invalid regex in the schema fails open (no error) — the publish
        // validator owns schema quality; the client must not crash on it.
        try {
          if (!new RegExp(pattern).test(value)) {
            errors.push(err(id, 'pattern', 'Answer does not match the required pattern.'));
          }
        } catch {
          /* fail open */
        }
      }
      break;
    }

    case 'paragraph': {
      if (typeof value !== 'string') {
        errors.push(err(id, 'type', 'Paragraph answer must be a JSON string.'));
        break;
      }
      const minLength = question['minLength'];
      const maxLength = question['maxLength'];
      if (isFiniteNumber(minLength) && value.length < minLength)
        errors.push(err(id, 'minLength', `Answer length ${value.length} is less than minLength ${minLength}.`, { n: minLength, actual: value.length }));
      if (isFiniteNumber(maxLength) && value.length > maxLength)
        errors.push(err(id, 'maxLength', `Answer length ${value.length} exceeds maxLength ${maxLength}.`, { n: maxLength, actual: value.length }));
      break;
    }

    case 'number': {
      if (!isFiniteNumber(value)) {
        errors.push(err(id, 'type', 'Number answer must be a JSON number.'));
        break;
      }
      const min = question['min'];
      const max = question['max'];
      if (isFiniteNumber(min) && value < min)
        errors.push(err(id, 'min', `Answer ${value} is less than min ${min}.`, { n: min }));
      if (isFiniteNumber(max) && value > max)
        errors.push(err(id, 'max', `Answer ${value} exceeds max ${max}.`, { n: max }));
      break;
    }

    case 'rating': {
      if (!isFiniteNumber(value)) {
        errors.push(err(id, 'type', 'Rating answer must be a JSON number.'));
        break;
      }
      const max = isFiniteNumber(question['max']) ? (question['max'] as number) : 0;
      if (value < 0 || value > max)
        errors.push(err(id, 'range', `Rating ${value} is outside 0..${max}.`, { min: 0, max }));
      if (question['allowHalf'] !== true && value !== Math.floor(value))
        errors.push(err(id, 'halfNotAllowed', 'Rating does not allow half values.'));
      break;
    }

    case 'nps': {
      // C# reads the value with GetInt32(), which rejects non-integers — the
      // client mirrors that as an explicit type error instead of a crash.
      if (!isFiniteNumber(value) || !Number.isInteger(value)) {
        errors.push(err(id, 'type', 'NPS answer must be a JSON number.'));
        break;
      }
      const min = isFiniteNumber(question['min']) ? (question['min'] as number) : 0;
      const max = isFiniteNumber(question['max']) ? (question['max'] as number) : 10;
      if (value < min || value > max)
        errors.push(err(id, 'range', `NPS answer ${value} is outside ${min}..${max}.`, { min, max }));
      break;
    }

    case 'singleChoice':
    case 'dropdown':
    case 'navigationList': {
      if (typeof value !== 'string') {
        errors.push(err(id, 'type', 'Choice answer must be a JSON string (option id).'));
        break;
      }
      // Sourced questions (optionsSource != null) skip membership — the option
      // list lives on an external endpoint, unknowable at the schema level.
      // Mirrors the C# relaxation; the choice widgets themselves only offer
      // fetched options, so membership is enforced by construction in the UI.
      if (question['optionsSource'] != null) break;
      const options = Array.isArray(question['options']) ? (question['options'] as WireQuestion[]) : [];
      if (!options.some((o) => o['id'] === value))
        errors.push(err(id, 'invalidOption', `'${value}' is not a valid option id for this question.`, { option: value }));
      break;
    }

    case 'multiChoice': {
      if (!Array.isArray(value)) {
        errors.push(err(id, 'type', 'MultiChoice answer must be a JSON array of option ids.'));
        break;
      }
      const options = Array.isArray(question['options']) ? (question['options'] as WireQuestion[]) : [];
      const validIds = new Set(options.map((o) => o['id'] as string));
      const picked: string[] = [];
      let badEntry = false;
      for (const item of value) {
        if (typeof item !== 'string') {
          errors.push(err(id, 'type', 'Each MultiChoice array entry must be a string option id.'));
          badEntry = true;
          break;
        }
        picked.push(item);
      }
      if (badEntry) break;
      // Same sourced-question membership relaxation as the single-value choices.
      if (question['optionsSource'] == null) {
        for (const optionId of picked) {
          if (!validIds.has(optionId))
            errors.push(err(id, 'invalidOption', `'${optionId}' is not a valid option id for this question.`, { option: optionId }));
        }
      }
      const minSelected = question['minSelected'];
      const maxSelected = question['maxSelected'];
      if (isFiniteNumber(minSelected) && picked.length < minSelected)
        errors.push(err(id, 'minSelected', `At least ${minSelected} option(s) must be selected.`, { n: minSelected }));
      if (isFiniteNumber(maxSelected) && picked.length > maxSelected)
        errors.push(err(id, 'maxSelected', `At most ${maxSelected} option(s) may be selected.`, { n: maxSelected }));
      break;
    }

    case 'date': {
      if (typeof value !== 'string') {
        errors.push(err(id, 'type', 'Date answer must be a JSON string in yyyy-MM-dd format.'));
        break;
      }
      const t = parseIsoDate(value);
      if (t === null) {
        errors.push(err(id, 'invalidDate', `Date '${value}' is not yyyy-MM-dd.`));
        break;
      }
      const minDate = question['minDate'];
      const maxDate = question['maxDate'];
      if (typeof minDate === 'string') {
        const minT = parseIsoDate(minDate);
        if (minT !== null && t < minT)
          errors.push(err(id, 'minDate', `Date ${value} is before minDate ${minDate}.`, { min: minDate }));
      }
      if (typeof maxDate === 'string') {
        const maxT = parseIsoDate(maxDate);
        if (maxT !== null && t > maxT)
          errors.push(err(id, 'maxDate', `Date ${value} is after maxDate ${maxDate}.`, { max: maxDate }));
      }
      break;
    }

    case 'dateTime': {
      if (typeof value !== 'string') {
        errors.push(err(id, 'type', 'DateTime answer must be a JSON string in ISO 8601 format.'));
        break;
      }
      const t = parseIsoDateTime(value);
      if (t === null) {
        errors.push(err(id, 'invalidDateTime', `DateTime '${value}' is not valid ISO 8601.`));
        break;
      }
      const minDateTime = question['minDateTime'];
      const maxDateTime = question['maxDateTime'];
      if (typeof minDateTime === 'string' && minDateTime.length > 0) {
        const minT = parseIsoDateTime(minDateTime);
        if (minT !== null && t < minT)
          errors.push(err(id, 'minDateTime', `DateTime is before minDateTime ${minDateTime}.`, { min: minDateTime }));
      }
      if (typeof maxDateTime === 'string' && maxDateTime.length > 0) {
        const maxT = parseIsoDateTime(maxDateTime);
        if (maxT !== null && t > maxT)
          errors.push(err(id, 'maxDateTime', `DateTime is after maxDateTime ${maxDateTime}.`, { max: maxDateTime }));
      }
      break;
    }

    case 'file': {
      if (typeof value !== 'string' || value.length === 0)
        errors.push(err(id, 'empty', 'Answer must be a non-empty file reference string.'));
      break;
    }

    case 'signature': {
      if (typeof value !== 'string' || value.length === 0)
        errors.push(err(id, 'empty', 'Answer must be a non-empty signature data url string.'));
      break;
    }

    case 'yesNo': {
      if (typeof value !== 'boolean')
        errors.push(err(id, 'type', 'Yes/No answer must be a JSON boolean.'));
      break;
    }

    // Unknown types fail open — the registry renders an unsupported-type
    // placeholder; there's nothing meaningful to validate.
  }

  return errors;
}

/**
 * Shape-checks every PRESENT answer for a screen's questions. Absent answers
 * (undefined / null) are skipped — required/presence enforcement belongs to
 * the renderer's required gate (client) and the path-aware server validator.
 */
export function validatePresentAnswers(
  questions: readonly unknown[] | undefined,
  answers: Record<string, unknown>,
): AnswerValidationError[] {
  const errors: AnswerValidationError[] = [];
  for (const raw of questions ?? []) {
    const question = raw as WireQuestion;
    const id = question['id'];
    if (typeof id !== 'string') continue;
    const value = answers[id];
    if (value === undefined || value === null) continue;
    errors.push(...validateAnswerValue(question, value));
  }
  return errors;
}
