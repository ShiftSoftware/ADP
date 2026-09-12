import { Build } from '@stencil/core';

import { version } from '../../../package.json';
import { MockFileName, MockFiles } from './types';
import { localizeMockAssets } from './mock-assets';

const cachedMocks = {};

export async function getMockFile<T>(mockFileName: MockFileName, externalUrl: string = ''): Promise<Record<string, T>> {
  const fileName = MockFiles[mockFileName];

  if (!fileName || !fileName.length) throw new Error(`Mock file not found for: ${mockFileName}`);

  const response = await requestMockFile(fileName, externalUrl);

  return response;
}

async function requestMockFile(mockFile: string, externalUrl: string) {
  let fetchUrl = externalUrl?.trim();

  try {
    if (!fetchUrl && Build.isDev) fetchUrl = 'http://localhost:3000/mocks/' + mockFile;
    else if (!fetchUrl) fetchUrl = `https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/mocks/${mockFile}`;

    // The same file kind exists in every generated environment. Caching by the
    // old logical filename made an environment change resurrect the first file
    // after toggling back into development mode.
    if (cachedMocks[fetchUrl]) return await cachedMocks[fetchUrl];

    const fetchPromise = fetch(fetchUrl)
      .then(res => {
        if (!res.ok) delete cachedMocks[fetchUrl];
        return res.json();
      })
      // Fixture pictures point at the CDN copy of the package; a dev build reads the dev server's.
      .then(localizeMockAssets);

    cachedMocks[fetchUrl] = fetchPromise;

    const result = await fetchPromise;

    const count = Object.keys(result).length;
    console.log(`✅ Loaded mock: ${mockFile} (${count} items)`);
    console.table(
      Object.entries(result).map(([key, value]) => ({
        key,
        value,
      })),
    );

    cachedMocks[fetchUrl] = result;

    return result;
  } catch {
    delete cachedMocks[fetchUrl];
    return {};
  }
}
