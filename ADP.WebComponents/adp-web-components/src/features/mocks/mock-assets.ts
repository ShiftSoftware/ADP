import { Build } from '@stencil/core';

import { devFileBase } from '~lib/package-files';

/**
 * The generated fixtures point their pictures (paint panels, company badges, claim documents,
 * accessories) at the copy of `dist/mocks/assets/` on the CDN, so one URL works wherever a fixture
 * is loaded. On the CDN those files exist only from the first release that carries them; the dev
 * server has them right away, at `/mocks/assets/` (the build copies `features/mocks/data` there).
 * In a dev build every such URL is rewritten to the local copy, whichever way the fixture arrived
 * (the component's own fetch or a harness pushing `setMockData`). Prod builds leave the data alone.
 */
const CDN_MOCKS_PREFIX = /^https:\/\/cdn\.jsdelivr\.net\/npm\/adp-web-components@[^/]+\/dist\/mocks\//;

export function localizeMockAssets<T>(value: T): T {
  if (!Build.isDev) return value;

  // Whichever server the bundle came from — not a fixed port (see ~lib/package-files).
  return rewrite(value, devFileBase() + 'mocks/') as T;
}

function rewrite(value: unknown, base: string): unknown {
  if (typeof value === 'string') return value.replace(CDN_MOCKS_PREFIX, base);

  if (Array.isArray(value)) return value.map(entry => rewrite(entry, base));

  if (value && typeof value === 'object') {
    const copy: Record<string, unknown> = {};

    for (const [key, entry] of Object.entries(value)) copy[key] = rewrite(entry, base);

    return copy;
  }

  return value;
}
