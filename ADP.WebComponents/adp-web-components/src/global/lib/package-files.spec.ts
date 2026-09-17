import { Build } from '@stencil/core';

import { version } from '../../../package.json';
import { cdnFileBase, devFileBase, fetchPackageFile } from './package-files';

describe('package files', () => {
  const build = Build as { isDev: boolean };
  const wasDev = build.isDev;

  afterEach(() => {
    build.isDev = wasDev;
    document.head.innerHTML = '';
    jest.restoreAllMocks();
  });

  it('a dev build reads from the server the bundle came from, whatever its port', () => {
    const script = document.createElement('script');
    script.src = 'http://localhost:3001/build/shift-components.esm.js';
    document.head.appendChild(script);

    expect(devFileBase()).toBe('http://localhost:3001/');
  });

  it('a dev build without a visible bundle tag falls back to the page root', () => {
    expect(devFileBase()).toBe(new URL('/', document.baseURI).href);
  });

  it('a prod build reads its own version from the CDN', async () => {
    build.isDev = false;
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true, status: 200 } as Response);

    await fetchPackageFile('locales/en.json');

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith(`https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/locales/en.json`);
  });

  it('an unpublished version stands down to the newest published one, and nothing else does', async () => {
    build.isDev = false;
    const fetchMock = jest
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce({ ok: false, status: 404 } as Response)
      .mockResolvedValueOnce({ ok: true, status: 200 } as Response)
      .mockResolvedValueOnce({ ok: false, status: 500 } as Response);

    const fallen = await fetchPackageFile('locales/en.json');

    expect(fallen.ok).toBe(true);
    expect(fetchMock).toHaveBeenLastCalledWith(cdnFileBase('latest') + 'locales/en.json');

    const failed = await fetchPackageFile('locales/ar.json');

    expect(failed.status).toBe(500);
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
