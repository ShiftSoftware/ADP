import { MockJson } from '~types/components';
import { partLookupDTO } from '~types/part/partLookupDTO';

export interface PartInformationInterface {
  isDev: boolean;
  baseUrl: string;
  queryString?: string;
  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;
  loadedResponse?: (response: partLookupDTO) => void;
}

type GetPartInformationProps = {
  partNumber: string;
  notAvailableMessage?: string;
  mockData: MockJson<partLookupDTO>;
  scopedTimeoutRef: ReturnType<typeof setTimeout>;
  middlewareCallback?: (part: partLookupDTO) => void;
};

export const getPartInformation = async (component: PartInformationInterface, generalProps: GetPartInformationProps, headers: any = {}): Promise<partLookupDTO> => {
  const { notAvailableMessage, mockData, partNumber, scopedTimeoutRef, middlewareCallback } = generalProps;

  const { isDev, baseUrl, queryString, abortController, networkTimeoutRef, loadedResponse } = component;

  const handleResult = (newPartInformation: partLookupDTO): partLookupDTO => {
    if (networkTimeoutRef === scopedTimeoutRef) {
      if (!newPartInformation && partNumber) throw new Error(notAvailableMessage || 'wrongResponseFormat');

      if (loadedResponse) loadedResponse(newPartInformation);
      if (middlewareCallback) middlewareCallback(newPartInformation);
      return newPartInformation;
    }
  };

  if (isDev) {
    const newData = mockData[partNumber];

    return handleResult(newData);
  } else {
    if (!baseUrl) throw new Error('noBaseUrl');

    const response = await fetch(`${baseUrl}${partNumber}?${queryString}`, { signal: abortController.signal, headers: headers });

    if (response.status === 204) throw new Error('noPartsFound');

    const newData = (await response.json()) as partLookupDTO;

    return handleResult(newData);
  }
};
