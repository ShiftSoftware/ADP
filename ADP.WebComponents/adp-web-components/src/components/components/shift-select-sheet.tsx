import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { FormSelectItem } from '~features/form-hook';
import type { LanguageKeys } from '~features/multi-lingual';
import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';

import { popoverStrings } from './shift-popover/popover-locale';

const NAVIGATION_KEYS = ['ArrowDown', 'ArrowUp', 'Home', 'End'];

// On touch, focusing the search would raise the on-screen keyboard over the list the moment the drawer opens.
const coarsePointer = () => typeof window !== 'undefined' && typeof window.matchMedia === 'function' && !!window.matchMedia('(pointer: coarse)')?.matches;

/**
 * @part search - the search field pinned above the list, rendered only for a searchable select.
 * @part list - the listbox.
 * @part option - each option row.
 * @part option-selected - the selected option row, beside option.
 * @part option-label - the text of an option.
 * @part option-tick - the tick, shown on the selected option.
 * @part empty - the no-options, no-matches or fetch error text.
 * @part spinner - the loading spinner inside empty.
 */
@Component({
  shadow: true,
  tag: 'shift-select-sheet',
  styleUrl: 'shift-select-sheet.css',
})
export class ShiftSelectSheet {
  @Element() el!: HTMLElement;

  @Prop() name: string = '';
  @Prop() open: boolean = false;
  @Prop() label?: string;
  @Prop() searchLabel?: string;
  @Prop() language?: LanguageKeys;
  @Prop() hasTick: boolean = true;
  @Prop() searchable: boolean = false;
  @Prop() isLoading?: boolean;
  @Prop() direction: string = 'ltr';
  @Prop() selectedValue: string = '';
  @Prop() options: FormSelectItem[] = [];
  @Prop() reverseOptions: boolean = false;
  @Prop() noSelectionText?: string;
  @Prop() fetchingErrorText?: string | boolean;
  @Prop() handleSelection?: (option: FormSelectItem) => void;

  @State() query: string = '';
  @State() activeIndex: number = -1;
  @State() lockedHeight: number | null = null;

  private searchEl: HTMLInputElement | null = null;
  private listEl: HTMLElement | null = null;
  private measurePending = false;
  private listWaited = false;
  private panelObserver: ResizeObserver | null = null;
  private openHeight = 0;

  // The search is reset as the sheet opens rather than as it closes, so the closing sheet keeps its content while it slides away.
  @Watch('open')
  onOpenChange(open: boolean) {
    if (!open) {
      this.unwatchPanel();
      return;
    }

    this.query = '';
    this.activeIndex = -1;
    this.lockedHeight = null;
    this.measurePending = this.searchable;
    this.listWaited = false;
  }

  disconnectedCallback() {
    this.unwatchPanel();
  }

  componentDidRender() {
    if (!this.measurePending || !this.open) return;

    this.measurePending = false;
    this.lockHeight();
  }

  @Method()
  async setFocus() {
    const scroller = this.scroller();
    if (scroller) scroller.scrollTop = 0;
    if (this.listEl) this.listEl.scrollTop = 0;

    if (this.searchEl && !coarsePointer()) {
      this.searchEl.focus({ preventScroll: true });
      return;
    }

    const rows = this.rows();
    const selected = rows.findIndex(row => row.getAttribute('aria-selected') === 'true');
    this.focusRow(selected >= 0 ? selected : 0);
  }

  private visibleOptions(): FormSelectItem[] {
    const ordered = this.reverseOptions ? [...this.options].reverse() : this.options;
    const query = this.query.toLowerCase();

    return query ? ordered.filter(option => option?.label?.toLowerCase().includes(query)) : ordered;
  }

  private rows(): HTMLElement[] {
    return Array.from(this.listEl?.querySelectorAll<HTMLElement>('[role="option"]') ?? []);
  }

  // The panel the popover carries this element into bounds the sheet's height.
  private scroller(): HTMLElement | null {
    for (let node: HTMLElement | null = this.el.assignedSlot?.parentElement ?? this.el.parentElement; node; node = node.parentElement) {
      const { overflowY } = getComputedStyle(node);
      if (overflowY === 'auto' || overflowY === 'scroll') return node;
    }

    return null;
  }

  // A filtered list keeps the height the sheet opened with, so the rows under the reader's thumb do not jump.
  private lockHeight() {
    const scroller = this.scroller();
    if (!scroller) return;

    const host = this.el.getBoundingClientRect();
    const view = scroller.getBoundingClientRect();
    const room = view.top + scroller.clientTop + scroller.clientHeight - host.top;
    const height = Math.min(host.height, room);

    if (height <= 0) return;

    this.openHeight = height;
    this.lockedHeight = height;
    this.watchPanel(scroller);
  }

  // The lock is measured once; when the panel's cap changes afterwards, such as on rotation, it follows what fits so the list scrolls and the panel does not.
  private watchPanel(panel: HTMLElement) {
    this.unwatchPanel();
    if (typeof ResizeObserver === 'undefined') return;

    this.panelObserver = new ResizeObserver(() => this.refitLock());
    this.panelObserver.observe(panel);
  }

  private refitLock() {
    if (!this.lockedHeight) return;

    const { style } = this.el;
    const kept = style.minBlockSize;
    style.minBlockSize = '0';
    style.blockSize = `${this.openHeight}px`;
    const fit = this.el.getBoundingClientRect().height;
    style.blockSize = '';
    style.minBlockSize = kept;

    if (fit > 0 && Math.abs(fit - this.lockedHeight) >= 0.5) this.lockedHeight = fit;
  }

  private unwatchPanel() {
    this.panelObserver?.disconnect();
    this.panelObserver = null;
  }

  private reveal(row: HTMLElement) {
    const list = this.listEl;
    if (!list) return;

    const box = row.getBoundingClientRect();
    const top = list.getBoundingClientRect().top + list.clientTop;
    const bottom = top + list.clientHeight;

    if (box.top < top) list.scrollTop -= top - box.top;
    else if (box.bottom > bottom) list.scrollTop += box.bottom - bottom;
  }

  private focusRow(index: number) {
    const rows = this.rows();
    if (!rows.length) return;

    const at = Math.max(0, Math.min(index, rows.length - 1));
    this.activeIndex = at;
    rows[at].focus({ preventScroll: true });
    this.reveal(rows[at]);
  }

  private pick(option: FormSelectItem) {
    this.handleSelection?.(option);
  }

  private onSearchInput = (event: Event) => {
    this.query = (event.target as HTMLInputElement).value;
    this.activeIndex = -1;
  };

  private onSearchKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'ArrowDown' && event.key !== 'Enter') return;

    event.preventDefault();
    const options = this.visibleOptions();
    if (event.key === 'Enter' && options.length === 1) this.pick(options[0]);
    else this.focusRow(0);
  };

  private onListKeyDown = (event: KeyboardEvent) => {
    const rows = this.rows();
    const index = rows.indexOf(event.target as HTMLElement);
    if (index < 0) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      const option = this.visibleOptions()[index];
      if (option) this.pick(option);
      return;
    }

    if (!NAVIGATION_KEYS.includes(event.key)) return;
    event.preventDefault();

    if (event.key === 'ArrowUp' && index === 0 && this.searchEl) {
      this.activeIndex = -1;
      this.searchEl.focus({ preventScroll: true });
      return;
    }

    const next = { ArrowDown: index + 1, ArrowUp: index - 1, Home: 0, End: rows.length - 1 }[event.key];
    this.focusRow(next);
  };

  private renderEmpty(hasOptions: boolean) {
    if (this.fetchingErrorText && !hasOptions)
      return (
        <div class="empty" part="empty" data-error="">
          {this.fetchingErrorText}
        </div>
      );

    return (
      <div class="empty" part="empty">
        {this.isLoading && !hasOptions ? <img class="spinner" part="spinner" src={Loader} alt="" /> : this.noSelectionText}
      </div>
    );
  }

  render() {
    const options = this.visibleOptions();
    const selectedIndex = options.findIndex(option => option.value === this.selectedValue);
    const tabStop = this.activeIndex >= 0 && this.activeIndex < options.length ? this.activeIndex : Math.max(selectedIndex, 0);
    if (this.open && !options.length) this.listWaited = true;
    const searchLabel = this.searchable ? this.searchLabel || popoverStrings(this.language || 'en').search : undefined;

    return (
      <Host style={this.lockedHeight ? { minBlockSize: `${this.lockedHeight}px` } : undefined}>
        <div class="sheet" dir={this.direction}>
          {this.searchable && (
            <div class="search-box">
              <input
                ref={el => (this.searchEl = el as HTMLInputElement)}
                class="search"
                part="search"
                type="text"
                value={this.query}
                placeholder={searchLabel}
                aria-label={searchLabel}
                autocomplete="off"
                spellcheck={false}
                enterKeyHint="search"
                onInput={this.onSearchInput}
                onKeyDown={this.onSearchKeyDown}
              />
            </div>
          )}

          <div
            class="list"
            part="list"
            role="listbox"
            aria-label={this.label || undefined}
            data-waited={this.listWaited ? '' : undefined}
            ref={el => (this.listEl = el as HTMLElement)}
            onKeyDown={this.onListKeyDown}
          >
            {options.map((option, index) => {
              const selected = option.value === this.selectedValue;

              return (
                <div
                  class="option"
                  part={selected ? 'option option-selected' : 'option'}
                  role="option"
                  aria-selected={selected ? 'true' : 'false'}
                  tabindex={index === tabStop ? 0 : -1}
                  onClick={() => this.pick(option)}
                >
                  <span class="label" part="option-label">
                    {option.label}
                  </span>
                  {this.hasTick && <TickIcon class="tick" part="option-tick" />}
                </div>
              );
            })}
          </div>

          {!options.length && this.renderEmpty(!!this.options.length)}
        </div>
      </Host>
    );
  }
}
