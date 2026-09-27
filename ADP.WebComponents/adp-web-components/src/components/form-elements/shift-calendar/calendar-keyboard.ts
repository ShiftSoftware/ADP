import { addDays, addMonths, addYears, clampDate, endOfWeek, startOfWeek, Weekday } from '~lib/calendar-date';

export interface KeyboardContext {
  rtl: boolean;
  weekStartsOn: Weekday;
  min?: string | null;
  max?: string | null;
}

export interface KeyInput {
  key: string;
  shiftKey?: boolean;
}

// Left and right follow the reading direction, as the APG grid asks.
export function moveFocus(focused: string, { key, shiftKey }: KeyInput, context: KeyboardContext): string | null {
  const forward = context.rtl ? 'ArrowLeft' : 'ArrowRight';
  const backward = context.rtl ? 'ArrowRight' : 'ArrowLeft';

  let target: string;

  switch (key) {
    case forward:
      target = addDays(focused, 1);
      break;
    case backward:
      target = addDays(focused, -1);
      break;
    case 'ArrowDown':
      target = addDays(focused, 7);
      break;
    case 'ArrowUp':
      target = addDays(focused, -7);
      break;
    case 'Home':
      target = startOfWeek(focused, context.weekStartsOn);
      break;
    case 'End':
      target = endOfWeek(focused, context.weekStartsOn);
      break;
    case 'PageUp':
      target = shiftKey ? addYears(focused, -1) : addMonths(focused, -1);
      break;
    case 'PageDown':
      target = shiftKey ? addYears(focused, 1) : addMonths(focused, 1);
      break;
    default:
      return null;
  }

  return clampDate(target, context.min, context.max);
}
