import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import cn from '~lib/cn';
import { formatDateTime } from '~lib/format-date-time';

import { TriangleIcon } from '~assets/triangle-icon';
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

export const POPOVER_WIDTH = 540;
const HALF_POPOVER_WIDTH = POPOVER_WIDTH / 2;
const ARROW_PADDING = 19;
const ARROW_HALF_WIDTH = 25;
const VIEWPORT_PADDING = 16;

export type PopoverTarget = { centerX: number; topY: number; bottomY: number };

type ClaimableItemPopoverProps = {
  showPopover: boolean;
  item: VehicleServiceItemDTO;
  popoverHeight: number;
  target: PopoverTarget;
  fadingOut: boolean;
  contentFading: boolean;
  bodyContentHeight: number;
  claim: (item: VehicleServiceItemDTO) => void;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
};

export const ClaimableItemPopover: FunctionalComponent<ClaimableItemPopoverProps> = ({
  locale,
  item,
  target,
  popoverHeight,
  showPopover,
  fadingOut,
  contentFading,
  bodyContentHeight,
  claim,
  onMouseEnter,
  onMouseLeave,
}) => {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;

  const minArrowX = VIEWPORT_PADDING + ARROW_HALF_WIDTH;
  const maxArrowX = viewportWidth - VIEWPORT_PADDING - ARROW_HALF_WIDTH;
  const arrowX = Math.max(minArrowX, Math.min(target.centerX, maxArrowX));

  const fitsBelow = target.bottomY + ARROW_PADDING + popoverHeight + VIEWPORT_PADDING <= viewportHeight;
  const flipVertically = !fitsBelow;

  // Anchor by `bottom` when flipped so the popover's height doesn't drag its visible
  // position up/down between items with different content. Anchor by `top` otherwise.
  const verticalStyle: { [key: string]: string } = flipVertically
    ? { top: 'auto', bottom: `${viewportHeight - target.topY + ARROW_PADDING}px` }
    : { top: `${target.bottomY + ARROW_PADDING}px`, bottom: 'auto' };
  const left = arrowX - HALF_POPOVER_WIDTH;

  const naturalBodyLeft = arrowX - HALF_POPOVER_WIDTH;
  const naturalBodyRight = arrowX + HALF_POPOVER_WIDTH;

  let bodyOffset = 0;
  if (naturalBodyLeft < VIEWPORT_PADDING) bodyOffset = VIEWPORT_PADDING - naturalBodyLeft;
  else if (naturalBodyRight > viewportWidth - VIEWPORT_PADDING) bodyOffset = viewportWidth - VIEWPORT_PADDING - naturalBodyRight;

  return (
    <div
      aria-expanded={showPopover.toString()}
      dir={locale.sharedLocales.direction}
      style={{ ...verticalStyle, left: `${left}px` }}
      class={cn('claimable-item-popover', { 'fading-out': fadingOut, 'content-fading': contentFading })}
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
            <div class="popover-body-inner">
              {detailRows.map(row => (
                <div class="popover-info-row">
                  <b>{locale[row.label]}</b>
                  <div class="popover-info-value">{item ? (row?.formatter ? row.formatter(item) : item[row.key]) : ''}</div>
                </div>
              ))}

              {item?.claimable && (
                <button onClick={() => claim && claim(item)} class="claim-button">
                  <ActivationIcon />
                  <span>{locale.claim}</span>
                </button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
