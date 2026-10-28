import { Component, Element, Prop, State, Watch, h } from '@stencil/core';

/** One tab. A plain string is a label alone. */
export type ShiftTabItem = {
  label: string;
  /**
   * The small line beneath the label (a date). `undefined` draws no line at all. `null` or a string
   * draws the line as a slot of its own: a string opens it, `null` shuts it, and it slides between
   * the two instead of being mounted and unmounted.
   */
  caption?: string | null;
  /** A stable key for the tab, when the label can repeat or change under the same tab. */
  key?: string;
};

/**
 * The tab a key press moves to, or null when the key is not one the tabs answer to. Left and right
 * follow the reading direction (right-to-left reverses them); Home and End go to the ends; a move
 * wraps.
 */
const tabForKey = (key: string, current: number, count: number, rtl: boolean): number | null => {
  if (count < 1) return null;
  const step = key === 'ArrowRight' ? (rtl ? -1 : 1) : key === 'ArrowLeft' ? (rtl ? 1 : -1) : 0;
  if (step !== 0) return (current + step + count) % count;
  if (key === 'Home') return 0;
  if (key === 'End') return count - 1;
  return null;
};

/** The selected tab: the index when one is given, otherwise the tab named by the label; -1 when neither names one. */
const selectedTabIndex = (tabs: ShiftTabItem[], activeTabIndex?: number, activeTabLabel?: string): number => {
  if (typeof activeTabIndex === 'number' && !Number.isNaN(activeTabIndex)) return Math.min(Math.max(0, activeTabIndex), Math.max(0, tabs.length - 1));
  return activeTabLabel === undefined ? -1 : tabs.findIndex(tab => tab.label === activeTabLabel);
};

/** The underline's height. The one number the geometry needs; `.shift-tabs-indicator` in the stylesheet is the same. */
const UNDERLINE = 2;

/** `--settle` (lookup-tokens.css), ms, when the host does not define it. */
const SETTLE_MS = 480;

/** The part of a selection glide the line spends reaching out; the rest is the trailing edge following. */
const STRETCH = 0.4;

/** How far a tab arriving or leaving travels, px: it comes out from under, and goes back under, its neighbour. */
const TRAVEL = 12;

type Box = { x: number; y: number; w: number };
type Placed = Box & { h: number };
type Leaving = { key: string; item: ShiftTabItem; box: Placed };
type Mode = 'rest' | 'flip' | 'slide';

const keyOf = (item: ShiftTabItem, idx: number): string => item.key ?? item.label + idx;

const toItems = (tabs?: (string | ShiftTabItem)[]): ShiftTabItem[] => (tabs || []).map(tab => (typeof tab === 'string' ? { label: tab } : tab));

/** What a list says, so a new array with the same tabs is not a change. */
const signature = (tabs?: (string | ShiftTabItem)[]): string =>
  JSON.stringify(toItems(tabs).map((item, idx) => [keyOf(item, idx), item.label, item.caption === undefined ? 0 : item.caption]));

const transformOf = ({ x, y, w }: Box): string => `translate(${x}px, ${y}px) scaleX(${w})`;

/** The line's path. A slide first reaches out (the leading edge spans both tabs), then the trailing edge follows onto the new tab's width. */
const glidePath = (from: Box, to: Box, stretch: boolean): Keyframe[] => {
  if (!stretch) return [{ transform: transformOf(from), easing: 'ease' }, { transform: transformOf(to) }];

  const left = Math.min(from.x, to.x);
  const right = Math.max(from.x + from.w, to.x + to.w);

  return [
    { transform: transformOf(from), easing: 'ease' },
    { transform: transformOf({ x: left, y: from.y, w: right - left }), offset: STRETCH, easing: 'ease' },
    { transform: transformOf(to) },
  ];
};

const reducedMotion = (): boolean => typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

/**
 * A slim tab strip: a label per tab with an optional caption beneath it, and one blue underline that
 * stretches out to the next selected tab and settles onto it. A list that gains or loses tabs moves as one:
 * tabs that go fade back under their neighbour, tabs that come fade out from under it, and the tabs that stay
 * (and the underline) move to their new places by transform.
 *
 * Original API, unchanged: `tabs` (labels), `activeTabLabel`, `activeTabIndex`, `changeActiveTab({ label, idx })`,
 * `tabClasses` (on the strip), `containerClasses` (on the wrapper) and the default slot, shown below the strip in a
 * `flexible-container`.
 *
 * Additive and optional:
 *  - `tabs` may hold `{ label, caption?, key? }` objects as well as strings;
 *  - `label` names the tablist, `panelId` is the tabpanel it controls (`aria-controls`), `tabIdPrefix` gives each tab an
 *    id (`<prefix>-<index>`) so a panel can say `aria-labelledby`;
 *  - `disabled` keeps the tabs where they are but answers to nothing (`aria-disabled`);
 *  - `skeleton` puts `shift-skeleton` on each tab's text block, one cover per tab, so a host's `.loading` covers the
 *    labels in place (the host imports shift-skeleton.css);
 *  - `direction` ('ltr' | 'rtl'); by default the nearest `dir` above the strip;
 *  - `bare` draws the strip alone, without the slot's `flexible-container`.
 *
 * The contract: `role=tablist` / `tab`, `aria-selected`, a roving tabindex, Left / Right (reversed in rtl), Home and End,
 * wrapping, and selection that follows focus. The component is controlled: it asks (`changeActiveTab`) and shows what the
 * props say. It renders without a shadow root, so its stylesheet lands in the shadow root of the component that holds it.
 */
@Component({
  shadow: false,
  tag: 'shift-tabs',
  styleUrl: 'shift-tabs.css',
})
export class ShiftTabs {
  @Element() el: HTMLElement;

  @Prop() tabs: (string | ShiftTabItem)[] = [];
  @Prop() tabClasses?: string;
  @Prop() activeTabIndex?: number;
  @Prop() activeTabLabel?: string;
  @Prop() containerClasses?: string;
  @Prop() changeActiveTab?: (activeStatus: { label: string; idx: number }) => void;

  @Prop() label?: string;
  @Prop() panelId?: string;
  @Prop() tabIdPrefix?: string;
  @Prop() disabled: boolean = false;
  @Prop() skeleton: boolean = false;
  @Prop() direction?: 'ltr' | 'rtl';
  @Prop() bare: boolean = false;

  /** Tabs the last list dropped, kept drawn (out of the flow, where they stood) until they have faded. */
  @State() leaving: Leaving[] = [];

  private bar?: HTMLElement;
  private indicator?: HTMLElement;
  private observer?: ResizeObserver;
  private observed = new Set<Element>();
  private tabRefs = new Map<string, HTMLElement>();
  private selected = -1;
  private placedAt = -1;
  private placed = false;
  private at?: Box;
  private glide?: Animation;
  private glideFrom?: Box;
  private glideStretch = false;
  private frame?: number;
  private pending: Mode = 'rest';
  private listChanged = false;
  private firsts = new Map<string, Placed>();
  private arrivals: string[] = [];
  private departed = new Set<string>();
  private settleMs?: number;

  private items = (): ShiftTabItem[] => toItems(this.tabs);

  private isRtl = (): boolean => (this.direction ?? this.el.closest('[dir]')?.getAttribute('dir')) === 'rtl';

  private tabElements = (): HTMLElement[] => Array.from(this.bar?.querySelectorAll<HTMLElement>('[role="tab"]') ?? []);

  /** `--settle`, read once: the host's token, so the line and the tabs run on the clock of the content around them. */
  private settle(): number {
    if (this.settleMs === undefined) {
      const raw = getComputedStyle(this.el).getPropertyValue('--settle').trim();
      const value = parseFloat(raw);
      this.settleMs = value > 0 ? (raw.endsWith('ms') ? value : value * 1000) : SETTLE_MS;
    }

    return this.settleMs;
  }

  /** A tab asked for. The tab that is already selected asks for nothing, whether it was pressed or a key landed on it (a one-tab strip wraps onto itself). */
  private choose(items: ShiftTabItem[], idx: number) {
    if (this.disabled || idx === this.selected) return;
    this.changeActiveTab?.({ label: items[idx].label, idx });
  }

  @Watch('tabs')
  onTabs(next: (string | ShiftTabItem)[], prev: (string | ShiftTabItem)[]) {
    if (signature(next) === signature(prev)) return;

    this.listChanged = true;
    this.firsts.clear();
    this.arrivals = [];

    const after = toItems(next);
    const before = toItems(prev);
    const keys = new Set(after.map(keyOf));

    // Nothing on screen to move between: the strip's own slot is the transition.
    if (!before.length || !after.length || !this.bar || reducedMotion()) {
      this.leaving = [];
      return;
    }

    // A tab still on its way out that the new list brings back is a current tab again.
    this.leaving
      .filter(gone => keys.has(gone.key))
      .forEach(gone => {
        this.tabRefs
          .get(gone.key)
          ?.getAnimations?.()
          .forEach(animation => animation.cancel());
        this.departed.delete(gone.key);
      });

    const kept = this.leaving.filter(gone => !keys.has(gone.key));
    const now = new Set(before.map(keyOf));
    const dropped: Leaving[] = [];

    before.forEach((item, idx) => {
      const key = keyOf(item, idx);
      const el = this.tabRefs.get(key);

      if (!el || el.dataset.leaving) return;

      const box = { x: el.offsetLeft, y: el.offsetTop, w: el.offsetWidth, h: el.offsetHeight };

      this.firsts.set(key, box);
      if (!keys.has(key)) dropped.push({ key, item, box });
    });

    this.leaving = [...kept, ...dropped];
    this.arrivals = after.map(keyOf).filter(key => !now.has(key));
  }

  @Watch('direction')
  onDirection() {
    this.later('rest');
  }

  componentShouldUpdate(next: unknown, prev: unknown, prop: string) {
    return prop !== 'tabs' || signature(next as ShiftTabItem[]) !== signature(prev as ShiftTabItem[]);
  }

  componentDidLoad() {
    this.observe();
    // A font arriving changes every label's width. The observer sees the tabs resize; this covers a strip placed before it started.
    (document as Document & { fonts?: { ready?: Promise<unknown> } }).fonts?.ready?.then(() => this.place('rest'));
  }

  connectedCallback() {
    // Moved or re-attached: disconnectedCallback let the observer go, and the first render is long past, so it is not started again there.
    if (this.bar) this.observe();
  }

  componentDidRender() {
    const selectionChanged = this.selected !== this.placedAt;

    if (this.listChanged) {
      // Before the next paint, so no frame shows the new layout un-moved.
      this.listChanged = false;
      this.observe();
      this.flip();
      this.place(this.placed ? (selectionChanged ? 'slide' : 'flip') : 'rest');
    } else if (selectionChanged || !this.placed) {
      this.later(this.placed ? 'slide' : 'rest');
    }

    this.depart();
  }

  disconnectedCallback() {
    this.observer?.disconnect();
    this.observed.clear();
    this.glide?.cancel();
    if (this.frame !== undefined) cancelAnimationFrame(this.frame);
    this.frame = undefined;
  }

  /** The strip and its tabs are watched, so a resize of the host, a caption opening or a font arriving moves the underline with the tab. */
  private observe() {
    if (typeof ResizeObserver === 'undefined' || !this.bar) return;

    this.observer ??= new ResizeObserver(() => this.place('rest'));
    const now = new Set<Element>([this.bar, ...this.tabElements()]);

    this.observed.forEach(element => {
      if (!now.has(element)) this.observer!.unobserve(element);
    });
    now.forEach(element => {
      if (!this.observed.has(element)) this.observer!.observe(element);
    });
    this.observed = now;
  }

  /** Measures in the next frame's read phase, once, however many renders asked. */
  private later(mode: Mode) {
    if (this.pending !== 'slide') this.pending = mode;
    if (this.frame !== undefined) return;

    this.frame = requestAnimationFrame(() => {
      this.frame = undefined;
      const mode = this.pending;
      this.pending = 'rest';
      this.place(mode);
    });
  }

  /** The tabs that stay move from where they stood (a transform); the ones that arrive fade in from under their neighbour. */
  private flip() {
    const first = this.firsts;
    const arrivals = this.arrivals;
    this.firsts = new Map();
    this.arrivals = [];

    if (reducedMotion()) return;

    const ms = this.settle();
    const travel = this.isRtl() ? TRAVEL : -TRAVEL;

    first.forEach((box, key) => {
      const el = this.tabRefs.get(key);
      if (!el?.animate || el.dataset.leaving) return;

      const dx = box.x - el.offsetLeft;
      const dy = box.y - el.offsetTop;
      if (Math.abs(dx) > 0.5 || Math.abs(dy) > 0.5) el.animate([{ transform: `translate(${dx}px, ${dy}px)` }, { transform: 'none' }], { duration: ms, easing: 'ease' });
    });

    arrivals.forEach(key =>
      this.tabRefs.get(key)?.animate?.(
        [
          { opacity: 0, transform: `translateX(${travel}px)` },
          { opacity: 1, transform: 'none' },
        ],
        { duration: ms, easing: 'ease' },
      ),
    );
  }

  /** Starts the fade of each tab the list dropped; it is let go of when it has gone. */
  private depart() {
    const travel = this.isRtl() ? TRAVEL : -TRAVEL;

    this.leaving.forEach(({ key }) => {
      if (this.departed.has(key)) return;
      this.departed.add(key);

      const el = this.tabRefs.get(key);
      const done = () => {
        this.departed.delete(key);
        this.leaving = this.leaving.filter(gone => gone.key !== key);
      };

      if (!el?.animate || reducedMotion()) {
        done();
        return;
      }

      el.animate(
        [
          { opacity: 1, transform: 'none' },
          { opacity: 0, transform: `translateX(${travel}px)` },
        ],
        { duration: this.settle(), easing: 'ease', fill: 'forwards' },
      ).onfinish = done;
    });
  }

  /** Where the line is on screen now: its resting place, or wherever a glide has taken it. */
  private visible(): Box | undefined {
    if (!this.glide || !this.indicator || typeof DOMMatrixReadOnly === 'undefined') return this.at;

    const matrix = new DOMMatrixReadOnly(getComputedStyle(this.indicator).transform);
    return { x: matrix.m41, y: matrix.m42, w: matrix.a };
  }

  /**
   * Puts the underline under the selected tab with `transform` alone (translate to the tab's corner, scaleX to its width),
   * measured from layout so a transformed ancestor cannot skew it. Physical, not logical: `offsetLeft` is physical in
   * both directions, so a right-to-left strip needs no special case. `slide` (the selection changed) reaches out and
   * follows; `flip` (the same tab moved with its list) goes straight; `rest` (first placement, a resize) lands in place,
   * and retargets a glide in flight instead of cutting it.
   */
  private place(mode: Mode) {
    const indicator = this.indicator;
    const tab = this.tabElements()[this.selected];

    if (!indicator) return;

    if (!tab) {
      indicator.removeAttribute('data-placed');
      this.glide?.cancel();
      this.at = undefined;
      return;
    }

    // Reads first, then the writes.
    const w = tab.offsetWidth;
    // Not laid out (display: none in a dormant tab): the observer places it when it wakes.
    if (!w) return;

    const to: Box = { x: tab.offsetLeft, y: tab.offsetTop + tab.offsetHeight - UNDERLINE, w };
    const from = this.visible();

    this.at = to;
    this.placedAt = this.selected;
    this.placed = true;
    indicator.style.transform = transformOf(to);
    indicator.setAttribute('data-placed', 'true');

    if (this.glide && mode === 'rest' && this.glideFrom) {
      (this.glide.effect as KeyframeEffect | null)?.setKeyframes?.(glidePath(this.glideFrom, to, this.glideStretch));
      return;
    }

    if (!from || mode === 'rest' || !indicator.animate || reducedMotion() || (from.x === to.x && from.y === to.y && from.w === to.w)) return;

    this.glide?.cancel();
    this.glideFrom = from;
    this.glideStretch = mode === 'slide';

    const glide = indicator.animate(glidePath(from, to, this.glideStretch), { duration: this.settle(), easing: 'linear' });
    const over = () => {
      if (this.glide === glide) this.glide = undefined;
    };
    glide.onfinish = over;
    glide.oncancel = over;
    this.glide = glide;
  }

  private renderText(item: ShiftTabItem) {
    return (
      // One block per tab, so a lookup covers a tab's text with a single cover and one sweep.
      <span class={{ 'shift-tabs-text': true, 'shift-skeleton': this.skeleton }}>
        <span class="shift-tabs-label">{item.label}</span>
        {item.caption !== undefined && (
          <span class="shift-tabs-caption" data-open={item.caption !== null ? 'true' : 'false'} aria-hidden={item.caption !== null ? null : 'true'}>
            <span class="shift-tabs-caption-body">
              <span class="shift-tabs-date">{item.caption}</span>
            </span>
          </span>
        )}
      </span>
    );
  }

  render() {
    const items = this.items();
    this.selected = selectedTabIndex(items, this.activeTabIndex, this.activeTabLabel);
    // The roving tabindex: the selected tab, or the first when none is selected yet.
    const entry = this.selected >= 0 ? this.selected : 0;
    const rtl = this.isRtl();

    return (
      <div class={{ 'shift-tabs': true, [this.containerClasses || '']: !!this.containerClasses }}>
        <div class={{ 'shift-tabs-bar': true, [this.tabClasses || '']: !!this.tabClasses }} role="tablist" aria-label={this.label} ref={element => (this.bar = element)}>
          {items.map((item, idx) => {
            const isSelected = idx === this.selected;
            const key = keyOf(item, idx);

            return (
              <button
                key={key}
                ref={element => (element ? this.tabRefs.set(key, element) : this.tabRefs.delete(key))}
                type="button"
                role="tab"
                class="shift-tabs-tab"
                id={this.tabIdPrefix ? `${this.tabIdPrefix}-${idx}` : null}
                aria-selected={isSelected ? 'true' : 'false'}
                aria-controls={this.panelId}
                tabIndex={idx === entry ? 0 : -1}
                data-selected={isSelected ? 'true' : 'false'}
                aria-disabled={this.disabled ? 'true' : null}
                onClick={() => this.choose(items, idx)}
                onKeyDown={event => {
                  const next = tabForKey(event.key, idx, items.length, rtl);
                  if (next === null) return;
                  // The page does not scroll under a handled key even when the strip is disabled; a disabled strip does nothing else.
                  event.preventDefault();
                  if (this.disabled) return;
                  this.choose(items, next);
                  this.tabElements()[next]?.focus({ preventScroll: true });
                }}
              >
                {this.renderText(item)}
              </button>
            );
          })}
          {this.leaving.map(({ key, item, box }) => (
            <button
              key={key}
              ref={element => (element ? this.tabRefs.set(key, element) : this.tabRefs.delete(key))}
              type="button"
              class="shift-tabs-tab"
              data-leaving="true"
              data-selected="false"
              aria-hidden="true"
              tabIndex={-1}
              disabled
              style={{ left: `${box.x}px`, top: `${box.y}px`, width: `${box.w}px`, height: `${box.h}px` }}
            >
              {this.renderText(item)}
            </button>
          ))}
          <span class="shift-tabs-indicator" aria-hidden="true" ref={element => (this.indicator = element)} />
        </div>
        {!this.bare && (
          <flexible-container>
            <slot></slot>
          </flexible-container>
        )}
      </div>
    );
  }
}
