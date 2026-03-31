/**
 * Mock data loader with source switching.
 *
 * Default: uses the inline `mockData` global from mock-data.js
 * With ?source=generated: fetches from the generator output JSON
 *
 * Optional query params:
 *   ?source=generated          — use generated mock data
 *   ?source=generated&env=broker-dealer  — use a specific environment
 */
async function loadMockData(mockFileName) {
  const params = new URLSearchParams(location.search);
  const source = params.get('source');

  if (source === 'generated') {
    const env = params.get('env') || 'standard-dealer';
    const url = `/mocks/generated/${env}/${mockFileName}.json`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Failed to load ${url}: ${res.status}`);
    return res.json();
  }

  // Default: return the inline mockData variable from mock-data.js
  // (declared with const, so available as a global but not on window)
  return typeof mockData !== 'undefined' ? mockData : undefined;
}
