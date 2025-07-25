import { ErrorKeys } from '~features/multi-lingual';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { vehicleRequestHeaders } from './types';
import { VehicleLookupComponent } from './interface';

export const setVehicleLookupData = async (
  context: VehicleLookupComponent,
  newData: VehicleLookupDTO | string,
  headers: any = {},
  { beforeAssignment }: { beforeAssignment?: (vehicleLookup: VehicleLookupDTO, extra: { scopedTimeoutRef: ReturnType<typeof setTimeout> }) => Promise<VehicleLookupDTO> } = {},
) => {
  if (newData === null || newData === undefined) newData = context.vehicleLookup?.vin || '';

  // clears network timeoutRef which serves as await for animation
  clearTimeout(context.networkTimeoutRef);

  // handles request spam by canceling the previous ones
  if (context.abortController) context.abortController.abort();
  context.abortController = new AbortController();

  // syncing the internal timeout ref with external network ref
  let scopedTimeoutRef: ReturnType<typeof setTimeout>;

  const isVinRequest = typeof newData === 'string';

  const vin = typeof newData === 'string' ? newData : newData?.vin;

  try {
    if (!vin || vin.trim().length === 0) {
      context.isError = false;
      context.isLoading = false;
      context.vehicleLookup = undefined;
      return;
    }

    context.isLoading = true;

    await new Promise(r => {
      scopedTimeoutRef = setTimeout(r, 1000);
      context.networkTimeoutRef = scopedTimeoutRef;
    });

    const vehicleResponse = isVinRequest ? await getVehicleLookup(context, { scopedTimeoutRef, vin }, headers) : (newData as VehicleLookupDTO);

    if (context.networkTimeoutRef === scopedTimeoutRef) {
      if (!vehicleResponse) throw new Error('wrongResponseFormat');
      if (beforeAssignment) context.vehicleLookup = await beforeAssignment(vehicleResponse, { scopedTimeoutRef });
      else context.vehicleLookup = vehicleResponse;
    }

    context.errorMessage = null;
    context.isLoading = false;
    context.isError = false;
  } catch (error) {
    if (error && error?.name === 'AbortError') return;
    if (context.errorCallback) context.errorCallback(error.message);
    console.error(error);
    context.setErrorMessage(error.message);
  }
};

export const setVehicleLookupErrorState = (context: VehicleLookupComponent, message: ErrorKeys) => {
  context.isError = true;
  context.isLoading = false;
  context.errorMessage = message;
  context.vehicleLookup = undefined;
};

type GetVehicleLookupProps = {
  vin: string;
  scopedTimeoutRef: ReturnType<typeof setTimeout>;
};

export const getVehicleLookup = async (context: VehicleLookupComponent, generalProps: GetVehicleLookupProps, headers: any = {}): Promise<VehicleLookupDTO> => {
  const { vin, scopedTimeoutRef } = generalProps;

  const handleResult = (newVehicleInformation: VehicleLookupDTO): VehicleLookupDTO => {
    if (context?.networkTimeoutRef === scopedTimeoutRef) {
      if (!newVehicleInformation && vin) throw new Error('wrongResponseFormat');

      if (context?.loadedResponse) context?.loadedResponse(newVehicleInformation);
      return newVehicleInformation;
    }
  };

  if (context?.isDev) {
    const newData = context?.mockData[vin];

    return handleResult(newData);
  } else {
    if (!context?.baseUrl) throw new Error('noBaseUrl');

    const componentHeaders = { ...headers };

    Object.entries(vehicleRequestHeaders).forEach(([componentHeaderKey, headerField]) => {
      if (context[componentHeaderKey]) componentHeaders[headerField] = context[componentHeaderKey];
    });

    const response = await fetch(`${context?.baseUrl}${vin}?${context?.queryString}`, { signal: context?.abortController.signal, headers: componentHeaders });

    const newData = (await response.json()) as VehicleLookupDTO;

    return handleResult(newData);
  }
};
