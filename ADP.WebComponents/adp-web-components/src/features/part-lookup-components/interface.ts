import { ErrorKeys } from '~features/multi-lingual';
import { BlazorInvokableFunction } from '~features/blazor-ref';

import { PartLookupMock } from './types';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

export interface PartLookupComponent {
  isDev: boolean;
  baseUrl: string;
  headers: object;
  isError: boolean;
  isLoading: boolean;
  queryString: string;
  searchString: string;
  errorMessage?: ErrorKeys;
  mockData: PartLookupMock;
  partLookup?: PartLookupDTO;

  el: HTMLElement;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  loadedResponse?: BlazorInvokableFunction<(response: PartLookupDTO) => void>;

  getMockData: () => Promise<PartLookupMock>;
  onLoadingChange: (newValue: boolean) => void;
  setErrorMessage: (message: ErrorKeys) => void;
  onIsDevChange: (isDev: boolean) => Promise<void>;
  setMockData: (newMockData: PartLookupMock) => Promise<void>;
  fetchData: (newData: PartLookupDTO | string, headers?: object) => Promise<void>;
}
