import { Build } from '@stencil/core';
import { readFileSync } from 'fs';
import { join } from 'path';
import ts from 'typescript';

import { version } from '../../../package.json';
import { fetchPackageFile } from '~lib/package-files';
import { adpFetch, formRequestInfo, optionsRequestInfo, requestInfo } from './adp-fetch';
import { HOST_CONFIG_KEY, peekHostConfig } from './host-config';

// host-loader.js is shipped as plain ESM, which Jest does not transform.
function loadHostLoader(): { configureAdpHost: (options: Record<string, unknown>) => void } {
  const source = readFileSync(join(__dirname, '../../integration/host-loader.js'), 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2021 } });
  const module = { exports: {} };
  new Function('exports', 'module', outputText)(module.exports, module);

  return module.exports as { configureAdpHost: (options: Record<string, unknown>) => void };
}

const { configureAdpHost } = loadHostLoader();

const element = () => document.createElement('test-drive-form');
const ok = (status = 200) => ({ ok: status < 400, status }) as Response;

const originalFetch = globalThis.fetch;

afterEach(() => {
  delete (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY];
  globalThis.fetch = originalFetch;
  jest.restoreAllMocks();
});

describe('adpFetch without a host fetch', () => {
  it('calls the global fetch with the same two arguments and returns its response', async () => {
    const response = ok();
    const fetchMock = jest.fn(async () => response);
    globalThis.fetch = fetchMock as unknown as typeof fetch;
    const init: RequestInit = { method: 'POST', headers: { 'Accept-Language': 'en' }, body: '{"a":1}' };

    const result = await adpFetch('https://example.invalid/submit', init, requestInfo('submit', element()));

    expect(result).toBe(response);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [call] = fetchMock.mock.calls as unknown[][];
    expect(call).toHaveLength(2);
    expect(call[0]).toBe('https://example.invalid/submit');
    expect(call[1]).toBe(init);
  });

  it('resolves the global fetch at call time, so a host that patches it later is still used', async () => {
    const first = jest.fn(async () => ok());
    const second = jest.fn(async () => ok(201));
    globalThis.fetch = first as unknown as typeof fetch;
    const info = requestInfo('lookup', element());

    await adpFetch('https://example.invalid/one', {}, info);
    globalThis.fetch = second as unknown as typeof fetch;
    const later = await adpFetch('https://example.invalid/two', {}, info);

    expect(first).toHaveBeenCalledTimes(1);
    expect(second).toHaveBeenCalledWith('https://example.invalid/two', {});
    expect(later.status).toBe(201);
  });

  it('passes a rejection through unchanged', async () => {
    const failure = new TypeError('Failed to fetch');
    globalThis.fetch = jest.fn(async () => {
      throw failure;
    }) as unknown as typeof fetch;

    await expect(adpFetch('https://example.invalid/submit', {}, requestInfo('submit', element()))).rejects.toBe(failure);
  });
});

describe('adpFetch with configureAdpHost({ fetch })', () => {
  it('hands the host the url, the init and the request info, and never calls the global fetch', async () => {
    const globalFetch = jest.fn(async () => ok());
    globalThis.fetch = globalFetch as unknown as typeof fetch;
    const response = ok(202);
    const hostFetch = jest.fn(async () => response);
    configureAdpHost({ fetch: hostFetch });
    const el = element();
    const init: RequestInit = { method: 'GET', headers: { 'Accept-Language': 'ru' } };

    const result = await adpFetch('https://example.invalid/branches', init, formRequestInfo('options', { context: { el } }, 'companyBranchId'));

    expect(result).toBe(response);
    expect(globalFetch).not.toHaveBeenCalled();
    expect(hostFetch).toHaveBeenCalledWith('https://example.invalid/branches', init, { kind: 'options', tag: 'test-drive-form', element: el, field: 'companyBranchId' });
  });

  it('turns a host function that throws synchronously into a rejection', async () => {
    const failure = new Error('host refused');
    configureAdpHost({
      fetch: () => {
        throw failure;
      },
    });

    const request = adpFetch('https://example.invalid/x', {}, requestInfo('calendar', element(), 'bookingCalendar'));

    expect(request).toBeInstanceOf(Promise);
    await expect(request).rejects.toBe(failure);
  });

  it('is cleared by passing anything but a function, and other keys stay', async () => {
    const globalFetch = jest.fn(async () => ok());
    globalThis.fetch = globalFetch as unknown as typeof fetch;
    configureAdpHost({ fetch: jest.fn(), assetsBaseUrl: 'https://app.example/adp/' });
    configureAdpHost({ fetch: null });

    await adpFetch('https://example.invalid/y', {}, requestInfo('lookup', element()));

    expect(globalFetch).toHaveBeenCalledTimes(1);
    expect(Object.keys(peekHostConfig())).toEqual(['assetsBaseUrl']);
  });
});

describe('request info', () => {
  it('names the top-level tag, the element and, only when given, the field', () => {
    const el = element();

    expect(requestInfo('submit', el)).toEqual({ kind: 'submit', tag: 'test-drive-form', element: el });
    expect(optionsRequestInfo({ form: { context: { el } }, name: 'vehicle' })).toEqual({ kind: 'options', tag: 'test-drive-form', element: el, field: 'vehicle' });
    expect(formRequestInfo('upload', undefined, 'file')).toEqual({ kind: 'upload', tag: '', element: undefined, field: 'file' });
  });
});

describe('configureAdpHost({ assetsBaseUrl })', () => {
  const build = Build as { isDev: boolean };
  const wasDev = build.isDev;

  afterEach(() => {
    build.isDev = wasDev;
  });

  it('unset: a prod build still reads the CDN copy of its own version', async () => {
    build.isDev = false;
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(ok());

    await fetchPackageFile('locales/en.json');

    expect(fetchMock).toHaveBeenCalledWith(`https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/locales/en.json`);
  });

  it('set: every package file comes from the base, a trailing slash is added, and a 404 has no CDN fallback', async () => {
    build.isDev = false;
    configureAdpHost({ assetsBaseUrl: 'https://app.example/adp' });
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(ok(404));

    const missing = await fetchPackageFile('locales/forms/uz.json');
    await fetchPackageFile('mocks/vehicle-lookup.json');

    expect(missing.status).toBe(404);
    expect(fetchMock.mock.calls).toEqual([['https://app.example/adp/locales/forms/uz.json'], ['https://app.example/adp/mocks/vehicle-lookup.json']]);
  });

  it('set: a relative base resolves against the document and wins in a dev build too', async () => {
    build.isDev = true;
    configureAdpHost({ assetsBaseUrl: './adp/' });
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(ok());

    await fetchPackageFile('locales/en.json');

    expect(fetchMock).toHaveBeenCalledWith(new URL('./adp/locales/en.json', document.baseURI).href);
    expect(String(fetchMock.mock.calls[0][0])).not.toContain('jsdelivr');
  });

  it('cleared: an empty value restores the default', async () => {
    build.isDev = false;
    configureAdpHost({ assetsBaseUrl: 'https://app.example/adp/' });
    configureAdpHost({ assetsBaseUrl: '' });
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue(ok());

    await fetchPackageFile('locales/en.json');

    expect(peekHostConfig()).toEqual({});
    expect(String(fetchMock.mock.calls[0][0])).toContain(`adp-web-components@${version}/dist/locales/en.json`);
  });
});
