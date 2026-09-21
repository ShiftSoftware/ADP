import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { sharedLocalesSchema } from '~features/multi-lingual';

import { LookupHead, recordVerdict } from './lookup-head';

/**
 * The honesty rule for a record panel's verdict, and the standard head's anchors. The rule that
 * failed twice before it was written down — an empty list on an unauthorized vehicle is not green —
 * is the third case here.
 */

const locale = { ...sharedLocalesSchema.getDefault(), noRecords: 'No records', notInRecords: 'Not in distributor records' };

describe('recordVerdict', () => {
  it('carries a failed lookup in the negative tone, in its own words, over anything else', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true, error: 'Wrong response format' })).toEqual({
      state: 'negative',
      text: 'Wrong response format',
      accent: 'negative',
    });
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: false, error: 'Wrong response format' })).toEqual({
      state: 'negative',
      text: 'Wrong response format',
      accent: 'negative',
    });
  });

  it('asserts nothing before a vehicle has been looked up', () => {
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: false })).toEqual({ state: 'idle', text: '', accent: 'idle' });
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: true })).toEqual({ state: 'idle', text: '', accent: 'idle' });
  });

  it('says nothing in the pill when an authorized vehicle has records — the records are the statement — and goes green on the bar', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true })).toEqual({ state: 'idle', text: '', accent: 'positive' });
    // An older API that does not send isAuthorized reads as authorized, as it does everywhere else.
    expect(recordVerdict({ locale, vehicleLoaded: true, hasRecords: true })).toEqual({ state: 'idle', text: '', accent: 'positive' });
  });

  it("colours the bar from the panel's own judgement of its records, when it has one", () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true, recordsVerdict: 'negative' })).toEqual({ state: 'idle', text: '', accent: 'negative' });
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true, recordsVerdict: 'positive' })).toEqual({ state: 'idle', text: '', accent: 'positive' });
  });

  it('ignores the records judgement in every state where the pill has something to say', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: false, hasRecords: true, recordsVerdict: 'negative' }).accent).toBe('neutral');
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: false, recordsVerdict: 'negative' }).accent).toBe('idle');
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: true, recordsVerdict: 'negative' }).accent).toBe('idle');
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true, error: 'Wrong response format', recordsVerdict: 'positive' }).accent).toBe('negative');
  });

  it('is a statement without a verdict for a vehicle the distributor has no record of, whatever the list holds', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: false, hasRecords: false })).toEqual({
      state: 'neutral',
      text: 'Not in distributor records',
      accent: 'neutral',
    });
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: false, hasRecords: true })).toEqual({
      state: 'neutral',
      text: 'Not in distributor records',
      accent: 'neutral',
    });
  });

  it('says "no records" in grey, never green, for an authorized vehicle with nothing on file', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: false })).toEqual({ state: 'idle', text: 'No records', accent: 'idle' });
  });
});

describe('LookupHead', () => {
  const render = (verdict: Parameters<typeof LookupHead>[0]['verdict']) =>
    newSpecPage({
      components: [],
      template: () => (
        <LookupHead title="Records" verdict={verdict}>
          <button class="trace-trigger-button" type="button" />
        </LookupHead>
      ),
    });

  it('renders the title as a class on a span, the controls, then the pill last', async () => {
    const page = await render({ state: 'negative', text: 'Wrong response format' });
    const summary = page.body.querySelector('.lookup-summary');

    expect(page.body.querySelector('header.lookup-head .lookup-title')?.textContent).toBe('Records');
    expect(page.body.querySelector('h1, h2, h3, h4, h5, h6')).toBeNull();
    expect(summary?.lastElementChild?.classList.contains('status-badge')).toBe(true);
    expect(summary?.firstElementChild?.classList.contains('trace-trigger-button')).toBe(true);
    expect(summary?.querySelector('.status-badge.is-negative > span')?.textContent).toBe('Wrong response format');
    expect(summary?.querySelector('.status-badge')?.getAttribute('aria-hidden')).toBeNull();
  });

  it('renders no pill when there is nothing to say — the summary keeps the row, not a shape', async () => {
    const page = await render({ state: 'idle', text: '' });

    expect(page.body.querySelector('.status-badge')).toBeNull();
    expect(page.body.querySelector('.lookup-summary')).not.toBeNull();
  });

  it('renders an idle pill that has words, without a glyph', async () => {
    const page = await render({ state: 'idle', text: 'No records' });
    const badge = page.body.querySelector('.status-badge');

    expect(badge?.classList.contains('is-idle')).toBe(true);
    expect(badge?.getAttribute('aria-hidden')).toBeNull();
    expect(badge?.querySelector('path')).toBeNull();
    expect(badge?.querySelector('span')?.textContent).toBe('No records');
  });
});
