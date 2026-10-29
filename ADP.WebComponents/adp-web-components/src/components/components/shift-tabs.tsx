import { Component, Element, Prop, State, Watch, h } from '@stencil/core';

export type ShiftTabItem = {
  label: string;
  caption?: string | null;
  key?: string;
};

const tabForKey = (key: string, current: number, count: number, rtl: boolean): number | null => {
  if (count < 1) return null;
  const step = key === 'ArrowRight' ? (rtl ? -1 : 1) : key === 'ArrowLeft' ? (rtl ? 1 : -1) : 0;
  if (step !== 0) return (current + step + count) % count;
  if (key === 'Home') return 0;
  if (key === 'End') return count - 1;
  return null;
};

const selectedTabIndex = (tabs: ShiftTabItem[], activeTabIndex?: number, activeTabLabel?: string): number => {
  if (typeof activeTabIndex === 'number' && !Number.isNaN(activeTabIndex)) return Math.min(Math.max(0, activeTabIndex), Math.max(0, tabs.length - 1));
  return activeTabLabel === undefined ? -1 : tabs.findIndex(tab => tab.label === activeTabLabel);
};

const UNDERLINE = 2;

const SETTLE_MS = 480;

const STRETCH = 0.4;

const TRAVEL = 12;

type Box = { x: number; y: number; w: number };
type Placed = Box & { h: number };
type Leaving = { key: string; item: ShiftTabItem; box: Placed };
type Mode = 'rest' | 'flip' | 'slide';

const keyOf = (item: ShiftTabItem, idx: number): string => item.key ?? item.label + idx;

const toItems = (tabs?: (string | ShiftTabItem)[]): ShiftTabItem[] => (tabs || []).map(tab => (typeof tab === 'string' ? { label: tab } : tab));

const signature = (tabs?: (string | ShiftTabItem)[]): string =>
  JSON.stringify(toItems(tabs).map((item, idx) => [keyOf(item, idx), item.label, item.caption === undefined ? 0 : item.caption]));

const transformOf = ({ x, y, w }: Box, base: number): string => `translate(${x}px, ${y}px) scaleX(${w / base})`;

const glidePath = (from: Box, to: Box, stretch: boolean): Keyframe[] => {
  if (!stretch) return [{ transform: transformOf(from, to.w), easing: 'ease' }, { transform: transformOf(to, to.w) }];

  const left = Math.min(from.x, to.x);
  const right = Math.max(from.x + from.w, to.x + to.w);

  return [
    { transform: transformOf(from, to.w), easing: 'ease' },
    { transform: transformOf({ x: left, y: from.y, w: right - left }, to.w), offset: STRETCH, easing: 'ease' },
    { transform: transformOf(to, to.w) },
  ];
};

const reducedMotion = (): boolean => typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

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

  @State() leaving: Leaving[] = [];

  private bar?: HTMLElement;
  private indicator?: HTMLElement;
  private observer?: ResizeObserver;
  private dirObserver?: MutationObserver;
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

  private settle(): number {
    if (this.settleMs === undefined) {
      const raw = getComputedStyle(this.el).getPropertyValue('--settle').trim();
      const value = parseFloat(raw);
      this.settleMs = value > 0 ? (raw.endsWith('ms') ? value : value * 1000) : SETTLE_MS;
    }

    return this.settleMs;
  }

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

    if (!before.length || !after.length || !this.bar || reducedMotion()) {
      this.leaving = [];
      return;
    }

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
    (document as Document & { fonts?: { ready?: Promise<unknown> } }).fonts?.ready?.then(() => this.place('rest'));
  }

  connectedCallback() {
    if (this.bar) this.observe();
  }

  componentDidRender() {
    const selectionChanged = this.selected !== this.placedAt;

    if (this.listChanged) {
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
    this.dirObserver?.disconnect();
    this.observed.clear();
    this.glide?.cancel();
    if (this.frame !== undefined) cancelAnimationFrame(this.frame);
    this.frame = undefined;
  }

  private observe() {
    if (typeof ResizeObserver === 'undefined' || !this.bar) return;

    this.observer ??= new ResizeObserver(() => this.place('rest'));
    const now = new Set<Element>([this.bar, ...this.tabElements()]);

    this.observed.forEach(element => {
      if (!now.has(element)) this.observer!.unobserve(element);
    });
    now.forEach(element => {
      if (!this.observed.has(element)) this.observer!.observe(element, { box: 'border-box' });
    });
    this.observed = now;

    if (typeof MutationObserver === 'undefined') return;

    // A host that flips `dir` on an ancestor moves every tab without resizing one.
    this.dirObserver ??= new MutationObserver(() => this.later('rest'));
    this.dirObserver.observe(this.el.getRootNode(), { attributes: true, attributeFilter: ['dir'], subtree: true });
  }

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

  private visible(): Box | undefined {
    if (!this.glide || !this.indicator || !this.at || typeof DOMMatrixReadOnly === 'undefined') return this.at;

    const matrix = new DOMMatrixReadOnly(getComputedStyle(this.indicator).transform);
    return { x: matrix.m41, y: matrix.m42, w: matrix.a * this.at.w };
  }

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

    const text = tab.querySelector<HTMLElement>('.shift-tabs-text') ?? tab;
    const w = text.offsetWidth;
    if (!w) return;

    // A tab mid-FLIP has a transform, which makes it the text's offsetParent.
    const inset = text.offsetParent === tab ? text.offsetLeft : text.offsetLeft - tab.offsetLeft;
    const to: Box = { x: tab.offsetLeft + inset, y: tab.offsetTop + tab.offsetHeight - UNDERLINE, w };
    const from = this.visible();

    this.at = to;
    this.placedAt = this.selected;
    this.placed = true;
    indicator.style.width = `${w}px`;
    indicator.style.transform = transformOf(to, w);
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
