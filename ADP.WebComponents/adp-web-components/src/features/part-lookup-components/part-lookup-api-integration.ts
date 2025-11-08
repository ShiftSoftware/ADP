import { PartLookupComponent } from './interface';

import { ErrorKeys } from '~features/multi-lingual';
import { BlazorInvokable, smartInvokable } from '~features/blazor-ref';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';
import { fetchFrom, FetchFromProps } from '~lib/fetch-from';

export const setPartLookupData = async (
  context: PartLookupComponent & BlazorInvokable,
  newData: PartLookupDTO | string,
  { beforeAssignment }: { beforeAssignment?: (partLookup: PartLookupDTO, extra: { scopedTimeoutRef: ReturnType<typeof setTimeout> }) => Promise<PartLookupDTO> } = {},
) => {
  // @ts-ignore
  const loadingKey = context?.localLoadingName ? context?.localLoadingName : 'isLoading';

  if (newData === null || newData === undefined) newData = context?.searchString || '';

  // clears network timeoutRef which serves as await for animation
  clearTimeout(context.networkTimeoutRef);

  // handles request spam by canceling the previous ones
  if (context.abortController) context.abortController.abort();
  context.abortController = new AbortController();

  // syncing the internal timeout ref with external network ref
  let scopedTimeoutRef: ReturnType<typeof setTimeout>;

  const isSearchRequest = typeof newData === 'string';

  const searchString = typeof newData === 'string' ? newData : newData?.partNumber;

  try {
    if (!searchString || searchString.trim().length === 0) {
      context.isError = false;
      context[loadingKey] = false;
      context.searchString = '';
      context.partLookup = undefined;
      return;
    }

    context[loadingKey] = true;

    await new Promise(r => {
      scopedTimeoutRef = setTimeout(r, 1000);
      context.networkTimeoutRef = scopedTimeoutRef;
    });

    context.searchString = searchString;

    const partResponse = isSearchRequest ? await getPartLookup(context, { scopedTimeoutRef }) : (newData as PartLookupDTO);

    if (context.networkTimeoutRef === scopedTimeoutRef) {
      if (!partResponse) throw new Error('wrongResponseFormat');
      if (beforeAssignment) context.partLookup = await beforeAssignment(partResponse, { scopedTimeoutRef });
      else context.partLookup = partResponse;
    }

    context.errorMessage = null;
    context[loadingKey] = false;
    context.isError = false;
  } catch (error) {
    if (error && error?.name === 'AbortError') return;
    smartInvokable.bind(context)(context.errorCallback, error.message);
    console.error(error);
    context.setErrorMessage(error.message);
  }
};

export const setPartLookupErrorState = (context: PartLookupComponent, message: ErrorKeys) => {
  // @ts-ignore
  const loadingKey = context?.localLoadingName ? context?.localLoadingName : 'isLoading';

  context.isError = true;
  context[loadingKey] = false;
  context.errorMessage = message;
  context.partLookup = undefined;
};

type GetPartLookupProps = {
  scopedTimeoutRef: ReturnType<typeof setTimeout>;
};

export async function getPartLookup(context: PartLookupComponent, generalProps: GetPartLookupProps): Promise<PartLookupDTO> {
  const { scopedTimeoutRef } = generalProps;

  const handleResult = (newPartLookup: PartLookupDTO): PartLookupDTO => {
    if (context?.networkTimeoutRef === scopedTimeoutRef) {
      if (!newPartLookup && context?.searchString) throw new Error('wrongResponseFormat');

      smartInvokable.bind(context)(context?.loadedResponse, newPartLookup);
      return newPartLookup;
    }
  };

  if (context?.isDev) {
    const newData = context?.mockData[context?.searchString];

    return handleResult(newData);
  } else {
    if (!context?.endpoint) throw new Error('noBaseUrl');

    const fetchConfig: FetchFromProps = { signal: context?.abortController.signal, additionalPath: '/' + context.searchString };

    const response = await fetchFrom(context.endpoint, fetchConfig);

    if (response.status === 204) throw new Error('noPartsFound');

    if (!response.ok) throw new Error('wildCard');

    const newData = (await response.json()) as PartLookupDTO;

    return handleResult(newData);
  }
}
