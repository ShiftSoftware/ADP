import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

import '~lib/middleware';
import { today as clockToday } from '~lib/clock';
import { Availability, blockKind, blockReason, buildAvailability, isInRange } from '~lib/calendar-availability';
import {
  addMonthsToMonth,
  clampDate,
  clampMonth,
  dateParts,
  daysInMonth,
  firstOfMonth,
  formatDate,
  lastOfMonth,
  localDate,
  monthGrid,
  monthOf,
  monthParts,
  normaliseWeekday,
  parseDate,
  parseMonth,
  Weekday,
} from '~lib/calendar-date';

import { moveFocus } from './calendar-keyboard';
import { CalendarLocale, CalendarNumerals, calendarLocale, dateLabel, formatDigits } from './calendar-locale';
import {
  canPage,
  CalendarView,
  headerButtons,
  indexBounds,
  monthFromIndex,
  monthIndex,
  monthIsOutside,
  moveInGrid,
  PAGE_SIZE,
  pageContaining,
  yearIsOutside,
  yearsPageStart,
} from './calendar-views';

export type CalendarDayTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';
export type CalendarAppearance = 'vanilla' | 'material' | 'soft' | 'sharp' | 'minimal';
export type CalendarColorScheme = 'light' | 'dark' | 'auto';
export type CalendarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface CalendarDayMeta {
  badge?: string;
  tone?: CalendarDayTone;
  description?: string;
}

export type CalendarDayMetaMap = Record<string, CalendarDayMeta>;

export interface CalendarDateChangeDetail {
  value: string;
}

export interface CalendarMonthChangeDetail {
  month: string;
  firstDate: string;
  lastDate: string;
}

export interface CalendarViewChangeDetail {
  view: CalendarView;
}

interface Snapshot {
  view: CalendarView;
  month: string;
  year: number;
  start: number;
}

interface SlotLeaving {
  value: number;
  offset: number;
}

interface Leaving {
  snapshot: Snapshot;
  offset: number;
  travel: 1 | -1;
}

const TONES: CalendarDayTone[] = ['neutral', 'info', 'success', 'warning', 'danger'];

const PICK_PARTS = {
  months: { base: 'month', selected: 'month-selected', today: 'month-today', disabled: 'month-disabled' },
  years: { base: 'year', selected: 'year-selected', today: 'year-today', disabled: 'year-disabled' },
};

function parseDayMeta(input: CalendarDayMetaMap | string | null | undefined): CalendarDayMetaMap {
  if (!input) return {};
  if (typeof input !== 'string') return input;

  try {
    const parsed = JSON.parse(input);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

const prefersReducedMotion = () => typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

const DEPTH: Record<CalendarView, number> = { days: 0, months: 1, years: 2 };

// In the days view the title stays put and its year and month roll on their own.
const titleKey = (snapshot: Snapshot) => (snapshot.view === 'days' ? 'days' : pageKey(snapshot));

const pageKey = (snapshot: Snapshot) => (snapshot.view === 'days' ? `d${snapshot.month}` : snapshot.view === 'months' ? `m${snapshot.year}` : `y${snapshot.start}`);

// The rendered offset of a layer that may be mid-flight, so an interrupted movement continues from where it is.
function offsetOf(element: Element | null | undefined, axis: 'x' | 'y'): number {
  if (!element || typeof getComputedStyle !== 'function') return 0;

  const transform = getComputedStyle(element).transform;
  const match = transform && transform !== 'none' ? transform.match(/matrix\(([^)]+)\)/) : null;
  if (!match) return 0;

  const parts = match[1].split(',').map(Number);
  return (axis === 'x' ? parts[4] : parts[5]) || 0;
}

@Component({
  shadow: true,
  tag: 'shift-calendar',
  styleUrl: 'shift-calendar.css',
})
export class ShiftCalendar {
  @Element() el!: HTMLElement;

  @Prop({ mutable: true, reflect: true }) value: string = '';
  @Prop() min?: string;
  @Prop() max?: string;
  @Prop({ mutable: true, reflect: true }) month?: string;

  @Prop() disabledDates?: string | string[];
  @Prop() disabledWeekdays?: string | number[];
  @Prop() enabledDates?: string | string[] | null;
  @Prop() isDateDisabled?: (date: string) => boolean;
  @Prop() dayMeta?: CalendarDayMetaMap | string;

  @Prop() today?: string;
  @Prop() highlightToday: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() weekStartsOn?: number;
  @Prop() numerals?: CalendarNumerals;
  @Prop() label?: string;
  @Prop({ reflect: true }) disabled: boolean = false;
  @Prop({ reflect: true }) busy: boolean = false;
  @Prop() disableViews: boolean = false;
  @Prop() showToday: boolean = true;

  @Prop({ reflect: true }) appearance?: CalendarAppearance;
  @Prop({ reflect: true }) colorScheme: CalendarColorScheme = 'light';
  @Prop({ reflect: true }) size: CalendarSize = 'md';

  @Event({ bubbles: true, composed: true }) dateChange!: EventEmitter<CalendarDateChangeDetail>;
  @Event({ bubbles: true, composed: true }) monthChange!: EventEmitter<CalendarMonthChangeDetail>;
  @Event({ bubbles: true, composed: true }) viewChange!: EventEmitter<CalendarViewChangeDetail>;

  @State() view: CalendarView = 'days';
  @State() pageYear: number = 0;
  @State() yearsStart: number = 0;
  @State() focusDate: string = '';
  @State() focusIndex: number = 0;
  @State() leavingPage: Leaving | null = null;
  @State() leavingTitle: Leaving | null = null;
  @State() leavingYear: SlotLeaving | null = null;
  @State() leavingMonthName: SlotLeaving | null = null;
  @State() leavingView: Snapshot | null = null;

  private shownMonth = '';
  private emitMonthChange = false;
  private pendingFocus: 'grid' | 'month-button' | 'year-button' | null = null;
  private returnView: CalendarView = 'days';
  private monthsOpener: 'month-button' | 'year-button' = 'month-button';
  private headerObserver?: ResizeObserver;
  private observedHeader: HTMLElement | null = null;
  private loaded = false;
  private settleTimer?: ReturnType<typeof setTimeout>;
  private headingId = `shift-calendar-heading-${Math.random().toString(36).slice(2, 10)}`;

  componentWillLoad() {
    const month = this.clampToRange(parseMonth(this.month) ?? this.openingMonth());

    this.shownMonth = month;
    this.month = month;
    this.pageYear = monthParts(month)[0];
    this.focusDate = this.focusFor(month);
  }

  componentDidLoad() {
    this.loaded = true;
  }

  componentDidRender() {
    this.watchHeader();

    if (!this.pendingFocus) return;

    const target = this.pendingFocus;
    this.pendingFocus = null;

    if (target === 'grid') this.focusCell();
    else this.el.shadowRoot?.querySelector<HTMLElement>(`.cal-title[data-current] [part~="${target}"]`)?.focus({ preventScroll: true });
  }

  disconnectedCallback() {
    clearTimeout(this.settleTimer);
    this.headerObserver?.disconnect();
    this.observedHeader = null;
  }

  // Measured once as soon as the header exists, before the first paint; then again on every resize and when fonts settle.
  private watchHeader() {
    const header = this.el.shadowRoot?.querySelector<HTMLElement>('.cal-header');
    if (!header || header === this.observedHeader || typeof ResizeObserver === 'undefined') return;

    this.headerObserver?.disconnect();
    this.headerObserver = new ResizeObserver(() => this.fitTodayLabel());
    this.headerObserver.observe(header);
    this.observedHeader = header;
    this.fitTodayLabel();
    document.fonts?.ready.then(() => this.fitTodayLabel());
  }

  // The label shows while the longest month name, the label and the month buttons all fit on one line.
  private fitTodayLabel() {
    const root = this.el.shadowRoot?.querySelector<HTMLElement>('.cal-root');
    const header = root?.querySelector<HTMLElement>('.cal-header');
    const measure = root?.querySelector<HTMLElement>('.cal-measure');
    const today = root?.querySelector<HTMLElement>('.cal-nav [part="today"]');
    if (!root || !header || !measure || !today) return;

    const width = (el: Element) => el.getBoundingClientRect().width;
    const title = Math.max(...Array.from(measure.querySelectorAll('.cal-measure-title')).map(width));
    const label = width(measure.querySelector('.cal-measure-today'));
    const nav = width(root.querySelector('.cal-nav')) - width(today) + label;
    const band = parseFloat(getComputedStyle(root.querySelector('.cal-title-band')).marginInlineStart) || 0;
    const gap = parseFloat(getComputedStyle(header).columnGap) || 0;

    root.toggleAttribute('data-today-label', title + band + gap + nav <= width(header));
  }

  @Watch('month')
  onMonthChange(next: string) {
    const month = this.clampToRange(parseMonth(next) ?? (this.shownMonth || this.openingMonth()));

    if (month !== next) {
      this.month = month;
      return;
    }

    const previous = this.shownMonth;
    const emit = this.emitMonthChange;

    this.emitMonthChange = false;
    if (month === previous) return;

    if (this.view === 'days') this.animateFrom({ ...this.snapshot(), month: previous });
    this.shownMonth = month;

    if (monthOf(this.focusDate) !== month) this.focusDate = this.focusFor(month, this.focusDate);
    if (emit) this.monthChange.emit({ month, firstDate: firstOfMonth(month), lastDate: lastOfMonth(month) });
  }

  @Watch('value')
  onValueChange(next: string) {
    const date = parseDate(next);
    if (!date) return;

    if (monthOf(date) !== this.month) this.showMonth(monthOf(date));
    if (isInRange(date, this.bounds)) this.focusDate = date;
  }

  @Watch('min')
  @Watch('max')
  onRangeChange() {
    this.showMonth(this.clampToRange(this.month ?? this.shownMonth));
    this.focusDate = this.focusFor(this.month, this.focusDate);
  }

  @Watch('disableViews')
  onDisableViews(disabled: boolean) {
    if (disabled && this.view !== 'days') this.setView('days');
  }

  @Method()
  async setFocus(date?: string) {
    const target = parseDate(date);

    if (target && this.view === 'days' && monthOf(target) === this.month && isInRange(target, this.bounds) && target !== this.focusDate) {
      this.focusDate = target;
      this.pendingFocus = 'grid';
      return;
    }

    this.focusCell();
  }

  private get minDate() {
    return parseDate(this.min);
  }

  private get maxDate() {
    return parseDate(this.max);
  }

  private get bounds() {
    return { min: this.minDate, max: this.maxDate };
  }

  // min after max leaves no selectable day; the month stays where it opened, with nowhere to go.
  private get rangeIsEmpty(): boolean {
    return !!this.minDate && !!this.maxDate && this.minDate > this.maxDate;
  }

  private clampToRange(month: string): string {
    return this.rangeIsEmpty ? month : clampMonth(month, this.minDate, this.maxDate);
  }

  private get todayDate(): string {
    return parseDate(this.today) ? clockToday(this.today) : localDate();
  }

  private get locale(): CalendarLocale {
    return calendarLocale(this.language);
  }

  private get rtl(): boolean {
    return this.locale.direction === 'rtl';
  }

  private get weekStart(): Weekday {
    return normaliseWeekday(this.weekStartsOn, this.locale.weekStartsOn);
  }

  private get digits(): CalendarNumerals {
    return this.numerals === 'latn' || this.numerals === 'arab' ? this.numerals : this.locale.numerals;
  }

  private availability(): Availability {
    return buildAvailability({
      min: this.min,
      max: this.max,
      disabledDates: this.disabledDates,
      disabledWeekdays: this.disabledWeekdays,
      enabledDates: this.enabledDates,
      isDateDisabled: this.isDateDisabled,
    });
  }

  private snapshot(): Snapshot {
    return { view: this.view, month: this.month, year: this.pageYear, start: this.yearsStart };
  }

  private openingMonth(): string {
    const selected = parseDate(this.value);

    return monthOf(selected ?? this.todayDate);
  }

  private focusFor(month: string, carried?: string): string {
    const selected = parseDate(this.value);
    const today = this.todayDate;
    const [year, m] = monthParts(month);

    if (this.rangeIsEmpty) return firstOfMonth(month);

    let candidate: string;

    if (selected && monthOf(selected) === month && isInRange(selected, this.bounds)) candidate = selected;
    else if (monthOf(today) === month && isInRange(today, this.bounds)) candidate = today;
    else if (carried) candidate = formatDate(year, m, Math.min(dateParts(carried)[2], daysInMonth(year, m)));
    else candidate = firstOfMonth(month);

    const min = this.minDate && this.minDate > firstOfMonth(month) ? this.minDate : firstOfMonth(month);
    const max = this.maxDate && this.maxDate < lastOfMonth(month) ? this.maxDate : lastOfMonth(month);

    return clampDate(candidate, min, max);
  }

  private showMonth(month: string) {
    if (month === this.month) return;

    this.emitMonthChange = true;
    this.month = month;
    this.emitMonthChange = false;
  }

  // Title, page and view all start on the same frame from where they are now: one movement, one clock.
  private animateFrom(previous: Snapshot) {
    const next = this.snapshot();
    const root = this.el.shadowRoot;

    clearTimeout(this.settleTimer);

    if (!this.loaded || prefersReducedMotion()) {
      this.leavingPage = this.leavingTitle = this.leavingView = this.leavingYear = this.leavingMonthName = null;
      return;
    }

    if (titleKey(previous) !== titleKey(next)) {
      this.leavingTitle = { snapshot: previous, offset: offsetOf(root?.querySelector('.cal-title[data-current]'), 'y'), travel: 1 };
      this.leavingYear = this.leavingMonthName = null;
    } else if (next.view === 'days') {
      const [fromYear, fromMonth] = monthParts(previous.month);
      const [toYear, toMonth] = monthParts(next.month);
      const slot = (name: string) => offsetOf(root?.querySelector(`.cal-title[data-current] [data-slot="${name}"] .cal-slot-layer[data-current]`), 'y');

      if (fromYear !== toYear) this.leavingYear = { value: fromYear, offset: slot('year') };
      if (fromMonth !== toMonth) this.leavingMonthName = { value: fromMonth, offset: slot('month') };
    }

    if (previous.view !== next.view) {
      this.leavingView = previous;
      this.leavingPage = null;
    } else if (pageKey(previous) !== pageKey(next)) {
      const forward = previous.view === 'days' ? next.month > previous.month : previous.view === 'months' ? next.year > previous.year : next.start > previous.start;

      this.leavingPage = { snapshot: previous, offset: offsetOf(root?.querySelector('.cal-page[data-current]'), 'x'), travel: forward ? 1 : -1 };
    }

    this.settleTimer = setTimeout(() => (this.leavingPage = this.leavingTitle = this.leavingView = this.leavingYear = this.leavingMonthName = null), 1500);
  }

  private onSettled = (event: AnimationEvent, which: 'page' | 'title' | 'view' | 'year' | 'month') => {
    if (event.target !== event.currentTarget) return;

    if (which === 'page') this.leavingPage = null;
    if (which === 'title') this.leavingTitle = null;
    if (which === 'year') this.leavingYear = null;
    if (which === 'month') this.leavingMonthName = null;
    if (which === 'view') this.leavingView = null;
  };

  private setView(view: CalendarView, focus?: 'grid' | 'month-button' | 'year-button') {
    if (view === this.view) return;

    const previous = this.snapshot();

    this.view = view;
    this.animateFrom(previous);
    if (focus) this.pendingFocus = focus;
    this.viewChange.emit({ view });
  }

  private openMonths() {
    const [year, month] = monthParts(this.month);

    this.monthsOpener = 'month-button';
    this.pageYear = year;
    this.focusIndex = this.clampIndex('months', monthIndex(year, month));
    this.setView('months', 'grid');
  }

  private openYears() {
    const year = this.view === 'months' ? this.pageYear : monthParts(this.month)[0];

    this.returnView = this.view;
    this.yearsStart = yearsPageStart(year);
    this.focusIndex = this.clampIndex('years', year);
    this.setView('years', 'grid');
  }

  private closeView() {
    if (this.view === 'months') {
      this.setView('days', this.monthsOpener);
      return;
    }

    if (this.returnView === 'months') {
      this.focusIndex = this.clampIndex('months', monthIndex(this.pageYear, monthParts(this.month)[1]));
      this.setView('months', 'year-button');
      return;
    }

    this.setView('days', 'year-button');
  }

  private clampIndex(view: 'months' | 'years', index: number): number {
    const [low, high] = indexBounds(view, this.minDate, this.maxDate);

    return Math.min(high, Math.max(low, index));
  }

  private chooseYear(year: number) {
    if (this.disabled || yearIsOutside(year, this.minDate, this.maxDate)) return;

    const previous = this.snapshot();
    const month = this.clampToRange(`${String(year).padStart(4, '0')}-${this.month.slice(5, 7)}`);

    this.pageYear = year;
    this.focusIndex = this.clampIndex('months', monthIndex(year, monthParts(month)[1]));
    this.monthsOpener = 'year-button';
    this.view = 'months';
    this.showMonth(month);
    this.animateFrom(previous);
    this.pendingFocus = 'grid';
    this.viewChange.emit({ view: 'months' });
  }

  private chooseMonth(index: number) {
    const [year, month] = monthFromIndex(index);
    if (this.disabled || monthIsOutside(year, month, this.minDate, this.maxDate)) return;

    const previous = this.snapshot();

    this.view = 'days';
    this.showMonth(`${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}`);
    this.focusDate = this.focusFor(this.month, this.focusDate);
    this.animateFrom(previous);
    this.pendingFocus = 'month-button';
    this.viewChange.emit({ view: 'days' });
  }

  private focusCell() {
    const cell = this.el.shadowRoot?.querySelector<HTMLElement>('.cal-view[data-current] .cal-page[data-current] [tabindex="0"]');

    // Never scroll the clipping boxes to the cell: that would cut the slide short.
    cell?.focus({ preventScroll: true });
  }

  private select(date: string, rules: Availability) {
    if (this.disabled || this.busy) return;
    if (monthOf(date) !== this.month || blockReason(date, rules)) return;

    this.value = date;
    this.dateChange.emit({ value: date });
  }

  private page(step: 1 | -1) {
    if (this.disabled || this.rangeIsEmpty) return;

    if (this.view === 'days') {
      this.showMonth(this.clampToRange(addMonthsToMonth(this.month, step)));
      return;
    }

    const previous = this.snapshot();

    if (this.view === 'months') {
      if (!canPage('months', this.pageYear, step, this.minDate, this.maxDate)) return;
      this.pageYear += step;
      this.focusIndex = this.clampIndex('months', this.focusIndex + step * PAGE_SIZE);
    } else {
      if (!canPage('years', this.yearsStart, step, this.minDate, this.maxDate)) return;
      this.yearsStart += step * PAGE_SIZE;
      this.focusIndex = this.clampIndex('years', this.focusIndex + step * PAGE_SIZE);
    }

    this.animateFrom(previous);
  }

  private goToToday() {
    const today = this.todayDate;
    if (this.disabled || !isInRange(today, this.bounds)) return;

    if (this.view !== 'days') {
      const previous = this.snapshot();

      this.view = 'days';
      this.showMonth(monthOf(today));
      this.animateFrom(previous);
      this.viewChange.emit({ view: 'days' });
    } else {
      this.showMonth(monthOf(today));
    }

    this.focusDate = today;
    this.pendingFocus = 'grid';
    this.select(today, this.availability());
  }

  private onRootKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' || this.view === 'days') return;

    event.preventDefault();
    event.stopPropagation();
    this.closeView();
  };

  private onDaysKeyDown(event: KeyboardEvent, rules: Availability) {
    if (this.disabled || event.altKey || event.ctrlKey || event.metaKey) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.select(this.focusDate, rules);
      return;
    }

    if (this.rangeIsEmpty) return;

    const target = moveFocus(this.focusDate, event, { rtl: this.rtl, weekStartsOn: this.weekStart, min: this.minDate, max: this.maxDate });
    if (!target) return;

    event.preventDefault();
    if (monthOf(target) !== this.month) this.showMonth(monthOf(target));

    this.focusDate = target;
    this.pendingFocus = 'grid';
  }

  private onPickerKeyDown(event: KeyboardEvent, view: 'months' | 'years') {
    if (this.disabled || event.altKey || event.ctrlKey || event.metaKey) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      if (view === 'months') this.chooseMonth(this.focusIndex);
      else this.chooseYear(this.focusIndex);
      return;
    }

    const pageStart = view === 'months' ? monthIndex(this.pageYear, 1) : this.yearsStart;
    const [low, high] = indexBounds(view, this.minDate, this.maxDate);
    const target = moveInGrid(this.focusIndex, event.key, { rtl: this.rtl, pageStart, low, high });

    if (target === null) return;

    event.preventDefault();

    const page = pageContaining(view, target, view === 'months' ? this.pageYear : this.yearsStart);
    const current = view === 'months' ? this.pageYear : this.yearsStart;

    if (page !== current) {
      const previous = this.snapshot();

      if (view === 'months') this.pageYear = page;
      else this.yearsStart = page;
      this.animateFrom(previous);
    }

    this.focusIndex = target;
    this.pendingFocus = 'grid';
  }

  private onCellClick(date: string, rules: Availability) {
    if (this.disabled || monthOf(date) !== this.month || !isInRange(date, rules)) return;

    this.focusDate = date;
    this.select(date, rules);
  }

  private titleText(snapshot: Snapshot): string {
    const strings = this.locale.strings;
    const [year, month] = monthParts(snapshot.month);

    if (snapshot.view === 'days') return `${strings.months[month - 1]} ${formatDigits(year, this.digits)}`;
    if (snapshot.view === 'months') return formatDigits(snapshot.year, this.digits);
    return `${formatDigits(snapshot.start, this.digits)}–${formatDigits(snapshot.start + PAGE_SIZE - 1, this.digits)}`;
  }

  private renderTitle(snapshot: Snapshot, leaving: Leaving | null) {
    const current = !leaving;
    const strings = this.locale.strings;
    const [year, month] = monthParts(snapshot.month);
    const allowed = headerButtons(this.minDate, this.maxDate);
    const views = !this.disableViews && !this.disabled;
    const yearShown = snapshot.view === 'months' ? snapshot.year : year;
    const inert = current ? undefined : true;

    const yearButton = (value: number, live: boolean) => (
      <button
        type="button"
        class="cal-title-button cal-year"
        part={live ? 'year-button' : undefined}
        tabindex={current && live ? undefined : '-1'}
        disabled={!views || !allowed.year}
        aria-label={`${strings.chooseYear}, ${formatDigits(value, this.digits)}`}
        onClick={() => this.openYears()}
      >
        {formatDigits(value, this.digits)}
      </button>
    );

    const monthButton = (value: number, live: boolean) => (
      <button
        type="button"
        class="cal-title-button"
        part={live ? 'month-button' : undefined}
        tabindex={current && live ? undefined : '-1'}
        disabled={!views || !allowed.month}
        aria-label={`${strings.chooseMonth}, ${strings.months[value - 1]}`}
        onClick={() => this.openMonths()}
      >
        {strings.months[value - 1]}
      </button>
    );

    const slot = (name: 'year' | 'month', value: number, gone: SlotLeaving | null, button: (value: number, live: boolean) => unknown) => (
      <span class="cal-slot" data-slot={name}>
        {gone && gone.value !== value && (
          <span key={`${name}-${gone.value}`} class="cal-slot-layer" data-phase="leave" style={{ '--_offset': `${gone.offset}px` }} aria-hidden="true" inert>
            {button(gone.value, false)}
          </span>
        )}
        <span
          key={`${name}-${value}`}
          class="cal-slot-layer"
          data-current=""
          data-phase={gone && gone.value !== value ? 'enter' : 'rest'}
          style={{ '--_offset': `${gone?.offset ?? 0}px` }}
          onAnimationEnd={(event: AnimationEvent) => this.onSettled(event, name)}
        >
          {button(value, true)}
        </span>
      </span>
    );

    return (
      <div
        key={`title-${titleKey(snapshot)}`}
        class="cal-title"
        part="heading"
        data-current={current ? '' : undefined}
        data-phase={leaving ? 'leave' : this.leavingTitle ? 'enter' : 'rest'}
        style={{ '--_offset': `${(leaving ?? this.leavingTitle)?.offset ?? 0}px` }}
        aria-hidden={current ? undefined : 'true'}
        inert={inert}
        onAnimationEnd={current ? (event: AnimationEvent) => this.onSettled(event, 'title') : undefined}
      >
        {snapshot.view === 'days' && [slot('year', year, current ? this.leavingYear : null, yearButton), slot('month', month, current ? this.leavingMonthName : null, monthButton)]}
        {snapshot.view === 'months' && yearButton(yearShown, true)}
        {snapshot.view === 'years' && <span class="cal-title-text">{this.titleText(snapshot)}</span>}
      </div>
    );
  }

  private pageAttributes(snapshot: Snapshot, leaving: Leaving | null, onSettled: boolean) {
    const moving = leaving ?? this.leavingPage;

    return {
      'key': `page-${pageKey(snapshot)}`,
      'class': 'cal-page',
      'data-current': leaving ? undefined : '',
      'data-phase': leaving ? 'leave' : this.leavingPage ? 'enter' : 'rest',
      'data-travel': moving ? (moving.travel > 0 ? 'forward' : 'back') : undefined,
      'style': { '--_offset': `${moving?.offset ?? 0}px`, '--_travel': String(moving?.travel ?? 1) },
      'aria-hidden': leaving ? 'true' : undefined,
      'inert': leaving ? true : undefined,
      'onAnimationEnd': onSettled && !leaving ? (event: AnimationEvent) => this.onSettled(event, 'page') : undefined,
    };
  }

  private renderDaysPage(snapshot: Snapshot, rules: Availability, meta: CalendarDayMetaMap, leaving: Leaving | null, live: boolean) {
    const selected = parseDate(this.value);
    const today = this.todayDate;
    const cells = monthGrid(snapshot.month, this.weekStart);
    const weeks = Array.from({ length: 6 }, (_, row) => cells.slice(row * 7, row * 7 + 7));
    const interactive = live && !leaving;

    return (
      <div {...this.pageAttributes(snapshot, leaving, live)}>
        {weeks.map(week => (
          <div role="row" class="cal-week" part="week">
            {week.map(({ date, inMonth }) => {
              const reason = inMonth ? blockReason(date, rules) : null;
              const kind = blockKind(reason);
              const isSelected = inMonth && date === selected;
              const isToday = inMonth && date === today;
              const marked = isToday && this.highlightToday;
              const reachable = interactive && inMonth && !this.disabled && isInRange(date, rules);
              const dayMeta = inMonth ? meta[date] : undefined;
              const badge = dayMeta?.badge ? String(dayMeta.badge) : '';
              const tone = dayMeta?.tone && TONES.includes(dayMeta.tone) ? dayMeta.tone : 'neutral';
              const description = dayMeta?.description ? `, ${dayMeta.description}` : '';
              const blocked = !inMonth || !!reason || this.disabled;

              const part = ['day', !inMonth && 'day-outside', isSelected && 'day-selected', marked && 'day-today', blocked && 'day-disabled', kind === 'closed' && 'day-closed']
                .filter(Boolean)
                .join(' ');

              return (
                <div
                  role="gridcell"
                  class={{ 'cal-day': true, 'has-badge': !!badge }}
                  part={part}
                  data-date={inMonth ? date : undefined}
                  data-outside={inMonth ? undefined : ''}
                  data-kind={inMonth ? kind || 'open' : undefined}
                  data-reason={reason || undefined}
                  data-selected={isSelected ? '' : undefined}
                  data-today={marked ? '' : undefined}
                  tabindex={reachable ? (date === this.focusDate ? '0' : '-1') : undefined}
                  aria-selected={isSelected ? 'true' : 'false'}
                  aria-disabled={blocked ? 'true' : undefined}
                  aria-current={isToday ? 'date' : undefined}
                  onClick={interactive ? () => this.onCellClick(date, rules) : undefined}
                >
                  <span class="cal-day-number" part="day-number" aria-hidden="true">
                    {formatDigits(dateParts(date)[2], this.digits)}
                  </span>
                  <span class="cal-badge" part="badge" data-tone={tone} data-empty={badge ? undefined : ''} aria-hidden="true">
                    {formatDigits(badge, this.digits)}
                  </span>
                  <span class="cal-sr">{dateLabel(date, this.locale, this.digits) + (inMonth ? description : '')}</span>
                </div>
              );
            })}
          </div>
        ))}
      </div>
    );
  }

  private renderPickerPage(view: 'months' | 'years', snapshot: Snapshot, leaving: Leaving | null, live: boolean) {
    const strings = this.locale.strings;
    const today = this.todayDate;
    const [todayYear, todayMonth] = dateParts(today);
    const [shownYear, shownMonth] = monthParts(this.month);
    const interactive = live && !leaving;
    const items =
      view === 'months' ? Array.from({ length: PAGE_SIZE }, (_, i) => monthIndex(snapshot.year, i + 1)) : Array.from({ length: PAGE_SIZE }, (_, i) => snapshot.start + i);
    const rows = Array.from({ length: 4 }, (_, row) => items.slice(row * 3, row * 3 + 3));

    return (
      <div {...this.pageAttributes(snapshot, leaving, live)}>
        {rows.map(row => (
          <div role="row" class="cal-picker-row">
            {row.map(index => {
              const [year, month] = view === 'months' ? monthFromIndex(index) : [index, 0];
              const outside = view === 'months' ? monthIsOutside(year, month, this.minDate, this.maxDate) : yearIsOutside(year, this.minDate, this.maxDate);
              const isSelected = view === 'months' ? year === shownYear && month === shownMonth : year === shownYear;
              const isCurrent = view === 'months' ? year === todayYear && month === todayMonth : year === todayYear;
              const marked = isCurrent && this.highlightToday;
              const blocked = outside || this.disabled;
              const text = view === 'months' ? strings.months[month - 1] : formatDigits(year, this.digits);
              const names = PICK_PARTS[view];
              const part = [names.base, isSelected && names.selected, marked && names.today, blocked && names.disabled].filter(Boolean).join(' ');

              return (
                <div
                  role="gridcell"
                  class="cal-pick"
                  part={part}
                  data-index={String(index)}
                  data-selected={isSelected ? '' : undefined}
                  data-today={marked ? '' : undefined}
                  data-kind={blocked ? 'unavailable' : 'open'}
                  tabindex={interactive && !this.disabled ? (index === this.focusIndex ? '0' : '-1') : undefined}
                  aria-selected={isSelected ? 'true' : 'false'}
                  aria-disabled={blocked ? 'true' : undefined}
                  onClick={interactive ? () => (view === 'months' ? this.chooseMonth(index) : this.chooseYear(index)) : undefined}
                >
                  <span class="cal-pick-text">{text}</span>
                </div>
              );
            })}
          </div>
        ))}
      </div>
    );
  }

  private renderView(snapshot: Snapshot, rules: Availability, meta: CalendarDayMetaMap, live: boolean) {
    const strings = this.locale.strings;
    const weekdays = Array.from({ length: 7 }, (_, index) => (this.weekStart + index) % 7);
    const pageLeaving = live && this.leavingPage && this.leavingPage.snapshot.view === snapshot.view ? this.leavingPage : null;
    const page = (s: Snapshot, leaving: Leaving | null) =>
      s.view === 'days' ? this.renderDaysPage(s, rules, meta, leaving, live) : this.renderPickerPage(s.view, s, leaving, live);

    return (
      <div
        key={`view-${snapshot.view}`}
        class="cal-view"
        data-view={snapshot.view}
        data-current={live ? '' : undefined}
        data-phase={live ? (this.leavingView ? 'enter' : 'rest') : 'leave'}
        data-shift={this.leavingView ? (DEPTH[this.view] > DEPTH[this.leavingView.view] ? 'deeper' : 'shallower') : undefined}
        aria-hidden={live ? undefined : 'true'}
        inert={live ? undefined : true}
        onAnimationEnd={live ? (event: AnimationEvent) => this.onSettled(event, 'view') : undefined}
      >
        <div
          role="grid"
          class="cal-grid"
          part={snapshot.view === 'days' ? 'grid' : snapshot.view}
          aria-labelledby={live ? this.headingId : undefined}
          aria-disabled={this.disabled ? 'true' : undefined}
          aria-busy={this.busy && snapshot.view === 'days' ? 'true' : undefined}
          onKeyDown={live ? event => (snapshot.view === 'days' ? this.onDaysKeyDown(event, rules) : this.onPickerKeyDown(event, snapshot.view as 'months' | 'years')) : undefined}
        >
          {snapshot.view === 'days' && (
            <div role="row" class="cal-weekdays" part="weekdays">
              {weekdays.map(weekday => (
                <div role="columnheader" class="cal-weekday" part="weekday">
                  <span class="cal-weekday-short" aria-hidden="true">
                    {strings.weekdaysShort[weekday]}
                  </span>
                  <span class="cal-weekday-narrow" aria-hidden="true">
                    {strings.weekdaysNarrow[weekday]}
                  </span>
                  <span class="cal-sr">{strings.weekdays[weekday]}</span>
                </div>
              ))}
            </div>
          )}

          <div class="cal-stage">
            {pageLeaving && page(pageLeaving.snapshot, pageLeaving)}
            {page(snapshot, null)}
          </div>
        </div>
      </div>
    );
  }

  render() {
    const locale = this.locale;
    const strings = locale.strings;
    const snapshot = this.snapshot();
    const rules = this.availability();
    const meta = parseDayMeta(this.dayMeta);
    const open = !this.disabled && !this.rangeIsEmpty;
    const [canBack, canForward] =
      this.view === 'days'
        ? [open && !(this.minDate && this.month <= monthOf(this.minDate)), open && !(this.maxDate && this.month >= monthOf(this.maxDate))]
        : this.view === 'months'
          ? [open && canPage('months', this.pageYear, -1, this.minDate, this.maxDate), open && canPage('months', this.pageYear, 1, this.minDate, this.maxDate)]
          : [open && canPage('years', this.yearsStart, -1, this.minDate, this.maxDate), open && canPage('years', this.yearsStart, 1, this.minDate, this.maxDate)];
    const [previousLabel, nextLabel] =
      this.view === 'days'
        ? [strings.previousMonth, strings.nextMonth]
        : this.view === 'months'
          ? [strings.previousYear, strings.nextYear]
          : [strings.previousYears, strings.nextYears];
    const canGoToday = !this.disabled && isInRange(this.todayDate, this.bounds);
    const titleLeaving = this.leavingTitle && titleKey(this.leavingTitle.snapshot) !== titleKey(snapshot) ? this.leavingTitle : null;
    const viewLeaving = this.leavingView && this.leavingView.view !== this.view ? this.leavingView : null;

    return (
      <Host>
        <div
          key={`${this.appearance || 'vanilla'}-${this.size}`}
          class="cal-root"
          part="root"
          dir={locale.direction}
          lang={locale.language === 'ku' ? 'ckb' : locale.language}
          role="group"
          aria-label={this.label || strings.calendar}
          aria-disabled={this.disabled ? 'true' : undefined}
          data-busy={this.busy ? '' : undefined}
          data-view={this.view}
          onKeyDown={this.onRootKeyDown}
        >
          <div class="cal-header" part="header">
            <div class="cal-title-band">
              {titleLeaving && this.renderTitle(titleLeaving.snapshot, titleLeaving)}
              {this.renderTitle(snapshot, null)}
            </div>
            <div class="cal-measure" aria-hidden="true">
              {strings.months.map(name => (
                <div class="cal-title cal-measure-title">
                  <span class="cal-title-button cal-year">{formatDigits(monthParts(this.month)[0], this.digits)}</span>
                  <span class="cal-title-button">{name}</span>
                </div>
              ))}
              <span class="cal-button cal-today cal-measure-today">
                <svg class="cal-today-icon" viewBox="0 0 16 16" focusable="false" />
                <span class="cal-today-text">{strings.today}</span>
              </span>
            </div>
            <span class="cal-sr" id={this.headingId} aria-live="polite">
              {this.titleText(snapshot)}
            </span>

            <div class="cal-nav" part="nav">
              <button
                type="button"
                class="cal-button cal-today"
                part="today"
                aria-label={strings.today}
                aria-hidden={this.showToday ? undefined : 'true'}
                data-hidden={this.showToday ? undefined : ''}
                tabindex={this.showToday ? undefined : '-1'}
                disabled={!canGoToday || !this.showToday}
                onClick={() => this.goToToday()}
              >
                <svg class="cal-today-icon" viewBox="0 0 16 16" aria-hidden="true" focusable="false">
                  <circle cx="8" cy="8" r="5.5" fill="none" stroke="currentColor" stroke-width="1.5" />
                  <circle cx="8" cy="8" r="2" fill="currentColor" />
                </svg>
                <span class="cal-today-text" aria-hidden="true">
                  {strings.today}
                </span>
              </button>
              <button type="button" class="cal-button cal-step" part="prev" aria-label={previousLabel} disabled={!canBack} onClick={() => this.page(-1)}>
                <ChevronLeftIcon class="cal-step-icon" />
              </button>
              <button type="button" class="cal-button cal-step" part="next" aria-label={nextLabel} disabled={!canForward} onClick={() => this.page(1)}>
                <ChevronRightIcon class="cal-step-icon" />
              </button>
            </div>
          </div>

          <div class="cal-body" data-switching={viewLeaving ? '' : undefined}>
            {viewLeaving && this.renderView(viewLeaving, rules, meta, false)}
            {this.renderView(snapshot, rules, meta, true)}
          </div>
        </div>
      </Host>
    );
  }
}
