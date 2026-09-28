import type { ShiftCalendar } from '../../../shift-calendar/shift-calendar';

export const CALENDAR_PROPS = [
  'value',
  'month',
  'min',
  'max',
  'enabledDates',
  'disabledDates',
  'disabledWeekdays',
  'isDateDisabled',
  'dayMeta',
  'today',
  'highlightToday',
  'weekStartsOn',
  'numerals',
  'disableViews',
  'showToday',
  'busy',
  'disabled',
  'label',
  'language',
  'appearance',
  'colorScheme',
  'size',
] as const;

export type CalendarProp = (typeof CALENDAR_PROPS)[number];

export type CalendarProps = Partial<Pick<ShiftCalendar, CalendarProp>>;

export const calendarProps = (source: CalendarProps): CalendarProps => Object.fromEntries(CALENDAR_PROPS.map(name => [name, source[name]]));
