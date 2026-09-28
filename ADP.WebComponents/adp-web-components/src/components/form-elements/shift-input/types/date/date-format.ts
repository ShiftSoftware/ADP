import { daysInMonth, formatDate } from '~lib/calendar-date';

export type DateField = 'd' | 'M' | 'y';

interface Token {
  field: DateField;
  width: number;
}

export interface DateFormat {
  pattern: string;
  tokens: Token[];
  literals: string[];
}

export type ParsedText = { kind: 'empty' } | { kind: 'invalid' } | { kind: 'date'; value: string };

export const DEFAULT_FORMAT = 'dd/MM/yyyy';

const TOKEN = /d{1,2}|M{1,2}|y{4}/g;

export function compileFormat(pattern: string | undefined): DateFormat {
  const source = pattern || DEFAULT_FORMAT;
  const tokens: Token[] = [];
  const literals: string[] = [];
  let cursor = 0;

  for (const match of source.matchAll(TOKEN)) {
    literals.push(source.slice(cursor, match.index));
    tokens.push({ field: match[0][0] as DateField, width: match[0].length });
    cursor = match.index + match[0].length;
  }
  literals.push(source.slice(cursor));

  const fields = tokens
    .map(token => token.field)
    .sort()
    .join('');
  const literalsAreSeparators = literals.every(literal => !/[\p{L}\p{N}]/u.test(literal));

  if (fields !== 'Mdy' || !literalsAreSeparators) return compileFormat(DEFAULT_FORMAT);

  return { pattern: source, tokens, literals };
}

export function formatValue(value: string, format: DateFormat): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
  if (!match) return '';

  const parts: Record<DateField, number> = { y: Number(match[1]), M: Number(match[2]), d: Number(match[3]) };

  return format.tokens.reduce(
    (text, token, index) => text + String(parts[token.field]).padStart(token.field === 'y' ? 4 : token.width, '0') + format.literals[index + 1],
    format.literals[0],
  );
}

export function formatHint(format: DateFormat, letters: Record<DateField, string>): string {
  return format.tokens.reduce((text, token, index) => text + letters[token.field].repeat(token.field === 'y' ? 4 : 2) + format.literals[index + 1], format.literals[0]);
}

const ARABIC_INDIC = /[\u0660-\u0669\u06f0-\u06f9]/g;

export const latinDigits = (text: string): string => text.replace(ARABIC_INDIC, digit => String(digit.charCodeAt(0) & 0xf));

export function parseText(text: string, format: DateFormat): ParsedText {
  const input = latinDigits(String(text ?? '')).trim();

  if (!input) return { kind: 'empty' };
  if (/\p{L}/u.test(input)) return { kind: 'invalid' };

  const iso = /^(\d{4})-(\d{1,2})-(\d{1,2})$/.exec(input);
  if (iso) return toDate(Number(iso[1]), Number(iso[2]), Number(iso[3]));

  const groups = input.split(/\D+/).filter(Boolean);
  const order = format.tokens.map(token => token.field);

  if (groups.length === 1 && groups[0].length === 8) {
    let cursor = 0;
    const compact = order.map(field => {
      const width = field === 'y' ? 4 : 2;
      const part = groups[0].slice(cursor, cursor + width);
      cursor += width;
      return part;
    });

    return fromGroups(compact, order);
  }

  if (groups.length !== 3) return { kind: 'invalid' };

  return fromGroups(groups, order);
}

function fromGroups(groups: string[], order: DateField[]): ParsedText {
  const parts = {} as Record<DateField, string>;
  order.forEach((field, index) => (parts[field] = groups[index]));

  if (parts.y.length !== 4 || parts.M.length > 2 || parts.d.length > 2) return { kind: 'invalid' };

  return toDate(Number(parts.y), Number(parts.M), Number(parts.d));
}

function toDate(year: number, month: number, day: number): ParsedText {
  if (year < 1 || month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month)) return { kind: 'invalid' };

  return { kind: 'date', value: formatDate(year, month, day) };
}
