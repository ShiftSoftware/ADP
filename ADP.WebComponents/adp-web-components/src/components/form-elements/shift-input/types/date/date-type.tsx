import { h } from '@stencil/core';

import { today as clockToday } from '~lib/clock';
import { blockReason, buildAvailability } from '~lib/calendar-availability';
import { addDays, firstOfMonth, lastOfMonth, localDate, monthOf, parseDate } from '~lib/calendar-date';

import type { CalendarDateChangeDetail, CalendarMonthChangeDetail } from '../../../shift-calendar/shift-calendar';
import { calendarLocale, dateLabel } from '../../../shift-calendar/calendar-locale';
import type { FormInputLocalization } from '~features/form-hook/inputs/form-input';
import type { LanguageKeys } from '~features/multi-lingual';
import { fill } from '../../input-locale';
import type { InputField, InputType } from '../input-type';
import { CalendarProps, calendarProps } from './calendar-pass-through';
import { compileFormat, formatHint, formatValue, parseText } from './date-format';

export interface DateFieldProps extends CalendarProps {
  format: string;
  openOnControlClick: boolean;
  localization?: FormInputLocalization<{ unavailableMessage?: string }>;
  appearance?: CalendarProps['appearance'];
  colorScheme: NonNullable<CalendarProps['colorScheme']>;
  size: NonNullable<CalendarProps['size']>;
}

type DateField = InputField & DateFieldProps;

const popovers = new WeakMap<DateField, HTMLShiftPopoverElement>();
const calendars = new WeakMap<DateField, HTMLShiftCalendarElement>();

const letters = (field: DateField) => ({ d: field.strings.day, M: field.strings.month, y: field.strings.year });

const nextFrame = () => new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

function rules(field: DateField) {
  return buildAvailability({
    min: field.min,
    max: field.max,
    disabledDates: field.disabledDates,
    disabledWeekdays: field.disabledWeekdays,
    enabledDates: field.enabledDates,
    isDateDisabled: field.isDateDisabled,
  });
}

function focusTarget(field: DateField): string {
  const availability = rules(field);
  const selected = parseDate(field.value);
  const today = parseDate(field.today) ? clockToday(field.today) : localDate();

  if (selected && !blockReason(selected, availability)) return selected;
  if (!blockReason(today, availability)) return today;

  const month = calendars.get(field)?.month ?? monthOf(selected ?? today);
  for (let date = firstOfMonth(month), last = lastOfMonth(month); date <= last; date = addDays(date, 1)) if (!blockReason(date, availability)) return date;

  return '';
}

async function open(field: DateField, focus: boolean) {
  const popover = popovers.get(field);
  if (!popover || field.fieldDisabled || field.readonly) return;

  await popover.show({ focus: false });
  if (!focus) return;

  // The panel becomes visible on its next render; focus waits for it.
  await nextFrame();
  await nextFrame();
  calendars.get(field)?.setFocus(focusTarget(field) || undefined);
}

export const dateType: InputType<DateFieldProps> = {
  inputMode: 'numeric',
  inputRole: 'combobox',

  placeholder: field => formatHint(compileFormat(field.format), letters(field)),

  normalize: value => parseDate(value) ?? '',

  display: (value, field) => formatValue(value, compileFormat(field.format)),

  read(text, field) {
    const format = compileFormat(field.format);
    const parsed = parseText(text, format);
    const own = field.localization?.[field.language];
    const shown = (date?: string) => formatValue(parseDate(date) ?? '', format);
    const slots = (template: string) => template.replace(/\$minDate\$/g, shown(field.min)).replace(/\$maxDate\$/g, shown(field.max));

    if (parsed.kind === 'empty') return { value: '' };
    if (parsed.kind === 'invalid') return { error: own?.format || fill(field.strings.invalid, { format: formatHint(format, letters(field)) }) };

    const reason = blockReason(parsed.value, rules(field));
    if (!reason) return { value: parsed.value };

    const min = parseDate(field.min);
    if (reason === 'range' && min && parsed.value < min) return { error: own?.minMessage ? slots(own.minMessage) : fill(field.strings.tooEarly, { date: shown(field.min) }) };
    if (reason === 'range') return { error: own?.maxMessage ? slots(own.maxMessage) : fill(field.strings.tooLate, { date: shown(field.max) }) };

    return { error: own?.unavailableMessage || fill(field.strings.unavailable, { date: text.trim() }) };
  },

  mounted(field) {
    const popover = popovers.get(field);
    if (popover) popover.anchor = field.input;
  },

  keydown(event, field) {
    if (event.key !== 'ArrowDown' || !(event.altKey || field.expanded)) return;

    event.preventDefault();
    open(field, true);
  },

  controlClick(field) {
    if (field.openOnControlClick && !field.expanded) open(field, false);
  },

  adornments(field) {
    const selected = parseDate(field.value);
    const locale = calendarLocale(field.language);
    const label = selected ? fill(field.strings.changeDate, { date: dateLabel(selected, locale, locale.numerals) }) : field.strings.chooseDate;
    const unavailable = field.fieldDisabled || field.readonly;

    return (
      <button
        type="button"
        class="in-icon in-calendar-button"
        part="calendar-button"
        aria-label={label}
        aria-haspopup="dialog"
        aria-expanded={String(field.expanded)}
        disabled={unavailable}
        onClick={() => {
          if (field.expanded) popovers.get(field)?.toggle();
          else open(field, true);
        }}
      >
        <svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">
          <rect x="2.5" y="3.5" width="11" height="10" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.4" />
          <path d="M2.5 6.5h11M5.5 2v3M10.5 2v3" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
        </svg>
      </button>
    );
  },

  outside(field) {
    return (
      <shift-popover
        ref={element => element && popovers.set(field, element)}
        class={field.el.className
          .split(/\s+/)
          .filter(name => name && name !== 'hydrated')
          .join(' ')}
        anchor={field.input}
        label={field.strings.chooseDate}
        language={field.language as LanguageKeys}
        appearance={field.appearance}
        colorScheme={field.colorScheme}
        size={field.size}
        mobileSheet={field.mobileSheet}
        onOpenChange={(event: CustomEvent<{ open: boolean }>) => {
          event.stopPropagation();
          field.expanded = event.detail.open;
        }}
      >
        <shift-calendar
          ref={element => element && calendars.set(field, element)}
          {...calendarProps(field)}
          value={field.value}
          label={field.fieldLabel || field.strings.chooseDate}
          disabled={field.fieldDisabled}
          onDateChange={(event: CustomEvent<CalendarDateChangeDetail>) => {
            event.stopPropagation();
            field.choose(event.detail.value);
            popovers.get(field)?.hide();
          }}
          onMonthChange={(event: CustomEvent<CalendarMonthChangeDetail>) => {
            event.stopPropagation();
            field.forwardMonthChange(event.detail);
          }}
          onViewChange={(event: Event) => event.stopPropagation()}
        />
      </shift-popover>
    );
  },
};
