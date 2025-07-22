import { ErrorKeys } from '~features/multi-lingual';

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

  onLoadingChange: (newValue: boolean) => void;
  setErrorMessage: (message: ErrorKeys) => void;
  loadingStateChange?: (isLoading: boolean) => void;
  errorCallback?: (errorMessage: ErrorKeys) => void;
  loadedResponse?: (response: PartLookupDTO) => void;
  setMockData: (newMockData: PartLookupMock) => Promise<void>;
  fetchData: (newData: PartLookupDTO | string, headers?: object) => Promise<void>;
}
