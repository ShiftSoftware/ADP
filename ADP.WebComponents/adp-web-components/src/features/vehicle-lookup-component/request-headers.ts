import { DotNetObjectReference } from '~features/blazor-ref';

import { vehicleRequestHeaders } from './types';

/**
 * Supplies the request headers for a call made on the host's behalf. Returning a promise lets the
 * host refresh an access token before answering.
 */
export type RequestHeadersProvider = () => Promise<object | null | undefined> | object | null | undefined;

/**
 * What a component needs in order to build the headers for any request it makes itself — the
 * vehicle lookup, but also the follow-up calls that used to reuse a stale snapshot: the trace
 * fetches, the claim submission, the unauthorized campaign lookup.
 */
export interface RequestHeadersSource {
  /** Static headers the host set on the element. The floor every request starts from. */
  headers: object;
  /** The headers the host passed with the most recent fetchVin call. */
  lastRequestHeaders?: object;
  /** A host callback that answers with the current headers, asked on every request. */
  requestHeadersProvider?: RequestHeadersProvider;
  /** Name of a [JSInvokable] method on the Blazor reference that answers with the current headers. */
  blazorRequestHeadersProvider?: string;
  blazorRef?: DotNetObjectReference;
}

const hasEntries = (value?: object | null): value is object => !!value && typeof value === 'object' && Object.keys(value).length > 0;

/**
 * The headers for a request the component is about to make.
 *
 * The token a Blazor host hands over with `fetchVin(vin, headers)` is a snapshot: it is refreshed
 * the moment the user searches, and not again. A trace opened twenty minutes later used to carry
 * that same expired bearer, because it read the `headers` prop instead of asking the host. Every
 * request now resolves its headers through here, freshest source first:
 *
 * 1. `explicit` — headers passed with this very call (a `fetchVin(vin, headers)`), which are also
 *    remembered as `lastRequestHeaders` for the follow-up calls;
 * 2. the host's provider — a JavaScript callback, or a `[JSInvokable]` Blazor method — which can
 *    refresh the token before answering;
 * 3. `lastRequestHeaders`, then the static `headers` prop.
 *
 * The identity props (`cityId`, `userId`, …) are mapped onto their header names last, exactly as
 * the vehicle lookup itself has always done, so a follow-up call carries the same context.
 */
export const resolveRequestHeaders = async (context: RequestHeadersSource & Record<string, any>, explicit?: object): Promise<Record<string, string>> => {
  let provided: object | null | undefined;

  if (hasEntries(explicit)) {
    context.lastRequestHeaders = explicit;
    provided = explicit;
  } else if (context.requestHeadersProvider) {
    provided = await context.requestHeadersProvider();
  } else if (context.blazorRequestHeadersProvider && context.blazorRef) {
    provided = await context.blazorRef.invokeMethodAsync(context.blazorRequestHeadersProvider);
  }

  const merged: Record<string, string> = {
    ...((context.headers as Record<string, string>) || {}),
    ...((context.lastRequestHeaders as Record<string, string>) || {}),
    ...((provided as Record<string, string>) || {}),
  };

  Object.entries(vehicleRequestHeaders).forEach(([componentHeaderKey, headerField]) => {
    if (context[componentHeaderKey]) merged[headerField] = context[componentHeaderKey];
  });

  return merged;
};
