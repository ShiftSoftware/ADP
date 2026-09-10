import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ErrorKeys } from '~features/multi-lingual';
import { BlazorInvokableFunction } from '~features/blazor-ref';

import { VehicleLookupMock, VehicleRequestHeaders } from './types';
import { RequestHeadersSource } from './request-headers';

export interface VehicleLookupComponent extends VehicleRequestHeaders, RequestHeadersSource {
  isDev: boolean;
  disableVinValidation?: boolean;
  baseUrl: string;
  headers: object;
  isError: boolean;
  isLoading: boolean;
  queryString: string;
  /**
   * Appended to the vehicle lookup request only — never to a component's follow-up requests such as
   * a trace. Where a host puts a lookup-logging flag: a trace re-reads a lookup that was already
   * logged and must not be counted again.
   */
  lookupQueryString?: string;
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
  fetchVin: (newData: VehicleLookupDTO | string, headers?: object) => Promise<void>;
}
