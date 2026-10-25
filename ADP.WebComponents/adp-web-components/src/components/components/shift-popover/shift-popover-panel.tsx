import { Component, Element, Host, Prop, State, h } from '@stencil/core';

export type PopoverAppearance = 'vanilla' | 'material' | 'soft' | 'sharp' | 'minimal';
export type PopoverColorScheme = 'light' | 'dark' | 'auto';
export type PopoverSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface PopoverPanelHandlers {
  keydown: (event: KeyboardEvent) => void;
  focus: () => void;
  strayFocus: () => void;
  dismiss: () => void;
  ready: (panel: HTMLElement) => void;
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
  @Prop() focusToken: number = 0;
  @Prop() handlers?: PopoverPanelHandlers;

  @Prop({ reflect: true }) appearance?: PopoverAppearance;
  @Prop({ reflect: true }) colorScheme: PopoverColorScheme = 'light';
  @Prop({ reflect: true }) size: PopoverSize = 'md';

  @State() slotVersion: number = 0;

  private focusedToken = 0;
  private closingWithFocus = false;

  componentDidLoad() {
    this.handlers?.ready(this.el);
  }

  componentWillRender() {
    this.closingWithFocus = !this.open && !!document.activeElement && (document.activeElement === this.el || this.el.contains(document.activeElement));
  }

  componentDidRender() {
    // Focus queued by the content before the panel closed can land after it; inert would drop it on <body>.
    if (this.closingWithFocus) this.handlers?.strayFocus();
    if (!this.open || this.focusToken === this.focusedToken) return;

    this.focusedToken = this.focusToken;
    this.handlers?.focus();
  }

  render() {
    const scrollsInside = Array.from(this.el.children).some(child => child.localName === 'shift-select-sheet');

    return (
      <Host
        id={this.panelId || undefined}
        role="dialog"
        aria-label={this.panelLabel || undefined}
        aria-labelledby={this.panelLabelledBy || undefined}
        aria-hidden={this.open ? undefined : 'true'}
        inert={this.open ? undefined : true}
      >
        <div class="pop-backdrop" part="backdrop" aria-hidden="true" onClick={() => this.handlers?.dismiss()} data-open={this.open ? '' : undefined} data-sheet={this.sheet ? '' : undefined} />
        <div
          class="pop-panel"
          part={this.panelPart}
          dir={this.direction}
          data-open={this.open ? '' : undefined}
          data-sheet={this.sheet ? '' : undefined}
          data-side={this.side}
          data-inner-scroll={scrollsInside ? '' : undefined}
          onKeyDown={(event: KeyboardEvent) => this.handlers?.keydown(event)}
        >
          <slot onSlotchange={() => this.slotVersion++} />
        </div>
      </Host>
    );
  }
}
