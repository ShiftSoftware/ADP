import { getMockFile } from './get-mock-files';

describe('getMockFile', () => {
  afterEach(() => jest.restoreAllMocks());

  it('caches generated fixture files by their full environment URL', async () => {
    const response = (key: string) => ({ ok: true, json: async () => ({ [key]: { vin: key } }) }) as Response;
    const fetchMock = jest.spyOn(globalThis, 'fetch').mockImplementation(async input => response(String(input)));
    const firstUrl = 'https://example.invalid/mocks/generated/alpha-market/vehicle-lookup.json';
    const secondUrl = 'https://example.invalid/mocks/generated/beta-market/vehicle-lookup.json';

    expect(Object.keys(await getMockFile('vehicle-lookup', firstUrl))).toEqual([firstUrl]);
    expect(Object.keys(await getMockFile('vehicle-lookup', secondUrl))).toEqual([secondUrl]);
    expect(Object.keys(await getMockFile('vehicle-lookup', firstUrl))).toEqual([firstUrl]);

    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
