import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';
import { AnyObjectSchema, string } from 'yup';

import { FormHook } from '~features/form-hook/form-hook';
import { FormElement, FormInputLocalization, FormInputMeta, getInputLocalization } from '~features/form-hook';
import type { LanguageKeys } from '~features/multi-lingual';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';

import '~lib/middleware';
import { BookingAvailability, BookingDay, CalendarApiVersion, calendarApiVersion, createAvailabilityLoader, slotValue } from '~lib/booking-availability';
import { blockReason, buildAvailability } from '~lib/calendar-availability';
import { dateParts, monthOf, weekdayOf } from '~lib/calendar-date';

import { y } from '../../forms/defaults/validation';
import type { BranchSlotSelection } from '../branch-slot-picker';
import { calendarLocale, dateLabel, formatDigits, monthTitle } from '../shift-calendar/calendar-locale';
import type { CalendarAppearance, CalendarColorScheme, CalendarDayMetaMap, CalendarSize } from '../shift-calendar/shift-calendar';
import { bookingStrings, fill, formatTime, HourCycle } from '../shift-time-slots/time-slots';

export type BookingStatus = 'idle' | 'loading' | 'ready' | 'empty' | 'error';
export type BookingView = 'days' | 'times';

const MESSAGES = ['idle', 'loading', 'empty', 'error', 'pickDay', 'pickTime', 'selected', 'invalid'] as const;

type Message = (typeof MESSAGES)[number];

type Frame = Record<string, string>;

@Component({
  shadow: { delegatesFocus: true },
  tag: 'shift-booking-calendar',
  styleUrl: 'shift-booking-calendar.css',
})
export class ShiftBookingCalendar implements FormElement {
  @Element() el!: HTMLElement;

  @Prop({ reflect: true }) name: string = 'bookingCalendar';
  @Prop() form?: FormHook<unknown>;

  @Prop() calendarApi?: string;
  @Prop() calendarApiVersion: CalendarApiVersion = 'v1';
  @Prop() companyId?: string;
  @Prop() branchId?: string;
  @Prop() departmentId?: string;
  @Prop() brandId?: string;
  @Prop() today?: string;

  @Prop() disabledWeekdays?: string | number[];
  @Prop() disabledDates?: string | string[];
  @Prop() slotCounts: boolean = false;
  @Prop() hourCycle: HourCycle = 'h23';

  @Prop() label?: string;
  @Prop() wrapperId?: string;
  @Prop() wrapperClass?: string;
  @Prop() isRequired: boolean = false;
  @Prop() isDisabled: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() localization?: FormInputLocalization = {};
  @Prop({ mutable: true }) defaultValue?: string;

  @Prop({ reflect: true }) appearance?: CalendarAppearance;
  @Prop({ reflect: true }) colorScheme: CalendarColorScheme = 'light';
  @Prop({ reflect: true }) size: CalendarSize = 'md';

  @Event({ bubbles: true, composed: true }) slotChange!: EventEmitter<BranchSlotSelection>;

  @State() status: BookingStatus = 'idle';
  @State() availability: BookingAvailability | null = null;
  @State() selectedDate: string = '';
  @State() selectedRaw: string = '';
  @State() month?: string;
  @State() view: BookingView = 'days';
  @State() leavingTime: string | null = null;
  @State() announced: boolean = false;

  private loader = createAvailabilityLoader();
  private loadQueued = false;
  private pendingFocus: 'times' | 'day' | null = null;
  private timeTimer?: ReturnType<typeof setTimeout>;
  private frameKey = '';
  private frameObserver?: ResizeObserver;
  private observedRoot: HTMLElement | null = null;
  private labelId = `shift-booking-calendar-label-${Math.random().toString(36).slice(2, 10)}`;
  private titleId = `shift-booking-calendar-title-${Math.random().toString(36).slice(2, 10)}`;

  componentWillLoad() {
    this.form?.subscribe(this.name, this);
    this.load();
  }

  componentDidRender() {
    this.watchFrame();

    const target = this.pendingFocus;
    this.pendingFocus = null;

    if (target === 'day') this.calendarEl?.setFocus();
    if (target === 'times') {
      const slots = this.slotsEl;

      slots?.componentOnReady?.().then(() => {
        if (this.view !== 'times') return;
        this.reveal(slots.shadowRoot?.querySelector<HTMLElement>('[data-selected]'), true);
        slots.setFocus();
      });
    }
  }

  disconnectedCallback() {
    this.loader.cancel();
    this.form?.unsubscribe(this.name);
    clearTimeout(this.timeTimer);
    this.frameObserver?.disconnect();
    this.observedRoot = null;
  }

  @Watch('calendarApi')
  @Watch('calendarApiVersion')
  @Watch('companyId')
  @Watch('branchId')
  @Watch('departmentId')
  @Watch('brandId')
  @Watch('today')
  onTargetChange() {
    // A preset belongs to the branch it was given with; ids arriving while idle are that branch.
    if (this.status !== 'idle') this.defaultValue = '';
    this.selectedRaw = '';
    this.selectedDate = '';
    this.closeTimes();
    this.queueLoad();
  }

  @Watch('disabledWeekdays')
  @Watch('disabledDates')
  onRulesChange() {
    if (this.selectedDate && !this.isOpen(this.selectedDate)) {
      this.selectedRaw = '';
      this.selectedDate = '';
      this.closeTimes();
    }
  }

  @Method()
  async refresh() {
    this.load();
  }

  @Method()
  async setFocus() {
    if (this.view === 'times') await this.slotsEl?.setFocus();
    else await this.calendarEl?.setFocus();
  }

  reset(newValue?: unknown) {
    this.defaultValue = (newValue as string) ?? '';
    this.selectedRaw = '';
    this.selectedDate = '';
    this.closeTimes();
  }

  getValue() {
    return this.selectedRaw ? slotValue(this.selectedRaw) : this.defaultValue || '';
  }

  validate() {
    const required = (schema: ReturnType<typeof string>) => schema.required(y.require(this.name));

    return string()
      .meta(y.meta(this.name))
      .when(y.condition(this.name), {
        is: true,
        then: required,
        otherwise: schema => (this.isRequired ? required(schema) : schema.optional()),
      }) as unknown as AnyObjectSchema;
  }

  private get locale() {
    return calendarLocale(this.language);
  }

  private get strings() {
    return bookingStrings(this.locale.language);
  }

  private get calendarEl() {
    return this.el.shadowRoot?.querySelector('shift-calendar') as HTMLShiftCalendarElement | null;
  }

  private get slotsEl() {
    return this.el.shadowRoot?.querySelector('shift-time-slots') as HTMLShiftTimeSlotsElement | null;
  }

  private get day(): BookingDay | undefined {
    return this.availability?.days.find(day => day.date === this.selectedDate);
  }

  private isOpen(date: string): boolean {
    const rules = buildAvailability({ disabledWeekdays: this.disabledWeekdays, disabledDates: this.disabledDates });

    return !!this.availability?.days.some(day => day.date === date) && !blockReason(date, rules);
  }

  private firstOpenDay(): BookingDay | undefined {
    return this.availability?.days.find(day => this.isOpen(day.date));
  }

  // A form sets the ids, the version and the endpoint one after another; one request answers them all.
  private queueLoad() {
    if (this.loadQueued) return;

    this.loadQueued = true;
    queueMicrotask(() => {
      this.loadQueued = false;
      this.load();
    });
  }

  private async load() {
    const target = {
      url: this.calendarApi,
      version: calendarApiVersion(this.calendarApiVersion),
      companyId: this.companyId,
      branchId: this.branchId,
      departmentId: this.departmentId,
      brandId: this.brandId,
    };

    if (!target.url || !target.companyId || !target.branchId || !target.departmentId || !target.brandId) {
      this.loader.cancel();
      this.status = 'idle';
      this.availability = null;
      this.selectedDate = '';
      this.closeTimes();
      return;
    }

    this.status = 'loading';
    this.closeTimes();

    const outcome = await this.loader.load(target, { today: this.today, language: this.language });

    if (outcome.status === 'cancelled') return;

    if (outcome.status !== 'ready') {
      this.availability = null;
      this.selectedDate = '';
      this.status = outcome.status;
      return;
    }

    this.availability = outcome.availability;

    const preset = this.defaultValue ? this.availability.days.find(day => this.defaultValue.startsWith(day.date) && this.isOpen(day.date)) : undefined;
    const opening = preset ?? this.firstOpenDay();

    if (!opening) {
      this.status = 'empty';
      return;
    }

    this.selectedDate = preset?.date ?? '';
    this.selectedRaw = preset?.times.find(time => slotValue(time.raw) === this.defaultValue)?.raw ?? '';
    this.month = monthOf(opening.date);
    this.status = 'ready';
  }

  private openTimes() {
    clearTimeout(this.timeTimer);
    this.leavingTime = null;
    this.announced = true;
    this.view = 'times';
    this.pendingFocus = 'times';
  }

  // Focus follows only when it was inside the view that closes; a branch picked elsewhere keeps its own focus.
  private closeTimes(focusDay = this.focusIsInTimes()) {
    if (this.view !== 'times') return;

    clearTimeout(this.timeTimer);
    this.leavingTime = null;
    this.announced = true;
    this.view = 'days';
    if (focusDay) this.pendingFocus = 'day';
  }

  private focusIsInTimes(): boolean {
    const active = this.el.shadowRoot?.activeElement;

    return !!active && !!this.el.shadowRoot?.querySelector('.bc-times')?.contains(active);
  }

  private onDateChange = (event: CustomEvent<{ value: string }>) => {
    event.stopPropagation();

    const date = event.detail.value;
    const day = this.availability?.days.find(item => item.date === date);
    if (this.status !== 'ready' || !day || !this.isOpen(date)) return;

    const kept = this.selectedRaw ? day.times.find(time => time.time === this.timeOf(this.selectedRaw)) : undefined;
    const raw = kept?.raw ?? '';

    this.selectedDate = date;
    if (raw !== this.selectedRaw) {
      this.selectedRaw = raw;
      if (kept) this.commit(raw);
    }
    this.openTimes();
  };

  private onMonthChange = (event: CustomEvent<{ month: string }>) => {
    event.stopPropagation();
    this.month = event.detail.month;
  };

  private onTimeChange = (event: CustomEvent<{ value: string }>) => {
    event.stopPropagation();

    const time = this.day?.times.find(item => item.time === event.detail.value);
    if (!time || time.raw === this.selectedRaw) return;

    const before = this.timeText();

    this.selectedRaw = time.raw;
    this.rollTime(before);
    this.commit(time.raw);
  };

  private rollTime(before: string) {
    clearTimeout(this.timeTimer);

    if (this.view !== 'times' || before === this.timeText() || window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) {
      this.leavingTime = null;
      return;
    }

    this.leavingTime = before;
    this.timeTimer = setTimeout(() => (this.leavingTime = null), 1500);
  }

  private onTimeSettled = (event: AnimationEvent) => {
    if (event.target === event.currentTarget) this.leavingTime = null;
  };

  private onTimesKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' || this.view !== 'times') return;

    event.preventDefault();
    event.stopPropagation();
    this.closeTimes(true);
  };

  // Chips take focus without scrolling (the slide must not be cut short), so the list scrolls to them here.
  private onTimesFocus = (event: FocusEvent) => {
    const target = event.composedPath()[0] as HTMLElement | undefined;
    if (target?.classList?.contains('ts-chip')) this.reveal(target, false);
  };

  private reveal(chip: HTMLElement | null | undefined, centre: boolean) {
    const scroller = this.el.shadowRoot?.querySelector<HTMLElement>('.bc-times-scroll');
    if (!scroller) return;
    if (!chip) {
      if (centre) scroller.scrollTop = 0;
      return;
    }

    const view = scroller.getBoundingClientRect();
    const box = chip.getBoundingClientRect();
    const top = box.top - view.top + scroller.scrollTop;
    const room = scroller.clientHeight;

    if (centre) scroller.scrollTop = Math.max(0, top - (room - box.height) / 2);
    else if (box.top < view.top) scroller.scrollTop = top;
    else if (box.bottom > view.bottom) scroller.scrollTop = top + box.height - room;
  }

  private commit(raw: string) {
    const value = slotValue(raw);

    this.slotChange.emit({ date: raw.split(' ')[0], raw, value });
    this.form?.validateForm(this.name, value);
  }

  private timeOf(raw: string): string {
    return slotValue(raw).slice(11);
  }

  private timeText(): string {
    if (!this.selectedRaw || !this.selectedRaw.startsWith(this.selectedDate)) return '';

    return `· ${formatTime(this.timeOf(this.selectedRaw), this.hourCycle, this.locale.numerals, this.strings)}`;
  }

  private dayTitle(date: string): string {
    if (!date) return '';

    const locale = this.locale;
    const [, month, day] = dateParts(date);

    return fill(this.strings.dayTitle, {
      weekday: locale.strings.weekdaysShort[weekdayOf(date)],
      day: formatDigits(day, locale.numerals),
      month: this.strings.monthsShort[month - 1],
    });
  }

  // The times view sits exactly on the calendar's panel, header and body, whatever appearance, size or host overrides set them to.
  private watchFrame() {
    const root = this.calendarEl?.shadowRoot?.querySelector<HTMLElement>('.cal-root');

    if (root && root !== this.observedRoot && typeof ResizeObserver !== 'undefined') {
      this.frameObserver?.disconnect();
      this.frameObserver = new ResizeObserver(() => this.syncFrame());
      this.frameObserver.observe(root);
      this.observedRoot = root;
    }

    this.syncFrame();
  }

  private syncFrame() {
    const shadow = this.calendarEl?.shadowRoot;
    const root = shadow?.querySelector<HTMLElement>('.cal-root');
    const header = shadow?.querySelector<HTMLElement>('.cal-header');
    const band = shadow?.querySelector<HTMLElement>('.cal-title-band');
    const title = shadow?.querySelector<HTMLElement>('.cal-title[data-current] .cal-title-button');
    if (!root || !header || !band || !title || typeof getComputedStyle !== 'function' || typeof root.getClientRects !== 'function' || !root.getClientRects().length) return;

    const panel = getComputedStyle(root);
    const heading = getComputedStyle(title);
    const frame: Frame = {
      '--_f-border': `${panel.borderTopWidth} ${panel.borderRightWidth} ${panel.borderBottomWidth} ${panel.borderLeftWidth}`,
      '--_f-radius': `${panel.borderTopLeftRadius} ${panel.borderTopRightRadius} ${panel.borderBottomRightRadius} ${panel.borderBottomLeftRadius}`,
      '--_f-padding': `${panel.paddingTop} ${panel.paddingRight} ${panel.paddingBottom} ${panel.paddingLeft}`,
      '--_f-travel': panel.paddingLeft,
      '--_f-gap': panel.rowGap,
      '--_f-head': `${header.offsetHeight}px`,
      '--_f-head-gap': getComputedStyle(header).columnGap,
      '--_f-band': getComputedStyle(band).marginInlineStart,
      '--_f-title-font': heading.fontFamily,
      '--_f-title-size': heading.fontSize,
      '--_f-title-weight': heading.fontWeight,
      '--_f-title-line': heading.lineHeight,
      '--_f-title-padding': heading.paddingInlineStart,
    };
    const key = JSON.stringify(frame);

    const box = this.el.shadowRoot?.querySelector<HTMLElement>('.bc-box');
    if (!box || key === this.frameKey) return;

    this.frameKey = key;
    for (const [name, value] of Object.entries(frame)) box.style.setProperty(name, value);
  }

  private get dayMeta(): CalendarDayMetaMap | undefined {
    if (!this.slotCounts || !this.availability) return undefined;

    const strings = this.strings;

    return Object.fromEntries(
      this.availability.days.map(day => [day.date, { badge: String(day.times.length), tone: 'neutral', description: fill(strings.dayTimes, { count: day.times.length }) }]),
    );
  }

  private message(isError: boolean): Message {
    if (this.status !== 'ready') return this.status;
    if (isError) return 'invalid';
    if (this.selectedRaw) return 'selected';

    return this.view === 'times' ? 'pickTime' : 'pickDay';
  }

  private messageText(message: Message, errorText: string): string {
    const strings = this.strings;
    const locale = this.locale;

    if (message === 'invalid') return errorText;
    if (message === 'selected') {
      return this.selectedRaw
        ? fill(strings.selected, {
            date: dateLabel(this.selectedRaw.slice(0, 10), locale, locale.numerals),
            time: formatTime(this.timeOf(this.selectedRaw), this.hourCycle, locale.numerals, strings),
          })
        : '';
    }

    return strings[message];
  }

  private renderTimeSlot() {
    const text = this.timeText();
    const leaving = this.leavingTime !== null && this.leavingTime !== text ? this.leavingTime : null;

    return (
      <span class="bc-slot" part="times-time">
        {leaving && (
          <span key={`time-${leaving}`} class="bc-slot-layer" data-phase="leave" aria-hidden="true">
            {leaving}
          </span>
        )}
        <span key={`time-${text}`} class="bc-slot-layer" data-phase={leaving !== null ? 'enter' : 'rest'} onAnimationEnd={leaving !== null ? this.onTimeSettled : undefined}>
          {text}
        </span>
      </span>
    );
  }

  render() {
    const locale = this.locale;
    const strings = this.strings;
    const state = this.form?.getInputState<FormInputMeta>(this.name);
    const localised = this.form ? getInputLocalization(this, state?.meta, state?.errorMessage) : null;
    const ownLabel = localised?.label && localised.label !== y.label(this.name) ? localised.label : '';
    const label = ownLabel || this.localization?.[this.language]?.label || this.label || strings.bookingCalendar;
    const isRequired = !!state?.isRequired || this.isRequired;
    const isError = !!state?.isError;
    const errorText = localised?.errorTextMessage && localised.errorTextMessage !== y.require(this.name) ? localised.errorTextMessage : strings.required;
    const disabled = this.isDisabled || !!state?.disabled;
    const ready = this.status === 'ready';
    const times = this.view === 'times';
    const current = this.message(isError);
    const day = this.day;
    const fullDate = this.selectedDate ? dateLabel(this.selectedDate, locale, locale.numerals) : '';
    const timesLabel = fullDate ? fill(strings.timesOn, { date: fullDate }) : strings.times;
    const announcement = !this.announced ? '' : times ? timesLabel : this.month ? monthTitle(this.month, locale, locale.numerals) : '';

    return (
      <Host translate="no">
        <div
          class={{ 'bc-root': true, [this.wrapperClass]: !!this.wrapperClass }}
          part="root"
          id={this.wrapperId}
          dir={locale.direction}
          lang={locale.language === 'ku' ? 'ckb' : locale.language}
          role="group"
          aria-labelledby={this.labelId}
          aria-disabled={disabled ? 'true' : undefined}
          data-status={this.status}
          data-view={this.view}
        >
          <div class="bc-label" part="label" id={this.labelId}>
            {label}
            <span class="bc-required" part="required" aria-hidden="true" data-hidden={isRequired ? undefined : ''}>
              *
            </span>
          </div>

          <div class="bc-box" part="box" data-view={this.view}>
            <shift-calendar
              part="calendar"
              label={label}
              language={this.language}
              today={this.today}
              appearance={this.appearance}
              colorScheme={this.colorScheme}
              size={this.size}
              value={this.selectedDate}
              month={this.month}
              min={ready ? this.availability.first : undefined}
              max={ready ? this.availability.last : undefined}
              enabledDates={ready ? this.availability.enabledDates : []}
              disabledDates={this.disabledDates}
              disabledWeekdays={this.disabledWeekdays}
              dayMeta={ready ? this.dayMeta : undefined}
              busy={this.status === 'loading'}
              disabled={disabled}
              aria-hidden={times ? 'true' : undefined}
              inert={times ? true : undefined}
              onDateChange={this.onDateChange}
              onMonthChange={this.onMonthChange}
            />

            <div
              class="bc-times"
              part="times"
              role="group"
              aria-labelledby={this.titleId}
              aria-hidden={times ? undefined : 'true'}
              inert={times ? undefined : true}
              onKeyDown={this.onTimesKeyDown}
              onFocusin={this.onTimesFocus}
            >
              <div class="bc-times-header" part="times-header">
                <div class="bc-title-band">
                  <div class="bc-title" part="times-heading" id={this.titleId}>
                    <span class="bc-title-day">{this.dayTitle(this.selectedDate)}</span>
                    {this.renderTimeSlot()}
                  </div>
                </div>
                <div class="bc-back-band">
                  <button type="button" class="bc-back" part="back" aria-label={strings.changeDate} onClick={() => this.closeTimes(true)}>
                    <ChevronLeftIcon class="bc-back-icon" />
                    <span class="bc-back-text" aria-hidden="true">
                      {strings.changeDate}
                    </span>
                  </button>
                </div>
              </div>

              <div class="bc-times-body" part="times-body">
                <div class="bc-times-scroll">
                  <shift-time-slots
                    key={`slots-${this.selectedDate}`}
                    part="slots"
                    times={day ? day.times.map(time => time.time) : []}
                    value={this.selectedRaw && day ? this.timeOf(this.selectedRaw) : ''}
                    label={timesLabel}
                    hourCycle={this.hourCycle}
                    language={this.language}
                    appearance={this.appearance}
                    colorScheme={this.colorScheme}
                    size={this.size}
                    disabled={disabled}
                    onTimeChange={this.onTimeChange}
                  />
                </div>
              </div>
            </div>
          </div>

          <span class="bc-sr" aria-live="polite">
            {announcement}
          </span>

          <div class="bc-status" part="status" aria-live="polite">
            {MESSAGES.map(message => (
              <div
                key={message}
                class="bc-message"
                data-message={message}
                data-active={message === current ? '' : undefined}
                aria-hidden={message === current ? undefined : 'true'}
                inert={message === current ? undefined : true}
              >
                {message === 'loading' && <span class="bc-spinner" aria-hidden="true" />}
                <span
                  class={message === 'invalid' ? 'bc-message-text bc-error-text' : 'bc-message-text'}
                  part={message === 'invalid' ? 'error' : message === 'error' ? 'status-text status-error' : 'status-text'}
                  role={message === 'invalid' && message === current ? 'alert' : undefined}
                >
                  {this.messageText(message, errorText)}
                </span>
                {message === 'error' && (
                  <button type="button" class="bc-retry" part="retry" onClick={() => this.load()}>
                    {strings.retry}
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      </Host>
    );
  }
}
