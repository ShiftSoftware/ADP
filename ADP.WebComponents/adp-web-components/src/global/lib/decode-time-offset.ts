import { addDays, addHours, addMinutes, addMonths, addSeconds, addYears, format, startOfDay } from 'date-fns';
import { now } from './clock';

export type DateTypes = 'date' | 'time' | 'datetime-local';
export function decodeTimeOffset({ offsets, type, date, today }: { date?: Date; offsets: number[]; type?: DateTypes; today?: string }) {
  if (!date) {
    const clock = now(today);
    // date-fns performs calendar arithmetic in local time. Mirror an explicit
    // UTC calendar date into local time first so a negative offset cannot roll
    // the anchor back to the previous day.
    date = today ? new Date(clock.getUTCFullYear(), clock.getUTCMonth(), clock.getUTCDate()) : startOfDay(clock);
  }

  if (offsets.length > 0) date = addYears(date, offsets[0]);
  if (offsets.length > 1) date = addMonths(date, offsets[1]);
  if (offsets.length > 2) date = addDays(date, offsets[2]);
  if (offsets.length > 3) date = addHours(date, offsets[3]);
  if (offsets.length > 4) date = addMinutes(date, offsets[4]);
  if (offsets.length > 5) date = addSeconds(date, offsets[5]);

  if (type) {
    switch (type) {
      case 'date':
        return format(date, 'yyyy-MM-dd');
      case 'time':
        return format(date, 'HH:mm');
      case 'datetime-local':
        return format(date, "yyyy-MM-dd'T'HH:mm");
      default:
        return date;
    }
  }

  return date;
}
