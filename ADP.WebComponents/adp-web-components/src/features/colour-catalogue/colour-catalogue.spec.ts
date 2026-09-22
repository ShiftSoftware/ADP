import { readFileSync } from 'node:fs';
import path from 'node:path';

import { BrandColours, ColourCatalogue, colourEntry, exteriorColours, loadColourCatalogue, parseBrandSlugs, slugOf } from './index';

const here = path.resolve(process.cwd(), 'src', 'features', 'colour-catalogue');
const rawCatalogue = readFileSync(path.join(here, 'vehicle-colors.json'), 'utf8');
const catalogue = JSON.parse(rawCatalogue) as ColourCatalogue;
const meta = JSON.parse(readFileSync(path.join(here, 'vehicle-colors.meta.json'), 'utf8'));

const FINISHES = ['solid', 'metallic', 'pearl'];
const HEX = /^#[0-9a-fA-F]{6}$/;

const tables = (brand: BrandColours) => Object.entries(brand).filter(([, table]) => !!table) as [string, Record<string, { name: string; approxHex?: string; finish: string }>][];

/**
 * The asset is data a reviewer cannot read line by line, so the shape is asserted instead of
 * eyeballed: a broken entry here is an invented colour beside a real code on a service advisor's
 * screen, which is the failure mode the whole catalogue exists to avoid.
 */
describe('the colour catalogue asset', () => {
  it('is keyed by brand slug, then by table, then by code', () => {
    expect(Object.keys(catalogue).length).toBeGreaterThan(0);

    for (const [slug, brand] of Object.entries(catalogue)) {
      // A slug is a stable, human-readable name; a brandID is a per-deployment hash. A key that
      // looked like an id would mean the asset had been written against one deployment.
      expect(slug).toMatch(/^[a-z][a-z0-9-]*$/);
      expect(Object.keys(brand).every(key => key === 'exterior' || key === 'interior')).toBe(true);
    }
  });

  it('gives every entry a non-empty name, a known finish and — where present — a valid hex', () => {
    for (const [slug, brand] of Object.entries(catalogue)) {
      for (const [table, codes] of tables(brand)) {
        for (const [code, entry] of Object.entries(codes)) {
          const where = `${slug}.${table}.${code}`;

          expect(`${where}: ${typeof entry.name === 'string' && entry.name.trim() !== ''}`).toBe(`${where}: true`);
          expect(`${where}: ${FINISHES.includes(entry.finish)}`).toBe(`${where}: true`);
          // Absent is allowed and is the commoner case — an entry with no defensible hex supplies a
          // name and no swatch. Present but malformed would reach the DOM as a background colour.
          if (entry.approxHex !== undefined) expect(`${where}: ${HEX.test(entry.approxHex)}`).toBe(`${where}: true`);

          expect(Object.keys(entry).sort()).toEqual(entry.approxHex === undefined ? ['finish', 'name'] : ['approxHex', 'finish', 'name']);
        }
      }
    }
  });

  it('carries no duplicate code within a brand', () => {
    // JSON.parse collapses a duplicate key silently — the last one wins and the first colour is
    // lost — so duplicates are counted on the source text rather than on the parsed object.
    const occurrences = new Map<string, number>();
    for (const hit of rawCatalogue.match(/"[0-9A-Za-z]{1,8}"\s*:\s*\{\s*"name"/g) ?? []) {
      const code = hit.slice(1, hit.indexOf('"', 1));
      occurrences.set(code, (occurrences.get(code) ?? 0) + 1);
    }

    expect([...occurrences].filter(([, count]) => count > 1)).toEqual([]);

    // The scan is only worth anything if it found the entries: a regex that stopped matching — an
    // entry written with `approxHex` first, a reformat that put the brace on its own line — would
    // make this test pass over a file it never read. So the count it found must be the count the
    // parse holds, and duplicates must be counted per brand rather than across the whole file.
    const parsed = Object.values(catalogue).flatMap(brand => tables(brand).flatMap(([, codes]) => Object.keys(codes)));
    expect([...occurrences.values()].reduce((total, count) => total + count, 0)).toBe(parsed.length);

    for (const [slug, brand] of Object.entries(catalogue)) {
      for (const [table, codes] of tables(brand)) {
        const seen = [...rawCatalogue.matchAll(/"([0-9A-Za-z]{1,8})"\s*:\s*\{\s*"name"/g)].map(hit => hit[1]).filter(code => code in codes);
        expect(`${slug}.${table}: ${seen.length}`).toBe(`${slug}.${table}: ${Object.keys(codes).length}`);
      }
    }
  });

  it('ships its provenance beside it: every source, the retrieval date and the caveat', () => {
    expect(meta.retrieved).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(Array.isArray(meta.sources)).toBe(true);
    expect(meta.sources.length).toBeGreaterThan(1);

    for (const source of meta.sources) {
      expect(source.url).toMatch(/^https:\/\//);
      expect(typeof source.title).toBe('string');
      expect(source.title.trim()).not.toBe('');
    }

    // The accuracy caveat the panel shows on every swatch is a fact about the data, so it lives
    // with the data as well as in the locale files that translate it.
    expect(meta.caveat).toContain('approximate');
    expect(typeof meta.method).toBe('string');
    // The interior decision is recorded here because its evidence is an absence, and an absence is
    // exactly what the next reader will otherwise assume was an oversight.
    expect(meta.interior).toContain('no interior swatch');
  });

  it('is a table of codes under `exterior` under a slug, and nothing else', () => {
    // The shape the panel reads is `catalogue[slug].exterior[code]`, so each of those three levels
    // is asserted: a brand with no exterior table would resolve to no swatch for every vehicle it
    // covers, and a code key with a space or a lower-case letter would never match a record's code,
    // which is compared after a trim and nothing else.
    for (const [slug, brand] of Object.entries(catalogue)) {
      expect(`${slug}: ${typeof brand.exterior === 'object' && brand.exterior !== null}`).toBe(`${slug}: true`);

      for (const [table, codes] of tables(brand)) {
        expect(`${slug}.${table}: ${Object.keys(codes).length > 0}`).toBe(`${slug}.${table}: true`);

        for (const [code, entry] of Object.entries(codes)) {
          expect(`${slug}.${table}.${code}`).toMatch(/^[a-z0-9-]+\.[a-z]+\.[0-9A-Z]{1,8}$/);
          expect(`${slug}.${table}.${code}: ${typeof entry === 'object' && entry !== null && !Array.isArray(entry)}`).toBe(`${slug}.${table}.${code}: true`);
        }
      }
    }
  });

  it('counts what it says it counts, for every brand it ships', () => {
    // Every brand in the data has a line in the meta file and every line has a brand, so a brand
    // added without its provenance — or provenance for a brand that was dropped — fails here.
    expect(Object.keys(meta.brands).sort()).toEqual(Object.keys(catalogue).sort());

    for (const [slug, brand] of Object.entries(catalogue)) {
      const exterior = brand.exterior ?? {};
      const withHex = Object.values(exterior).filter(entry => entry.approxHex !== undefined);

      expect(`${slug}: ${Object.keys(exterior).length}`).toBe(`${slug}: ${meta.brands[slug].exterior}`);
      expect(`${slug}: ${withHex.length}`).toBe(`${slug}: ${meta.brands[slug].exteriorWithApproxHex}`);
      // No interior table ships, and the meta file says so in a figure as well as in prose.
      expect(brand.interior).toBeUndefined();
      expect(`${slug}: ${meta.brands[slug].interior}`).toBe(`${slug}: 0`);
    }
  });

  it('keeps a pure black or white only where the finish makes it defensible', () => {
    // A chart render that flattened a metallic or pearl paint to pure black or white is the chart's
    // shorthand, not the paint: "Black Sand Pearl" is not #000000. Those entries ship name-only.
    for (const [slug, brand] of Object.entries(catalogue)) {
      for (const [table, codes] of tables(brand)) {
        for (const [code, entry] of Object.entries(codes)) {
          if (!['#000000', '#ffffff'].includes(String(entry.approxHex).toLowerCase())) continue;
          expect(`${slug}.${table}.${code}: ${entry.finish}`).toBe(`${slug}.${table}.${code}: solid`);
        }
      }
    }
  });
});

describe('reaching the catalogue', () => {
  it('loads it from the bundle and caches the promise', async () => {
    const first = loadColourCatalogue();
    expect(loadColourCatalogue()).toBe(first);

    const loaded = await first;
    expect(loaded.toyota?.exterior?.['1G3']).toEqual({ name: 'Magnetic Gray Metallic', approxHex: '#59585d', finish: 'metallic' });
  });

  it('takes a host map as an object or as the JSON string of one', () => {
    expect(parseBrandSlugs({ '1': 'toyota' })).toEqual({ '1': 'toyota' });
    expect(parseBrandSlugs('{"1":"toyota","2":" toyota "}')).toEqual({ '1': 'toyota', '2': 'toyota' });
  });

  it('treats a map it cannot read as no map at all', () => {
    // A half-read map would resolve some brands and silently not others, which is worse than none.
    expect(parseBrandSlugs('not json')).toBeUndefined();
    expect(parseBrandSlugs('["toyota"]')).toBeUndefined();
    expect(parseBrandSlugs({ '1': 42 } as never)).toBeUndefined();
    expect(parseBrandSlugs(undefined)).toBeUndefined();
    expect(parseBrandSlugs('')).toBeUndefined();
  });

  it('never guesses a slug for a brand the host did not map', () => {
    const slugs = { '1': 'toyota' };

    expect(slugOf('1', slugs)).toBe('toyota');
    expect(slugOf('2', slugs)).toBeUndefined();
    expect(slugOf('1', undefined)).toBeUndefined();
    expect(slugOf(undefined, slugs)).toBeUndefined();
  });

  it('matches a code exactly, so a leading zero is never a neighbouring colour', () => {
    const table = exteriorColours(catalogue, 'toyota');

    expect(colourEntry(table, ' 070 ')?.name).toBe('Blizzard Pearl');
    expect(colourEntry(table, '70')).toBeUndefined();
    expect(colourEntry(table, '1g3')).toBeUndefined();
    expect(colourEntry(table, '')).toBeUndefined();
    expect(exteriorColours(catalogue, 'not-a-brand')).toBeUndefined();
    expect(exteriorColours(undefined, 'toyota')).toBeUndefined();
  });
});
