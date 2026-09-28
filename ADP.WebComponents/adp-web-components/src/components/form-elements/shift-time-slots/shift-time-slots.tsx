import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';

import '~lib/middleware';

import { calendarLocale, CalendarNumerals } from '../shift-calendar/calendar-locale';
import type { CalendarAppearance, CalendarColorScheme, CalendarSize } from '../shift-calendar/shift-calendar';

import { bookingStrings, fill, formatTime, HourCycle, moveInTimes, parseTimes } from './time-slots';

export interface TimeChangeDetail {
  value: string;
}

type LayerKind = 'loading' | 'empty' | 'times';

interface Layer {
  key: string;
  kind: LayerKind;
  times: string[];
}

const prefersReducedMotion = () => typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

@Component({
  shadow: true,
  tag: 'shift-time-slots',
  styleUrl: 'shift-time-slots.css',
})
export class ShiftTimeSlots {
  @Element() el!: HTMLElement;

  @Prop() times?: string[] | string = [];
  @Prop({ mutable: true, reflect: true }) value: string = '';
  @Prop() disabledTimes?: string[] | string;
  @Prop() hourCycle: HourCycle = 'h23';
  @Prop() language: LanguageKeys = 'en';
  @Prop() numerals?: CalendarNumerals;
  @Prop() label?: string;
  @Prop() emptyText?: string;
  @Prop({ reflect: true }) loading: boolean = false;
  @Prop() skeletonCount: number = 8;
  @Prop({ reflect: true }) disabled: boolean = false;

  @Prop({ reflect: true }) appearance?: CalendarAppearance;
  @Prop({ reflect: true }) colorScheme: CalendarColorScheme = 'light';
  @Prop({ reflect: true }) size: CalendarSize = 'md';

  @Event({ bubbles: true, composed: true }) timeChange!: EventEmitter<TimeChangeDetail>;

  @State() shown: Layer = { key: 'empty', kind: 'empty', times: [] };
  @State() leaving: Layer | null = null;
  @State() focusTime: string = '';

  private loaded = false;
  private pendingFocus = false;
  private settleTimer?: ReturnType<typeof setTimeout>;
  private observer?: ResizeObserver;
  private observed: HTMLElement | null = null;

  componentWillLoad() {
    this.shown = this.layerFor();
  }

  componentDidLoad() {
    this.loaded = true;
  }

  componentDidRender() {
    this.watchHeight();

    if (!this.pendingFocus) return;
    this.pendingFocus = false;
    this.focusChip();
  }

  disconnectedCallback() {
    clearTimeout(this.settleTimer);
    this.observer?.disconnect();
    this.observed = null;
  }

  @Watch('times')
  @Watch('loading')
  onContentChange() {
    const next = this.layerFor();
    if (next.key === this.shown.key) return;

    clearTimeout(this.settleTimer);

    const animate = this.loaded && !prefersReducedMotion();

    this.leaving = animate ? this.shown : null;
    this.shown = next;
    if (!next.times.includes(this.focusTime)) this.focusTime = '';
    if (animate) this.settleTimer = setTimeout(() => (this.leaving = null), 1500);
  }

  @Method()
  async setFocus() {
    this.focusChip();
  }

  private get locale() {
    return calendarLocale(this.language);
  }

  private get strings() {
    return bookingStrings(this.locale.language);
  }

  private get digits(): CalendarNumerals {
    return this.numerals === 'latn' || this.numerals === 'arab' ? this.numerals : this.locale.numerals;
  }

  private layerFor(): Layer {
    if (this.loading) return { key: 'loading', kind: 'loading', times: [] };

    const times = parseTimes(this.times);

    return times.length ? { key: `times:${times.join(',')}`, kind: 'times', times } : { key: 'empty', kind: 'empty', times };
  }

  private get blocked(): Set<string> {
    return new Set(parseTimes(this.disabledTimes));
  }

  private tabStop(times: string[]): string {
    const blocked = this.blocked;

    if (times.includes(this.focusTime)) return this.focusTime;
    if (times.includes(this.value)) return this.value;

    return times.find(time => !blocked.has(time)) ?? times[0] ?? '';
  }

  // The layer sits at its content height; the frame's row follows it, so the height change is one transition.
  private watchHeight() {
    const layer = this.el.shadowRoot?.querySelector<HTMLElement>('.ts-layer[data-current]');
    if (!layer || layer === this.observed || typeof ResizeObserver === 'undefined') return;

    this.observer?.disconnect();
    this.observer = new ResizeObserver(() => this.syncHeight());
    this.observer.observe(layer);
    this.observed = layer;
    this.syncHeight();
  }

  private syncHeight() {
    const frame = this.el.shadowRoot?.querySelector<HTMLElement>('.ts-frame');
    // A hidden host reports no layout; keeping the last height lets it reappear settled instead of growing from 0.
    if (!frame || !this.observed || !this.observed.getClientRects().length) return;

    frame.style.setProperty('--_rows', `${this.observed.getBoundingClientRect().height}px`);
    if (!frame.hasAttribute('data-measured')) requestAnimationFrame(() => frame.setAttribute('data-measured', ''));
  }

  private columns(): number {
    const grid = this.el.shadowRoot?.querySelector('.ts-layer[data-current] .ts-grid');
    const template = grid && typeof getComputedStyle === 'function' ? getComputedStyle(grid).gridTemplateColumns : '';

    return template && template !== 'none' ? template.trim().split(/\s+/).length : 1;
  }

  private focusChip() {
    this.el.shadowRoot?.querySelector<HTMLElement>('.ts-layer[data-current] [tabindex="0"]')?.focus({ preventScroll: true });
  }

  private select(time: string) {
    if (this.disabled || this.loading || this.blocked.has(time)) return;

    this.focusTime = time;
    this.value = time;
    this.timeChange.emit({ value: time });
  }

  private onKeyDown(event: KeyboardEvent, times: string[]) {
    if (this.disabled || event.altKey || event.ctrlKey || event.metaKey) return;

    const current = this.tabStop(times);

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.select(current);
      return;
    }

    const blocked = this.blocked;
    const next = moveInTimes(times.indexOf(current), event.key, {
      count: times.length,
      columns: this.columns(),
      rtl: this.locale.direction === 'rtl',
      skip: index => blocked.has(times[index]),
    });
    if (next === null) return;

    event.preventDefault();
    this.focusTime = times[next];
    this.pendingFocus = true;
  }

  private onSettled = (event: AnimationEvent) => {
    if (event.target === event.currentTarget) this.leaving = null;
  };

  private renderTimes(layer: Layer, live: boolean) {
    const blocked = this.blocked;
    const stop = live ? this.tabStop(layer.times) : '';
    const rise = live && !!this.leaving;

    return (
      <div class="ts-grid" part="grid" onKeyDown={live ? (event: KeyboardEvent) => this.onKeyDown(event, layer.times) : undefined}>
        {layer.times.map((time, index) => {
          const selected = time === this.value;
          const off = this.disabled || blocked.has(time);
          const part = ['time', selected && 'time-selected', off && 'time-disabled'].filter(Boolean).join(' ');

          return (
            <button
              key={time}
              type="button"
              role="radio"
              class="ts-chip"
              part={part}
              data-time={time}
              data-selected={selected ? '' : undefined}
              aria-checked={selected ? 'true' : 'false'}
              aria-disabled={off ? 'true' : undefined}
              tabindex={live && !this.disabled && time === stop ? '0' : '-1'}
              style={rise ? { animationDelay: `${Math.min(index, 11) * 24}ms` } : undefined}
              onClick={live ? () => this.select(time) : undefined}
            >
              {formatTime(time, this.hourCycle, this.digits, this.strings)}
            </button>
          );
        })}
      </div>
    );
  }

  private renderLayer(layer: Layer, live: boolean) {
    const count = Math.max(1, Math.min(48, Math.round(Number(this.skeletonCount) || 8)));

    return (
      <div
        key={`layer-${layer.key}`}
        class="ts-layer"
        data-kind={layer.kind}
        data-current={live ? '' : undefined}
        data-phase={live ? (this.leaving ? 'enter' : 'rest') : 'leave'}
        aria-hidden={live ? undefined : 'true'}
        inert={live ? undefined : true}
        onAnimationEnd={live ? undefined : this.onSettled}
      >
        {layer.kind === 'times' && this.renderTimes(layer, live)}
        {layer.kind === 'loading' && (
          <div class="ts-grid" part="grid" aria-hidden="true">
            {Array.from({ length: count }, () => (
              <span class="ts-chip ts-skeleton" part="skeleton" />
            ))}
          </div>
        )}
        {layer.kind === 'empty' && (
          <p class="ts-empty" part="empty">
            {this.emptyText || this.strings.noTimes}
          </p>
        )}
      </div>
    );
  }

  render() {
    const locale = this.locale;
    const strings = this.strings;
    const shown = this.shown;
    const announcement =
      shown.kind === 'loading' ? strings.loadingTimes : shown.kind === 'empty' ? this.emptyText || strings.noTimes : fill(strings.timesCount, { count: shown.times.length });

    return (
      <Host>
        <div
          key={`${this.appearance || 'vanilla'}-${this.size}`}
          class="ts-root"
          part="root"
          dir={locale.direction}
          lang={locale.language === 'ku' ? 'ckb' : locale.language}
          role="radiogroup"
          aria-label={this.label || strings.times}
          aria-busy={this.loading ? 'true' : undefined}
          aria-disabled={this.disabled ? 'true' : undefined}
          data-kind={shown.kind}
        >
          <div class="ts-frame" data-switching={this.leaving ? '' : undefined}>
            {this.leaving && this.renderLayer(this.leaving, false)}
            {this.renderLayer(shown, true)}
          </div>
          <span class="ts-sr" aria-live="polite">
            {announcement}
          </span>
        </div>
      </Host>
    );
  }
}
