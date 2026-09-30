import { slotIso } from './booking-availability';

export interface PickerChangeDetail {
  value: string;
  label: string;
  complete: boolean;
}

export interface PickerStatusDetail {
  text: string;
  busy: boolean;
}

const TOKENS = /yyyy|MM|dd|HH|hh|mm|ss|tt/g;
const ISO = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::\d{2}(?:\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?$/;
const OFFSET = /^([+-])(\d{2}):?(\d{2})$/;

const pad = (value: number) => String(value).padStart(2, '0');

const offsetMinutes = (offset: string): number | null => {
  if (offset === 'Z') return 0;

  const match = OFFSET.exec(offset || '');
  return match ? (match[1] === '-' ? -1 : 1) * (Number(match[2]) * 60 + Number(match[3])) : null;
};

export function valueOffset(value: string): string {
  const offset = ISO.exec((value ?? '').trim())?.[6];
  if (!offset) return '';
  if (offset === 'Z') return '+00:00';

  const match = OFFSET.exec(offset);
  return match ? `${match[1]}${match[2]}:${match[3]}` : '';
}

const isIso = (format?: string) => !format || format.trim().toLowerCase() === 'iso';

const local = (year: number, month: number, day: number, hours: number, minutes: number) => `${year}-${pad(month)}-${pad(day)}T${pad(hours)}:${pad(minutes)}`;

// A slot local time (YYYY-MM-DDTHH:mm) written as the host asked: ISO (with an offset only when the host set one), or a pattern of yyyy MM dd HH hh mm ss tt.
export function formatPickerValue(slot: string, format?: string, utcOffset?: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(slot || '');
  if (!match) return '';
  if (isIso(format)) return offsetMinutes(utcOffset) === null ? `${slot}:00` : slotIso(slot, utcOffset);

  const hours = Number(match[4]);
  const parts: Record<string, string> = {
    yyyy: match[1],
    MM: match[2],
    dd: match[3],
    HH: match[4],
    hh: pad(hours % 12 || 12),
    mm: match[5],
    ss: '00',
    tt: hours < 12 ? 'AM' : 'PM',
  };

  return format.replace(TOKENS, token => parts[token]);
}

function readPattern(value: string, format: string): string {
  const order: string[] = format.match(TOKENS) ?? [];
  const group = (token: string) => (token === 'yyyy' ? '(\\d{4})' : token === 'tt' ? '([AaPp][Mm])' : '(\\d{1,2})');
  const source = format
    .split(TOKENS)
    .map((text, index) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + (order[index] ? group(order[index]) : ''))
    .join('');
  const match = new RegExp(`^${source}$`).exec(value.trim());
  if (!match) return '';

  const read = (token: string) => (order.includes(token) ? match[order.indexOf(token) + 1] : undefined);
  const pm = /p/i.test(read('tt') ?? '');
  const hours = read('HH') !== undefined ? Number(read('HH')) : (Number(read('hh') ?? 0) % 12) + (pm ? 12 : 0);
  const [year, month, day, minutes] = [Number(read('yyyy')), Number(read('MM')), Number(read('dd')), Number(read('mm') ?? 0)];

  if (!year || month < 1 || month > 12 || day < 1 || day > 31 || hours > 23 || minutes > 59) return '';
  return local(year, month, day, hours, minutes);
}

// Any value a host passes in, ISO (moved to utcOffset when both carry one) or the value format, as the slot's local time; '' when unreadable.
export function readPickerValue(value: string, format?: string, utcOffset?: string): string {
  const text = (value ?? '').trim();
  if (!text) return '';

  const iso = ISO.exec(text);
  if (iso) {
    const [year, month, day, hours, minutes] = iso.slice(1, 6).map(Number);
    const from = iso[6] ? offsetMinutes(iso[6]) : null;
    const to = offsetMinutes(utcOffset);
    if (from === null || to === null || from === to) return local(year, month, day, hours, minutes);

    const moved = new Date(0);
    moved.setUTCFullYear(year, month - 1, day);
    moved.setUTCHours(hours, minutes + to - from);
    return local(moved.getUTCFullYear(), moved.getUTCMonth() + 1, moved.getUTCDate(), moved.getUTCHours(), moved.getUTCMinutes());
  }

  return isIso(format) ? '' : readPattern(text, format);
}
