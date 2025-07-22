import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ErrorKeys } from '~features/multi-lingual';

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

  onLoadingChange: (newValue: boolean) => void;
  setErrorMessage: (message: ErrorKeys) => void;
  loadingStateChange?: (isLoading: boolean) => void;
  errorCallback?: (errorMessage: ErrorKeys) => void;
  loadedResponse?: (response: VehicleLookupDTO) => void;
  setMockData: (newMockData: VehicleLookupMock) => Promise<void>;
  fetchData: (newData: VehicleLookupDTO | string, headers?: object) => Promise<void>;
}
