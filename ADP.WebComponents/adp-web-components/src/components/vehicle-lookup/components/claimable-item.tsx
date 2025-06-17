import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import expiredIcon from '~assets/expired.svg';
import pendingIcon from '~assets/pending.svg';
import cancelledIcon from '~assets/cancelled.svg';
import processedIcon from '~assets/processed.svg';
import activationRequiredIcon from '~assets/activationRequired.svg';
import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import { InferType } from 'yup';

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
};

type ClaimableItemProps = {
  item: VehicleServiceItemDTO;
  locale: InferType<typeof dynamicClaimSchema>;
};

export const ClaimableItem: FunctionalComponent<ClaimableItemProps> = ({ item, locale }) => {
  const removeLoadAnimationClass = (event: AnimationEvent) => {
    const component = event.target as HTMLDivElement;
    component.classList.remove('load-animation');
  };

  return (
    <div class="claimable-item">
      <div class="claimable-item-container">
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-header load-animation">
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
