import { InferType } from 'yup';
import { h, FunctionalComponent } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { XIcon } from '~assets/x-icon';
import { BanIcon } from '~assets/ban-icon';
import { LockIcon } from '~assets/lock-icon';
import { TickIcon } from '~assets/tick-icon';
import { PauseIcon } from '~assets/pause-icon';
import { LoaderIcon } from '~assets/loader-icon';
import { HourglassIcon } from '~assets/hourglass-icon';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

/**
 * The mark inside the node, drawn inline so the node can colour it from the item's status class.
 * The raster icons this replaced baked their colour in, which the timeline cannot do: the same
 * pending item is drawn green when it is the one on offer and grey when it is queued behind another.
 */
const statusGlyphs = {
  expired: XIcon,
  pending: PauseIcon,
  processed: TickIcon,
  cancelled: BanIcon,
  activationRequired: LoaderIcon,
};

// The only two words this feature adds to the screen, kept together so moving them into the locale
// files is one edit. Left untranslated deliberately for now — see the locked-items plan.
const lockStateLabels = {
  Locked: 'Locked',
  Missed: 'Missed',
};

type ClaimableItemProps = {
  addStatusClass: boolean;
  item: VehicleServiceItemDTO;
  locale: InferType<typeof dynamicClaimSchema>;
  setClaimableItemPopover: (showPopover: boolean, claimableItem?: VehicleServiceItemDTO, claimableItemPopoverRef?: HTMLDivElement) => void;
};

export const ClaimableItem: FunctionalComponent<ClaimableItemProps> = ({ item, locale, addStatusClass, setClaimableItemPopover }) => {
  const removeLoadAnimationClass = (event: AnimationEvent) => {
    const component = event.target as HTMLDivElement;
    component.classList.remove('load-animation');
  };

  let hitEl: HTMLDivElement;

  const openPopover = () => setClaimableItemPopover(true, item, hitEl);

  const closePopover = () => setClaimableItemPopover(false);

  // A locked or missed item carries an ordinary status underneath — usually pending, because
  // nothing about its lifecycle changed. The lock is what the customer is being told, so it takes
  // over the card: the status would say "Pending" about a reward they cannot claim.
  const lockState = item?.lock?.state;

  const StatusGlyph = statusGlyphs[item?.status] ?? PauseIcon;

  const statusLabel = lockState ? lockStateLabels[lockState] : locale[item?.status];

  return (
    <div class={cn('claimable-item', { [item.status]: addStatusClass && !lockState, [`lock-${lockState}`]: !!lockState })}>
      <div class="claimable-item-container">
        {/* One hit target for the whole node — the status above it, the node itself and the name
            under it — so the popover opens from anywhere near the item rather than off the 16px
            band of text at the top, and so a pointer travelling between two items never falls
            through a gap and closes the card on the way. Drawn first but painted on top, which is
            what lets the focus ring reach the node through a sibling selector.

            It is also the popover's anchor: the card is placed off this box's base, so it never
            lands over the node the pointer is resting on. */}
        <div
          role="button"
          tabindex="0"
          aria-label={`${statusLabel} — ${item?.name ?? ''}`}
          ref={el => (hitEl = el as HTMLDivElement)}
          onBlur={closePopover}
          onFocus={openPopover}
          onClick={openPopover}
          onMouseEnter={openPopover}
          onMouseLeave={closePopover}
          class="claimable-item-hit"
        />

        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-header load-animation">
          {statusLabel}
        </div>

        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-circle load-animation">
          {lockState ? (
            <div class="claimable-item-lock-icon">{lockState === 'Missed' ? <HourglassIcon /> : <LockIcon />}</div>
          ) : (
            <div class="claimable-item-status-icon">
              <StatusGlyph />
            </div>
          )}
        </div>

        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-footer load-animation">
          {item?.name}
        </div>
      </div>
    </div>
  );
};
