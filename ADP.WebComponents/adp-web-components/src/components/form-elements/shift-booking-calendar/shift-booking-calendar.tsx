import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';
import { AnyObjectSchema, string } from 'yup';

import { FormHook } from '~features/form-hook/form-hook';
import { FormElement, FormInputLocalization, FormInputMeta, getInputLocalization } from '~features/form-hook';
import type { LanguageKeys } from '~features/multi-lingual';
import { BlazorInvokable, BlazorInvokableFunction, DotNetObjectReference, smartInvokable } from '~features/blazor-ref';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';

import '~lib/middleware';
import { BookingAvailability, BookingDay, createAvailabilityLoader, isCompleteTarget, slotValue } from '~lib/booking-availability';
import { blockReason, buildAvailability } from '~lib/calendar-availability';
import { monthOf } from '~lib/calendar-date';
import { formatPickerValue, readPickerValue, valueOffset } from '~lib/picker';
import { formatSlotLabel } from '~lib/slot-label';
import type { PickerChangeDetail, PickerStatusDetail } from '~lib/picker';

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

const IDENTITY = ['branchId', 'departmentId', 'services', 'brandId'] as const;

type Identity = Record<(typeof IDENTITY)[number], string>;

@Component({
  shadow: { delegatesFocus: true },
  tag: 'shift-booking-calendar',
  styleUrl: 'shift-booking-calendar.css',
})
export class ShiftBookingCalendar implements FormElement, BlazorInvokable {
  @Element() el!: HTMLElement;

  @Prop({ reflect: true }) name: string = 'bookingCalendar';
  @Prop() form?: FormHook<unknown>;

  @Prop() calendarApi?: string;
  // The branch's hash ID.
  @Prop() branchId?: string;
  @Prop() departmentId?: string;
  @Prop() services?: string;
  @Prop() brandId?: string;
  @Prop() today?: string;

  @Prop() disabledWeekdays?: string | number[];
  @Prop() disabledDates?: string | string[];
  @Prop() slotCounts: boolean = true;
  @Prop() dayTooltips: boolean = true;
  @Prop() fewSlots: number = 3;
  @Prop() showToday: boolean = true;
  @Prop() hourCycle: HourCycle = 'h23';
  @Prop() labelFormat?: string | Partial<Record<LanguageKeys, string>>;
  @Prop() dayTitleFormat?: string | Partial<Record<LanguageKeys, string>>;
  @Prop() utcOffset?: string;
  @Prop() valueFormat: string = 'iso';
  @Prop({ mutable: true }) value: string = '';
  @Prop() showLabel: boolean = false;
  @Prop() showStatus: boolean = false;
  @Prop() showEmptyState: boolean = true;

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
  @Event({ bubbles: true, composed: true }) pickerChange!: EventEmitter<PickerChangeDetail>;
  @Event({ bubbles: true, composed: true }) pickerStatus!: EventEmitter<PickerStatusDetail>;

  @State() status: BookingStatus = 'idle';
  @State() availability: BookingAvailability | null = null;
  @State() selectedDate: string = '';
  @State() selectedRaw: string = '';
  @State() month?: string;
  @State() view: BookingView = 'days';
  @State() leavingTime: string | null = null;
  @State() announced: boolean = false;

  private loader = createAvailabilityLoader();
  private syncQueued = false;
  private loadWanted = false;
  private current = '';
  private identity: Identity = { branchId: '', departmentId: '', services: '', brandId: '' };
  private valueTouched = false;
  private lastEmit = '\n';
  private pendingFocus: 'times' | 'day' | null = null;
  private timeTimer?: ReturnType<typeof setTimeout>;
  private frameKey = '';
  private frameObserver?: ResizeObserver;
  private observedRoot: HTMLElement | null = null;
  private labelId = `shift-booking-calendar-label-${Math.random().toString(36).slice(2, 10)}`;
  private titleId = `shift-booking-calendar-title-${Math.random().toString(36).slice(2, 10)}`;
  private emitted = '';
  private completing = false;
  private statusText = '';
  private detached = false;

  connectedCallback() {
    // A host that moves the picker (into a popover) disconnects it; a request cut off by the move is asked again.
    if (!this.detached) return;
    this.detached = false;
    this.watchFrame();
    if (this.status === 'loading') this.queueSync(true);
  }

  componentWillLoad() {
    this.form?.subscribe(this.name, this);
    this.current = this.read(this.value) || this.read(this.defaultValue);
    this.identity = this.ids();
    this.emitted = this.value || '';
    this.load();
  }

  componentDidLoad() {
    this.emitStatus();
    if (this.current) this.emitPicker();
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
    this.detached = true;
    this.loader.cancel();
    this.form?.unsubscribe(this.name);
    clearTimeout(this.timeTimer);
    this.frameObserver?.disconnect();
    this.observedRoot = null;
  }

  @Watch('calendarApi')
  @Watch('branchId')
  @Watch('departmentId')
  @Watch('services')
  @Watch('brandId')
  @Watch('today')
  onTargetChange() {
    this.closeTimes();
    // Busy at once, not on the queued load: a host that enables its field in this same pass must not paint it enabled first.
    if (isCompleteTarget(this.target())) this.status = 'loading';
    this.queueSync(true);
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

  @Watch('status')
  onStatusChange() {
    this.emitStatus();
  }

  @Watch('language')
  onLanguageChange() {
    this.emitStatus();
    this.emitPicker();
  }

  @Watch('valueFormat')
  @Watch('utcOffset')
  @Watch('hourCycle')
  @Watch('labelFormat')
  @Watch('dayTitleFormat')
  onFormatChange() {
    this.emitPicker();
  }

  @Watch('value')
  onValueChange(next: string) {
    if ((next || '') === this.emitted) return;

    const local = this.read(next);

    this.emitted = next || '';
    this.valueTouched = true;
    this.queueSync(false);
    if (local === this.current) return;

    this.current = local;
    if (!local) {
      this.lastEmit = '\n';
      this.selectedRaw = '';
      this.selectedDate = '';
      this.closeTimes();
      return;
    }
    this.applyPreset();
  }

  @Method()
  async refresh() {
    this.load();
  }

  @Method()
  async clear() {
    this.reset('');
  }

  @State() blazorRef?: DotNetObjectReference;

  @Prop() changeCallback?: BlazorInvokableFunction<(value: string, label: string, complete: boolean) => void>;

  // A host that attaches after load still hears the current value's label once.
  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
    await this.reportCurrent();
  }

  @Watch('changeCallback')
  onChangeCallbackChange() {
    if (typeof this.changeCallback === 'function') this.reportCurrent();
  }

  private async reportCurrent() {
    const label = this.slotLabel(this.current);
    if (label) await smartInvokable.bind(this)(this.changeCallback, formatPickerValue(this.current, this.valueFormat, this.offset()), label, false);
  }

  @Method()
  async getValueLabel() {
    return this.slotLabel(this.current);
  }

  @Method()
  async setFocus() {
    if (this.view === 'times') await this.slotsEl?.setFocus();
    else await this.calendarEl?.setFocus();
  }

  reset(newValue?: unknown) {
    this.defaultValue = (newValue as string) ?? '';
    this.current = this.read(this.defaultValue);
    this.selectedRaw = '';
    this.selectedDate = '';
    this.closeTimes();
    this.applyPreset();
  }

  getValue() {
    return this.current;
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

  private read(value: string | undefined): string {
    return readPickerValue(value || '', this.valueFormat, this.offset());
  }

  // With no utc-offset set, a value that arrived with an offset keeps it.
  private offset(): string | undefined {
    return this.utcOffset || valueOffset(this.value) || undefined;
  }

  private ids(): Identity {
    return Object.fromEntries(IDENTITY.map(key => [key, this[key] ? String(this[key]) : ''])) as Identity;
  }

  // A host sets the ids, the endpoint and the value one after another; one pass answers them all.
  private queueSync(load: boolean) {
    this.loadWanted ||= load;
    if (this.syncQueued) return;

    this.syncQueued = true;
    queueMicrotask(() => {
      this.syncQueued = false;
      this.checkIdentity();
      if (!this.loadWanted) return;

      this.loadWanted = false;
      this.load();
    });
  }

  // A value belongs to the branch it came with: an id that was set and then changes drops it, unless the host set the value in the same pass.
  private checkIdentity() {
    const next = this.ids();
    const moved = IDENTITY.some(key => this.identity[key] && next[key] !== this.identity[key]);
    const together = this.valueTouched;

    this.identity = next;
    this.valueTouched = false;
    if (!moved || together || !this.current) return;

    this.current = '';
    this.defaultValue = '';
    this.selectedRaw = '';
    this.selectedDate = '';
    this.closeTimes();
    this.emitPicker();
  }

  private target() {
    return {
      url: this.calendarApi,
      branchId: this.branchId,
      departmentId: this.departmentId,
      services: this.services,
      brandId: this.brandId,
    };
  }

  private async load() {
    const target = this.target();

    if (!isCompleteTarget(target)) {
      this.loader.cancel();
      this.status = 'idle';
      this.availability = null;
      this.selectedDate = '';
      this.closeTimes();
      return;
    }

    this.status = 'loading';
    this.selectedRaw = '';
    this.selectedDate = '';
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

    if (!this.firstOpenDay()) {
      this.status = 'empty';
      return;
    }

    this.applyPreset();
    this.status = 'ready';
  }

  // The current value is shown and kept whether or not the branch has it as a time; a time it matches is chosen.
  private applyPreset() {
    if (this.availability) {
      const current = this.current;
      const preset = current ? this.availability.days.find(day => current.startsWith(day.date) && this.isOpen(day.date)) : undefined;
      const opening = preset ?? this.firstOpenDay();

      this.selectedDate = preset?.date ?? '';
      this.selectedRaw = preset?.times.find(time => slotValue(time.raw) === current)?.raw ?? '';
      if (opening) this.month = monthOf(opening.date);
      if (this.selectedRaw) this.view = 'times';
    }
    this.emitPicker();
  }

  private emitPicker() {
    const value = formatPickerValue(this.current, this.valueFormat, this.offset());
    const label = this.slotLabel(this.current);
    const complete = this.completing;
    const key = `${value}\n${label}`;

    this.completing = false;
    if (key === this.lastEmit && !complete) return;

    this.lastEmit = key;
    this.emitted = value;
    this.value = value;
    this.pickerChange.emit({ value, label, complete });
    smartInvokable.bind(this)(this.changeCallback, value, label, complete);
  }

  private emitStatus() {
    const strings = this.strings;
    // Empty is said inside the picker, over its greyed month; a host's hint line only hears loading and failure.
    const text = this.status === 'loading' || this.status === 'error' ? strings[this.status] : '';
    if (text === this.statusText) return;

    this.statusText = text;
    this.pickerStatus.emit({ text, busy: this.status === 'loading' });
  }

  private slotLabel(local: string): string {
    if (!local) return '';

    const fallback = this.strings.labelFormat;

    return this.formatted(local, this.labelFormat, this.hourCycle === 'h12' ? fallback.replace(/H{1,2}:mm/, 'h:mm a') : fallback);
  }

  private formatted(local: string, given: string | Partial<Record<LanguageKeys, string>> | undefined, fallback: string): string {
    const locale = this.locale;
    const strings = this.strings;
    const pattern = typeof given === 'string' ? given : given?.[locale.language];
    const names = { ...locale.strings, monthsShort: strings.monthsShort, am: strings.am, pm: strings.pm };

    return formatSlotLabel(local, pattern || fallback, fallback, names, text => formatDigits(text, locale.numerals));
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
      this.current = raw ? slotValue(raw) : '';
      this.emitPicker();
      if (kept) this.commit(raw);
    }
    this.openTimes();
  };

  private stop = (event: Event) => event.stopPropagation();

  private onMonthChange = (event: CustomEvent<{ month: string }>) => {
    event.stopPropagation();
    this.month = event.detail.month;
  };

  private onTimeChange = (event: CustomEvent<{ value: string }>) => {
    event.stopPropagation();

    const time = this.day?.times.find(item => item.time === event.detail.value);
    if (!time) return;

    // The time already shown (kept from the previous day) is confirmed by clicking it.
    if (time.raw === this.selectedRaw) {
      this.completing = true;
      this.emitPicker();
      return;
    }

    const before = this.timeText();

    this.completing = true;
    this.selectedRaw = time.raw;
    this.current = slotValue(time.raw);
    this.emitPicker();
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
    // Focus moving between chips inside the slots' shadow root never reaches this tree as focusin, so a key that moved it reveals it here.
    if (event.key !== 'Escape') {
      requestAnimationFrame(() => {
        const chip = this.slotsEl?.shadowRoot?.activeElement as HTMLElement | null;
        if (chip?.classList?.contains('ts-chip')) this.reveal(chip, false);
      });
      return;
    }
    if (this.view !== 'times') return;

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
    return date ? this.formatted(date, this.dayTitleFormat, this.strings.dayTitle) : '';
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
      '--_f-top': `${header.offsetTop + header.offsetHeight + (parseFloat(panel.rowGap) || 0)}px`,
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

  // "3 times · 9:00 AM – 2:00 PM": the day's count and its first and last time, for the tooltip, the description and the times header.
  private daySummary(day: BookingDay | undefined): string {
    if (!day?.times.length) return '';

    const strings = this.strings;
    const numerals = this.locale.numerals;
    const count = day.times.length;

    return fill(strings.daySummary, {
      count: fill(count === 1 ? strings.timesCountOne : strings.timesCount, { count: formatDigits(count, numerals) }),
      from: formatTime(day.times[0].time, this.hourCycle, numerals, strings),
      to: formatTime(day.times[count - 1].time, this.hourCycle, numerals, strings),
    });
  }

  private get dayMeta(): CalendarDayMetaMap | undefined {
    if (!this.availability) return undefined;

    return Object.fromEntries(
      this.availability.days.map(day => {
        const summary = this.daySummary(day);

        return [
          day.date,
          {
            badge: this.slotCounts ? String(day.times.length) : undefined,
            tone: day.times.length <= this.fewSlots ? 'warning' : 'neutral',
            tooltip: this.dayTooltips ? summary : undefined,
            description: summary,
          },
        ];
      }),
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
    const empty = this.status === 'empty' && this.showEmptyState;
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
          <div class="bc-collapse" data-open={this.showLabel ? '' : undefined} aria-hidden={this.showLabel ? undefined : 'true'}>
            <div class="bc-collapse-body">
              <div class="bc-label" part="label" id={this.labelId}>
                {label}
                <span class="bc-required" part="required" aria-hidden="true" data-hidden={isRequired ? undefined : ''}>
                  *
                </span>
              </div>
            </div>
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
              showToday={this.showToday}
              busy={this.status === 'loading'}
              disabled={disabled}
              aria-hidden={times ? 'true' : undefined}
              inert={times ? true : undefined}
              onDateChange={this.onDateChange}
              onMonthChange={this.onMonthChange}
              onPickerChange={this.stop}
            />

            <div class="bc-empty" part="empty" aria-hidden="true" data-shown={empty ? '' : undefined}>
              <svg class="bc-empty-art" viewBox="0 0 48 48" focusable="false">
                <rect x="7" y="10" width="34" height="31" rx="5" fill="none" stroke="currentColor" stroke-width="2" />
                <path d="M7 19h34M16 6v8M32 6v8" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                <circle cx="24" cy="30" r="6" fill="none" stroke="currentColor" stroke-width="1.6" opacity="0.55" />
                <path d="M24 27v3l2 1.5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" opacity="0.55" />
                <path d="M11 38 37 13" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
              </svg>
              <span class="bc-empty-text">{strings.emptyTitle}</span>
            </div>

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
                    <span class="bc-title-line">
                      <span class="bc-title-day">{this.dayTitle(this.selectedDate)}</span>
                      {this.renderTimeSlot()}
                    </span>
                    <span class="bc-title-summary" part="times-summary">
                      {this.daySummary(day)}
                    </span>
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
          <span class="bc-sr" aria-live="polite">
            {empty ? strings.emptyTitle : ''}
          </span>

          <div class="bc-collapse" data-open={this.showStatus ? '' : undefined} aria-hidden={this.showStatus ? undefined : 'true'} inert={this.showStatus ? undefined : true}>
            <div class="bc-collapse-body">
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
          </div>
        </div>
      </Host>
    );
  }
}
