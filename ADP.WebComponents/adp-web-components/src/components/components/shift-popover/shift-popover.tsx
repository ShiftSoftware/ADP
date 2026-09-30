import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';
import type { PickerChangeDetail } from '~lib/picker';

import '~lib/middleware';

import { popoverStrings } from './popover-locale';
import { placePanel, visiblePart, Box, PopoverPlacement } from './popover-position';
import type { PopoverAppearance, PopoverColorScheme, PopoverPanelHandlers, PopoverSize } from './shift-popover-panel';

export interface PopoverOpenChangeDetail {
  open: boolean;
}

export interface PopoverShowOptions {
  focus?: boolean;
}

const SHEET_QUERY = '(max-width: 599px)';
const VIEWPORT_MARGIN = 8;

const openByAnchor = new Map<HTMLElement, { hide: () => Promise<void> }>();

// A bottom sheet holds the page still behind its backdrop; the scrollbar's width is kept as padding so nothing shifts.
let scrollLocks = 0;
let unlockPage: (() => void) | null = null;

function lockPageScroll() {
  if (scrollLocks++ > 0) return;

  const root = document.documentElement;
  const gap = window.innerWidth - root.clientWidth;
  const before = { overflow: root.style.overflow, paddingRight: root.style.paddingRight };

  root.style.overflow = 'hidden';
  if (gap > 0) root.style.paddingRight = `${gap}px`;
  unlockPage = () => {
    root.style.overflow = before.overflow;
    root.style.paddingRight = before.paddingRight;
  };
}

function unlockPageScroll() {
  if (scrollLocks === 0 || --scrollLocks > 0) return;

  unlockPage?.();
  unlockPage = null;
}

const parentOf = (node: Element): Element | null => node.parentElement ?? ((node.getRootNode() as ShadowRoot).host as Element | undefined) ?? null;

function deepActiveElement(): Element | null {
  let active = document.activeElement;
  while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
  return active;
}

function isTabbable(element: HTMLElement): boolean {
  if (element.tabIndex < 0 || (element as HTMLButtonElement).disabled || element.hasAttribute('inert')) return false;
  if (!element.matches('a[href], button, input, select, textarea, [tabindex], [contenteditable="true"]')) return false;

  return typeof element.checkVisibility === 'function' ? element.checkVisibility({ checkVisibilityCSS: true } as never) : element.getClientRects().length > 0;
}

// Document order through shadow roots, which is the order Tab walks for these components.
function tabbables(root: Element): HTMLElement[] {
  const found: HTMLElement[] = [];
  const visit = (node: Element) => {
    if (node.hasAttribute('inert') || node.getAttribute('aria-hidden') === 'true') return;
    if (node instanceof HTMLElement && isTabbable(node)) found.push(node);
    if (node.shadowRoot) Array.from(node.shadowRoot.children).forEach(visit);
    Array.from(node.children).forEach(visit);
  };

  Array.from(root.children).forEach(visit);
  return found;
}

function clipBoxes(element: Element): Box[] {
  const viewport = document.documentElement;
  const boxes: Box[] = [{ top: 0, left: 0, bottom: viewport.clientHeight, right: viewport.clientWidth }];

  for (let node = parentOf(element); node && node !== document.body && node !== viewport; node = parentOf(node)) {
    const style = getComputedStyle(node);
    if (style.display === 'contents') continue;
    if (style.overflowX !== 'visible' || style.overflowY !== 'visible') boxes.push(node.getBoundingClientRect());
    if (style.position === 'fixed') break;
  }

  return boxes;
}

const FORM_CONTROLS = 'input, textarea, select';
const EXPANDABLE_ROLES = ['button', 'combobox'];
const BUTTON_INPUTS = ['button', 'submit', 'reset', 'image'];

function supportsExpanded(element: HTMLElement): boolean {
  const role = element.getAttribute('role')?.trim().split(/\s+/)[0];
  if (role) return EXPANDABLE_ROLES.includes(role);

  return element.tagName === 'BUTTON' || (element.tagName === 'INPUT' && BUTTON_INPUTS.includes((element as HTMLInputElement).type));
}

const labelsOf = (element: HTMLElement) => Array.from((element as HTMLInputElement).labels ?? []);

function labelText(label: HTMLElement): string {
  const copy = label.cloneNode(true) as HTMLElement;
  copy.querySelectorAll(FORM_CONTROLS).forEach(control => control.remove());

  return copy.textContent.replace(/\s+/g, ' ').trim();
}

const hasNameSource = (element: HTMLElement): boolean =>
  !!element.getAttribute('aria-label')?.trim() ||
  !!element.getAttribute('aria-labelledby') ||
  labelsOf(element).length > 0 ||
  (element.matches('button, a[href]') && !!element.textContent.trim());

const nameText = (element: HTMLElement | null): string =>
  element
    ? element.getAttribute('aria-label')?.trim() ||
      labelsOf(element)
        .map(label => label.textContent.trim())
        .filter(Boolean)
        .join(' ')
    : '';

const contains = (root: Node, node: Node): boolean => {
  for (let current: Node | null = node; current; current = current.parentNode ?? (current as ShadowRoot).host ?? null) if (current === root) return true;
  return false;
};

/**
 * @part backdrop - rendered by the portaled panel behind a bottom sheet, so declared here for the docs.
 */
@Component({
  shadow: true,
  tag: 'shift-popover',
  styles: ':host{display:none}',
})
export class ShiftPopover {
  @Element() el!: HTMLElement;

  @Prop() anchor?: HTMLElement | string;
  @Prop({ mutable: true, reflect: true }) open: boolean = false;
  @Prop() label?: string;
  @Prop() offset: number = 8;
  @Prop({ reflect: true }) placement: PopoverPlacement = 'bottom';
  @Prop() language: LanguageKeys = 'en';
  @Prop({ reflect: true }) appearance?: PopoverAppearance;
  @Prop({ reflect: true }) colorScheme: PopoverColorScheme = 'light';
  @Prop({ reflect: true }) size: PopoverSize = 'md';

  @Event({ bubbles: true, composed: true }) openChange!: EventEmitter<PopoverOpenChangeDetail>;

  @State() sheet: boolean = false;
  @State() side: 'below' | 'above' = 'below';
  @State() direction: 'ltr' | 'rtl' = 'ltr';
  @State() focusToken: number = 0;
  @State() nameFrom: { labelledBy: string; label: string } = { labelledBy: '', label: '' };

  private panel: HTMLElement | null = null;
  private anchorEl: HTMLElement | null = null;
  private ariaAnchor: HTMLElement | null = null;
  private addedIds: HTMLElement[] = [];
  private panelId = `shift-popover-${Math.random().toString(36).slice(2, 10)}`;
  private sheetQuery?: MediaQueryList;
  private resizeObserver?: ResizeObserver;
  private contentObserver?: MutationObserver;
  private frame = 0;
  private listening = false;
  private pointerDismissed = false;
  private scrollLocked = false;
  private focusNext = true;
  private readonly panelInTemplate = false;

  connectedCallback() {
    if (typeof window === 'undefined' || !window.matchMedia) return;

    this.sheetQuery = window.matchMedia(SHEET_QUERY);
    this.sheet = this.sheetQuery.matches;
    this.sheetQuery.addEventListener?.('change', this.onSheetChange);
  }

  componentDidLoad() {
    this.resolveAnchor();
    if (this.open) this.onOpen();
  }

  disconnectedCallback() {
    this.sheetQuery?.removeEventListener?.('change', this.onSheetChange);
    this.syncScrollLock(false);
    this.contentObserver?.disconnect();
    this.stopListening();
    this.releaseAnchor();
    this.panel?.remove();
  }

  @Watch('placement')
  onPlacementChange() {
    if (this.open) this.schedule();
  }

  @Watch('anchor')
  onAnchorChange() {
    this.resolveAnchor();
    if (this.open) this.schedule();
  }

  @Watch('open')
  onOpenChange(open: boolean) {
    if (open) this.onOpen();
    else this.onClose();
  }

  @Method()
  async show(options?: PopoverShowOptions) {
    const focus = options?.focus !== false;

    if (this.open) {
      if (focus) this.focusToken++;
      return;
    }

    this.focusNext = focus;
    this.open = true;
  }

  @Method()
  async hide() {
    this.open = false;
  }

  @Method()
  async toggle(options?: PopoverShowOptions) {
    // The pointer-down that reached a trigger already dismissed the panel; the click that follows must not reopen it.
    if (this.pointerDismissed) return;

    if (this.open) await this.hide();
    else await this.show(options);
  }

  private onOpen() {
    this.resolveAnchor();

    const anchor = this.anchorEl;
    if (anchor) {
      const other = openByAnchor.get(anchor);
      if (other && other !== this.registration) other.hide();
      openByAnchor.set(anchor, this.registration);
    }

    this.direction = anchor && getComputedStyle(anchor).direction === 'rtl' ? 'rtl' : 'ltr';
    this.place();
    this.startListening();
    this.syncAria();
    this.syncScrollLock();
    if (this.focusNext) this.focusToken++;
    this.focusNext = true;
    this.openChange.emit({ open: true });
  }

  private onClose() {
    this.unregister();

    const active = deepActiveElement();
    if (this.panel && active && contains(this.panel, active)) this.focusAnchor();

    this.stopListening();
    this.syncAria();
    this.syncScrollLock();
    this.openChange.emit({ open: false });
  }

  private syncScrollLock(wanted = this.open && this.sheet) {
    if (typeof document === 'undefined' || wanted === this.scrollLocked) return;

    this.scrollLocked = wanted;
    if (wanted) lockPageScroll();
    else unlockPageScroll();
  }

  private resolveAnchor() {
    let anchor: HTMLElement | null = null;

    if (this.anchor && typeof this.anchor === 'object' && (this.anchor as Node).nodeType === 1) anchor = this.anchor as HTMLElement;
    else if (typeof this.anchor === 'string' && this.anchor.trim()) {
      try {
        const root = this.el.getRootNode() as Document | ShadowRoot;
        anchor = (root.querySelector?.(this.anchor) ?? document.querySelector(this.anchor)) as HTMLElement | null;
      } catch {
        anchor = null;
      }
    }

    if (anchor === this.anchorEl) return;

    this.releaseAnchor();
    this.anchorEl = anchor;
    this.syncAria();

    if (this.open) this.observe();
  }

  private releaseAnchor() {
    const anchor = this.ariaAnchor;
    if (anchor) {
      anchor.removeAttribute('aria-haspopup');
      if (supportsExpanded(anchor)) anchor.removeAttribute('aria-expanded');
      if (anchor.getAttribute('aria-controls') === this.panelId) anchor.removeAttribute('aria-controls');
    }
    for (const element of this.addedIds) if (element.id.startsWith(this.panelId)) element.removeAttribute('id');
    this.addedIds = [];
    this.nameFrom = { labelledBy: '', label: '' };
    this.unregister();
    this.ariaAnchor = null;
  }

  private registration = { hide: () => this.hide() };

  private unregister() {
    if (this.anchorEl && openByAnchor.get(this.anchorEl) === this.registration) openByAnchor.delete(this.anchorEl);
  }

  private syncAria() {
    const anchor = this.anchorEl;
    if (!anchor) return;

    this.ariaAnchor = anchor;
    // A foreign anchor keeps its own role; aria-expanded goes only where that role allows it.
    anchor.setAttribute('aria-haspopup', 'dialog');
    if (supportsExpanded(anchor)) anchor.setAttribute('aria-expanded', String(this.open));

    // An IDREF cannot cross a shadow boundary, so aria-controls and aria-labelledby need the anchor in the panel's tree.
    const reachable = anchor.getRootNode() === document;
    if (reachable) anchor.setAttribute('aria-controls', this.panelId);

    this.nameFrom = reachable ? this.nameFromTree(anchor) : { labelledBy: '', label: nameText(anchor) };
  }

  // Refreshed on every open and close, so an aria-label that changed in the meantime is picked up.
  private nameFromTree(anchor: HTMLElement): { labelledBy: string; label: string } {
    if (!hasNameSource(anchor)) return { labelledBy: '', label: '' };
    if (!anchor.matches(FORM_CONTROLS)) return { labelledBy: this.idOf(anchor, 'anchor'), label: '' };

    // Labelled by a form control itself, a dialog would be named by the control's value (accname 2E), so point at its name sources.
    const labelledBy = anchor.getAttribute('aria-labelledby')?.trim();
    if (labelledBy) return { labelledBy, label: '' };

    const labels = labelsOf(anchor);
    // A label wrapping the control would bring the control's value along, so its text is copied instead of referenced.
    if (labels.some(label => label.contains(anchor))) return { labelledBy: '', label: labels.map(labelText).filter(Boolean).join(' ') };
    if (labels.length) return { labelledBy: labels.map((label, index) => this.idOf(label, `label-${index}`)).join(' '), label: '' };

    return { labelledBy: '', label: nameText(anchor) };
  }

  private idOf(element: HTMLElement, suffix: string): string {
    if (element.id) return element.id;

    element.id = `${this.panelId}-${suffix}`;
    this.addedIds.push(element);
    return element.id;
  }

  private startListening() {
    if (this.listening) return;
    this.listening = true;

    document.addEventListener('pointerdown', this.onPointerDown, true);
    document.addEventListener('keydown', this.onKeyDown);
    document.addEventListener('focusin', this.onFocusIn);
    window.addEventListener('scroll', this.schedule, true);
    window.addEventListener('resize', this.schedule);
    window.visualViewport?.addEventListener('resize', this.schedule);
    this.observe();
  }

  private stopListening() {
    if (!this.listening) return;
    this.listening = false;

    document.removeEventListener('pointerdown', this.onPointerDown, true);
    document.removeEventListener('keydown', this.onKeyDown);
    document.removeEventListener('focusin', this.onFocusIn);
    window.removeEventListener('scroll', this.schedule, true);
    window.removeEventListener('resize', this.schedule);
    window.visualViewport?.removeEventListener('resize', this.schedule);
    this.resizeObserver?.disconnect();
    cancelAnimationFrame(this.frame);
    this.frame = 0;
  }

  private observe() {
    if (typeof ResizeObserver === 'undefined') return;

    this.resizeObserver?.disconnect();
    this.resizeObserver = new ResizeObserver(this.schedule);
    if (this.anchorEl) this.resizeObserver.observe(this.anchorEl);
    const box = this.panelBox();
    if (box) this.resizeObserver.observe(box);
  }

  private panelBox(): HTMLElement | null {
    return this.panel?.shadowRoot?.querySelector<HTMLElement>('.pop-panel') ?? null;
  }

  private schedule = () => {
    if (!this.frame) this.frame = requestAnimationFrame(() => this.place());
  };

  private place() {
    cancelAnimationFrame(this.frame);
    this.frame = 0;

    const anchor = this.anchorEl;
    const box = this.panelBox();
    if (!this.open || this.sheet || !anchor || !box || !this.panel) return;

    const rect = anchor.getBoundingClientRect();
    const viewport = { width: document.documentElement.clientWidth, height: document.documentElement.clientHeight };

    if (viewport.width && viewport.height && !visiblePart(rect, clipBoxes(anchor))) {
      this.hide();
      return;
    }

    // scrollHeight is the content's full height even while a cap makes the panel scroll.
    const height = box.scrollHeight + box.offsetHeight - box.clientHeight;
    const placement = placePanel(rect, { width: box.offsetWidth, height }, viewport, {
      offset: this.offset,
      margin: VIEWPORT_MARGIN,
      rtl: this.direction === 'rtl',
      placement: this.placement === 'top' ? 'top' : 'bottom',
    });

    this.panel.style.setProperty('--_top', `${placement.top}px`);
    this.panel.style.setProperty('--_left', `${placement.left}px`);
    if (placement.maxHeight === null) this.panel.style.removeProperty('--_max-height');
    else this.panel.style.setProperty('--_max-height', `${placement.maxHeight}px`);
    if (placement.side !== this.side) this.side = placement.side;
  }

  private onSheetChange = (event: MediaQueryListEvent) => {
    this.sheet = event.matches;
    this.syncScrollLock();
    if (this.open) this.schedule();
  };

  private within(path: EventTarget[]): boolean {
    // The sheet's backdrop lives in the panel's shadow root, but a tap on it is a tap outside.
    if ((path[0] as Element)?.classList?.contains('pop-backdrop')) return false;

    return (!!this.panel && path.includes(this.panel)) || (!!this.anchorEl && path.includes(this.anchorEl));
  }

  private onPointerDown = (event: PointerEvent) => {
    if (this.within(event.composedPath())) return;

    this.pointerDismissed = true;
    const release = () => {
      document.removeEventListener('pointerup', release, true);
      document.removeEventListener('pointercancel', release, true);
      // The click comes after pointerup in the same task, so this clears once it has been handled.
      setTimeout(() => (this.pointerDismissed = false));
    };
    document.addEventListener('pointerup', release, true);
    document.addEventListener('pointercancel', release, true);
    this.hide();
  };

  // Tab and Shift+Tab out of the panel are handled on the panel; here only focus that leaves for a non-popover element closes it.
  private onFocusIn = (event: FocusEvent) => {
    const path = event.composedPath();
    if (this.within(path) || path.some(node => (node as Element).tagName === 'SHIFT-POPOVER-PANEL')) return;

    this.hide();
  };

  private onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' || event.defaultPrevented || !this.within(event.composedPath())) return;

    // The Escape that closes the panel is spent: a dialog or page listening on window must not close as well.
    event.preventDefault();
    event.stopPropagation();
    this.hide();
    this.focusAnchor();
  };

  private onPanelKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Tab' || !this.panel) return;

    const stops = tabbables(this.panel);
    const active = deepActiveElement();
    if (!stops.length || !active) return;

    if (event.shiftKey && active === stops[0]) {
      event.preventDefault();
      this.focusAnchor();
      this.hide();
    } else if (!event.shiftKey && active === stops[stops.length - 1]) {
      event.preventDefault();
      const next = this.afterAnchor();
      if (next) next.focus({ preventScroll: true });
      else this.focusAnchor();
      this.hide();
    }
  };

  private afterAnchor(): HTMLElement | null {
    const anchor = this.anchorEl;
    if (!anchor) return null;

    const all = tabbables(document.body).filter(element => !this.panel || !contains(this.panel, element));
    const inAnchor = all.filter(element => contains(anchor, element));
    const last = inAnchor.length ? inAnchor[inAnchor.length - 1] : null;
    const index = last ? all.indexOf(last) : all.findIndex(element => anchor.compareDocumentPosition(element) & Node.DOCUMENT_POSITION_FOLLOWING);

    return last ? (all[index + 1] ?? null) : index >= 0 ? all[index] : null;
  }

  private focusAnchor() {
    const anchor = this.anchorEl;
    if (!anchor) return;

    const target = anchor.tabIndex >= 0 || anchor.matches('input, button, select, textarea, a[href]') ? anchor : tabbables(anchor)[0];
    target?.focus({ preventScroll: true });
  }

  // Content that has a Stencil-style setFocus() (shift-calendar does) chooses its own focus; anything else gets its first tab stop.
  private focusContent() {
    if (!this.panel) return;

    const withSetFocus = Array.from(this.panel.children).find(child => typeof (child as unknown as { setFocus?: unknown }).setFocus === 'function') as unknown as
      | { setFocus: () => Promise<void> }
      | undefined;
    if (withSetFocus) withSetFocus.setFocus();
    else tabbables(this.panel)[0]?.focus({ preventScroll: true });
  }

  // The content moves into the panel on <body>, so no overflow or stacking context on the page clips it.
  private adoptContent() {
    if (!this.panel) return;

    for (const node of Array.from(this.el.childNodes)) this.panel.appendChild(node);
  }

  // A picker in the panel that reports a finished choice (pickerChange with complete) is done with it.
  private onContentPick = (event: CustomEvent<PickerChangeDetail>) => {
    if (event.detail?.complete && this.open) this.hide();
  };

  private handlers: PopoverPanelHandlers = {
    keydown: event => this.onPanelKeyDown(event),
    focus: () => this.focusContent(),
    strayFocus: () => this.focusAnchor(),
    ready: panel => {
      this.panel = panel;
      panel.addEventListener('pickerChange', this.onContentPick);
      this.adoptContent();
      if (typeof MutationObserver !== 'undefined') {
        this.contentObserver = new MutationObserver(() => this.adoptContent());
        this.contentObserver.observe(this.el, { childList: true });
      }
      if (this.open) {
        this.observe();
        this.place();
      }
    },
  };

  private panelClasses(): string {
    return ['hydrated', ...this.el.className.split(/\s+/).filter(name => name && name !== 'hydrated')].join(' ');
  }

  render() {
    const panelProps = {
      open: this.open,
      sheet: this.sheet,
      side: this.side,
      direction: this.direction,
      panelId: this.panelId,
      // The dialog is always named: label, else the anchor's name, else a localized default.
      panelLabel: this.label || (this.nameFrom.labelledBy ? '' : this.nameFrom.label || popoverStrings(this.language).dialog),
      panelLabelledBy: this.label ? '' : this.nameFrom.labelledBy,
      panelPart: ['panel', this.sheet ? 'sheet' : ''].filter(Boolean).join(' '),
      focusToken: this.focusToken,
      handlers: this.handlers,
      appearance: this.appearance,
      colorScheme: this.colorScheme,
      size: this.size,
    };

    return (
      <Host>
        {
          // Stencil bundles a portaled tag only when it also sees it in a template; this branch never renders.
          this.panelInTemplate && <shift-popover-panel />
        }
        <shift-portal tag="shift-popover-panel" inheritedClasses={this.panelClasses()} componentProps={panelProps} />
      </Host>
    );
  }
}
