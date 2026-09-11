import { ErrorKeys } from '~features/multi-lingual';
import { BlazorInvokable, smartInvokable } from '~features/blazor-ref';
import { localizeMockAssets } from '~features/mocks';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { VehicleLookupComponent } from './interface';
import { resolveRequestHeaders } from './request-headers';
import validateVin from '~lib/validate-vin';

export const setVehicleLookupData = async (
  context: VehicleLookupComponent & BlazorInvokable,
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
    context.isLoading = true;

    await new Promise(r => {
      scopedTimeoutRef = setTimeout(r, 1000);
      context.networkTimeoutRef = scopedTimeoutRef;
    });

    if ((!vin || vin.trim().length === 0) && !context?.isDev) {
      context.isError = false;
      context.isLoading = false;
      context.vehicleLookup = undefined;
      throw new Error('vinNumberRequired');
    }

    if (!validateVin(vin) && !context?.isDev && !context?.disableVinValidation) throw new Error('invalidVin');

    const vehicleResponse = isVinRequest ? await getVehicleLookup(context, { scopedTimeoutRef, vin }, headers) : (newData as VehicleLookupDTO);

    if (context.networkTimeoutRef === scopedTimeoutRef) {
      if (!vehicleResponse) throw new Error('wrongResponseFormat');
      if (beforeAssignment) context.vehicleLookup = await beforeAssignment(vehicleResponse, { scopedTimeoutRef });
      else context.vehicleLookup = vehicleResponse;

      // Fire loadedResponse after vehicleLookup is set to ensure consistent state
      // Only for VIN requests — DTO distribution should not re-trigger the callback chain
      if (isVinRequest) {
        smartInvokable.bind(context)(context?.loadedResponse, vehicleResponse);
      }
    }

    context.errorMessage = null;
    context.isLoading = false;
    context.isError = false;
  } catch (error) {
    if (error && error?.name === 'AbortError') return;
    smartInvokable.bind(context)(context?.errorCallback, error.message);
    console.error(error);
    context.setErrorMessage(error.message);
    context.isLoading = false;
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

      return newVehicleInformation;
    }
  };

  if (context?.isDev) {
    // Fixture pictures point at the CDN copy of the package; a dev build reads the dev server's.
    const newData = localizeMockAssets(context?.mockData[vin]);

    return handleResult(newData);
  } else {
    if (!context?.baseUrl) throw new Error('noBaseUrl');

    // Headers passed with this call win and are remembered for the follow-up requests (trace,
    // claim, unauthorized campaign lookup); without them the host's provider is asked, so a host
    // that refreshes its token on demand never has to push headers into the element at all.
    const componentHeaders = await resolveRequestHeaders(context, headers);

    // The lookup-only part joins here and nowhere else: a follow-up request (a trace) is built from
    // queryString alone, so a logging flag placed in lookupQueryString never rides on a re-read.
    const query = [context?.queryString, context?.lookupQueryString].filter(Boolean).join('&');

    const response = await fetch(`${context?.baseUrl}${vin}?${query}`, { signal: context?.abortController.signal, headers: componentHeaders });

    const newData = (await response.json()) as VehicleLookupDTO;

    return handleResult(newData);
  }
};
