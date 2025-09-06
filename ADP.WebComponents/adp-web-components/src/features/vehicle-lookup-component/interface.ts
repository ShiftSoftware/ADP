import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ErrorKeys } from '~features/multi-lingual';
import { BlazorInvokableFunction } from '~features/blazor-ref';

import { VehicleLookupMock, VehicleRequestHeaders } from './types';

export interface VehicleLookupComponent extends VehicleRequestHeaders {
  isDev: boolean;
  baseUrl: string;
  headers: object;
  isError: boolean;
  isLoading: boolean;
  queryString: string;
  errorMessage?: ErrorKeys;
  mockData: VehicleLookupMock;
  vehicleLookup?: VehicleLookupDTO;

  el: HTMLElement;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  loadedResponse?: BlazorInvokableFunction<(response: VehicleLookupDTO) => void>;

  onLoadingChange: (newValue: boolean) => void;
  setErrorMessage: (message: ErrorKeys) => void;
  setMockData: (newMockData: VehicleLookupMock) => Promise<void>;
  fetchData: (newData: VehicleLookupDTO | string, headers?: object) => Promise<void>;
}
