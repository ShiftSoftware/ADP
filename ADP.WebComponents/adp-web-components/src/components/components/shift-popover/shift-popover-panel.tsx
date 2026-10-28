import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import { CloseIcon } from '~assets/close-icon';

export type PopoverAppearance = 'vanilla' | 'material' | 'soft' | 'sharp' | 'minimal';
export type PopoverColorScheme = 'light' | 'dark' | 'auto';
export type PopoverSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface PopoverPanelHandlers {
  keydown: (event: KeyboardEvent) => void;
  focus: () => void;
  strayFocus: () => void;
  dismiss: () => void;
  ready: (panel: HTMLElement) => void;
  shown?: () => void;
}

@Component({
  shadow: true,
  tag: 'shift-popover-panel',
  styleUrl: 'shift-popover.css',
})
export class ShiftPopoverPanel {
  @Element() el!: HTMLElement;

  @Prop() open: boolean = false;
  @Prop() sheet: boolean = false;
  @Prop() side: 'below' | 'above' = 'below';
  @Prop() direction: 'ltr' | 'rtl' = 'ltr';
  @Prop() panelId: string = '';
  @Prop() panelLabel: string = '';
  @Prop() panelLabelledBy: string = '';
  @Prop() panelPart: string = 'panel';
  // Content stays where its page put it and the panel is shown in the browser's top layer; a sheet then has a title bar and a close button.
  @Prop() topLayer: boolean = false;
  @Prop() closeLabel: string = '';
  @Prop() focusToken: number = 0;
  @Prop() handlers?: PopoverPanelHandlers;

  @Prop({ reflect: true }) appearance?: PopoverAppearance;
  @Prop({ reflect: true }) colorScheme: PopoverColorScheme = 'light';
  @Prop({ reflect: true }) size: PopoverSize = 'md';

  @State() slotVersion: number = 0;
  @State() entered: boolean = false;

  private focusedToken = 0;
  private closingWithFocus = false;
  private presented = false;
  private enterFrame = 0;
  private leaveTimer?: ReturnType<typeof setTimeout>;

  componentWillLoad() {
    if (this.topLayer && typeof this.el.showPopover === 'function') this.el.setAttribute('popover', 'manual');
  }

  componentDidLoad() {
    this.handlers?.ready(this.el);
  }

  disconnectedCallback() {
    cancelAnimationFrame(this.enterFrame);
    clearTimeout(this.leaveTimer);
  }

  // The panel is a top-layer element only while it is on screen. Its closed style is computed and painted once before it opens, so it moves from there.
  private present() {
    clearTimeout(this.leaveTimer);
    if (this.presented) return;

    this.presented = true;
    this.el.setAttribute('data-presented', '');
    if (this.el.hasAttribute('popover') && !this.el.matches(':popover-open')) this.el.showPopover();
    void this.el.offsetWidth;
    this.handlers?.shown?.();
    this.enterFrame = requestAnimationFrame(() => (this.enterFrame = requestAnimationFrame(() => (this.entered = true))));
  }

  // Taken out of the top layer once the closing movement has finished.
  private retire() {
    cancelAnimationFrame(this.enterFrame);
    if (!this.presented || this.open) return;

    const box = this.el.shadowRoot?.querySelector('.pop-panel');
    const toMs = (value: string) => (value.trim().endsWith('ms') ? parseFloat(value) : parseFloat(value) * 1000) || 0;
    const style = box ? getComputedStyle(box) : null;
    const delays = style ? style.transitionDelay.split(',').map(toMs) : [];
    const ms = style ? Math.max(0, ...style.transitionDuration.split(',').map((duration, index) => toMs(duration) + (delays[index % delays.length] ?? 0))) : 0;

    clearTimeout(this.leaveTimer);
    this.leaveTimer = setTimeout(() => {
      if (this.open) return;
      this.presented = false;
      this.entered = false;
      this.el.removeAttribute('data-presented');
      if (this.el.hasAttribute('popover') && this.el.matches(':popover-open')) this.el.hidePopover();
    }, ms + 60);
  }

  componentWillRender() {
    this.closingWithFocus = !this.open && !!document.activeElement && (document.activeElement === this.el || this.el.contains(document.activeElement));
  }

  componentDidRender() {
    // Focus queued by the content before the panel closed can land after it; inert would drop it on <body>.
    if (this.closingWithFocus) this.handlers?.strayFocus();
    if (this.topLayer) {
      if (this.open) this.present();
      else this.retire();
      if (!this.entered) return;
    }
    if (!this.open || this.focusToken === this.focusedToken) return;

    this.focusedToken = this.focusToken;
    this.handlers?.focus();
  }

  render() {
    const scrollsInside = Array.from(this.el.children).some(child => child.localName === 'shift-select-sheet');
    const open = this.topLayer ? this.open && this.entered : this.open;

    return (
      <Host
        id={this.panelId || undefined}
        role="dialog"
        aria-label={this.panelLabel || undefined}
        aria-labelledby={this.panelLabelledBy || undefined}
        aria-hidden={this.open ? undefined : 'true'}
        inert={this.open ? undefined : true}
        data-top-layer={this.topLayer ? '' : undefined}
      >
        <div class="pop-backdrop" part="backdrop" aria-hidden="true" onClick={() => this.handlers?.dismiss()} data-open={open ? '' : undefined} data-sheet={this.sheet ? '' : undefined} />
        <div
          class="pop-panel"
          part={this.panelPart}
          dir={this.direction}
          data-open={open ? '' : undefined}
          data-sheet={this.sheet ? '' : undefined}
          data-titled={this.topLayer ? '' : undefined}
          data-side={this.side}
          data-inner-scroll={scrollsInside ? '' : undefined}
          onKeyDown={(event: KeyboardEvent) => this.handlers?.keydown(event)}
        >
          {this.topLayer ? (
            [
              <div class="pop-head" part="sheet-head">
                <span class="pop-title" part="sheet-title">
                  <slot name="heading" />
                </span>
                <button type="button" class="pop-close" part="sheet-close" aria-label={this.closeLabel || undefined} onClick={() => this.handlers?.dismiss()}>
                  <CloseIcon class="pop-close-icon" />
                </button>
              </div>,
              <div class="pop-body" part="sheet-body">
                <slot onSlotchange={() => this.slotVersion++} />
              </div>,
            ]
          ) : (
            <slot onSlotchange={() => this.slotVersion++} />
          )}
        </div>
      </Host>
    );
  }
}
