import { Build } from '@stencil/core';

import { version } from '../../../package.json';
import { MockFileName, MockFiles } from './types';

const cachedMocks = {};

export async function getMockFile<T>(mockFileName: MockFileName): Promise<Record<string, T>> {
  const fileName = MockFiles[mockFileName];

  if (!fileName || !fileName.length) throw new Error(`Mock file not found for: ${mockFileName}`);

  const response = await requestMockFile(fileName);

  return response;
}

async function requestMockFile(mockFile: string) {
  if (cachedMocks[mockFile]) return await cachedMocks[mockFile];

  try {
    const fetchPromise = (
      Build.isDev ? fetch('http://localhost:3000/mocks/' + mockFile) : fetch(`https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/mocks/${mockFile}`)
    ).then(res => {
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
