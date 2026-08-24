import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import cn from '~lib/cn';
import { formatDateTime } from '~lib/format-date-time';

import { TriangleIcon } from '~assets/triangle-icon';
import { CheckIcon } from '~assets/check-icon';
import { ActivationIcon } from '~assets/activation-icon';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

import { ComponentLocale } from '~features/multi-lingual';

type DetailRow = { label: keyof ComponentLocale<typeof dynamicClaimSchema>; key: keyof VehicleServiceItemDTO; formatter?: (item: VehicleServiceItemDTO) => string };

const detailRows: DetailRow[] = [
  {
    label: 'serviceType',
    key: 'type',
    formatter: (item: VehicleServiceItemDTO) => item.type.charAt(0).toUpperCase() + item.type.slice(1),
  },
  { label: 'activationDate', key: 'activatedAt' },
  { label: 'expireDate', key: 'expiresAt' },
  { label: 'claimAt', key: 'claimDate', formatter: item => formatDateTime(item.claimDate) },
  { label: 'claimingCompany', key: 'companyName' },
  { label: 'invoiceNumber', key: 'invoiceNumber' },
  { label: 'jobNumber', key: 'jobNumber' },
  { label: 'packageCode', key: 'packageCode' },
];

// A locked reward has no catalog entry naming its prerequisites — they are services the customer
// pays for outside this catalog, which is why the mileage label is the only name there is.
const lockExplanations = {
  Locked: 'Complete the services below to unlock this item.',
  Missed: 'This item was available until the next periodic service was performed.',
};

export const POPOVER_WIDTH = 540;
const HALF_POPOVER_WIDTH = POPOVER_WIDTH / 2;
const ARROW_PADDING = 19;
const ARROW_HALF_WIDTH = 25;
const VIEWPORT_PADDING = 16;

// Three columns to a row, as the claim voucher lays its specs out. The count is what makes the
// popover short enough to need no scrolling: eight fields is three rows here and eight rows stacked.
const SPEC_COLUMNS = 3;

// Every field but one has a bounded length — a date, a type, an invoice or job number — and fits a
// third of the card comfortably. A package code is whatever the source system writes, so it gets two
// columns and is packed as if it were two fields.
const specColumns = (row: DetailRow) => (row.key === 'packageCode' ? 2 : 1);

const detailValue = (item: VehicleServiceItemDTO, row: DetailRow) => (item ? (row.formatter ? row.formatter(item) : item[row.key]) : '');

export type PopoverTarget = { centerX: number; topY: number; bottomY: number };

type PopoverContentProps = {
  item: VehicleServiceItemDTO;
  claim: (item: VehicleServiceItemDTO) => void;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
};

/**
 * One item's worth of card content. Split out so two of them can be rendered at once during a
 * hover-to-hover switch — see the layer stack in ClaimableItemPopover.
 */
const PopoverContent: FunctionalComponent<PopoverContentProps> = ({ item, locale, claim }) => {
  // A field with nothing in it is not information. Claim details are the common case — an item that
  // is pending, expired, cancelled or locked has never been claimed, so its claim date, claiming
  // company, invoice and job number are all blank — but the rule is about the value, not the state,
  // so it needs no list of which statuses to except.
  //
  // Falls back to the full set if that would leave nothing at all, so the popover is never empty.
  const populatedRows = detailRows.filter(row => !!detailValue(item, row));
  const visibleRows = populatedRows.length > 0 ? populatedRows : detailRows;

  // Packed by column width rather than by count, so a two-column field never gets squeezed into a
  // third of a row and wrapped.
  const specRows: DetailRow[][] = [];
  let currentRow: DetailRow[] = [];
  let columnsUsed = 0;

  for (const row of visibleRows) {
    const columns = specColumns(row);

    if (currentRow.length && columnsUsed + columns > SPEC_COLUMNS) {
      specRows.push(currentRow);
      currentRow = [];
      columnsUsed = 0;
    }

    currentRow.push(row);
    columnsUsed += columns;
  }

  if (currentRow.length) specRows.push(currentRow);

  return (
    <div class="popover-body-inner">
      {specRows.map(specRow => (
        <div class="popover-specs">
          {specRow.map(row => (
            <div class={cn('popover-spec', { pkg: specColumns(row) > 1, full: specColumns(row) > 1 && specRow.length === 1 })}>
              <div class="lab">{locale[row.label]}</div>
              <div class="val" dir="ltr">
                {detailValue(item, row)}
              </div>
            </div>
          ))}
        </div>
      ))}

      {item?.lock && (
        <div class={cn('popover-lock', `popover-lock-${item.lock.state}`)}>
          <div class="lab">{lockExplanations[item.lock.state]}</div>

          {item.lock.prerequisites?.length > 0 && (
            <div class="popover-lock-prerequisites">
              {item.lock.prerequisites.map(prerequisite => (
                <div class={cn('popover-prerequisite', { satisfied: prerequisite.satisfied })}>
                  <div class="popover-prerequisite-mark">{prerequisite.satisfied ? <CheckIcon /> : null}</div>
                  <div class="popover-prerequisite-text">
                    <div class="val" dir="ltr">
                      {prerequisite.label}
                    </div>
                    <div class="popover-prerequisite-date" dir="ltr">
                      {prerequisite.satisfiedOn ? formatDateTime(prerequisite.satisfiedOn) : ''}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Action slot. A grid with every occupant in the same cell, so states that are never on
          screen together — a claim button and, in future, what replaced it once claimed — swap by
          opacity in one place instead of by reflow. Collapsed outright when nothing occupies it,
          which is what lets the card shrink rather than hold an empty band. */}
      <div class={cn('popover-actions', { 'is-empty': !item?.claimable })}>
        {item?.claimable && (
          <button onClick={() => claim && claim(item)} class="popover-action claim-button">
            <ActivationIcon />
            <span>{locale.claim}</span>
          </button>
        )}
      </div>
    </div>
  );
};

type ClaimableItemPopoverProps = {
  showPopover: boolean;
  item: VehicleServiceItemDTO;
  popoverHeight: number;
  target: PopoverTarget;
  fadingOut: boolean;
  /** Held for the length of a hover-to-hover cross-fade; drives which layer is opaque. */
  swapping: boolean;
  /** The previously hovered item, rendered underneath while it fades out. */
  outgoingItem?: VehicleServiceItemDTO;
  /** Bumped on every item change so the vdom mounts a fresh layer instead of patching the old one. */
  layerKey: number;
  /** Cross-fade + resize duration, in ms. Drives the CSS transitions and the outgoing-layer timer. */
  swapMs: number;
  bodyContentHeight: number;
  claim: (item: VehicleServiceItemDTO) => void;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
  /** Handed back so the owner can promote it into the top layer when a host ancestor would clip it. */
  rootRef: (element: HTMLElement) => void;
};

export const ClaimableItemPopover: FunctionalComponent<ClaimableItemPopoverProps> = ({
  locale,
  item,
  target,
  popoverHeight,
  showPopover,
  fadingOut,
  swapping,
  outgoingItem,
  layerKey,
  swapMs,
  bodyContentHeight,
  claim,
  onMouseEnter,
  onMouseLeave,
  rootRef,
}) => {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;

  const minArrowX = VIEWPORT_PADDING + ARROW_HALF_WIDTH;
  const maxArrowX = viewportWidth - VIEWPORT_PADDING - ARROW_HALF_WIDTH;
  const arrowX = Math.max(minArrowX, Math.min(target.centerX, maxArrowX));

  const spaceBelow = viewportHeight - target.bottomY - ARROW_PADDING - VIEWPORT_PADDING;
  const spaceAbove = target.topY - ARROW_PADDING - VIEWPORT_PADDING;

  // Flip only when below cannot hold it AND above genuinely has more room. Flipping on "does not fit
  // below" alone moves the overflow to the top of the window rather than removing it.
  //
  // There is deliberately no height cap and nothing scrolls: the popover is laid out to stay short —
  // three specs to a row and prerequisites as chips that wrap — so the content cannot grow into a
  // column tall enough to need one.
  const flipVertically = popoverHeight > spaceBelow && spaceAbove > spaceBelow;

  // Anchor by `bottom` when flipped so the popover's height doesn't drag its visible
  // position up/down between items with different content. Anchor by `top` otherwise.
  const verticalStyle: { [key: string]: string } = flipVertically
    ? { top: 'auto', bottom: `${viewportHeight - target.topY + ARROW_PADDING}px` }
    : { top: `${target.bottomY + ARROW_PADDING}px`, bottom: 'auto' };
  // Where the card would sit if the arrow could always be its midpoint. Near a window edge it cannot,
  // so the body slides back inside by bodyOffset while the arrow stays on the item it points at.
  const left = arrowX - HALF_POPOVER_WIDTH;
  const naturalBodyRight = arrowX + HALF_POPOVER_WIDTH;

  let bodyOffset = 0;
  if (left < VIEWPORT_PADDING) bodyOffset = VIEWPORT_PADDING - left;
  else if (naturalBodyRight > viewportWidth - VIEWPORT_PADDING) bodyOffset = viewportWidth - VIEWPORT_PADDING - naturalBodyRight;

  return (
    <div
      ref={el => rootRef(el as HTMLElement)}
      aria-expanded={showPopover.toString()}
      dir={locale.sharedLocales.direction}
      style={{ ...verticalStyle, 'left': `${left}px`, '--popover-swap': `${swapMs}ms` }}
      class={cn('claimable-item-popover', { 'fading-out': fadingOut, 'swapping': swapping })}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
    >
      <div class="popover-relative-container">
        <div class={cn('popover-arrow-icon', { flipped: flipVertically })}>
          <TriangleIcon class="popover-arrow-icon-svg" />
          <div class="popover-arrow-bottom-line" />
        </div>
        <div style={{ transform: `translateX(${bodyOffset}px)` }} class="popover-body">
          <div class="popover-body-content" style={bodyContentHeight > 0 ? { height: `${bodyContentHeight}px` } : {}}>
            {/* Both renders sit in the same grid cell, so moving from one item to the next is a
                single cross-fade against a box that resizes on the same clock — rather than the old
                fade-out, hard swap, fade-in, which left the height to catch up afterwards and made
                a claim button on only one of the two items mount and unmount mid-move. */}
            <div class="popover-layers">
              {outgoingItem && (
                <div key={`popover-layer-${layerKey - 1}`} class="popover-layer popover-layer-outgoing" aria-hidden="true">
                  <PopoverContent item={outgoingItem} locale={locale} claim={claim} />
                </div>
              )}
              <div key={`popover-layer-${layerKey}`} class="popover-layer popover-layer-current">
                <PopoverContent item={item} locale={locale} claim={claim} />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
