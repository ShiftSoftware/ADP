import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';
import { VehicleServiceRequirement } from './vehicle-service-requirement';
import { VehicleClaimableItems } from './vehicle-claimable-items';
import localeData from '../../locales/vehicleLookup/claimableItems/en.json';
import sharedLocales from '../../locales/en.json';
import errors from '../../locales/errors/en.json';
import fixtures from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

const locale = { ...localeData, sharedLocales } as any;
const completed = {
  mileage: 50000,
  label: '50K',
  satisfied: true,
  satisfiedOn: '2026-02-02',
  // A supplied evidence code must never rename the expected milestone, regardless of the matcher.
  evidence: { invoiceDate: '2026-02-02T08:00:00Z', packageCode: 'PGM 45K', odometer: 49950, serviceDescription: 'Oil and filter replacement' },
};
const marker = (requirement = completed) =>
  newSpecPage({
    components: [VehicleServiceRequirement],
    template: () => <vehicle-service-requirement prerequisite={requirement} locale={locale} />,
  });

describe('required service evidence', () => {
  it('keeps the expected label and shows the distinct recorded code and invoice date in the detail', async () => {
    const page = await marker();
    const root = page.root.shadowRoot;
    expect(root.querySelector('.requirement-label').textContent).toBe('50K');
    expect(root.querySelector('.requirement-detail').textContent).toContain('PGM 45K');
    expect(root.querySelector('.requirement-detail').textContent).toContain('Invoice date');
    expect(root.querySelector('.requirement-detail').textContent).toContain('2026-02-02');
    expect(root.querySelector('.requirement-detail').textContent).toContain('49950');
    expect(root.querySelectorAll('button')).toHaveLength(1);
    expect(root.querySelector('button').getAttribute('aria-label')).toContain('Not claimable');
  });

  it('opens by clicking, stays pinned for touch reading, and closes with Escape', async () => {
    const page = await marker();
    const button = page.root.shadowRoot.querySelector('button');
    button.click();
    await page.waitForChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(page.root.shadowRoot.querySelector('.requirement-detail').getAttribute('aria-hidden')).toBe('false');
    button.dispatchEvent(new Event('pointerleave'));
    await page.waitForChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await page.waitForChanges();
    expect(button.getAttribute('aria-expanded')).toBe('false');
    // A pointer already over the fading card must not undo keyboard dismissal.
    page.root.shadowRoot.querySelector('.requirement-detail').dispatchEvent(new Event('pointerenter'));
    button.dispatchEvent(new Event('pointerenter'));
    await page.waitForChanges();
    expect(button.getAttribute('aria-expanded')).toBe('false');
    button.dispatchEvent(new Event('pointerleave'));
    button.dispatchEvent(new Event('pointerenter'));
    await page.waitForChanges();
    expect(button.getAttribute('aria-expanded')).toBe('true');
  });

  it('clamps a long evidence card to the mobile viewport and exposes it to keyboard scrolling', async () => {
    const page = await marker();
    const button = page.root.shadowRoot.querySelector('button');
    const detail = page.root.shadowRoot.querySelector('.requirement-detail') as HTMLElement;
    Object.defineProperty(page.win, 'innerWidth', { value: 360, configurable: true });
    Object.defineProperty(page.win, 'innerHeight', { value: 800, configurable: true });
    button.getBoundingClientRect = () => ({ left: -50, right: 120, bottom: 600 }) as DOMRect;
    detail.getBoundingClientRect = () => ({ height: 776 }) as DOMRect;
    button.click();
    await page.waitForChanges();
    expect(detail.style.left).toBe('12px');
    expect(detail.style.width).toBe('336px');
    expect(detail.style.top).toBe('12px');
    expect(detail.tabIndex).toBe(0);
  });

  it('does not invent evidence for pending or completed markers with missing source details', async () => {
    const page = await marker({ ...completed, satisfied: false, evidence: undefined });
    expect(page.root.shadowRoot.textContent).toContain(locale.requirementAwaiting);
    expect(page.root.shadowRoot.querySelectorAll('.requirement-fact')).toHaveLength(0);
    page.root.prerequisite = { ...completed, evidence: undefined };
    await page.waitForChanges();
    expect(page.root.shadowRoot.textContent).toContain(locale.requirementEvidenceUnavailable);
    expect(page.root.shadowRoot.querySelectorAll('.requirement-fact')).toHaveLength(0);
  });

  it('closes evidence and disables the marker during a vehicle transition', async () => {
    const page = await marker();
    page.root.shadowRoot.querySelector('button').click();
    await page.waitForChanges();
    page.root.busy = true;
    await page.waitForChanges();
    expect(page.root.shadowRoot.querySelector('button').hasAttribute('disabled')).toBe(true);
    expect(page.root.getAttribute('aria-hidden')).toBe('true');
    expect(page.root.shadowRoot.querySelector('.requirement-detail').getAttribute('aria-hidden')).toBe('true');
  });
});

describe('requirements stay outside claiming', () => {
  const data = () => {
    const lookup = structuredClone((fixtures as any)['ZT8P9NAL1LG988010']);
    lookup.serviceItems = [
      { ...lookup.serviceItems[0], group: undefined },
      { ...lookup.serviceItems[2], group: undefined, prerequisites: [completed], lock: undefined },
    ];
    return lookup;
  };
  const panel = async () => {
    (global as any).fetch = async (url: string) => ({
      ok: true,
      json: async () => (url.includes('claimableItems') ? localeData : url.includes('errors') ? errors : sharedLocales),
    });
    const page = await newSpecPage({ components: [VehicleClaimableItems, VehicleServiceRequirement], html: '<vehicle-claimable-items></vehicle-claimable-items>' });
    page.rootInstance.vehicleLookup = data();
    await page.waitForChanges();
    return page;
  };

  it('renders persistent requirements with no lock and retains them after completing a benefit claim', async () => {
    const page = await panel();
    expect(page.root.shadowRoot.querySelectorAll('.claimable-item')).toHaveLength(2);
    expect(page.root.shadowRoot.querySelectorAll('vehicle-service-requirement')).toHaveLength(1);
    const owner = page.rootInstance as VehicleClaimableItems;
    owner.selectedClaimItem = owner.vehicleLookup.serviceItems[1];
    await owner.completeClaim({});
    await page.waitForChanges();
    expect(owner.vehicleLookup.serviceItems).toHaveLength(2);
    expect(owner.vehicleLookup.serviceItems[1].status).toBe('processed');
    expect(owner.vehicleLookup.serviceItems[1].prerequisites[0].satisfied).toBe(true);
    expect(page.root.shadowRoot.querySelectorAll('vehicle-service-requirement')).toHaveLength(1);
  });

  it('opens milestone details without selecting a benefit or opening its form, and refuses it at the claim API', async () => {
    const page = await panel();
    const owner = page.rootInstance as VehicleClaimableItems;
    owner.claimForm = { open: jest.fn() } as any;
    page.root.shadowRoot.querySelector('vehicle-service-requirement').shadowRoot.querySelector('button').click();
    await page.waitForChanges();
    expect(owner.selectedClaimItem).toBeUndefined();
    await owner.claim(completed as any);
    expect(owner.claimForm.open).not.toHaveBeenCalled();
    expect(owner.selectedClaimItem).toBeUndefined();
  });

  it('supports older lock-only payloads without adding synthetic items to the claim collection', async () => {
    const page = await panel();
    const lookup = data();
    lookup.serviceItems[1].prerequisites = undefined;
    lookup.serviceItems[1].lock = { state: 'Locked', prerequisites: [completed] };
    page.rootInstance.vehicleLookup = lookup;
    await page.waitForChanges();
    expect(page.root.shadowRoot.querySelectorAll('vehicle-service-requirement')).toHaveLength(1);
    expect(page.rootInstance.vehicleLookup.serviceItems).toHaveLength(2);
  });

  it('cancels an earlier benefit hover before opening evidence and ignores its hover keepalive', async () => {
    const page = await panel();
    const owner = page.rootInstance as VehicleClaimableItems;
    const button = page.root.shadowRoot.querySelector('vehicle-service-requirement').shadowRoot.querySelector('button');
    const anchor = page.root.shadowRoot.querySelector('.claimable-item') as HTMLElement;
    const frames: FrameRequestCallback[] = [];
    const frame = jest.spyOn(global, 'requestAnimationFrame').mockImplementation(callback => frames.push(callback));
    try {
      owner.setClaimableItemPopover(true, owner.vehicleLookup.serviceItems[1], anchor);
      button.click();
      frames.splice(0).forEach(callback => callback(0));
      owner.setClaimableItemPopover(true);
      await page.waitForChanges();
      expect(button.getAttribute('aria-expanded')).toBe('true');
      expect(owner.showClaimableItemPopover).toBe(false);
      expect(owner['activeRequirement']).toBe(page.root.shadowRoot.querySelector('vehicle-service-requirement'));
      // A fresh intentional benefit hover replaces the evidence card.
      owner.setClaimableItemPopover(true, owner.vehicleLookup.serviceItems[1], anchor);
      frames.splice(0).forEach(callback => callback(0));
      // Stencil's public method proxy waits for the child readiness promise.
      await Promise.resolve();
      await page.waitForChanges();
      expect(button.getAttribute('aria-expanded')).toBe('false');
      expect(owner.showClaimableItemPopover).toBe(true);
    } finally {
      frame.mockRestore();
    }
  });
});
