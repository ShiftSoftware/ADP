import { Component, Element, Host, Prop, Watch, h } from '@stencil/core';

import { CloseIcon } from '~assets/close-icon';

export type ImageLayerHandlers = {
  /** The reader asked to close: Escape, the close button, or a click outside the picture. */
  dismiss: () => void;
  /** The opening movement has finished. */
  opened?: () => void;
  /** The closing movement has finished and the layer is gone from the screen. */
  closed?: () => void;
};

type Box = { left: number; top: number; width: number; height: number };

/** The flight's timing when the page sets none (the stylesheet's --shift-image-* custom properties). */
const DEFAULT_DURATION_MS = 460;
const DEFAULT_OPEN_EASE = 'cubic-bezier(0.2, 0, 0, 1)';
const DEFAULT_CLOSE_EASE = 'cubic-bezier(0.3, 0, 0.1, 1)';
/** The card's corner radius when it is open (the stylesheet's .frame). */
const CARD_RADIUS = 12;
/** Keyframes per flight: enough that the curve between them is not seen. */
const FLIGHT_STEPS = 24;

// Room around the picture: the close button's row above it (16 + 44 + 12), a caption's line below it
// when there is one, and a side gutter that keeps the picture off the window's edges (narrower on a
// phone, where every pixel of the picture counts).
const TOP_RESERVE = 72;
const BOTTOM_RESERVE = 32;
const CAPTION_RESERVE = 64;
const WIDE_GUTTER = 72;
const NARROW_GUTTER = 16;
const NARROW_WINDOW = 600;

const SCROLL_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End', ' ']);

/** The picture's box inside its thumbnail: a thumbnail usually draws it `object-fit: contain`, so it can be narrower or shorter than the element. */
const containedBox = (image: HTMLImageElement): Box | null => {
  const rect = image.getBoundingClientRect();
  if (!rect.width || !rect.height) return null;

  const fit = typeof getComputedStyle === 'function' ? getComputedStyle(image).objectFit : '';
  if (fit !== 'contain' || !image.naturalWidth || !image.naturalHeight) return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };

  const ratio = image.naturalWidth / image.naturalHeight;
  let width = rect.width;
  let height = width / ratio;
  if (height > rect.height) {
    height = rect.height;
    width = height * ratio;
  }
  return { left: rect.left + (rect.width - width) / 2, top: rect.top + (rect.height - height) / 2, width, height };
};

/**
 * The corner radius of the thumbnail as the reader sees it: the thumbnail's own, or the first frame
 * around it that rounds its corners (a thumbnail is often a plain image inside a rounded box).
 */
const referenceRadius = (image: HTMLImageElement): number => {
  let element: Element | null = image;
  for (let depth = 0; element && depth < 4; depth++, element = element.parentElement) {
    const radius = parseFloat(getComputedStyle(element).borderTopLeftRadius);
    if (radius > 0) return radius;
  }
  return 0;
};

/** A box on screen and the corner radius it shows. */
type Pose = Box & { radius: number };

const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

/**
 * How the size travels between two poses.
 *
 * - `linear` (opening): every edge moves in a straight line, so position and size change together and
 *   the picture opens out of the thumbnail towards its place in one movement (owner, 2026-10-25: with
 *   the size changing by ratio it seemed to travel to the middle of the window first and open there).
 * - `ratio` (closing): the size changes by the same ratio every step. The card shrinks about twenty
 *   times on its way back, and a straight-line shrink keeps it visibly oversized until the very end.
 */
type SizeTravel = 'linear' | 'ratio';

/**
 * The flight from pose `a` to pose `b`, for a card laid out at `to`, as keyframes. The centre travels
 * in a straight line; the size as `travel` says. The corners are set so the radius on screen eases
 * from one pose's to the other's, whatever the scale.
 */
const flightFrames = (a: Pose, b: Pose, to: Box, travel: SizeTravel): Keyframe[] =>
  Array.from({ length: FLIGHT_STEPS + 1 }, (_, step) => {
    const t = step / FLIGHT_STEPS;
    const width = travel === 'linear' ? lerp(a.width, b.width, t) : a.width * Math.pow(b.width / a.width, t);
    const height = travel === 'linear' ? lerp(a.height, b.height, t) : a.height * Math.pow(b.height / a.height, t);
    const centreX = lerp(a.left + a.width / 2, b.left + b.width / 2, t);
    const centreY = lerp(a.top + a.height / 2, b.top + b.height / 2, t);
    const scaleX = width / to.width;
    const scaleY = height / to.height;
    const radius = lerp(a.radius, b.radius, t);
    return {
      offset: t,
      transform: `translate(${centreX - width / 2 - to.left}px, ${centreY - height / 2 - to.top}px) scale(${scaleX}, ${scaleY})`,
      borderRadius: `${radius / scaleX}px / ${radius / scaleY}px`,
    };
  });

/**
 * The image viewer's layer, mounted on <body> by `shift-image` through `shift-portal` (or into the open
 * modal <dialog> it was opened from), and raised into the browser's top layer while open, so nothing
 * on the page can trap, clip or cover it. Not used directly: pages use `<shift-image>`.
 *
 * Smoothness is the design constraint (owner, 2026-10-25). The picture is laid out once, at its final
 * size and place, and its flight out of the thumbnail and back is `transform` and `opacity` only, which
 * the compositor animates without laying out or repainting the page. There is no backdrop blur (it
 * repaints the whole page under it every frame). The flight starts on the thumbnail's own pixels, which
 * are already loaded, and the full-size picture cross-fades in over them when it has arrived, so
 * opening never waits on the network.
 */
@Component({
  shadow: true,
  tag: 'shift-image-layer',
  styleUrl: 'shift-image.css',
})
export class ShiftImageLayer {
  @Element() el!: HTMLElement;

  /** The thumbnail the picture grows out of and shrinks back into. */
  @Prop() origin?: HTMLImageElement | null;
  /** The full-size picture. */
  @Prop() src: string = '';
  /** A line under the picture: what it shows. Optional. */
  @Prop() caption: string = '';
  /** The dialog's accessible name. */
  @Prop() viewerLabel: string = '';
  /** The close button's accessible name. */
  @Prop() closeLabel: string = '';
  @Prop() handlers?: ImageLayerHandlers;
  /**
   * Where the layer lives while open: <body>, or the open modal <dialog> the viewer was opened from.
   * A modal dialog makes everything outside it inert, so a layer on <body> would be painted above
   * the dialog but could not be clicked.
   */
  @Prop() container?: HTMLElement | null;
  @Prop() open: boolean = false;

  private frame?: HTMLDivElement;
  private preview?: HTMLImageElement;
  private full?: HTMLImageElement;
  private closeButton?: HTMLButtonElement;
  private captionLine?: HTMLParagraphElement;

  private generation = 0;
  private finishTimer?: ReturnType<typeof setTimeout>;
  private lowerTimer?: ReturnType<typeof setTimeout>;
  private target?: Box;
  private flightAnimation?: Animation;
  private fadeAnimation?: Animation;
  private hiddenOrigin?: HTMLElement;
  private returnFocus?: HTMLElement | null;

  // The Popover API, typed here: the DOM typings Stencil compiles against predate it.
  private get popover() {
    return this.el as HTMLElement & { showPopover?: () => void; hidePopover?: () => void };
  }

  @Watch('open')
  onOpenChange(open: boolean) {
    if (open) this.enter();
    else this.leave();
  }

  // Another picture while open: it flies from its own thumbnail.
  @Watch('src')
  onSrcChange() {
    if (this.open) this.enter();
  }

  private cssValue(property: string): string {
    return typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue(property).trim() : '';
  }

  private durationMs(property = '--shift-image-duration'): number {
    const parsed = parseFloat(this.cssValue(property));
    return Number.isFinite(parsed) && parsed >= 0 ? parsed : DEFAULT_DURATION_MS;
  }

  /** The thumbnail as the card must match it: its picture's box and its corners. */
  private originPose(): Pose | null {
    const origin = this.origin?.isConnected ? this.origin : null;
    if (!origin || this.reducedMotion()) return null;
    const box = containedBox(origin);
    return box ? { ...box, radius: referenceRadius(origin) } : null;
  }

  /** Where the card is on screen right now, mid-flight included, so a reversal starts from there. */
  private currentPose(to: Box): Pose {
    const frame = this.frame;
    const matrix = frame && typeof DOMMatrixReadOnly === 'function' ? new DOMMatrixReadOnly(getComputedStyle(frame).transform) : null;
    if (!matrix || matrix.isIdentity) return { ...to, radius: CARD_RADIUS };
    const radius = parseFloat(getComputedStyle(frame).borderTopLeftRadius);
    return {
      left: to.left + matrix.e,
      top: to.top + matrix.f,
      width: to.width * matrix.a,
      height: to.height * matrix.d,
      radius: Number.isFinite(radius) ? radius * matrix.a : CARD_RADIUS,
    };
  }

  private stopAnimations() {
    this.flightAnimation?.cancel();
    this.fadeAnimation?.cancel();
    this.flightAnimation = undefined;
    this.fadeAnimation = undefined;
  }

  /** Runs keyframes on the card, then `done`. Where the Web Animations API is missing (tests), `done` runs after the duration. */
  private animate(frames: Keyframe[], duration: number, easing: string, done: () => void): Animation | undefined {
    const frame = this.frame;
    // Reduced motion keeps the change and makes it immediate.
    if (this.reducedMotion()) duration = 1;
    if (!frame || typeof frame.animate !== 'function') {
      this.finishTimer = setTimeout(done, duration);
      return undefined;
    }
    const animation = frame.animate(frames, { duration, easing, fill: 'both' });
    // Some environments (the test DOM) have an animate() that returns nothing.
    if (!animation) {
      this.finishTimer = setTimeout(done, duration);
      return undefined;
    }
    animation.onfinish = done;
    return animation;
  }

  private reducedMotion(): boolean {
    return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  /** Into the top layer, from <body> or from inside the modal dialog the viewer was opened from. */
  private raise() {
    clearTimeout(this.lowerTimer);
    // Moved before anything is set up: a move disconnects and reconnects the element.
    const home = this.container?.isConnected ? this.container : document.body;
    if (this.el.parentNode !== home) home.appendChild(this.el);

    if (typeof this.popover.showPopover !== 'function') return;
    if (!this.el.hasAttribute('popover')) this.el.setAttribute('popover', 'manual');
    if (!this.el.matches(':popover-open')) this.popover.showPopover();
  }

  private lower() {
    if (typeof this.popover.hidePopover === 'function' && this.el.matches(':popover-open')) this.popover.hidePopover();
  }

  /** Where the picture lands: as large as the window allows, keeping its proportions, centred between the close button's row and the caption. */
  private targetBox(ratio: number): Box {
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const gutter = viewportWidth <= NARROW_WINDOW ? NARROW_GUTTER : WIDE_GUTTER;
    const bottom = this.caption ? CAPTION_RESERVE : BOTTOM_RESERVE;
    const maxWidth = Math.max(1, viewportWidth - 2 * gutter);
    const maxHeight = Math.max(1, viewportHeight - TOP_RESERVE - bottom);

    let width = maxWidth;
    let height = width / ratio;
    if (height > maxHeight) {
      height = maxHeight;
      width = height * ratio;
    }

    return { left: (viewportWidth - width) / 2, top: TOP_RESERVE + (maxHeight - height) / 2, width, height };
  }

  private showOrigin() {
    if (this.hiddenOrigin) this.hiddenOrigin.style.visibility = '';
    this.hiddenOrigin = undefined;
  }

  private enter() {
    const frame = this.frame;
    if (!frame) return;

    const generation = ++this.generation;
    clearTimeout(this.finishTimer);
    const wasShowing = this.el.hasAttribute('data-state');
    this.raise();

    // Focus goes back to whatever opened it: the thumbnail's button, or what had focus.
    if (!wasShowing) this.returnFocus = (this.origin?.closest('button') as HTMLElement | null) ?? (document.activeElement as HTMLElement | null);

    const origin = this.origin?.isConnected ? this.origin : null;
    const ratio = origin?.naturalWidth && origin.naturalHeight ? origin.naturalWidth / origin.naturalHeight : 4 / 3;
    const to = this.targetBox(ratio);

    // Reopened while it was still closing: the card flies on from where it is now.
    const from = wasShowing && this.target ? this.currentPose(this.target) : this.originPose();
    this.stopAnimations();
    this.showOrigin();
    this.target = to;

    // The thumbnail's pixels fly at once; the full picture fades in over them when it has loaded.
    if (this.preview) this.preview.src = origin?.currentSrc || origin?.src || this.src;
    if (this.full) {
      this.full.removeAttribute('data-ready');
      this.full.src = this.src;
      if (this.full.complete && this.full.naturalWidth) this.full.setAttribute('data-ready', '');
    }

    Object.assign(frame.style, { left: `${to.left}px`, top: `${to.top}px`, width: `${to.width}px`, height: `${to.height}px` });
    if (this.captionLine) this.captionLine.style.top = `${to.top + to.height + 16}px`;

    // The thumbnail is hidden under the card, which starts exactly over it: one object lifting out.
    if (from && origin) {
      origin.style.visibility = 'hidden';
      this.hiddenOrigin = origin;
    }

    const duration = this.durationMs('--shift-image-duration');
    const easing = this.cssValue('--shift-image-ease') || DEFAULT_OPEN_EASE;
    const frames = from
      ? flightFrames(from, { ...to, radius: CARD_RADIUS }, to, 'linear')
      : [
          { transform: 'scale(0.96)', opacity: 0 },
          { transform: 'none', opacity: 1 },
        ];

    this.el.setAttribute('data-state', 'open');
    this.flightAnimation = this.animate(frames, duration, easing, () => {
      if (generation !== this.generation) return;
      // At rest the card is its stylesheet's: no transform, its own corners.
      this.stopAnimations();
      this.handlers?.opened?.();
    });

    document.addEventListener('keydown', this.onKey, true);
    this.closeButton?.focus({ preventScroll: true });
  }

  private leave() {
    const generation = ++this.generation;
    clearTimeout(this.finishTimer);
    document.removeEventListener('keydown', this.onKey, true);

    if (!this.el.hasAttribute('data-state') || !this.target) return;
    const to = this.target;
    this.el.setAttribute('data-state', 'closing');

    // From wherever the card is now (it may still be opening) back into the thumbnail where it is now
    // (the page may have moved under the viewer). The thumbnail shows again at once, under the scrim;
    // the card lands exactly on it, with its corners, and only then dissolves into it, so there is no
    // frame where one is swapped for the other and no moment where the two are seen at once.
    const from = this.currentPose(to);
    this.stopAnimations();
    this.showOrigin();
    const back = this.originPose();

    const finish = () => {
      if (generation !== this.generation) return;
      this.el.removeAttribute('data-state');
      this.stopAnimations();
      this.lower();
      // Only now: a focus ring on the thumbnail while the card is still flying back to it would arrive first.
      this.returnFocus?.focus?.({ preventScroll: true });
      this.handlers?.closed?.();
    };

    const duration = this.durationMs('--shift-image-close-duration');
    const easing = this.cssValue('--shift-image-close-ease') || DEFAULT_CLOSE_EASE;

    if (!back) {
      this.flightAnimation = this.animate(
        [
          { transform: 'none', opacity: 1 },
          { transform: 'scale(0.96)', opacity: 0 },
        ],
        duration,
        easing,
        finish,
      );
      return;
    }

    this.flightAnimation = this.animate(flightFrames(from, back, to, 'ratio'), duration, easing, () => {
      if (generation !== this.generation) return;
      const fade = this.durationMs('--shift-image-land-fade');
      const frame = this.frame;
      if (!frame || typeof frame.animate !== 'function') return finish();
      this.fadeAnimation = frame.animate([{ opacity: 1 }, { opacity: 0 }], { duration: fade, easing: 'linear', fill: 'forwards' });
      if (this.fadeAnimation) this.fadeAnimation.onfinish = finish;
      else finish();
    });
  }

  private onKey = (event: KeyboardEvent) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.handlers?.dismiss();
      return;
    }
    // One control: focus stays on it.
    if (event.key === 'Tab') {
      event.preventDefault();
      this.closeButton?.focus({ preventScroll: true });
      return;
    }
    // The page under the viewer does not scroll; the close button keeps Space and Enter.
    if (SCROLL_KEYS.has(event.key) && event.composedPath()[0] !== this.closeButton) event.preventDefault();
  };

  // The page under the viewer does not scroll. Swallowed here rather than hiding the page's scrollbar,
  // which would shift the whole page sideways by the scrollbar's width.
  private swallow = (event: Event) => event.preventDefault();

  componentDidLoad() {
    this.el.addEventListener('wheel', this.swallow, { passive: false });
    this.el.addEventListener('touchmove', this.swallow, { passive: false });
  }

  disconnectedCallback() {
    this.stopAnimations();
    clearTimeout(this.finishTimer);
    clearTimeout(this.lowerTimer);
    document.removeEventListener('keydown', this.onKey, true);
    this.showOrigin();
  }

  render() {
    return (
      <Host translate="no" role="dialog" aria-modal={this.open ? 'true' : null} aria-label={this.viewerLabel || null} aria-hidden={this.open ? null : 'true'}>
        {/* Everything sits in one stage, which carries the hidden state: the page's own rule for a
            hydrated component (`visibility: inherit`) out-ranks any visibility set on the host. */}
        <div class="stage">
          <div class="scrim" onClick={() => this.handlers?.dismiss()} />

          <div class="frame" ref={element => (this.frame = element)}>
            {/* The clip carries the corners and the white; the frame's shadow sits outside it, so it can fade on its own. */}
            <div class="clip">
              <img class="picture preview" alt="" ref={element => (this.preview = element)} />
              <img class="picture full" alt="" ref={element => (this.full = element)} onLoad={event => (event.currentTarget as HTMLElement).setAttribute('data-ready', '')} />
            </div>
          </div>

          <p class="caption" ref={element => (this.captionLine = element)} data-empty={this.caption ? null : 'true'}>
            {this.caption}
          </p>

          <button
            type="button"
            class="close"
            aria-label={this.closeLabel || null}
            title={this.closeLabel || null}
            ref={element => (this.closeButton = element)}
            onClick={() => this.handlers?.dismiss()}
          >
            <CloseIcon class="close-icon" />
          </button>
        </div>
      </Host>
    );
  }
}
