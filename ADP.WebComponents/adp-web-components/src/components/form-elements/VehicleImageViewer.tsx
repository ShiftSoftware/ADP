import { FunctionalComponent, h } from '@stencil/core';
import { FormHook } from '~features/form-hook';
import { LoaderIcon } from '~assets/loader-icon';
import cn from '~lib/cn';

interface VehicleImageViewerProps {
  form: FormHook<any>;
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

  let vehicleId = form.getValue<any>('vehicle');

  let openContainer = !!vehicleId;

  const selectedVehicle = form.context['vehicleList']?.find(vehicle => vehicle.value === vehicleId);

  let imSrc;

  if (selectedVehicle) {
    imSrc = selectedVehicle?.meta?.image;
    vehicleId = selectedVehicle.value;
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
      <div style={{ 'min-height': '60px' }} part="vehicle-image-wrapper" class={cn('vehicle-image-wrapper', { loading: isLoading })}>
        <div
          part={cn('vehicle-image-loading-wrapper', { 'vehicle-image-active-loading-wrapper': isLoading })}
          class={cn('loading-wrapper vehicle-image-loading-wrapper', { 'vehicle-image-active-loading-wrapper': isLoading })}
        >
          <LoaderIcon part="vehicle-image-loader-icon" class="img vehicle-image-loader-icon" />
        </div>
        <img
          alt="vehicle image"
          part="vehicle-image"
          src={imageSrcBase64}
          style={{ 'border-radius': '6px', 'transition': 'opacity 150ms cubic-bezier(0.4, 0, 0.2, 1);', 'opacity': isLoading ? '0' : '1' }}
        />
      </div>
    </flexible-container>
  );
};
