import { FunctionalComponent, h } from '@stencil/core';
import { FormHook } from '~features/form-hook';
import { VehicleQuotation } from './validations';
import { LoaderIcon } from '~assets/loader-icon';
import cn from '~lib/cn';

interface VehicleImageViewerProps {
  form: FormHook<VehicleQuotation>;
}

const cachedImages: Record<string, any> = {};

const cacheImage = (form: FormHook<any>, imSrc: string, id: string) => {
  fetch(imSrc)
    .then(res => res.blob())
    .then(blob => {
      const reader = new FileReader();
      reader.onloadend = () => {
        cachedImages[id] = reader.result as string;

        form.rerender({ rerenderForm: true });
      };
      reader.readAsDataURL(blob);
    })
    .catch(e => {
      console.log(e);

      // fail silently, keep fallback
    });
};

let imageSrcBase64 = '';

export const VehicleImageViewer: FunctionalComponent<VehicleImageViewerProps> = ({ form }) => {
  form.addWatcher('vehicle');

  let vehicleId = form.getValue<VehicleQuotation>('vehicle');

  let openContainer = !!vehicleId;

  const selectedVehicle = form.context['vehicleList']?.find(vehicle => `${vehicle?.ID}` === vehicleId);

  let imSrc;

  if (selectedVehicle) {
    imSrc = selectedVehicle?.Image;
    vehicleId = selectedVehicle.ID;
  } else if (form.context['vehicleList'] && form.context['vehicleList'][0]) {
    imSrc = form.context['vehicleList'][0]?.meta?.image;
    vehicleId = form.context['vehicleList'][0].value;
  }

  let isLoading = false;

  if (cachedImages[vehicleId]) {
    imageSrcBase64 = cachedImages[vehicleId];
  } else if (form && imSrc && vehicleId) {
    cacheImage(form, imSrc, vehicleId);
    isLoading = true;
  }

  return (
    <flexible-container isOpened={openContainer}>
      <div part="vehicle-image-wrapper" class={cn('vehicle-image-wrapper', { loading: isLoading })}>
        <div part={cn('vehicle-image-loading-wrapper', { 'vehicle-image-active-loading-wrapper': isLoading })} class="loading-wrapper">
          <LoaderIcon part="vehicle-image-loader-icon" class="img" />
        </div>
        <img part="vehicle-image" src={imageSrcBase64} alt="toyota vehicle" />
      </div>
    </flexible-container>
  );
};
