import { Component, Element, Host, Prop, h } from '@stencil/core';

import { PaintThicknessCertificateUrlDTO } from '~types/generated/vehicle-lookup/paint-thickness-certificate-url-dto';

import { CloseIcon } from '~assets/close-icon';

import { PaintRow, PaintThicknessLocale, PhotoActions, rowPhotosContent } from './components/VehiclePaintThicknessPanel';

const MENU_KEYS = ['ArrowDown', 'ArrowUp', 'Home', 'End'];

/**
 * What vehicle-paint-thickness shows in its popovers: the certificate links, or one row's photos in a
 * bottom sheet. shift-popover carries its content onto <body>, out of the panel's shadow root, so the
 * content brings its own (as shift-calendar does in a date field's popover).
 */
@Component({
  shadow: true,
  tag: 'vehicle-paint-thickness-popup',
  styleUrl: 'vehicle-paint-thickness-popup.css',
})
export class VehiclePaintThicknessPopup {
  @Element() el!: HTMLElement;

  @Prop() kind: 'menu' | 'photos' = 'menu';
  @Prop() sheet: boolean = false;
  @Prop() heading: string = '';
  @Prop() closeLabel: string = '';
  @Prop() busy: boolean = false;
  @Prop() direction: string = 'ltr';
  @Prop() links: PaintThicknessCertificateUrlDTO[] = [];
  @Prop() row?: PaintRow;
  @Prop() locale?: PaintThicknessLocale;
  @Prop() photos?: PhotoActions;
  @Prop() choose?: () => void;
  @Prop() dismiss?: () => void;

  private onMenuKeyDown = (event: KeyboardEvent) => {
    if (!MENU_KEYS.includes(event.key)) return;

    const items = Array.from(this.el.shadowRoot?.querySelectorAll<HTMLElement>('.paint-popup-item') ?? []);
    if (!items.length) return;

    event.preventDefault();
    const at = items.indexOf(this.el.shadowRoot?.activeElement as HTMLElement);
    const last = items.length - 1;
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? last : at < 0 ? 0 : (at + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
    items[next].focus({ preventScroll: true });
  };

  render() {
    const menu = this.kind === 'menu';

    return (
      <Host>
        <div class={{ 'paint-popup': true, 'loading': this.busy }} dir={this.direction} data-kind={this.kind} data-sheet={this.sheet ? 'true' : 'false'}>
          <div class="paint-popup-head">
            <span class="paint-popup-title">
              <span class={{ 'shift-skeleton': !menu }}>{this.heading}</span>
            </span>
            <button type="button" class="paint-popup-close" aria-label={this.closeLabel} onClick={() => this.dismiss?.()}>
              <CloseIcon class="paint-popup-close-icon" />
            </button>
          </div>

          {menu ? (
            <div class="paint-popup-menu" role="menu" aria-label={this.heading} inert={this.busy} onKeyDown={this.onMenuKeyDown}>
              {this.links.map(({ language: code, name, url }) => (
                <a key={code} role="menuitem" class="paint-popup-item" href={url} target="_blank" rel="noopener noreferrer" onClick={() => this.choose?.()}>
                  <span class="shift-skeleton">{name || code}</span>
                </a>
              ))}
            </div>
          ) : (
            <div class="paint-popup-photos" inert={this.busy}>
              {this.row && this.locale && this.photos ? rowPhotosContent(this.row, this.locale, this.photos, true) : null}
            </div>
          )}
        </div>
      </Host>
    );
  }
}
