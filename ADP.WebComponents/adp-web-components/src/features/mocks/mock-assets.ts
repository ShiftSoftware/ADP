import { Build } from '@stencil/core';

/**
 * The generated fixtures point their pictures (paint panels, company badges, claim documents,
 * accessories) at the copy of `dist/mocks/assets/` on the CDN, so one URL works wherever a fixture
 * is loaded. On the CDN those files exist only from the first release that carries them; the dev
 * server has them right away, at `/mocks/assets/` (the build copies `features/mocks/data` there).
 * In a dev build every such URL is rewritten to the local copy, whichever way the fixture arrived
 * (the component's own fetch or a harness pushing `setMockData`). Prod builds leave the data alone.
 */
const CDN_MOCKS_PREFIX = /^https:\/\/cdn\.jsdelivr\.net\/npm\/adp-web-components@[^/]+\/dist\/mocks\//;
const DEV_MOCKS_BASE = 'http://localhost:3000/mocks/';

export function localizeMockAssets<T>(value: T): T {
  if (!Build.isDev) return value;

  return rewrite(value) as T;
}

function rewrite(value: unknown): unknown {
  if (typeof value === 'string') return value.replace(CDN_MOCKS_PREFIX, DEV_MOCKS_BASE);

  if (Array.isArray(value)) return value.map(rewrite);

  if (value && typeof value === 'object') {
    const copy: Record<string, unknown> = {};

    for (const [key, entry] of Object.entries(value)) copy[key] = rewrite(entry);

    return copy;
  }

  return value;
}
