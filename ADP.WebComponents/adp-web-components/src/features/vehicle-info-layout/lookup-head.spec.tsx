import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { sharedLocalesSchema } from '~features/multi-lingual';

import { LookupHead, recordVerdict } from './lookup-head';

/**
 * The honesty rule for a record panel's verdict, and the standard head's anchors. The rule that
 * failed twice before it was written down — an empty list on an unauthorized vehicle is not green —
 * is the third case here.
 */

const locale = { ...sharedLocalesSchema.getDefault(), onRecord: 'On record', noRecords: 'No records', notInRecords: 'Not in distributor records' };

describe('recordVerdict', () => {
  it('asserts nothing before a vehicle has been looked up', () => {
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: false })).toEqual({ state: 'idle', text: '' });
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: true })).toEqual({ state: 'idle', text: '' });
  });

  it('is positive when an authorized vehicle has records', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true })).toEqual({ state: 'positive', text: 'On record' });
    // An older API that does not send isAuthorized reads as authorized, as it does everywhere else.
    expect(recordVerdict({ locale, vehicleLoaded: true, hasRecords: true })).toEqual({ state: 'positive', text: 'On record' });
  });

  it('is a statement without a verdict for a vehicle the distributor has no record of, whatever the list holds', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: false, hasRecords: false })).toEqual({ state: 'neutral', text: 'Not in distributor records' });
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: false, hasRecords: true })).toEqual({ state: 'neutral', text: 'Not in distributor records' });
  });

  it('says "no records" in grey, never green, for an authorized vehicle with nothing on file', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: false })).toEqual({ state: 'idle', text: 'No records' });
  });
});

describe('LookupHead', () => {
  const render = (verdict: Parameters<typeof LookupHead>[0]['verdict']) =>
    newSpecPage({
      components: [],
      template: () => (
        <LookupHead title="Records" verdict={verdict}>
          <button class="lookup-trace-button" type="button" />
        </LookupHead>
      ),
    });

  it('renders the title as a class on a span, the controls, then the pill last', async () => {
    const page = await render({ state: 'positive', text: 'On record' });
    const summary = page.body.querySelector('.lookup-summary');

    expect(page.body.querySelector('header.lookup-head .lookup-title')?.textContent).toBe('Records');
    expect(page.body.querySelector('h1, h2, h3, h4, h5, h6')).toBeNull();
    expect(summary?.lastElementChild?.classList.contains('status-badge')).toBe(true);
    expect(summary?.firstElementChild?.classList.contains('lookup-trace-button')).toBe(true);
    expect(summary?.querySelector('.status-badge.is-positive > span')?.textContent).toBe('On record');
    expect(summary?.querySelector('.status-badge')?.getAttribute('aria-hidden')).toBeNull();
  });

  it('keeps an empty idle pill on screen for layout but hides it from assistive readers', async () => {
    const page = await render({ state: 'idle', text: '' });
    const badge = page.body.querySelector('.status-badge');

    expect(badge?.classList.contains('is-idle')).toBe(true);
    expect(badge?.getAttribute('aria-hidden')).toBe('true');
    expect(badge?.querySelector('path')).toBeNull();
  });

  it('reads an idle pill that has words', async () => {
    const page = await render({ state: 'idle', text: 'No records' });
    const badge = page.body.querySelector('.status-badge');

    expect(badge?.getAttribute('aria-hidden')).toBeNull();
    expect(badge?.classList.contains('has-text')).toBe(true);
  });
});
