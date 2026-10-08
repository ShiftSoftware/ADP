import { useEffect, useRef } from 'react';
import { substituteTokens, type LocalizedString, type TokenSurface } from '@shiftsoftware/survey-sdk';
import 'adp-web-components/components/shift-booking-calendar';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

/** What `<shift-booking-calendar>` emits on a pick. `value` is the stored answer. */
interface SlotChangeDetail {
  date: string;
  raw: string;
  value: string;
}

type BookingCalendarElement = HTMLElement & {
  calendarApi?: string;
  branchId?: string;
  departmentId?: string;
  departmentIds?: string[];
  brandId?: string;
  language?: string;
  label?: string;
  isRequired?: boolean;
  showStatus?: boolean;
  showToday?: boolean;
  hourCycle?: 'h12' | 'h23';
  defaultValue?: string;
};

const LANGUAGES = ['en', 'ar', 'ku', 'ru'];

// The same list as Identity's calendar settings and the forms' bookingCalendar, until Identity links services to departments itself.
const SERVICE_DEPARTMENTS: Record<string, string[]> = {
  'auto-repair-and-maintenance': ['service-center', 'quick-service-center', 'satellite-1', 'satellite-2', 'satellite-3', 'parts-shop'],
  'parts-counter-sale': ['parts-shop'],
  'body-and-paint': ['body-shop'],
  'dio-and-life-style-products': ['showroom', 'parts-shop'],
  'new-vehicle-sale': ['showroom'],
  'test-drive': ['showroom'],
  'installment-sales': ['showroom'],
  'used-cars': ['showroom'],
  'used-car-purchase': ['showroom'],
  insurance: ['showroom'],
  'insurance-pending': ['showroom'],
};

/** The calendar the stored slot was picked from, per question. A slot only means
 *  something at the branch it came from, so when the respondent goes back and
 *  picks another branch (or department or brand), the slot is cleared. Kept outside
 *  React state because the question unmounts while the respondent is on an
 *  earlier screen. */
const pickedFrom = new WeakMap<object, Map<string, string>>();

/**
 * A preferred appointment slot, picked from a branch calendar through
 * `<shift-booking-calendar>` (adp-web-components). The component fetches the
 * availability itself; this wrapper resolves the question's `{{answers.*}}` tokens
 * into its target, and stores the chosen slot — the branch's wall-clock time,
 * `yyyy-MM-ddTHH:mm` — as the answer.
 */
export function BookingSlotQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer, answerContext } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const value = (answers[id] as string | undefined) ?? '';

  // The same surfaces as the server's pass: the endpoint is encoded like a URL, and the
  // ids raw, because the component encodes them when it builds the query.
  const resolve = (field: string, surface: TokenSurface = 'queryValue') =>
    substituteTokens(String(question[field] ?? ''), answerContext, locale, surface).trim();
  const calendarApi = resolve('calendarApi', 'url');
  const branchId = resolve('branchId');
  const brandId = resolve('brandId');
  // The branch's own departments aren't known here, so a service asks all of its departments; one the branch lacks returns no times.
  const ofServices = [...new Set(resolve('services').split(',').flatMap(service => SERVICE_DEPARTMENTS[service.trim()] ?? []))];
  const departments = resolve('departmentId') ? [resolve('departmentId')] : question['mergeDepartments'] ? ofServices : ofServices.slice(0, 1);
  const departmentId = departments.length === 1 ? departments[0]! : '';
  const departmentIds = departments.length > 1 ? departments : undefined;
  const target = [calendarApi, branchId, departments.join(','), brandId].join('\n');
  const language = LANGUAGES.includes(locale.slice(0, 2)) ? locale.slice(0, 2) : 'en';
  const label = localize(title, locale, schema.defaultLocale);

  const picks = pickedFrom.get(schema) ?? new Map<string, string>();
  if (!pickedFrom.has(schema)) pickedFrom.set(schema, picks);

  // A slot picked from a different calendar is no longer a valid answer.
  useEffect(() => {
    const from = picks.get(id);
    if (value && from !== undefined && from !== target) {
      picks.delete(id);
      setAnswer(id, null);
    }
  }, [target]); // eslint-disable-line react-hooks/exhaustive-deps

  const ref = useRef<BookingCalendarElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    el.calendarApi = calendarApi;
    el.branchId = branchId;
    el.departmentId = departmentId;
    el.departmentIds = departmentIds;
    el.brandId = brandId;
    el.language = language;
    el.label = label;
    el.isRequired = required;
    // Loading, the failure with its Try again, and the pick-a-day hint. Off by default, for
    // hosts that show their own; the survey has none.
    el.showStatus = true;
    // Bookings start days out, so Today is never pickable.
    el.showToday = false;
    el.hourCycle = 'h12';
    el.defaultValue = picks.get(id) === target ? value : '';
  }, [calendarApi, branchId, target, language, label, required]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const onSlot = (event: Event) => {
      const detail = (event as CustomEvent<SlotChangeDetail>).detail;
      picks.set(id, target);
      setAnswer(id, detail?.value || null);
    };
    el.addEventListener('slotChange', onSlot);
    return () => el.removeEventListener('slotChange', onSlot);
  }, [id, target, setAnswer]); // eslint-disable-line react-hooks/exhaustive-deps

  // The survey's label is the one on screen. The component's own stays hidden (its default) but
  // still names the calendar's group, so a screen reader hears the title once, on the calendar.
  return (
    <div className="survey-question survey-question--booking-slot">
      <div className="survey-question__label" aria-hidden="true">
        {label}
        {required && <span className="survey-question__required"> *</span>}
      </div>
      {help && <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>}
      <shift-booking-calendar id={`q-${id}`} ref={ref} />
    </div>
  );
}

declare module 'react' {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace JSX {
    interface IntrinsicElements {
      'shift-booking-calendar': React.DetailedHTMLProps<React.HTMLAttributes<BookingCalendarElement>, BookingCalendarElement>;
    }
  }
}
