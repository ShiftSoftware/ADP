import { Build } from '@stencil/core';

import { version } from '../../../package.json';

/**
 * Where the package's own files — the locale strings under `locales/`, the generated fixtures and
 * their pictures under `mocks/` — are read from at runtime.
 *
 * A dev build reads them from the server the bundle itself was loaded from, whichever port it
 * got: the dev server copies `locales/` and `mocks/` to its root. It used to be pinned to
 * `localhost:3000`, so a second dev server (which lands on :3001) or a copy of `www/` served from
 * anywhere else rendered every string as `undefined`.
 *
 * A prod build reads the CDN copy of ITS OWN version, so a host still on 0.4.2 keeps 0.4.2's keys
 * after 0.4.3 ships. While its own version is not published yet — a prod build made before the
 * release tag, which is what the showcase is until the tag lands — the CDN answers 404, and the
 * newest published version is used instead of rendering nothing. `@latest` is resolved by the
 * CDN, so nothing here has to know what that version is.
 */
const CDN = 'https://cdn.jsdelivr.net/npm/adp-web-components@';

/** The dev server's root, as a URL string with a trailing slash. Dev builds only. */
export function devFileBase(): string {
  const script = document.querySelector<HTMLScriptElement>('script[src*="shift-components"]');

  return script?.src ? new URL('../', script.src).href : new URL('/', document.baseURI).href;
}

/** The CDN copy of this version's `dist/`, with a trailing slash. */
export function cdnFileBase(tag: string = version): string {
  return `${CDN}${tag}/dist/`;
}

/**
 * Fetches one of the package's files (`locales/en.json`, `mocks/vehicle-lookup.json`) from
 * wherever this build reads them, and returns the response as-is — callers decide what a
 * non-ok answer means to them.
 */
export async function fetchPackageFile(path: string): Promise<Response> {
  if (Build.isDev) return fetch(devFileBase() + path);

  const pinned = await fetch(cdnFileBase() + path);

  // 404 and nothing else: an unpublished version is the one case the newest one stands in for.
  return pinned.status === 404 ? fetch(cdnFileBase('latest') + path) : pinned;
}
