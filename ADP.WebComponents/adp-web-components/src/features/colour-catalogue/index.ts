/**
 * The exterior paint-colour catalogue: a code → name/hex/finish table, keyed by a brand *slug*, that
 * a panel may fall back to when the host's own backend resolved no name, and that is the **only**
 * source of a colour swatch anywhere in the library.
 *
 * Three things make this shape the one it is.
 *
 * **It is keyed by a slug, never by a `brandID`.** A `brandID` is a per-deployment hash id from the
 * host's identity system; a slug is a stable, human-readable name a catalogue can be written against
 * once and shipped to every deployment. The host supplies the `brandID → slug` map itself
 * (`brandSlugs`), and `slugOf` has no fallback: an id the map does not carry resolves to no slug,
 * which resolves to no entry, which draws no swatch. A guessed slug would put an invented colour
 * beside a real code, which is the one thing the unknown-code rule forbids.
 *
 * **It is loaded from the bundle, lazily, and never fetched.** The locales and the demo fixtures are
 * read from the package's own copy over the network (`~lib/package-files`); this is not. A colour
 * catalogue reached over the wire is a third-party dependency on the frame a service advisor is
 * reading a paint code, and a host that never configures `brandSlugs` should pay nothing for it at
 * all. So it is a dynamic `import()` of a JSON module: its own chunk, fetched from the host's own
 * bundle the first time a panel has a slug to look up, and cached for the page thereafter.
 *
 * **It is approximate, and says so.** `approxHex` is an approximation of paint under unknown light,
 * never a paint reference. The provenance — every source, the retrieval date, the method, the
 * accuracy caveat and why there is no interior table — is beside the data in
 * `vehicle-colors.meta.json`, which ships with it so the asset cannot travel without its evidence.
 */

/** How the paint reads, not what it is made of: the swatch says "a finish, not a flat" and no more. */
export type ColourFinish = 'solid' | 'metallic' | 'pearl';

/**
 * One code's entry. `approxHex` is absent whenever no source gives a defensible value — 346 of the
 * 701 exterior entries — and an entry without one supplies a *name* and no swatch, which is a
 * complete answer and not a degraded one.
 */
export type ColourEntry = {
  name: string;
  approxHex?: string;
  finish: ColourFinish;
};

/** One brand's tables. `interior` is reserved and absent today — see the meta file's `interior` note. */
export type BrandColours = {
  exterior?: Record<string, ColourEntry>;
  interior?: Record<string, ColourEntry>;
};

export type ColourCatalogue = Record<string, BrandColours>;

/** `{ "<brandID>": "<slug>" }`, as a host passes it — an object, or the JSON string of one. */
export type BrandSlugs = Record<string, string>;

let catalogue: Promise<ColourCatalogue> | undefined;

/**
 * The catalogue, from this bundle's own chunk. Cached as the *promise*, so two panels asking on the
 * same frame share one chunk load; the cache is dropped on a failure so a later lookup can try
 * again rather than the page keeping an empty catalogue forever.
 */
export function loadColourCatalogue(): Promise<ColourCatalogue> {
  catalogue ??= import('./vehicle-colors.json')
    .then(module => ((module as { default?: unknown }).default ?? module) as ColourCatalogue)
    .catch(() => {
      catalogue = undefined;
      return {} as ColourCatalogue;
    });

  return catalogue;
}

/**
 * A host's map, whichever way it arrived. An object is taken as it is; a string is parsed, because a
 * Blazor or plain-HTML host can only set an attribute. Anything that is not a flat string map — a
 * malformed string, an array, a value that is not a string — is no map at all rather than a partial
 * one: a half-read map would resolve some brands and silently not others.
 */
export function parseBrandSlugs(raw?: BrandSlugs | string): BrandSlugs | undefined {
  if (!raw) return undefined;

  let value: unknown = raw;

  if (typeof raw === 'string') {
    try {
      value = JSON.parse(raw);
    } catch {
      return undefined;
    }
  }

  if (typeof value !== 'object' || value === null || Array.isArray(value)) return undefined;

  const entries = Object.entries(value as Record<string, unknown>).filter(([key, slug]) => !!key.trim() && typeof slug === 'string' && !!slug.trim());
  if (!entries.length) return undefined;

  return Object.fromEntries(entries.map(([key, slug]) => [key.trim(), (slug as string).trim()]));
}

/**
 * The slug for a brand id — **no fallback, no normalisation, no guess**. An unmapped id has no slug,
 * and a vehicle whose brand has no slug simply never gets a swatch.
 */
export function slugOf(brandID: string | undefined | null, slugs?: BrandSlugs): string | undefined {
  const id = String(brandID ?? '').trim();
  if (!id || !slugs) return undefined;

  return slugs[id];
}

/**
 * One brand's exterior table, or nothing. Separated from the lookup so a caller can hand a *pure*
 * table to a pure render function: the brand id stops here and never reaches a component that draws.
 */
export function exteriorColours(catalogueData: ColourCatalogue | undefined, slug: string | undefined): Record<string, ColourEntry> | undefined {
  if (!catalogueData || !slug) return undefined;

  return catalogueData[slug]?.exterior;
}

/**
 * The entry for one code. Exact match on the code as the record sends it, after a trim only: the
 * codes are upper-case alphanumerics (`1G3`, `070`, `923`) and a leading zero is part of the code,
 * so nothing is stripped, padded or case-folded into a neighbouring colour.
 */
export function colourEntry(table: Record<string, ColourEntry> | undefined, code: string | undefined): ColourEntry | undefined {
  const key = String(code ?? '').trim();
  if (!table || !key) return undefined;

  return table[key];
}
