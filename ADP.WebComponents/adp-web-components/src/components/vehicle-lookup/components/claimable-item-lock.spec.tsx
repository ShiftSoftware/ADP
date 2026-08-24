import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import claimableItemsLocale from '../../../locales/vehicleLookup/claimableItems/en.json';
import sharedLocale from '../../../locales/en.json';
import standardDealerVehicleLookup from '../../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import type { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { ClaimableItem } from './claimable-item';
import { ClaimableItemPopover } from './claimable-item-popover';

const locale = { ...claimableItemsLocale, sharedLocales: sharedLocale } as any;

const serviceItems = (standardDealerVehicleLookup as any)['JTMHX01J8L4198293'].serviceItems as VehicleServiceItemDTO[];

/**
 * The locked reward the generator produces: one prerequisite performed, one outstanding. Read from
 * the generated mock rather than hand-built, so these assertions are about what the evaluator
 * actually emits.
 */
const lockedReward = serviceItems.find(item => item.serviceItemID === 'SI-006');
const offeredItem = serviceItems.find(item => item.serviceItemID === 'SI-005');

/** Claimed, so every field is populated — the only state that fills the grid. */
const claimedItem = serviceItems.find(item => item.serviceItemID === 'SI-001');

const missedReward = { ...lockedReward, lock: { ...lockedReward.lock, state: 'Missed' as const } };

const renderCard = (item: VehicleServiceItemDTO) =>
  newSpecPage({
    components: [],
    template: () => <ClaimableItem item={item} locale={locale} addStatusClass setClaimableItemPopover={() => {}} />,
  });

const renderPopover = (item: VehicleServiceItemDTO, target = { centerX: 200, topY: 100, bottomY: 140 }, popoverHeight = 200) =>
  newSpecPage({
    components: [],
    template: () => (
      <ClaimableItemPopover
        locale={locale}
        item={item}
        showPopover
        target={target}
        popoverHeight={popoverHeight}
        fadingOut={false}
        swapping={false}
        layerKey={0}
        swapMs={500}
        bodyContentHeight={0}
        claim={() => {}}
        rootRef={() => {}}
        onMouseEnter={() => {}}
        onMouseLeave={() => {}}
      />
    ),
  });

const bodyOf = (page: { body: HTMLElement }) => page.body.querySelector('.popover-body') as HTMLElement;
const containerOf = (page: { body: HTMLElement }) => page.body.querySelector('.claimable-item-popover') as HTMLElement;

const specRowCount = (page: { body: HTMLElement }) => page.body.querySelectorAll('.popover-specs').length;

describe('locked and missed reward cards', () => {
  it('the generated mock carries a locked reward with one prerequisite outstanding', () => {
    expect(lockedReward).toBeDefined();
    expect(lockedReward.lock.state).toBe('Locked');
    expect(lockedReward.claimable).toBe(false);
    expect(lockedReward.expiresAt).toBeUndefined();
    expect(lockedReward.lock.prerequisites.map(p => [p.label, p.satisfied])).toEqual([
      ['35K', true],
      ['40K', false],
    ]);
  });

  it('shows the lock state instead of the status underneath it', async () => {
    const page = await renderCard(lockedReward);
    const card = page.body.querySelector('.claimable-item');

    // The item is pending underneath — saying so would tell a customer it is theirs to claim.
    expect(lockedReward.status).toBe('pending');
    expect(card.classList.contains('lock-Locked')).toBe(true);
    expect(card.classList.contains('pending')).toBe(false);
    expect(card.querySelector('.claimable-item-header').textContent).toContain('Locked');
    expect(card.querySelector('.claimable-item-lock-icon')).not.toBeNull();
  });

  it('distinguishes a missed reward from a locked one', async () => {
    const page = await renderCard(missedReward);
    const card = page.body.querySelector('.claimable-item');

    expect(card.classList.contains('lock-Missed')).toBe(true);
    expect(card.querySelector('.claimable-item-header').textContent).toContain('Missed');
  });

  it('leaves an offered item exactly as it was', async () => {
    const page = await renderCard(offeredItem);
    const card = page.body.querySelector('.claimable-item');

    expect(card.classList.contains('pending')).toBe(true);
    expect(card.querySelector('.claimable-item-lock-icon')).toBeNull();
    expect(card.querySelector('.claimable-item-status-icon')).not.toBeNull();
  });

  it('lists each prerequisite with its own tick, and the date the done one happened', async () => {
    const page = await renderPopover(lockedReward);
    const rows = [...page.body.querySelectorAll('.popover-prerequisite')];

    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('35K');
    expect(rows[0].classList.contains('satisfied')).toBe(true);
    expect(rows[0].querySelector('svg')).not.toBeNull();
    expect(rows[0].querySelector('.popover-prerequisite-date').textContent).not.toBe('');

    expect(rows[1].textContent).toContain('40K');
    expect(rows[1].classList.contains('satisfied')).toBe(false);
    expect(rows[1].querySelector('svg')).toBeNull();
    expect(rows[1].querySelector('.popover-prerequisite-date').textContent).toBe('');
  });

  it('offers no claim button while locked', async () => {
    const page = await renderPopover(lockedReward);

    expect(page.body.querySelector('.claim-button')).toBeNull();
    expect(page.body.querySelector('.popover-lock .lab').textContent).not.toBe('');
  });

  it('drops the rows a locked item has nothing to put in', async () => {
    // Captured before the next render: spec pages share one document, so a body read after a second
    // render is the second render's.
    const labelsOf = (page: { body: HTMLElement }) => [...page.body.querySelectorAll('.popover-spec .lab')].map(el => el.textContent);

    const lockedLabels = labelsOf(await renderPopover(lockedReward));
    const pendingLabels = labelsOf(await renderPopover(offeredItem));
    const claimedLabels = labelsOf(await renderPopover(claimedItem));

    // Claimed At, Claiming Company, Invoice Number and Job Number describe a claim that has not
    // happened; Expiry Date does not start until the item unlocks.
    for (const absent of [
      claimableItemsLocale.claimAt,
      claimableItemsLocale.claimingCompany,
      claimableItemsLocale.invoiceNumber,
      claimableItemsLocale.jobNumber,
      claimableItemsLocale.expireDate,
    ])
      expect(lockedLabels).not.toContain(absent);

    // What it does know is still there.
    expect(lockedLabels).toContain(claimableItemsLocale.serviceType);
    expect(lockedLabels).toContain(claimableItemsLocale.activationDate);

    // The rule is about the value, not the state: a pending item has never been claimed either, so
    // its claim details go the same way.
    expect(pendingLabels).not.toContain(claimableItemsLocale.claimAt);
    expect(pendingLabels).not.toContain(claimableItemsLocale.jobNumber);
    expect(pendingLabels).toContain(claimableItemsLocale.expireDate);

    // A claimed item has all of it, and keeps all of it.
    expect(claimedLabels).toHaveLength(8);
    expect(lockedLabels.length).toBeLessThan(claimedLabels.length);
  });

  it('shows neither lock block nor prerequisites on an offered item', async () => {
    const page = await renderPopover(offeredItem);

    expect(page.body.querySelector('.popover-lock')).toBeNull();
    expect(page.body.querySelector('.claim-button')).not.toBeNull();
  });
});

/**
 * The popover used to flip above whenever it did not fit below, without checking that above had room
 * either — so a tall card clipped off the top of the window with nothing to scroll. These pin the
 * rule that replaced it: open to the side with more room, and never claim more height than is there.
 */
describe('popover placement against the viewport', () => {
  const viewportHeight = () => window.innerHeight;

  it('opens downward when there is room below', async () => {
    const page = await renderPopover(lockedReward, { centerX: 200, topY: 40, bottomY: 80 }, 200);

    expect(containerOf(page).style.bottom).toBe('auto');
    expect(containerOf(page).style.top).not.toBe('auto');
  });

  it('flips upward when the room below cannot hold it and above can', async () => {
    const nearBottom = viewportHeight() - 60;
    const page = await renderPopover(lockedReward, { centerX: 200, topY: nearBottom - 40, bottomY: nearBottom }, 400);

    expect(containerOf(page).style.top).toBe('auto');
    expect(containerOf(page).style.bottom).not.toBe('auto');
  });

  it('stays below rather than flipping into even less room', async () => {
    // Fits neither side, but below is the larger of the two. Flipping here would clip harder.
    const page = await renderPopover(lockedReward, { centerX: 200, topY: 30, bottomY: 70 }, 5000);

    expect(containerOf(page).style.bottom).toBe('auto');
  });

  it('never scrolls', async () => {
    // A scrollbar inside a hover popover is the failure this layout exists to avoid, so nothing here
    // may cap its own height and hide the remainder.
    const page = await renderPopover(lockedReward, { centerX: 200, topY: 40, bottomY: 80 }, 200);

    expect(bodyOf(page).style.maxHeight).toBe('');
    expect(bodyOf(page).style.overflowY).toBe('');
  });
});

/**
 * The height problem is solved by the layout, not by a cap: three specs to a row, and prerequisites
 * as chips that wrap. These pin the shape that keeps it short.
 */
describe('popover layout', () => {
  it('lays the populated fields out three to a row', async () => {
    const page = await renderPopover(claimedItem);

    // Eight fields stacked was eight rows tall; three to a row is three.
    expect(page.body.querySelectorAll('.popover-spec')).toHaveLength(8);
    expect(specRowCount(page)).toBe(3);
  });

  it('shrinks to a single row once the empty fields are dropped', async () => {
    const locked = specRowCount(await renderPopover(lockedReward));
    const pending = specRowCount(await renderPopover(offeredItem));

    expect(locked).toBe(1);
    expect(pending).toBe(1);
  });

  it('keeps prerequisites on one wrapping line rather than a row each', async () => {
    const manyPrerequisites = {
      ...lockedReward,
      lock: {
        ...lockedReward.lock,
        prerequisites: [15000, 20000, 25000, 30000, 35000, 40000].map(mileage => ({
          mileage,
          label: `${mileage / 1000}K`,
          satisfied: false,
        })),
      },
    } as VehicleServiceItemDTO;

    const page = await renderPopover(manyPrerequisites);
    const list = page.body.querySelector('.popover-lock-prerequisites');

    expect(page.body.querySelectorAll('.popover-prerequisite')).toHaveLength(6);
    // One flex-wrap container, not six stacked rows — this is what bounds the height.
    expect(list.classList.contains('popover-lock-prerequisites')).toBe(true);
    expect(specRowCount(page)).toBe(1);
  });

  it('gives the package code two columns, since its length is the one that is not bounded', async () => {
    const page = await renderPopover(claimedItem);
    const rows = [...page.body.querySelectorAll('.popover-specs')];
    const packageCell = page.body.querySelector('.popover-spec.pkg');

    expect(packageCell).not.toBeNull();

    // Packed by width, not by count: the last row holds the job number plus a double-width package
    // code, which is three columns even though it is two cells.
    const lastRow = rows[rows.length - 1];
    expect(lastRow.querySelectorAll('.popover-spec')).toHaveLength(2);
    expect(lastRow.contains(packageCell)).toBe(true);

    // Still three rows — the wider cell buys room without costing height.
    expect(rows).toHaveLength(3);
  });

  it('gives the package code the whole row when nothing shares it', async () => {
    // Type, activation and expiry fill the first row exactly, leaving the code on its own.
    const paidItem = serviceItems.find(item => item.serviceItemID === 'SI-PAID-15K');
    const page = await renderPopover(paidItem);
    const packageCell = page.body.querySelector('.popover-spec.pkg');

    expect(packageCell.classList.contains('full')).toBe(true);
    expect(specRowCount(page)).toBe(2);
  });

  it('never renders an empty card, even if every field were blank', async () => {
    const blank = {
      ...offeredItem,
      type: '',
      activatedAt: '',
      expiresAt: '',
      claimDate: '',
      companyName: '',
      invoiceNumber: '',
      jobNumber: '',
      packageCode: '',
    } as VehicleServiceItemDTO;

    const page = await renderPopover(blank);

    expect(page.body.querySelectorAll('.popover-spec').length).toBeGreaterThan(0);
  });
});
