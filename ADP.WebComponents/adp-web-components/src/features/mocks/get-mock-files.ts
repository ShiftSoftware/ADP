import { Build } from '@stencil/core';

import { version } from '../../../package.json';
import { MockFileName, MockFiles } from './types';

const cachedMocks = {};

export async function getMockFile<T>(mockFileName: MockFileName, externalUrl: string = ''): Promise<Record<string, T>> {
  const fileName = MockFiles[mockFileName];

  if (!fileName || !fileName.length) throw new Error(`Mock file not found for: ${mockFileName}`);

  const response = await requestMockFile(fileName, externalUrl);

  return response;
}

async function requestMockFile(mockFile: string, externalUrl: string) {
  if (cachedMocks[mockFile]) return await cachedMocks[mockFile];

  try {
    let fetchUrl;

    if (!!externalUrl && !!externalUrl.trim().length) fetchUrl = externalUrl;
    else if (Build.isDev) fetchUrl = 'http://localhost:3000/mocks/' + mockFile;
    else fetchUrl = `https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/mocks/${mockFile}`;

    const fetchPromise = fetch(fetchUrl).then(res => {
      if (!res.ok) delete cachedMocks[mockFile];
      return res.json();
    });

    cachedMocks[mockFile] = fetchPromise;

    const result = await fetchPromise;

    const count = Object.keys(result).length;
    console.log(`✅ Loaded mock: ${mockFile} (${count} items)`);
    console.table(
      Object.entries(result).map(([key, value]) => ({
        key,
        value,
      })),
    );

    cachedMocks[mockFile] = result;

    return result;
  } catch (error) {
    delete cachedMocks[mockFile];
    return {};
  }
}
