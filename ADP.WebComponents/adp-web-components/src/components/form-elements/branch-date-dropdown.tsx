import { Component, Host, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

import { BranchSlotDay } from './branch-slot-dropdown';

export interface BranchDateDropdownCopy {
  date: string;
  time: string;
  slots: string;
  empty: string;
  error: string;
  retry: string;
  back: string;
  noTimes: string;
  closed: string;
}

/** Which half of the sliding track is in view. */
export type BranchDateStep = 'date' | 'time';

/**
 * Panel half of `branch-date-picker`.
 *
 * Two panes on one horizontal track — a month grid, then that day's times —
 * rather than the two stacked sections `branch-slot-dropdown` uses. Only one
 * decision is on screen at a time, which is the point of the date-picker
 * variant: a customer who has met a month calendar before recognises step one
 * immediately, and step two arrives already scoped to the day they picked.
 *
 * Mounted on `document.body` by `shift-portal` and positioned through the
 * `--branch-date-*` variables the picker sets, exactly as the slot panel is.
 */
@Component({
  shadow: true,
  tag: 'branch-date-dropdown',
  styleUrl: 'branch-date-dropdown.css',
})
export class BranchDateDropdown {
  @Prop() name?: string = '';
  @Prop() isOpen: boolean = false;
  @Prop() direction: string = 'ltr';
  @Prop() status: 'idle' | 'loading' | 'ready' | 'empty' | 'error' = 'idle';

  /** Bookable days, already de-duplicated, sorted and rule-marked by the picker. */
  @Prop() days: BranchSlotDay[] = [];

  /** The month on screen, as `YYYY-MM`. */
  @Prop() monthKey: string = '';
  /** `1` when the last month change went forward, `-1` back. Drives the slide. */
  @Prop() monthDirection: number = 1;
  @Prop() canGoPrev: boolean = false;
  @Prop() canGoNext: boolean = false;

  @Prop() step: BranchDateStep = 'date';
  @Prop() activeDate: string = '';
  @Prop() selectedRaw: string = '';
  @Prop() today: string = '';
  /** `0` Sunday … `6` Saturday — resolved from the locale by the picker. */
  @Prop() weekStart: number = 1;
  @Prop() idleText?: string = '';
  @Prop() copy!: BranchDateDropdownCopy;

  @Prop() formatDay!: (date: string, options: Intl.DateTimeFormatOptions) => string;
  @Prop() formatMonth!: (monthKey: string) => string;
  @Prop() formatWeekday!: (weekday: number) => string;
  @Prop() displayTime!: (raw: string) => string;

  @Prop() handleMonth!: (delta: number) => void;
  @Prop() handleDate!: (date: string) => void;
  @Prop() handleTime!: (raw: string) => void;
  @Prop() handleBack!: () => void;
  @Prop() handleRetry!: () => void;
  /** Tapping the sheet scrim. Unused above 600px, where the scrim is hidden. */
  @Prop() handleDismiss?: () => void;
  @Prop() setElementRef?: (el: HTMLElement | null) => void;

  private containerEl: HTMLElement | null = null;

  componentDidLoad() {
    this.setElementRef?.(this.containerEl);
  }

  disconnectedCallback() {
    this.setElementRef?.(null);
  }

  private get dayMap(): Record<string, BranchSlotDay> {
    return this.days.reduce(
      (map, day) => {
        map[day.date] = day;
        return map;
      },
      {} as Record<string, BranchSlotDay>,
    );
  }

  /**
   * The visible month as a flat list of ISO dates, padded with nulls so the
   * first of the month lands under its own weekday column.
   */
  private monthCells(): (string | null)[] {
    if (!this.monthKey) return [];

    const [year, month] = this.monthKey.split('-').map(part => parseInt(part, 10));
    // Day 0 of the next month is the last day of this one.
    const length = new Date(year, month, 0).getDate();
    // Midday, so a negative-offset zone cannot roll the first back into last month.
    const lead = (new Date(year, month - 1, 1, 12).getDay() - this.weekStart + 7) % 7;

    const cells: (string | null)[] = new Array(lead).fill(null);

    for (let day = 1; day <= length; day++) cells.push(`${this.monthKey}-${String(day).padStart(2, '0')}`);

    // Always six rows. Some months need five and some need six, and letting the
    // grid choose makes the panel change height as you page through months.
    while (cells.length < 42) cells.push(null);

    return cells;
  }

  private renderMessage(text: string, isError = false) {
    const identifier = cn(`${this.name}-date-empty-container branch-date-empty-container`, {
      'branch-date-empty-container-error': isError,
    });

    return (
      <div part={identifier} class={identifier}>
        <div>
          {text}
          {isError && (
            <div>
              <button type="button" class="branch-date-retry" part={`${this.name}-date-retry branch-date-retry`} onClick={() => this.handleRetry()}>
                {this.copy.retry}
              </button>
            </div>
          )}
        </div>
      </div>
    );
  }

  private renderSkeleton() {
    return (
      <div class="branch-date-pane">
        <div class="branch-date-pane-inner">
          <div class="branch-date-monthbar">
            <div class="branch-date-skeleton branch-date-skeleton-title"></div>
          </div>
          <div class="branch-date-grid">
            {Array.from({ length: 35 }).map((_, index) => (
              <div key={index} class="branch-date-skeleton branch-date-skeleton-cell"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  private renderCalendarPane() {
    const map = this.dayMap;
    const enterClass = this.monthDirection < 0 ? 'branch-date-enter-back' : 'branch-date-enter-forward';

    return (
      <div class="branch-date-pane">
        <div class="branch-date-pane-inner">
          <div class="branch-date-monthbar">
            <button type="button" class="branch-date-nav" part={`${this.name}-date-prev branch-date-nav`} disabled={!this.canGoPrev} onClick={() => this.handleMonth(-1)}>
              <ChevronLeftIcon class="branch-date-nav-icon" />
            </button>

            {/* Re-keyed on the month so the label and the grid replay the slide-in. */}
            <span key={this.monthKey} class={cn('branch-date-month', enterClass)}>
              {this.formatMonth(this.monthKey)}
            </span>

            <button type="button" class="branch-date-nav" part={`${this.name}-date-next branch-date-nav`} disabled={!this.canGoNext} onClick={() => this.handleMonth(1)}>
              <ChevronRightIcon class="branch-date-nav-icon" />
            </button>
          </div>

          <div class="branch-date-dows" aria-hidden="true">
            {Array.from({ length: 7 }).map((_, index) => (
              <span key={index} class="branch-date-dow">
                {this.formatWeekday((this.weekStart + index) % 7)}
              </span>
            ))}
          </div>

          <div key={this.monthKey} class={cn('branch-date-grid', enterClass)} role="grid" aria-label={this.copy.date}>
            {this.monthCells().map((date, index) => {
              if (!date) return <span key={`pad-${index}`} class="branch-date-cell-pad"></span>;

              const entry = map[date];
              // Three outcomes, two of them dead ends: bookable, blocked by a rule,
              // or never offered at all — outside the window, or a closed day.
              const isBlocked = !!entry?.disabled;
              const isBookable = !!entry && !isBlocked;
              const isSelected = date === this.activeDate;

              const identifier = cn(`${this.name}-date-cell branch-date-cell`, {
                'branch-date-cell-open': isBookable,
                'branch-date-cell-blocked': isBlocked,
                'branch-date-cell-off': !entry,
                'branch-date-cell-today': date === this.today,
                'branch-date-cell-selected': isSelected && isBookable,
              });

              return (
                <button
                  key={date}
                  type="button"
                  part={identifier}
                  class={identifier}
                  disabled={!isBookable}
                  aria-disabled={isBookable ? 'false' : 'true'}
                  aria-pressed={isSelected && isBookable ? 'true' : 'false'}
                  title={isBlocked ? this.copy.closed : undefined}
                  onClick={() => isBookable && this.handleDate(date)}
                >
                  {this.formatDay(date, { day: 'numeric' })}
                </button>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  private renderTimePane() {
    const active = this.days.find(day => day.date === this.activeDate && !day.disabled);
    const times = active?.times ?? [];

    return (
      <div class="branch-date-pane">
        <div class="branch-date-pane-inner">
          <div class="branch-date-monthbar">
            <button type="button" class="branch-date-back" part={`${this.name}-date-back branch-date-back`} onClick={() => this.handleBack()}>
              <ChevronLeftIcon class="branch-date-nav-icon" />
              <span>{this.copy.back}</span>
            </button>

            <span class="branch-date-count">
              {times.length} {this.copy.slots}
            </span>
          </div>

          <div class="branch-date-chosen">{this.activeDate ? this.formatDay(this.activeDate, { weekday: 'long', day: 'numeric', month: 'long' }) : ''}</div>

          {!times.length && <div class="branch-date-notimes">{this.copy.noTimes}</div>}

          <div class="branch-date-times" role="radiogroup" aria-label={this.copy.time}>
            {times.map((raw, index) => {
              const isSelected = raw === this.selectedRaw;
              const identifier = cn(`${this.name}-date-time branch-date-time`, { 'branch-date-time-selected': isSelected });

              return (
                <button
                  key={raw}
                  type="button"
                  role="radio"
                  part={identifier}
                  class={identifier}
                  aria-checked={isSelected ? 'true' : 'false'}
                  // Capped, or a branch with a long ladder lands its last chip half
                  // a second after its first.
                  style={{ animationDelay: `${Math.min(index, 11) * 24}ms` }}
                  onClick={() => this.handleTime(raw)}
                >
                  {this.displayTime(raw)}
                </button>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  render() {
    const containerIdentifiers = cn(`${this.name}-date-container branch-date-container`, {
      'branch-date-container-open': this.isOpen,
    });

    const backdropIdentifiers = cn(`${this.name}-date-backdrop branch-date-backdrop`, {
      'branch-date-backdrop-open': this.isOpen,
    });

    return (
      <Host>
        {/* Sheet-mode scrim. CSS keeps it hidden above 600px. */}
        <div part={backdropIdentifiers} class={backdropIdentifiers} onClick={() => this.handleDismiss?.()}></div>

        <div
          dir={this.direction}
          part={containerIdentifiers}
          class={containerIdentifiers}
          // Keeps a scroll gesture inside the panel instead of moving the page
          // behind it — the panel is a child of <body>, not of the field.
          onWheel={event => event.stopPropagation()}
          onTouchMove={event => event.stopPropagation()}
          ref={el => (this.containerEl = el as HTMLElement | null)}
        >
          {this.status === 'idle' && this.renderMessage(this.idleText || '')}
          {this.status === 'loading' && this.renderSkeleton()}
          {this.status === 'empty' && this.renderMessage(this.copy.empty)}
          {this.status === 'error' && this.renderMessage(this.copy.error, true)}

          {this.status === 'ready' && (
            <div class="branch-date-body">
              <div class={cn('branch-date-track', { 'branch-date-track-time': this.step === 'time' })}>
                {this.renderCalendarPane()}
                {this.renderTimePane()}
              </div>
            </div>
          )}
        </div>
      </Host>
    );
  }
}
