import { FunctionalComponent, h } from '@stencil/core';

import { VerdictState } from './interface';

type VehicleInfoLayoutProps = {
  /** The vehicle identifier — the VIN. Read by screen readers only; the panel's own head is the visible head. */
  header?: string;
  /** A lookup failed: the accent goes negative whatever the verdict; the panel's own head says why. */
  isError: boolean;
  direction: string;
  /** Renders the children alone, for a panel embedded in a wrapper that draws the card itself. */
  coreOnly?: boolean;
  isLoading: boolean;
  /** Colours the accent bar. The panel passes its own verdict when standalone; a composite passes the active panel's. */
  verdict?: VerdictState;
};

/**
 * The card of the component design language, drawn once for every vehicle-lookup panel: a paper
 * surface with a 4px accent bar carrying the panel's verdict and the panel's own head, strip and
 * body inside. The card fills the element edge to edge; the host gives
 * the shadow whatever room it wants around it. Everything else — title, pill, strip, body — is the
 * panel's, and the card never carries a
 * visible identifier row; the VIN is a screen-reader-only span so an assistive reader knows which
 * vehicle the card is about.
 *
 * Two modes. Standalone, the card. With `coreOnly`, only the children, inside one div that carries
 * the `loading` class — every in-flight rule (the head's leave, a panel's own) is a `.loading .x`
 * descendant selector inside its own shadow root, and this element is what keeps those alive
 * whichever mode the panel is in. In both modes `loading` sits on the outermost element the wrapper
 * renders.
 *
 * An error has no band of its own (owner, 2026-09-20): the accent goes negative and the panel's own
 * verdict pill carries the translated message, in the negative tone, where the verdict would be.
 */
/**
 * The head's stand-in while a lookup is in flight: a spinner that rises into the band as the
 * head's content drops out of it, and sinks back out as the content returns (the wrapper's
 * `.lookup-head-wait` rules in vehicle-info-layout.css). Every head band renders it as its last
 * child in every state — an anchor, parked under the band's clip at rest — so it is never mounted
 * or unmounted. Decorative: the panel's own status regions say what is happening in words.
 */
export const LookupHeadWait: FunctionalComponent = () => <span class="lookup-head-wait" aria-hidden="true" />;

export const VehicleInfoLayout: FunctionalComponent<VehicleInfoLayoutProps> = (props, children) => {
  if (props.coreOnly)
    return (
      <div dir={props.direction} class={{ 'lookup-core': true, 'loading': props.isLoading }} part="vehicle-info-content">
        {children}
      </div>
    );

  return (
    <div dir={props.direction} class={{ 'lookup-panel': true, 'loading': props.isLoading }}>
      <section
        class="lookup-card"
        part="vehicle-info-container"
        data-phase={props.isLoading ? 'busy' : 'settled'}
        data-verdict={props.isError ? 'negative' : (props.verdict ?? 'idle')}
        data-error={props.isError ? 'true' : null}
      >
        <span class="vehicle-info-header-vin sr-only" part="vehicle-info-header-vin">
          {props.header ?? ''}
        </span>

        <div class="lookup-body" part="vehicle-info-body">
          <div class="lookup-content" part="vehicle-info-content">
            {children}
          </div>
        </div>
      </section>
    </div>
  );
};
