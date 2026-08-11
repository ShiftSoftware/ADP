import { Component, Host, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

/** One day, already de-duplicated and sorted by the picker. */
export interface BranchSlotDay {
  date: string;
  times: string[];
}

export interface BranchSlotDropdownCopy {
  day: string;
  time: string;
  days: string;
  slots: string;
  empty: string;
  error: string;
  retry: string;
}

/**
 * Panel half of `branch-slot-picker`.
 *
 * Mounted on `document.body` by `shift-portal` and positioned through the
 * `--branch-slot-*` variables the picker sets — the same split `shift-select` /
 * `shift-select-dropdown` use, so the panel escapes any `overflow: hidden` or
 * stacking context the form puts around the field.
 */
@Component({
  shadow: true,
  tag: 'branch-slot-dropdown',
  styleUrl: 'branch-slot-dropdown.css',
})
export class BranchSlotDropdown {
  @Prop() name?: string = '';
  @Prop() isOpen: boolean = false;
  @Prop() direction: string = 'ltr';
  @Prop() status: 'idle' | 'loading' | 'ready' | 'empty' | 'error' = 'idle';
  @Prop() days: BranchSlotDay[] = [];
  @Prop() activeDate: string = '';
  @Prop() selectedRaw: string = '';
  @Prop() idleText?: string = '';
  @Prop() copy!: BranchSlotDropdownCopy;
  @Prop() formatDay!: (date: string, options: Intl.DateTimeFormatOptions) => string;
  @Prop() displayTime!: (raw: string) => string;
  @Prop() handleDay!: (date: string) => void;
  @Prop() handleTime!: (raw: string) => void;
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

  private renderMessage(text: string, isError = false) {
    const identifier = cn(`${this.name}-slot-empty-container branch-slot-empty-container`, {
      'branch-slot-empty-container-error': isError,
    });

    return (
      <div part={identifier} class={identifier}>
        <div>
          {text}
          {isError && (
            <div>
              <button type="button" class="branch-slot-retry" part={`${this.name}-slot-retry branch-slot-retry`} onClick={() => this.handleRetry()}>
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
      <div>
        <div class="branch-slot-section">
          <div class="branch-slot-head">
            <span class="branch-slot-sublabel">{this.copy.day}</span>
          </div>
          <div class="branch-slot-strip">
            {[0, 1, 2, 3, 4].map(index => (
              <div key={index} class="branch-slot-skeleton branch-slot-skeleton-day"></div>
            ))}
          </div>
        </div>
        <div class="branch-slot-section">
          <div class="branch-slot-head">
            <span class="branch-slot-sublabel">{this.copy.time}</span>
          </div>
          <div class="branch-slot-times">
            {[0, 1, 2, 3, 4, 5].map(index => (
              <div key={index} class="branch-slot-skeleton branch-slot-skeleton-time"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  private renderSlots() {
    const active = this.days.find(day => day.date === this.activeDate);
    let lastMonth = '';

    return (
      <div>
        <div class="branch-slot-section">
          <div class="branch-slot-head">
            <span class="branch-slot-sublabel">{this.copy.day}</span>
            <span class="branch-slot-count">
              {this.days.length} {this.copy.days}
            </span>
          </div>

          <div class="branch-slot-strip" role="radiogroup" aria-label={this.copy.day}>
            {this.days.map(day => {
              const month = this.formatDay(day.date, { month: 'short' });
              const showMonth = month !== lastMonth;
              lastMonth = month;

              const isActive = day.date === this.activeDate;
              const identifier = cn(`${this.name}-slot-day branch-slot-day`, { 'branch-slot-day-selected': isActive });

              return (
                <button
                  key={day.date}
                  type="button"
                  role="radio"
                  part={identifier}
                  class={identifier}
                  aria-checked={isActive ? 'true' : 'false'}
                  onClick={() => this.handleDay(day.date)}
                >
                  <span class="branch-slot-day-dow">{this.formatDay(day.date, { weekday: 'short' })}</span>
                  <span class="branch-slot-day-num">{this.formatDay(day.date, { day: 'numeric' })}</span>
                  <span class="branch-slot-day-mon">{showMonth ? month : ''}</span>
                </button>
              );
            })}
          </div>
        </div>

        <div class="branch-slot-section">
          <div class="branch-slot-head">
            <span class="branch-slot-sublabel">{this.copy.time}</span>
            <span class="branch-slot-count">
              {active?.times.length ?? 0} {this.copy.slots}
            </span>
          </div>

          <div class="branch-slot-times" role="radiogroup" aria-label={this.copy.time}>
            {(active?.times ?? []).map(raw => {
              const isSelected = raw === this.selectedRaw;
              const identifier = cn(`${this.name}-slot-time branch-slot-time`, { 'branch-slot-time-selected': isSelected });

              return (
                <button key={raw} type="button" role="radio" part={identifier} class={identifier} aria-checked={isSelected ? 'true' : 'false'} onClick={() => this.handleTime(raw)}>
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
    const containerIdentifiers = cn(`${this.name}-slot-container branch-slot-container`, {
      'branch-slot-container-open': this.isOpen,
    });

    const backdropIdentifiers = cn(`${this.name}-slot-backdrop branch-slot-backdrop`, {
      'branch-slot-backdrop-open': this.isOpen,
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
          {this.status === 'ready' && this.renderSlots()}
        </div>
      </Host>
    );
  }
}
