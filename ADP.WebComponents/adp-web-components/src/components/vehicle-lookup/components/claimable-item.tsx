import { InferType } from 'yup';
import { h, FunctionalComponent } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import expiredIcon from '~assets/expired.svg';
import pendingIcon from '~assets/pending.svg';
import cancelledIcon from '~assets/cancelled.svg';
import processedIcon from '~assets/processed.svg';
import activationRequiredIcon from '~assets/activationRequired.svg';

import { LockIcon } from '~assets/lock-icon';
import { HourglassIcon } from '~assets/hourglass-icon';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
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

  let headerEl: HTMLDivElement;

  const openPopover = () => setClaimableItemPopover(true, item, headerEl);

  const closePopover = () => setClaimableItemPopover(false);

  // A locked or missed item carries an ordinary status underneath — usually pending, because
  // nothing about its lifecycle changed. The lock is what the customer is being told, so it takes
  // over the card: the status would say "Pending" about a reward they cannot claim.
  const lockState = item?.lock?.state;

  return (
    <div class={cn('claimable-item', { [item.status]: addStatusClass && !lockState, [`lock-${lockState}`]: !!lockState })}>
      <div class="claimable-item-container">
        <div
          ref={el => (headerEl = el as HTMLDivElement)}
          onBlur={closePopover}
          onClick={openPopover}
          onMouseEnter={openPopover}
          onMouseLeave={closePopover}
          onAnimationEnd={removeLoadAnimationClass}
          class="claimable-item-header load-animation"
        >
          {lockState ? <div class="claimable-item-lock-icon">{lockState === 'Missed' ? <HourglassIcon /> : <LockIcon />}</div> : <img src={icons[item.status]} alt="status icon" />}
          <div>{lockState ? lockStateLabels[lockState] : locale[item?.status]}</div>
        </div>
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-circle load-animation" />
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-footer load-animation">
          {item?.name}
        </div>
      </div>
    </div>
  );
};
