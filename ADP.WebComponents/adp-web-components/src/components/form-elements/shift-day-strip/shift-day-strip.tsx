import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

import '~lib/middleware';
import { today as clockToday } from '~lib/clock';
import { Availability, blockKind, blockReason, buildAvailability } from '~lib/calendar-availability';
import { dateParts, localDate, parseDate, weekdayOf } from '~lib/calendar-date';

import { CalendarNumerals, calendarLocale, dateLabel, formatDigits } from '../shift-calendar/calendar-locale';
import type { CalendarAppearance, CalendarColorScheme, CalendarDayMetaMap, CalendarDayTone, CalendarSize } from '../shift-calendar/shift-calendar';
import { dayStripStrings, shortMonth } from './day-strip-locale';
import { moveInStrip, stripRange } from './day-strip-range';
import { pageDelta, revealDelta, Span, visibleCount } from './day-strip-scroll';

export interface DayStripDateChangeDetail {
  value: string;
}

const TONES: CalendarDayTone[] = ['neutral', 'info', 'success', 'warning', 'danger'];

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

@Component({
  shadow: true,
  tag: 'shift-day-strip',
  styleUrl: 'shift-day-strip.css',
})
export class ShiftDayStrip {
  @Element() el!: HTMLElement;

  @Prop({ mutable: true, reflect: true }) value: string = '';
  @Prop() min?: string;
  @Prop() max?: string;

  @Prop() disabledDates?: string | string[];
  @Prop() disabledWeekdays?: string | number[];
  @Prop() enabledDates?: string | string[] | null;
  @Prop() isDateDisabled?: (date: string) => boolean;
  @Prop() dayMeta?: CalendarDayMetaMap | string;

  @Prop() today?: string;
  @Prop() highlightToday: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() numerals?: CalendarNumerals;
  @Prop() label?: string;
  @Prop() caption?: string;
  @Prop({ reflect: true }) disabled: boolean = false;
  @Prop({ reflect: true }) busy: boolean = false;

  @Prop({ reflect: true }) appearance?: CalendarAppearance;
  @Prop({ reflect: true }) colorScheme: CalendarColorScheme = 'light';
  @Prop({ reflect: true }) size: CalendarSize = 'md';

  @Event({ bubbles: true, composed: true }) dateChange!: EventEmitter<DayStripDateChangeDetail>;

  @State() focusDate: string = '';
  @State() atStart: boolean = true;
  @State() atEnd: boolean = false;
  @State() slottedCaption: boolean = false;
  @State() settled: boolean = false;

  private viewport?: HTMLElement;
  private prevButton?: HTMLButtonElement;
  private nextButton?: HTMLButtonElement;
  private resizeObserver?: ResizeObserver;
  private edgeFrame?: number;
  private loaded = false;
  private selecting = false;
  private settling = false;
  private pendingFocus = false;
  private pendingReveal: ScrollBehavior | null = 'instant';
  private firstTrackKey = '';
  private captionId = `shift-day-strip-caption-${Math.random().toString(36).slice(2, 10)}`;

  componentWillLoad() {
    this.focusDate = this.focusFor();
    this.firstTrackKey = this.trackKey(this.days());
    this.slottedCaption = !!this.el.querySelector('[slot="caption"]');
  }

  componentDidLoad() {
    this.loaded = true;

    if (typeof ResizeObserver !== 'undefined' && this.viewport) {
      this.resizeObserver = new ResizeObserver(() => {
        this.syncEdges();
        this.flushReveal();
      });
      this.resizeObserver.observe(this.viewport);
    }
  }

  componentDidRender() {
    this.flushReveal();
    if (!this.settled) this.syncEdges(true);
    this.scheduleEdges();

    if (!this.pendingFocus) return;
    this.pendingFocus = false;
    this.cardFor(this.focusDate)?.focus({ preventScroll: true });
  }

  disconnectedCallback() {
    this.resizeObserver?.disconnect();
    this.resizeObserver = undefined;
    if (this.edgeFrame) cancelAnimationFrame(this.edgeFrame);
  }

  @Watch('value')
  onValueChange(next: string) {
    if (this.selecting) return;

    const date = parseDate(next);
    if (date && this.days().includes(date)) this.focusDate = date;
    this.pendingReveal = this.loaded ? 'smooth' : 'instant';
  }

  @Watch('min')
  @Watch('max')
  @Watch('enabledDates')
  @Watch('today')
  onRangeChange() {
    const days = this.days();

    if (!days.includes(this.focusDate) || parseDate(this.value) !== this.focusDate) this.focusDate = this.focusFor();
    this.pendingReveal = 'instant';
  }

  @Watch('language')
  @Watch('appearance')
  @Watch('size')
  onGeometryChange() {
    this.pendingReveal = 'instant';
  }

  @Method()
  async setFocus() {
    this.cardFor(this.focusDate)?.focus({ preventScroll: true });
    this.reveal(this.focusDate, this.smooth());
  }

  private get todayDate(): string {
    return parseDate(this.today) ? clockToday(this.today) : localDate();
  }

  private get rtl(): boolean {
    return calendarLocale(this.language).direction === 'rtl';
  }

  private get digits(): CalendarNumerals {
    return this.numerals === 'latn' || this.numerals === 'arab' ? this.numerals : calendarLocale(this.language).numerals;
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

  private days(rules: Availability = this.availability()): string[] {
    return stripRange(rules, this.todayDate).days;
  }

  private trackKey(days: string[]): string {
    return days.length ? `${days[0]}:${days[days.length - 1]}` : 'empty';
  }

  private focusFor(): string {
    const rules = this.availability();
    const days = this.days(rules);
    const selected = parseDate(this.value);
    const today = this.todayDate;

    if (selected && days.includes(selected)) return selected;
    if (days.includes(today)) return today;
    return days.find(date => !blockReason(date, rules)) ?? days[0] ?? '';
  }

  private smooth(): ScrollBehavior {
    return prefersReducedMotion() ? 'instant' : 'smooth';
  }

  private cardFor(date: string): HTMLElement | null {
    return date ? (this.viewport?.querySelector<HTMLElement>(`.ds-day[data-date="${date}"]`) ?? null) : null;
  }

  private inset(): number {
    if (!this.viewport || typeof getComputedStyle !== 'function') return 0;
    return parseFloat(getComputedStyle(this.viewport).scrollPaddingInlineStart) || 0;
  }

  private spans(): { cards: Span[]; width: number; dates: string[] } {
    const viewport = this.viewport;
    const box = viewport.getBoundingClientRect();
    const cards = Array.from(viewport.querySelectorAll<HTMLElement>('.ds-day'));
    const rtl = this.rtl;

    return {
      width: box.width,
      dates: cards.map(card => card.dataset.date),
      cards: cards.map(card => {
        const rect = card.getBoundingClientRect();
        return rtl ? { start: box.right - rect.right, end: box.right - rect.left } : { start: rect.left - box.left, end: rect.right - box.left };
      }),
    };
  }

  private scrollBy(delta: number, behavior: ScrollBehavior) {
    if (!delta || !this.viewport) return;
    this.viewport.scrollTo({ left: this.viewport.scrollLeft + (this.rtl ? -delta : delta), behavior });
  }

  // Scrolls the strip only: scrollIntoView would also move the page and any overflow: hidden ancestor.
  private reveal(date: string, behavior: ScrollBehavior) {
    if (!this.viewport || !this.viewport.clientWidth) return;

    const { cards, width, dates } = this.spans();
    const index = dates.indexOf(date);
    if (index < 0) return;

    this.scrollBy(revealDelta(cards[index], cards, width, this.inset()), behavior);
  }

  private flushReveal() {
    if (!this.pendingReveal || !this.viewport?.clientWidth) return;

    const behavior = this.pendingReveal;
    this.pendingReveal = null;
    this.reveal(this.focusDate, behavior === 'smooth' ? this.smooth() : 'instant');
  }

  private page(step: 1 | -1) {
    if (!this.viewport) return;

    const { cards, width } = this.spans();
    this.scrollBy(pageDelta(cards, width, this.inset(), step), this.smooth());
  }

  private pageSize(): number {
    if (!this.viewport?.clientWidth) return 1;

    const { cards, width } = this.spans();
    return visibleCount(width, this.inset(), cards);
  }

  private onScroll = () => this.scheduleEdges();

  // Edge state is read after the frame, never during a render: setting @State there would re-render mid-cycle.
  private scheduleEdges() {
    if (this.edgeFrame || typeof requestAnimationFrame !== 'function') return;
    this.edgeFrame = requestAnimationFrame(() => {
      this.edgeFrame = undefined;
      this.syncEdges();
    });
  }

  // The fades grow with the distance scrolled, so they arrive and leave with the movement instead of switching.
  private syncEdges(inRender = false) {
    const viewport = this.viewport;
    if (!viewport) return;

    const range = Math.max(0, (viewport.scrollWidth || 0) - (viewport.clientWidth || 0));
    const scrolled = viewport.scrollLeft || 0;
    const fromLeft = this.rtl ? range + scrolled : scrolled;
    const fromRight = range - fromLeft;
    const fade = this.inset();

    viewport.style.setProperty('--_fade-left', `${Math.max(0, Math.min(fade, fromLeft))}px`);
    viewport.style.setProperty('--_fade-right', `${Math.max(0, Math.min(fade, fromRight))}px`);

    const atStart = (this.rtl ? fromRight : fromLeft) <= 1;
    const atEnd = (this.rtl ? fromLeft : fromRight) <= 1;

    if (!this.settled) {
      // Before the first paint the buttons are set directly, so the frame that shows them is already right; the state follows a frame later.
      const mark = (button: HTMLButtonElement | undefined, off: boolean) => (off ? button?.setAttribute('disabled', '') : button?.removeAttribute('disabled'));
      mark(this.prevButton, this.disabled || atStart);
      mark(this.nextButton, this.disabled || atEnd);
    }
    if (!inRender) {
      if (atStart !== this.atStart) this.atStart = atStart;
      if (atEnd !== this.atEnd) this.atEnd = atEnd;
    }

    // Transitions wait for the first measured paint, so first render never animates.
    if (!this.settled && !this.settling && viewport.clientWidth && typeof requestAnimationFrame === 'function') {
      this.settling = true;
      requestAnimationFrame(() => requestAnimationFrame(() => (this.settled = true)));
    }
  }

  private select(date: string, rules: Availability) {
    if (this.disabled || this.busy || blockReason(date, rules)) return;

    this.selecting = true;
    this.value = date;
    this.selecting = false;
    this.dateChange.emit({ value: date });
  }

  private onKeyDown(event: KeyboardEvent, days: string[], rules: Availability) {
    if (this.disabled || event.altKey || event.ctrlKey || event.metaKey) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.select(this.focusDate, rules);
      return;
    }

    const page = event.key === 'PageUp' || event.key === 'PageDown' ? this.pageSize() : 1;
    const target = moveInStrip(days.indexOf(this.focusDate), event.key, { rtl: this.rtl, count: days.length, page });
    if (target === null) return;

    event.preventDefault();
    this.focusDate = days[target];
    this.pendingFocus = true;
    this.pendingReveal = 'smooth';
  }

  private onCardClick(date: string, rules: Availability) {
    if (this.disabled) return;

    this.focusDate = date;
    this.cardFor(date)?.focus({ preventScroll: true });
    this.reveal(date, this.smooth());
    this.select(date, rules);
  }

  private onFocusIn = (event: FocusEvent) => {
    const date = (event.target as HTMLElement)?.dataset?.date;
    if (date) this.reveal(date, this.smooth());
  };

  private onSlotChange = (event: Event) => {
    this.slottedCaption = (event.target as HTMLSlotElement).assignedNodes({ flatten: true }).some(node => node.nodeType === 1 || !!node.textContent?.trim());
  };

  private renderDay(date: string, index: number, rules: Availability, meta: CalendarDayMetaMap, selected: string | null) {
    const locale = calendarLocale(this.language);
    const strings = dayStripStrings(locale.language);
    const [, month, day] = dateParts(date);
    const reason = blockReason(date, rules);
    const kind = blockKind(reason);
    const isSelected = date === selected;
    const isToday = date === this.todayDate;
    const marked = isToday && this.highlightToday;
    const blocked = !!reason || this.disabled;
    const dayMeta = meta[date];
    const badge = dayMeta?.badge ? String(dayMeta.badge) : '';
    const tone = dayMeta?.tone && TONES.includes(dayMeta.tone) ? dayMeta.tone : 'neutral';
    const description = dayMeta?.description ? `, ${dayMeta.description}` : '';
    const monthLabel = index === 0 || day === 1 ? shortMonth(month, strings, this.digits) : '';

    const part = ['day', isSelected && 'day-selected', marked && 'day-today', blocked && 'day-disabled', kind === 'closed' && 'day-closed'].filter(Boolean).join(' ');

    return (
      <div
        key={date}
        role="option"
        class={{ 'ds-day': true, 'has-badge': !!badge, 'has-month': !!monthLabel }}
        part={part}
        data-date={date}
        data-kind={kind || 'open'}
        data-reason={reason || undefined}
        data-selected={isSelected ? '' : undefined}
        data-today={marked ? '' : undefined}
        tabindex={this.disabled ? undefined : date === this.focusDate ? '0' : '-1'}
        aria-selected={isSelected ? 'true' : 'false'}
        aria-disabled={blocked ? 'true' : undefined}
        aria-current={isToday ? 'date' : undefined}
        onMouseDown={(event: MouseEvent) => event.preventDefault()}
        onClick={() => this.onCardClick(date, rules)}
      >
        <span class="ds-weekday" part="weekday" aria-hidden="true">
          {locale.strings.weekdaysShort[weekdayOf(date)]}
        </span>
        <span class="ds-number" part="day-number" aria-hidden="true">
          {formatDigits(day, this.digits)}
        </span>
        <span class="ds-foot" aria-hidden="true">
          <span class="ds-month" part="month">
            {monthLabel}
          </span>
          <span class="ds-badge" part="badge" data-tone={tone} data-empty={badge ? undefined : ''}>
            {formatDigits(badge, this.digits)}
          </span>
        </span>
        <span class="ds-sr">{dateLabel(date, locale, this.digits) + description}</span>
      </div>
    );
  }

  render() {
    const locale = calendarLocale(this.language);
    const strings = dayStripStrings(locale.language);
    const rules = this.availability();
    const days = this.days(rules);
    const meta = parseDayMeta(this.dayMeta);
    const selected = parseDate(this.value);
    const trackKey = this.trackKey(days);
    const captioned = !!this.caption || this.slottedCaption;

    return (
      <Host>
        <div
          class="ds-root"
          part="root"
          dir={locale.direction}
          lang={locale.language === 'ku' ? 'ckb' : locale.language}
          data-busy={this.busy ? '' : undefined}
          data-settled={this.settled ? '' : undefined}
        >
          <div class="ds-caption-wrap" data-open={captioned ? '' : undefined} inert={captioned ? undefined : true}>
            <div class="ds-caption-clip">
              <div class="ds-caption" part="caption" id={this.captionId}>
                <slot name="caption" onSlotchange={this.onSlotChange}>
                  {this.caption}
                </slot>
              </div>
            </div>
          </div>

          <div class="ds-row">
            <button
              type="button"
              class="ds-nav"
              part="prev"
              ref={el => (this.prevButton = el)}
              aria-label={strings.earlierDays}
              disabled={this.disabled || this.atStart}
              onClick={() => this.page(-1)}
            >
              <ChevronLeftIcon class="ds-nav-icon" />
            </button>

            <div class="ds-viewport" part="viewport" ref={el => (this.viewport = el)} onScroll={this.onScroll}>
              <div
                key={trackKey}
                class="ds-track"
                part="list"
                role="listbox"
                aria-orientation="horizontal"
                aria-label={this.label || strings.days}
                aria-describedby={captioned ? this.captionId : undefined}
                aria-disabled={this.disabled ? 'true' : undefined}
                aria-busy={this.busy ? 'true' : undefined}
                data-phase={trackKey === this.firstTrackKey ? 'rest' : 'enter'}
                onKeyDown={(event: KeyboardEvent) => this.onKeyDown(event, days, rules)}
                onFocusin={this.onFocusIn}
              >
                {days.map((date, index) => this.renderDay(date, index, rules, meta, selected))}
              </div>
            </div>

            <button
              type="button"
              class="ds-nav"
              part="next"
              ref={el => (this.nextButton = el)}
              aria-label={strings.laterDays}
              disabled={this.disabled || this.atEnd}
              onClick={() => this.page(1)}
            >
              <ChevronRightIcon class="ds-nav-icon" />
            </button>
          </div>
        </div>
      </Host>
    );
  }
}
