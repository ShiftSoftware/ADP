import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';

import type { ImageViewerShowOptions } from '../shift-image/shift-image';

/**
 * Page scroll lock, shared by every open sheet. The page is held still by `overflow: hidden` on the root
 * rather than by taking its scrollbar away: the scrollbar's width is kept as padding, so a desktop
 * page does not jump sideways (a phone has no gutter and the padding is 0). Counted, so two sheets
 * never fight over the root's inline style.
 */
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

function deepActiveElement(): HTMLElement | null {
  let active = document.activeElement;
  while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
  return active instanceof HTMLElement && active !== document.body ? active : null;
}

const FOCUSABLE = 'a[href], button, input, select, textarea, [tabindex], [contenteditable="true"]';

function isUsable(element: HTMLElement): boolean {
  if (element.tabIndex < 0 || (element as HTMLButtonElement).disabled || !element.matches(FOCUSABLE)) return false;

  return typeof element.checkVisibility === 'function' ? element.checkVisibility({ checkVisibilityCSS: true } as never) : element.getClientRects().length > 0;
}

/** Tab stops below `root` in document order, through shadow roots; an inert or aria-hidden branch is skipped. */
function tabbables(root: Element): HTMLElement[] {
  const found: HTMLElement[] = [];
  const visit = (node: Element) => {
    if (node.hasAttribute('inert') || node.getAttribute('aria-hidden') === 'true') return;
    if (node instanceof HTMLElement && isUsable(node)) found.push(node);
    if (node.shadowRoot) Array.from(node.shadowRoot.children).forEach(visit);
    Array.from(node.children).forEach(visit);
  };

  Array.from(root.children).forEach(visit);
  return found;
}

/** The longest transition on `element`, in ms (0 when none, or when reduced motion has made it immediate). */
function transitionMs(element: Element): number {
  const style = getComputedStyle(element);
  const toMs = (value: string) => (value.trim().endsWith('ms') ? parseFloat(value) : parseFloat(value) * 1000) || 0;
  const durations = style.transitionDuration.split(',').map(toMs);
  const delays = style.transitionDelay.split(',').map(toMs);

  return Math.max(0, ...durations.map((duration, index) => duration + (delays[index % delays.length] ?? 0)));
}

/**
 * A bottom sheet: a modal surface that rises from the bottom edge of the screen, with a visible
 * title, a close button, a scrim and its own scrolling body. Content goes in the default slot; the
 * title is `heading`, or the `heading` slot when the page wants to style it (a skeleton under a
 * lookup, say).
 *
 * Built on a `<dialog>` opened with `showModal()` in this component's own shadow root:
 * - the top layer lifts it out of a host's `container-type` containment and out of a composite
 *   tab's transform, either of which would trap a `position: fixed` box;
 * - everything outside it is inert, and Escape asks it to close (`cancel`);
 * - the slotted content stays in the page component's own tree, so the page component's stylesheet
 *   styles it, its skeleton covers apply to it, and it keeps its own event handlers.
 *
 * It rises on `transform` and its scrim fades on `opacity`, over `--settle`; closing reverses, and the
 * dialog stays open until the movement has finished. Reduced motion makes both immediate. Closed it
 * is a `display: none` dialog and listens to nothing.
 *
 * `sheetClose` means the reader asked to close (Escape, the scrim, the button): the page sets `open`
 * to false. `sheetClosed` fires once the sheet has really gone. Focus goes into the sheet on open,
 * Tab stays inside it, and focus returns to what had it (or to `restoreFocus`'s answer when that is
 * gone) on close.
 *
 * It holds an image viewer of its own: a viewer outside a modal dialog is inert while the dialog is
 * open, so a picture opened from the sheet's content is shown with `showImage()`.
 *
 * @slot - the sheet's content, in its scrolling body.
 * @slot heading - the visible title, in place of the `heading` text.
 */
@Component({
  shadow: true,
  tag: 'shift-sheet',
  styleUrl: 'shift-sheet.css',
})
export class ShiftSheet {
  @Element() el!: HTMLElement;

  /** Whether the sheet is open. Set it to false to close it: the dialog stays until the movement ends. */
  @Prop() open: boolean = false;

  /** The visible title; also the dialog's accessible name. */
  @Prop() heading: string = '';

  /** The close button's accessible name. */
  @Prop() closeLabel: string = '';

  @Prop() direction: string = 'ltr';

  /** For the image viewer inside. */
  @Prop() language?: LanguageKeys;

  /** Where focus goes on close when what had it at open is gone (hidden, removed, or never focused: a tap on iOS Safari does not focus a button). */
  @Prop() restoreFocus?: (sheet: HTMLElement) => HTMLElement | null | undefined;

  /** The reader asked to close: Escape, a tap on the scrim, or the close button. */
  @Event({ bubbles: false }) sheetClose!: EventEmitter<void>;

  /** The sheet has finished closing and the dialog is shut. */
  @Event({ bubbles: false }) sheetClosed!: EventEmitter<void>;

  /** Drives `data-open`: false for the first frame after `showModal()` (so the rise is a transition, not a first style), and from the moment a close begins. */
  @State() shown: boolean = false;

  private dialog?: HTMLDialogElement;
  private panel?: HTMLElement;
  private closeButton?: HTMLButtonElement;
  private viewer?: HTMLShiftImageElement;
  private viewerOpen = false;
  private trigger: HTMLElement | null = null;
  private locked = false;
  private finishTimer?: ReturnType<typeof setTimeout>;
  private mountFrame?: number;

  /** Opens a picture above the sheet (an image viewer inside the dialog), growing out of `options.origin`. */
  @Method()
  async showImage(src: string, options: ImageViewerShowOptions = {}): Promise<boolean> {
    return (await this.viewer?.show(src, options)) ?? false;
  }

  // Mounted open: the first render is the closed sheet, and it is opened a frame later so the rise is a transition.
  componentDidLoad() {
    if (!this.open) return;
    this.mountFrame = requestAnimationFrame(() => {
      this.mountFrame = undefined;
      if (this.open && this.el.isConnected) this.present();
    });
  }

  @Watch('open')
  onOpenChange(open: boolean) {
    if (open) this.present();
    else this.dismiss();
  }

  disconnectedCallback() {
    clearTimeout(this.finishTimer);
    if (this.mountFrame !== undefined) cancelAnimationFrame(this.mountFrame);
    // The dialog's queued `close` event must not read as a close the page should hear about.
    this.shown = false;
    this.release(false);
  }

  private present() {
    const dialog = this.dialog;
    if (!dialog) return;

    // Opened again while it was still closing: it is already up, so it turns round where it is.
    clearTimeout(this.finishTimer);

    if (!dialog.open) {
      this.trigger = deepActiveElement();
      if (typeof dialog.showModal === 'function') dialog.showModal();
      else dialog.setAttribute('open', '');

      this.lock();
      // The closed style must be computed once, or the first frame is the open style and nothing moves.
      void dialog.offsetWidth;
    }

    this.shown = true;
    this.closeButton?.focus({ preventScroll: true });
  }

  private dismiss() {
    const dialog = this.dialog;
    if (!dialog?.open) return;

    this.shown = false;
    clearTimeout(this.finishTimer);

    const ms = this.panel ? transitionMs(this.panel) : 0;
    // transitionend is the real signal; the timer is for the case it never comes (reduced motion, a hidden tab).
    this.finishTimer = setTimeout(this.finish, ms > 1 ? ms + 250 : 0);
  }

  private onPanelTransitionEnd = (event: TransitionEvent) => {
    if (event.target === this.panel && event.propertyName === 'transform' && !this.shown) this.finish();
  };

  private finish = () => {
    clearTimeout(this.finishTimer);
    if (this.shown || !this.dialog?.open) return;

    this.release(true);
    this.sheetClosed.emit();
  };

  /** Shuts the dialog, lets the page scroll again and gives focus back. */
  private release(restore: boolean) {
    const dialog = this.dialog;
    const wasOpen = !!dialog?.open;

    if (wasOpen) {
      if (typeof dialog.close === 'function') dialog.close();
      else dialog.removeAttribute('open');
    }

    this.unlock();

    if (!wasOpen) return;

    const usable = (element: HTMLElement | null | undefined): element is HTMLElement => !!element?.isConnected && isUsable(element);
    const target = usable(this.trigger) ? this.trigger : this.restoreFocus?.(this.el);
    this.trigger = null;
    if (restore && usable(target)) target.focus({ preventScroll: true });
  }

  private lock() {
    if (this.locked || typeof document === 'undefined') return;
    this.locked = true;
    lockPageScroll();
  }

  private unlock() {
    if (!this.locked) return;
    this.locked = false;
    unlockPageScroll();
  }

  // Escape. The browser would shut the dialog on the spot; the page decides, and the close is animated.
  private onCancel = (event: Event) => {
    event.preventDefault();
    if (!this.viewerOpen) this.sheetClose.emit();
  };

  // A close the browser made itself (not ours): put everything back and tell the page.
  private onNativeClose = () => {
    if (this.shown || this.locked) {
      this.shown = false;
      clearTimeout(this.finishTimer);
      this.release(true);
      this.sheetClose.emit();
      this.sheetClosed.emit();
    }
  };

  // The dialog keeps Tab inside it in most browsers, but may hand it to the browser's own chrome after the last stop; this wraps instead.
  private onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Tab' || event.defaultPrevented) return;

    const stops = [this.closeButton, ...tabbables(this.el)].filter((stop): stop is HTMLElement => !!stop && isUsable(stop));
    if (!stops.length) return;

    const active = deepActiveElement();
    const index = active ? stops.indexOf(active) : -1;
    const wrap = event.shiftKey ? index <= 0 : index === stops.length - 1 || index < 0;
    if (!wrap) return;

    event.preventDefault();
    stops[event.shiftKey ? stops.length - 1 : 0].focus({ preventScroll: true });
  };

  render() {
    return (
      <Host>
        <dialog
          ref={el => (this.dialog = el)}
          class="sheet"
          role="dialog"
          aria-modal="true"
          aria-labelledby="sheet-title"
          dir={this.direction}
          data-open={this.shown ? 'true' : 'false'}
          onCancel={this.onCancel}
          onClose={this.onNativeClose}
          onKeyDown={this.onKeyDown}
        >
          <div class="scrim" onClick={() => this.sheetClose.emit()} />

          <div class="panel" ref={el => (this.panel = el)} onTransitionEnd={this.onPanelTransitionEnd}>
            <div class="head">
              <span id="sheet-title" class="title">
                <slot name="heading">{this.heading}</slot>
              </span>
              <button ref={el => (this.closeButton = el)} type="button" class="close" aria-label={this.closeLabel || undefined} onClick={() => this.sheetClose.emit()}>
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                  <path d="M6 6l12 12M18 6 6 18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                </svg>
              </button>
            </div>

            <div class="body">
              <slot />
            </div>
          </div>

          <shift-image
            class="viewer"
            ref={el => (this.viewer = el)}
            language={this.language}
            onViewerChange={(event: CustomEvent<{ open: boolean }>) => (this.viewerOpen = event.detail.open)}
          />
        </dialog>
      </Host>
    );
  }
}
