import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { sharedLocalesSchema } from '~features/multi-lingual';

import { LookupHead, measurePill, recordVerdict } from './lookup-head';

/**
 * The honesty rule for a record panel's verdict, and the standard head's anchors. The rule that
 * failed twice before it was written down — an empty list on an unauthorized vehicle is not green —
 * is the third case here.
 */

const locale = { ...sharedLocalesSchema.getDefault(), onRecord: 'On record', noRecords: 'No records', notInRecords: 'Not in distributor records' };

describe('recordVerdict', () => {
  it('carries a failed lookup in the negative tone, in its own words, over anything else', () => {
    expect(recordVerdict({ locale, vehicleLoaded: true, authorized: true, hasRecords: true, error: 'Wrong response format' })).toEqual({
      state: 'negative',
      text: 'Wrong response format',
    });
    expect(recordVerdict({ locale, vehicleLoaded: false, hasRecords: false, error: 'Wrong response format' })).toEqual({ state: 'negative', text: 'Wrong response format' });
  });

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
          <button class="trace-trigger-button" type="button" />
        </LookupHead>
      ),
    });

  it('speaks a wordless positive pill through its label', async () => {
    const page = await render({ state: 'positive', text: '', label: 'On record' });
    const badge = page.body.querySelector('.status-badge');

    expect(badge?.classList.contains('has-text')).toBe(false);
    expect(badge?.getAttribute('aria-hidden')).toBeNull();
    expect(badge?.getAttribute('aria-label')).toBe('On record');
    expect(badge?.querySelector('span')?.textContent).toBe('');
  });

  it('renders the title as a class on a span, the controls, then the pill last', async () => {
    const page = await render({ state: 'positive', text: 'On record' });
    const summary = page.body.querySelector('.lookup-summary');

    expect(page.body.querySelector('header.lookup-head .lookup-title')?.textContent).toBe('Records');
    expect(page.body.querySelector('h1, h2, h3, h4, h5, h6')).toBeNull();
    expect(summary?.lastElementChild?.classList.contains('status-badge')).toBe(true);
    expect(summary?.firstElementChild?.classList.contains('trace-trigger-button')).toBe(true);
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

describe('measurePill', () => {
  /** A pill whose children report the widths given, so a font arriving can "change" them. */
  const pillOf = (doc: Document, widths: { cap: number; words: number }) => {
    const pill = doc.createElement('span');
    const cap = doc.createElement('svg');
    const words = doc.createElement('span');
    cap.getBoundingClientRect = () => ({ width: widths.cap }) as DOMRect;
    words.getBoundingClientRect = () => ({ width: widths.words }) as DOMRect;
    pill.append(cap, words);
    doc.body.appendChild(pill);
    return { pill, widths };
  };

  it('writes the natural width, cap plus words, on the pill', async () => {
    const page = await newSpecPage({ components: [], html: '<div></div>' });
    const { pill } = pillOf(page.doc, { cap: 34, words: 80 });
    measurePill(pill);
    expect(pill.style.getPropertyValue('--pill-width')).toBe('114px');
  });

  it('measures again when a web font lands after the words were measured, and forgets a pill that has left the page', async () => {
    const page = await newSpecPage({ components: [], html: '<div></div>' });
    const listeners: Array<() => void> = [];
    Object.defineProperty(page.doc, 'fonts', { value: { addEventListener: (_: string, fn: () => void) => listeners.push(fn) }, configurable: true });

    const a = pillOf(page.doc, { cap: 34, words: 80 });
    const b = pillOf(page.doc, { cap: 34, words: 60 });
    measurePill(a.pill);
    measurePill(b.pill);
    expect(listeners).toHaveLength(1);

    a.widths.words = 92;
    b.widths.words = 70;
    b.pill.remove();
    listeners[0]();
    expect(a.pill.style.getPropertyValue('--pill-width')).toBe('126px');
    expect(b.pill.style.getPropertyValue('--pill-width')).toBe('94px');

    a.widths.words = 100;
    listeners[0]();
    expect(a.pill.style.getPropertyValue('--pill-width')).toBe('134px');
  });
});
