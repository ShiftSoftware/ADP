import { FunctionalComponent, h } from '@stencil/core';

// The glyph set is the design language's shared vocabulary and lives with the panels that first
// drew it; the wrapper reads the same path data so the error band's glyph is the panels' glyph.
import { BADGE_GLYPHS } from '../../components/vehicle-lookup/components/glyphs';

import { VerdictState } from './interface';

type VehicleInfoLayoutProps = {
  /** The vehicle identifier — the VIN. Read by screen readers only; the panel's own head is the visible head. */
  header?: string;
  isError: boolean;
  direction: string;
  /** Renders the children alone, for a panel embedded in a wrapper that draws the card itself. */
  coreOnly?: boolean;
  isLoading: boolean;
  /** Already translated; shown in the error band while `isError`. */
  errorMessage: string;
  /** Colours the accent bar. The panel passes its own verdict when standalone; a composite passes the active panel's. */
  verdict?: VerdictState;
  /**
   * @deprecated There is no identifier band to place anything in. Ignored; a panel renders its
   * controls in its own head. Kept only until the last caller has moved (claimable-items, then
   * the composite), and deleted with them.
   */
  headerRight?: unknown;
};

/**
 * The card of the component design language, drawn once for every vehicle-lookup panel: a paper
 * surface with a 4px accent bar carrying the panel's verdict, the error band beneath it, and the
 * panel's own head, strip and body inside. The panel's wrapper is one padding, so the shadow has
 * room and a host that sets the element `display:block` at `width:100%` gets a card with breathing
 * space. Everything else — title, pill, strip, body — is the panel's, and the card never carries a
 * visible identifier row; the VIN is a screen-reader-only span so an assistive reader knows which
 * vehicle the card is about.
 *
 * Two modes. Standalone, the card. With `coreOnly`, only the children, inside one div that carries
 * the `loading` class — every panel's skeleton is a `.loading .x` descendant selector inside its own
 * shadow root, and this element is what keeps those alive whichever mode the panel is in. In both
 * modes `loading` sits on the outermost element the wrapper renders.
 *
 * The error band is a `.collapsible` that is always mounted and slides open when `isError`; the text
 * stays rendered while it shuts so it leaves with the band. While it is open the panel below is in
 * its idle state (the owner holds no vehicle), which is correct: an error is not a verdict.
 */
export const VehicleInfoLayout: FunctionalComponent<VehicleInfoLayoutProps> = (props, children) => {
  if (props.coreOnly)
    return (
      <div class={{ 'lookup-core': true, 'loading': props.isLoading }} part="vehicle-info-content">
        {children}
      </div>
    );

  return (
    <div dir={props.direction} class={{ 'lookup-panel': true, 'loading': props.isLoading }}>
      <section class="lookup-card" part="vehicle-info-container" data-phase={props.isLoading ? 'busy' : 'settled'} data-verdict={props.verdict ?? 'idle'}>
        <span class="vehicle-info-header-vin sr-only" part="vehicle-info-header-vin">
          {props.header ?? ''}
        </span>

        <div class="lookup-error collapsible" part="vehicle-info-error" data-open={props.isError ? 'true' : 'false'} aria-hidden={props.isError ? null : 'true'}>
          <div class="collapsible-body">
            <div class="lookup-error-notice" role="alert">
              <svg class="notice-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
                <path fill="currentColor" d={BADGE_GLYPHS.negative} />
              </svg>
              <span class="lookup-error-text">{props.errorMessage}</span>
            </div>
          </div>
        </div>

        <div class="lookup-body" part="vehicle-info-body">
          <div class="lookup-content" part="vehicle-info-content">
            {children}
          </div>
        </div>
      </section>
    </div>
  );
};
