import { describe, expect, it, vi } from 'vitest';
import { buildOptionsUrl, fetchOptions, mapOptionsResponse } from '../src/options-source.js';

describe('buildOptionsUrl', () => {
  it('appends queryParams to the source url', () => {
    const url = buildOptionsUrl({
      url: 'https://api.test/public/company-branch',
      queryParams: { services: 'body-and-paint' },
    });
    expect(url).toBe('https://api.test/public/company-branch?services=body-and-paint');
  });

  it('overwrites params already present in the url (override wins)', () => {
    const url = buildOptionsUrl({
      url: 'https://api.test/public/company-branch?services=new-vehicle-sale&top=50',
      queryParams: { services: 'auto-repair-and-maintenance' },
    });
    const parsed = new URL(url);
    expect(parsed.searchParams.get('services')).toBe('auto-repair-and-maintenance');
    expect(parsed.searchParams.get('top')).toBe('50');
  });

  it('passes the url through when there are no params', () => {
    expect(buildOptionsUrl({ url: 'https://api.test/public/city' })).toBe('https://api.test/public/city');
  });
});

describe('mapOptionsResponse', () => {
  // The confirmed TIQ public-endpoint shape: flat array of { ID, Name, … }.
  const tiqBody = [
    { ID: 'L0VEX', Name: 'Erbil', IntegrationId: 'Erbil' },
    { ID: 'MWjQ0', Name: 'Baghdad', IntegrationId: 'Baghdad' },
  ];

  it('defaults to ID/Name paths (ShiftEntity public shape)', () => {
    expect(mapOptionsResponse(tiqBody, { url: 'https://x.test' })).toEqual([
      { id: 'L0VEX', label: 'Erbil' },
      { id: 'MWjQ0', label: 'Baghdad' },
    ]);
  });

  it('honors custom value/label paths', () => {
    const options = mapOptionsResponse(tiqBody, {
      url: 'https://x.test',
      valuePath: 'IntegrationId',
      labelPath: 'Name',
    });
    expect(options[0]).toEqual({ id: 'Erbil', label: 'Erbil' });
  });

  it('digs into itemsPath for wrapped responses and supports nested paths', () => {
    const body = { data: { items: [{ key: { id: 7 }, display: 'Seven' }] } };
    const options = mapOptionsResponse(body, {
      url: 'https://x.test',
      itemsPath: 'data.items',
      valuePath: 'key.id',
      labelPath: 'display',
    });
    expect(options).toEqual([{ id: '7', label: 'Seven' }]);
  });

  it('skips items without a value and falls back to the value for missing labels', () => {
    const body = [{ ID: '', Name: 'empty' }, { Name: 'no id' }, { ID: 'x' }];
    expect(mapOptionsResponse(body, { url: 'https://x.test' })).toEqual([{ id: 'x', label: 'x' }]);
  });

  it('throws when the body (or itemsPath) is not an array', () => {
    expect(() => mapOptionsResponse({ nope: true }, { url: 'https://x.test' })).toThrow(/not an array/);
    expect(() =>
      mapOptionsResponse({ data: {} }, { url: 'https://x.test', itemsPath: 'data.items' }),
    ).toThrow(/data\.items/);
  });
});

describe('fetchOptions', () => {
  const okResponse = (body: unknown) =>
    ({ ok: true, status: 200, json: async () => body }) as Response;

  it('sends Accept-Language from the active locale; explicit headers win', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(okResponse([{ ID: 'a', Name: 'A' }]));
    await fetchOptions(
      { url: 'https://x.test/city', headers: { 'X-Public-Key': 'demo' } },
      { locale: 'ar', fetchImpl },
    );
    expect(fetchImpl).toHaveBeenCalledWith('https://x.test/city', {
      headers: { 'Accept-Language': 'ar', 'X-Public-Key': 'demo' },
    });

    const overriding = vi.fn().mockResolvedValue(okResponse([]));
    await fetchOptions(
      { url: 'https://x.test/city', headers: { 'Accept-Language': 'ku' } },
      { locale: 'ar', fetchImpl: overriding },
    );
    expect(overriding.mock.calls[0]![1].headers['Accept-Language']).toBe('ku');
  });

  it('maps the fetched body', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(okResponse([{ ID: 'MJLGr', Name: 'Saidya' }]));
    const options = await fetchOptions({ url: 'https://x.test/branch' }, { fetchImpl });
    expect(options).toEqual([{ id: 'MJLGr', label: 'Saidya' }]);
  });

  it('throws on HTTP errors', async () => {
    const fetchImpl = vi.fn().mockResolvedValue({ ok: false, status: 503 } as Response);
    await expect(fetchOptions({ url: 'https://x.test/city' }, { fetchImpl })).rejects.toThrow(/503/);
  });
});
