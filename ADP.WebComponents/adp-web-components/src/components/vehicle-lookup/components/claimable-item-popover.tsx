import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import ComponentLocale from '~lib/component-locale';
import { TriangleIcon } from '~assets/triangle-icon';
import { ActivationIcon } from '~assets/activation-icon';
import cn from '~lib/cn';

type DetailRow = { label: keyof ComponentLocale<typeof dynamicClaimSchema>; key: keyof VehicleServiceItemDTO; formatter?: (item: VehicleServiceItemDTO) => string };

const detailRows: DetailRow[] = [
  {
    label: 'serviceType',
    key: 'type',
    formatter: (item: VehicleServiceItemDTO) => item.type.charAt(0).toUpperCase() + item.type.slice(1),
  },
  {
    label: 'activationDate',
    key: 'activatedAt',
  },
  {
    label: 'expireDate',
    key: 'expiresAt',
  },
  {
    label: 'claimAt',
    key: 'claimDate',
  },
  {
    label: 'claimingCompany',
    key: 'companyName',
  },
  {
    label: 'invoiceNumber',
    key: 'invoiceNumber',
  },
  {
    label: 'jobNumber',
    key: 'jobNumber',
  },
  {
    label: 'packageCode',
    key: 'packageCode',
  },
];

const LEFT_PADDING = 16;

const RIGHT_PADDING = 16;

const BOTTOM_PADDING = 10;

const LEFT_SAFE_AREA = LEFT_PADDING + 25;

const RIGHT_SAFE_AREA = RIGHT_PADDING + 25;

const POPOVER_WIDTH = 400;

const HALF_POPOVER_WIDTH = POPOVER_WIDTH / 2;

const ARROW_PADDING = 19;

const CLAIMABLE_HEIGHT = 378;

const NONE_CLAIMABLE_HEIGHT = 322;

type ClaimableItemPopoverProps = {
  showPopover: boolean;
  item: VehicleServiceItemDTO;
  claim: (item: VehicleServiceItemDTO) => void;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
  targetLocation: { left: number; bottom: number; top: number };
};

export const ClaimableItemPopover: FunctionalComponent<ClaimableItemPopoverProps> = ({ locale, item, targetLocation, showPopover, claim }) => {
  let top = targetLocation.bottom + ARROW_PADDING;

  const popOverHeight = item?.claimable ? CLAIMABLE_HEIGHT : NONE_CLAIMABLE_HEIGHT;

  const flipVertically = popOverHeight + top + BOTTOM_PADDING > window.innerHeight;

  if (flipVertically) top = targetLocation.top - popOverHeight - ARROW_PADDING;

  if (targetLocation.left < LEFT_SAFE_AREA) targetLocation.left = LEFT_SAFE_AREA;

  if (targetLocation.left > window.innerWidth - RIGHT_SAFE_AREA) targetLocation.left = window.innerWidth - RIGHT_SAFE_AREA;

  let left = targetLocation.left - HALF_POPOVER_WIDTH;

  let popoverBodyOffset = 0;

  const popoverBodyLeft = targetLocation.left - HALF_POPOVER_WIDTH;

  const popoverBodyRight = targetLocation.left + HALF_POPOVER_WIDTH;

  if (popoverBodyLeft - LEFT_PADDING < 0) popoverBodyOffset += LEFT_PADDING - popoverBodyLeft;

  if (popoverBodyRight + RIGHT_PADDING > window.innerWidth) popoverBodyOffset -= popoverBodyRight + RIGHT_PADDING - window.innerWidth;

  return (
    <div aria-expanded={showPopover.toString()} dir={locale.sharedLocales.direction} style={{ top: `${top}px`, left: `${left}px` }} class="claimable-item-popover">
      <div class="popover-relative-container">
        <div class={cn('popover-arrow-icon', { flipped: flipVertically })}>
          <TriangleIcon class="popover-arrow-icon-svg" />
          <div class="popover-arrow-bottom-line" />
        </div>
        <div style={{ transform: `translateX(${popoverBodyOffset}px)` }} class="popover-body">
          {detailRows.map(row => (
            <div class="popover-info-row">
              <b>{locale[row.label]}</b>
              <div>{item ? (row?.formatter ? row.formatter(item) : item[row.key]) : ''}</div>
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
  );
};
