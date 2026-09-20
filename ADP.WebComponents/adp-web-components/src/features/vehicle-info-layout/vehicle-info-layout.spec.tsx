import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { VehicleInfoLayout } from './vehicle-info-layout';

/**
 * The wrapper's structure: what a panel can rely on being there in every state. Jest pins the
 * anchors; the motion between them is reviewed with screenshots (motion.md).
 */

type Props = Parameters<typeof VehicleInfoLayout>[0];

const render = (props: Partial<Props>) =>
  newSpecPage({
    components: [],
    template: () => (
      <VehicleInfoLayout isError={false} isLoading={false} direction="ltr" errorMessage="Something went wrong." {...props}>
        <p class="panel-content">content</p>
      </VehicleInfoLayout>
    ),
  });

describe('VehicleInfoLayout', () => {
  it('draws one card with an idle accent, the VIN for screen readers only, and the error band shut', async () => {
    const page = await render({ header: 'SAMPLE-VIN' });
    const card = page.body.querySelector('.lookup-card');

    expect(page.body.querySelector('.lookup-panel')?.getAttribute('dir')).toBe('ltr');
    expect(card?.getAttribute('data-verdict')).toBe('idle');
    expect(card?.getAttribute('data-phase')).toBe('settled');
    expect(card?.getAttribute('part')).toBe('vehicle-info-container');

    // The identifier is not a visible row: it is a screen-reader-only span that keeps the class the panels' specs query.
    const vin = page.body.querySelector('.vehicle-info-header-vin');
    expect(vin?.textContent).toBe('SAMPLE-VIN');
    expect(vin?.classList.contains('sr-only')).toBe(true);
    expect(page.body.querySelector('.vehicle-info-header')).toBeNull();

    // The error band is an anchor: mounted shut, hidden from assistive readers, its text ready to slide in.
    const error = page.body.querySelector('.lookup-error');
    expect(error?.classList.contains('collapsible')).toBe(true);
    expect(error?.getAttribute('data-open')).toBe('false');
    expect(error?.getAttribute('aria-hidden')).toBe('true');
    expect(error?.querySelector('[role="alert"]')).not.toBeNull();

    expect(page.body.querySelector('.lookup-body .lookup-content .panel-content')).not.toBeNull();
    expect(page.body.querySelector('.lookup-core')).toBeNull();
  });

  it('colours the accent from the verdict it is handed', async () => {
    for (const verdict of ['positive', 'negative', 'neutral', 'attention'] as const) {
      const page = await render({ verdict });
      expect(page.body.querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe(verdict);
    }
  });

  it('is busy while loading: the loading class on its outermost element, and the card phase for the accent', async () => {
    const page = await render({ isLoading: true, verdict: 'positive' });

    expect(page.body.querySelector('.lookup-panel')?.classList.contains('loading')).toBe(true);
    expect(page.body.querySelector('.lookup-card')?.getAttribute('data-phase')).toBe('busy');
  });

  it('opens the error band on the translated message and leaves the content in place beneath it', async () => {
    const page = await render({ isError: true, errorMessage: 'The VIN is not valid.' });
    const error = page.body.querySelector('.lookup-error');

    expect(error?.getAttribute('data-open')).toBe('true');
    expect(error?.getAttribute('aria-hidden')).toBeNull();
    expect(error?.querySelector('.lookup-error-text')?.textContent).toBe('The VIN is not valid.');
    expect(error?.querySelector('.notice-icon')).not.toBeNull();
    expect(page.body.querySelector('.panel-content')).not.toBeNull();
  });

  it('renders only the core, with the loading class, when embedded', async () => {
    const page = await render({ coreOnly: true, isLoading: true, isError: true, header: 'SAMPLE-VIN' });
    const core = page.body.querySelector('.lookup-core');

    expect(core?.classList.contains('loading')).toBe(true);
    expect(core?.getAttribute('part')).toBe('vehicle-info-content');
    expect(core?.querySelector('.panel-content')).not.toBeNull();
    // The card, its error band and the identifier belong to whatever draws the card.
    expect(page.body.querySelector('.lookup-card')).toBeNull();
    expect(page.body.querySelector('.lookup-error')).toBeNull();
    expect(page.body.querySelector('.vehicle-info-header-vin')).toBeNull();
  });
});
