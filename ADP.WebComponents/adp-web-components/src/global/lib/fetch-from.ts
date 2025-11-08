import { LanguageKeys } from '~features/multi-lingual';

export type QueryString = Record<string, string | number | boolean | undefined | null> | string | undefined;

export type EndpointObject = {
  url: string;
  body?: object;
  headers?: object;
  method?: string;
  query?: QueryString;
};

export type Endpoint = string | EndpointObject;

export type FetchFromProps = { headers?: any; method?: 'POST' | 'GET'; language?: LanguageKeys; body?: object; signal?: AbortSignal; query?: QueryString; additionalPath?: string };

export function buildQueryArray(query?: QueryString): string[] {
  if (!query) return [];

  if (typeof query === 'string') {
    const clean = query.startsWith('?') ? query.slice(1) : query;
    return clean
      .split('&')
      .filter(Boolean)
      .map(pair => pair.trim());
  }

  return Object.entries(query)
    .filter(([_, v]) => v !== undefined && v !== null)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`);
}

export function parseEndpointObject(endpoint: Endpoint): EndpointObject {
  let endpointObject: EndpointObject;

  try {
    if (typeof endpoint === 'string') endpointObject = JSON.parse(endpoint);
    else endpointObject = endpoint;
  } catch (error) {
    endpointObject = { url: endpoint as string };
  }

  return endpointObject;
}

export function fetchFrom(endpoint: Endpoint, props: FetchFromProps = {}) {
  let endpointObject = parseEndpointObject(endpoint);

  const defaultHeaders: Record<string, string> = {
    'Accept': 'application/json',
    'Content-Type': 'application/json',
    'Accept-Language': props?.language || 'en',
  };

  const propsQueryArray = buildQueryArray(props?.query);
  const endpointQueryArray = buildQueryArray(endpointObject?.query);

  const query = [...propsQueryArray, ...endpointQueryArray].join('&');

  const queryString = query ? `?${query}` : '';

  const method: FetchFromProps['method'] = (endpointObject?.method || props?.method || 'GET') as FetchFromProps['method'];

  const config: RequestInit = {
    method,
    signal: props?.signal,
    headers: { ...defaultHeaders, ...(endpointObject?.headers || {}), ...(props?.headers || {}) },
  };

  if (method !== 'GET') config.body = JSON.stringify({ ...(props?.body || {}), ...(endpointObject?.body || {}) });

  const url = `${endpointObject.url + (props?.additionalPath || '')}${queryString}`;

  return fetch(url.replace('//', '/'), config);
}
