import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import ComponentLocale from '~lib/component-locale';
import { TriangleIcon } from '~assets/triangle-icon';
import { ActivationIcon } from '~assets/activation-icon';

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

const LEFT_SAFE_AREA = LEFT_PADDING + 25;

const RIGHT_SAFE_AREA = RIGHT_PADDING + 25;

const POPOVER_WIDTH = 400;

const HALF_POPOVER_WIDTH = POPOVER_WIDTH / 2;

const ARROW_PADDING = 16;

type ClaimableItemPopoverProps = {
  showPopover: boolean;
  item: VehicleServiceItemDTO;
  targetLocation: { x: number; y: number };
  claim: (item: VehicleServiceItemDTO) => void;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
};

export const ClaimableItemPopover: FunctionalComponent<ClaimableItemPopoverProps> = ({ locale, item, targetLocation, showPopover, claim }) => {
  const top = targetLocation.y + ARROW_PADDING;

  if (targetLocation.x < LEFT_SAFE_AREA) targetLocation.x = LEFT_SAFE_AREA;

  if (targetLocation.x > window.innerWidth - RIGHT_SAFE_AREA) targetLocation.x = window.innerWidth - RIGHT_SAFE_AREA;

  let left = targetLocation.x - HALF_POPOVER_WIDTH;

  let popoverBodyOffset = 0;

  const popoverBodyLeft = targetLocation.x - HALF_POPOVER_WIDTH;

  const popoverBodyRight = targetLocation.x + HALF_POPOVER_WIDTH;

  if (popoverBodyLeft - LEFT_PADDING < 0) popoverBodyOffset += LEFT_PADDING - popoverBodyLeft;

  if (popoverBodyRight + RIGHT_PADDING > window.innerWidth) popoverBodyOffset -= popoverBodyRight + RIGHT_PADDING - window.innerWidth;

  return (
    <div aria-expanded={showPopover.toString()} dir={locale.sharedLocales.direction} style={{ top: `${top}px`, left: `${left}px` }} class="claimable-item-popover">
      <div class="popover-relative-container">
        <div class="popover-arrow-icon">
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
