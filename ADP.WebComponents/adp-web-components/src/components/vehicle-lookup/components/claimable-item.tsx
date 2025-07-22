import { InferType } from 'yup';
import { h, FunctionalComponent } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import expiredIcon from '~assets/expired.svg';
import pendingIcon from '~assets/pending.svg';
import cancelledIcon from '~assets/cancelled.svg';
import processedIcon from '~assets/processed.svg';
import activationRequiredIcon from '~assets/activationRequired.svg';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
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

  const openPopover = event => setClaimableItemPopover(true, item, event?.target);

  const closePopover = () => setClaimableItemPopover(false);

  return (
    <div class={cn('claimable-item', { [item.status]: addStatusClass })}>
      <div class="claimable-item-container">
        <div
          onBlur={closePopover}
          onClick={openPopover}
          onMouseEnter={openPopover}
          onMouseLeave={closePopover}
          onAnimationEnd={removeLoadAnimationClass}
          class="claimable-item-header load-animation"
        >
          <img src={icons[item.status]} alt="status icon" />
          <div>{locale[item?.status]}</div>
        </div>
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-circle load-animation" />
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-footer load-animation">
          {item?.name}
        </div>
      </div>
    </div>
  );
};
