import { h, FunctionalComponent } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

type ClaimableItemProps = {
  item: VehicleServiceItemDTO;
};

export const ClaimableItem: FunctionalComponent<ClaimableItemProps> = () => {
  const removeLoadAnimationClass = (event: AnimationEvent) => {
    const component = event.target as HTMLDivElement;
    component.classList.remove('load-animation');
  };

  return (
    <div class="claimable-item">
      <div class="claimable-item-container">
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-header load-animation"></div>
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-circle load-animation" />
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-footer load-animation">
          1,000 KM Free Service 1,000 KM Free Service
        </div>
      </div>
    </div>
  );
};
