import { MockFileName, MockFiles } from './types';
import { localizeMockAssets } from './mock-assets';
import { fetchPackageFile } from '~lib/package-files';

const cachedMocks = {};

export async function getMockFile<T>(mockFileName: MockFileName, externalUrl: string = ''): Promise<Record<string, T>> {
  const fileName = MockFiles[mockFileName];

  if (!fileName || !fileName.length) throw new Error(`Mock file not found for: ${mockFileName}`);

  const response = await requestMockFile(fileName, externalUrl);

  return response;
}

async function requestMockFile(mockFile: string, externalUrl: string) {
  const external = externalUrl?.trim();
  // The same file kind exists in every generated environment. Caching by the
  // old logical filename made an environment change resurrect the first file
  // after toggling back into development mode.
  const fetchUrl = external || 'package:mocks/' + mockFile;

  try {
    if (cachedMocks[fetchUrl]) return await cachedMocks[fetchUrl];

    // A host's own URL as given; otherwise the package's copy — the dev server's,
    // or this version's on the CDN (see ~lib/package-files).
    const fetchPromise = (external ? fetch(external) : fetchPackageFile('mocks/' + mockFile))
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
