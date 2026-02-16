import { addDays, addHours, addMinutes, addMonths, addSeconds, addYears, format, startOfDay } from 'date-fns';

export type DateTypes = 'date' | 'time' | 'datetime-local';
export function decodeTimeOffset({ offsets, type, date = startOfDay(new Date()) }: { date?: Date; offsets: number[]; type?: DateTypes }) {
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
