import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, h } from '@stencil/core';
import { InferType } from 'yup';

import imageViewerSchema from '~locales/imageViewer/type';
import { LanguageKeys } from '~features/multi-lingual';

import ar from '../../../locales/imageViewer/ar.json';
import en from '../../../locales/imageViewer/en.json';
import ku from '../../../locales/imageViewer/ku.json';
import ru from '../../../locales/imageViewer/ru.json';

import { ImageLayerHandlers } from './shift-image-layer';

type ImageViewerStrings = InferType<typeof imageViewerSchema>;

const STRINGS: Record<string, ImageViewerStrings> = { en, ar, ku, ru };

/** The open modal <dialog> the element sits in, through any shadow roots, or null. */
const modalDialogAround = (element: Element): HTMLElement | null => {
  for (let node: Node | null = element; node; node = node.parentNode ?? (node as ShadowRoot).host ?? null) {
    if (node instanceof HTMLElement && node.tagName === 'DIALOG') {
      try {
        if (node.matches(':modal')) return node;
      } catch {
        if (node.hasAttribute('open')) return node;
      }
    }
  }
  return null;
};

export type ImageViewerShowOptions = {
  /** The thumbnail the picture grows out of and shrinks back into: an <img> element, or a CSS selector for one. Defaults to this element's own thumbnail. */
  origin?: HTMLImageElement | string | null;
  /** The dialog's accessible name for this picture, e.g. "Open picture of 08471-00960". */
  label?: string;
  /** A line shown under the picture, e.g. "08471-00960 · Side steps". Defaults to `alt`. */
  caption?: string;
};

export type ImageViewerChangeDetail = { open: boolean; src: string };

/**
 * A picture that opens full-window. Two ways to use it:
 *
 * - **Automatic** (most uses): `<shift-image src="thumb.png" full-src="large.png" alt="…">` draws the
 *   thumbnail; a click opens the viewer on `full-src` (or `src`). A click handler of the page's own
 *   that calls `event.preventDefault()` stops it opening, for a page that wants to handle the click
 *   itself without the `manual` flag.
 * - **Manual**: with `manual`, a click does nothing and the page opens and closes it through a
 *   reference: `show(src, { origin })` and `hide()`. Without `src` the element draws nothing at all
 *   and is only a viewer for the page to drive, which is how the vehicle-lookup panels use it.
 *
 * `disabled` stops it opening by either way. `beforeOpen` and `beforeClose` can be cancelled with
 * `preventDefault()`; `afterOpen` and `afterClose` fire once the movement has finished. Escape, the
 * close button or a click outside the picture closes it. From Blazor,
 * call the methods and listen to the events through JS interop.
 *
 * The viewer is a layer portaled onto <body> (`shift-portal`, as `shift-popover` portals its panel),
 * or into the open modal <dialog> it was opened from, and it rises into the browser's top layer while
 * open: no container, transform, overflow or dialog on the page can trap, clip or cover it. The
 * picture flies out of its thumbnail and back on `transform` alone (shift-image-layer.tsx), so it
 * stays smooth on a slow machine.
 *
 * @event beforeOpen - About to open on `{ src }`; `preventDefault()` keeps it closed.
 * @event afterOpen - Fully open on `{ src }`.
 * @event beforeClose - About to close `{ src }`; `preventDefault()` keeps it open.
 * @event afterClose - Fully closed after showing `{ src }`.
 * @event viewerChange - Opened or closed, the moment it happens: `{ open, src }`.
 *
 * @part trigger - The button around the thumbnail (automatic use).
 * @part image - The thumbnail.
 */
@Component({
  shadow: true,
  tag: 'shift-image',
  styles: `
    :host {
      display: none;
    }
    :host([src]) {
      display: inline-block;
      max-width: 100%;
      vertical-align: middle;
    }
    .trigger {
      display: block;
      padding: 0;
      margin: 0;
      color: inherit;
      font: inherit;
      background: none;
      border: 0;
      border-radius: inherit;
      cursor: zoom-in;
    }
    .trigger:disabled,
    :host([manual]) .trigger {
      cursor: default;
    }
    .trigger:focus-visible {
      outline: 2px solid currentColor;
      outline-offset: 2px;
    }
    .image {
      display: block;
      max-width: 100%;
      height: auto;
      border-radius: inherit;
    }
  `,
})
export class ShiftImage {
  @Element() el!: HTMLElement;

  /** The thumbnail to draw. Without it the element draws nothing and is opened from code only. */
  @Prop({ reflect: true }) src?: string;
  /** The picture the viewer shows. Defaults to `src`. */
  @Prop() fullSrc?: string;
  /** The thumbnail's alternative text. Also names the button that opens it. */
  @Prop() alt: string = '';
  /** A click does not open it: the page opens it with `show()`. */
  @Prop({ reflect: true }) manual: boolean = false;
  /** Never opens, by click or by `show()`. */
  @Prop({ reflect: true }) disabled: boolean = false;
  /** The language of the accessible names. */
  @Prop() language: LanguageKeys = 'en';
  /** Overrides the localized accessible name of the close button. */
  @Prop() closeLabel?: string;
  /** Overrides the localized accessible name of the dialog, for every picture. */
  @Prop() label?: string;

  @State() isOpen: boolean = false;
  @State() current: string = '';
  @State() origin: HTMLImageElement | null = null;
  @State() pictureLabel: string = '';
  @State() caption: string = '';
  /** Where the layer lives while open (see shift-image-layer `container`). */
  @State() container: HTMLElement | null = null;

  @Event({ bubbles: true, composed: true, cancelable: true }) beforeOpen!: EventEmitter<{ src: string }>;
  @Event({ bubbles: true, composed: true }) afterOpen!: EventEmitter<{ src: string }>;
  @Event({ bubbles: true, composed: true, cancelable: true }) beforeClose!: EventEmitter<{ src: string }>;
  @Event({ bubbles: true, composed: true }) afterClose!: EventEmitter<{ src: string }>;
  @Event({ bubbles: true, composed: true }) viewerChange!: EventEmitter<ImageViewerChangeDetail>;

  private thumbnail?: HTMLImageElement;

  // Stencil bundles a portaled tag only when it also sees it in a template; the branch that names it never renders.
  private layerInTemplate = false;

  /**
   * Opens the viewer on `src` (by default the element's own picture), growing out of `origin` (by
   * default its own thumbnail). The same picture again while open is ignored, as before. Resolves
   * true when it opened.
   */
  @Method()
  async show(src?: string, options: ImageViewerShowOptions = {}): Promise<boolean> {
    const picture = src || this.fullSrc || this.src;
    if (!picture || this.disabled) return false;
    if (this.isOpen && this.current === picture) return false;
    if (this.beforeOpen.emit({ src: picture }).defaultPrevented) return false;

    const origin = typeof options.origin === 'string' ? (document.querySelector(options.origin) as HTMLImageElement | null) : (options.origin ?? this.thumbnail ?? null);

    this.origin = origin;
    this.container = modalDialogAround(this.el);
    this.pictureLabel = options.label ?? '';
    this.caption = options.caption ?? this.alt;
    this.current = picture;
    this.isOpen = true;
    this.viewerChange.emit({ open: true, src: picture });
    return true;
  }

  /** Closes the viewer: the picture shrinks back into its thumbnail. Resolves true when it closed. */
  @Method()
  async hide(): Promise<boolean> {
    if (!this.isOpen) return false;
    const picture = this.current;
    if (this.beforeClose.emit({ src: picture }).defaultPrevented) return false;

    this.isOpen = false;
    this.viewerChange.emit({ open: false, src: picture });
    return true;
  }

  /** Whether the viewer is open. */
  @Method()
  async opened(): Promise<boolean> {
    return this.isOpen;
  }

  // Automatic use. Decided after the click has reached every listener, so a page's own click handler
  // that calls preventDefault() wins whichever was attached first.
  private onClick = (event: MouseEvent) => {
    if (this.manual || this.disabled) return;
    setTimeout(() => {
      if (!event.defaultPrevented) void this.show();
    });
  };

  // afterOpen / afterClose come from the layer, when the movement has really finished.
  private handlers: ImageLayerHandlers = {
    dismiss: () => void this.hide(),
    opened: () => this.afterOpen.emit({ src: this.current }),
    closed: () => this.afterClose.emit({ src: this.current }),
  };

  render() {
    const strings = STRINGS[this.language] ?? STRINGS.en;

    // `open` last: the layer reads the thumbnail and the picture when it opens, and the portal sets these in order.
    const layerProps = {
      origin: this.origin,
      src: this.current,
      caption: this.caption,
      viewerLabel: this.pictureLabel || this.label || this.alt || strings.dialog,
      closeLabel: this.closeLabel || strings.close,
      handlers: this.handlers,
      container: this.container,
      open: this.isOpen,
    };

    return (
      <Host translate="no">
        {this.src && (
          <button
            type="button"
            class="trigger"
            part="trigger"
            disabled={this.disabled}
            aria-label={this.alt ? `${strings.open}: ${this.alt}` : strings.open}
            aria-haspopup="dialog"
            onClick={this.onClick}
          >
            <img class="image" part="image" src={this.src} alt={this.alt} ref={element => (this.thumbnail = element)} />
          </button>
        )}
        {this.layerInTemplate && <shift-image-layer />}
        {/* inheritedClasses: the portal writes the layer's class list, and Stencil keeps a component
            hidden until it carries `hydrated`, so the class is passed through (as shift-popover does). */}
        <shift-portal tag="shift-image-layer" inheritedClasses="hydrated" componentProps={layerProps} />
      </Host>
    );
  }
}
