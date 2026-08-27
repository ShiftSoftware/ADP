/**
 * Scans src/templates and writes src/templates/catalog.json — the index the
 * showcase home page renders from.
 *
 * The home page used to be four hand-written links, which drifts the moment
 * anyone adds a demo. Generating it means a new page shows up by existing (the
 * same reasoning as R7 for fixtures).
 */
import { readdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

export const CATALOG_FILE = 'catalog.json';

const AREA_LABELS = {
  'forms': 'Forms',
  'part-lookup': 'Part lookup',
  'vehicle-lookup': 'Vehicle lookup',
  'production-host': 'Host integration',
  'prototypes': 'Prototypes',
  'root': 'Standalone',
};

/** Order areas deliberately rather than however the filesystem returns them. */
const AREA_ORDER = ['vehicle-lookup', 'part-lookup', 'forms', 'root', 'production-host', 'prototypes'];

export async function writeCatalog(root) {
  const templates = path.join(root, 'src', 'templates');
  const entries = await readdir(templates, { recursive: true, withFileTypes: true });

  const pages = [];

  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.endsWith('.html')) continue;

    const absolute = path.join(entry.parentPath ?? entry.path, entry.name);
    const relative = path.relative(templates, absolute).split(path.sep).join('/');
    const source = await readFile(absolute, 'utf8');

    pages.push(describe(relative, source));
  }

  const counted = new Map();

  for (const page of pages) {
    if (page.kind === 'index') continue;
    counted.set(page.area, (counted.get(page.area) ?? 0) + 1);
  }

  const areas = [...counted.keys()].sort((a, b) => rank(a) - rank(b)).map(id => ({ id, label: AREA_LABELS[id] ?? id, count: counted.get(id) }));

  // Carried through so the showcase can show the version without anyone keeping
  // a second copy of it in sync.
  const { name, version } = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8'));

  const listed = disambiguate(pages.filter(page => page.kind !== 'index'));

  const catalog = {
    package: { name, version },
    areas,
    pages: listed.sort((a, b) => rank(a.area) - rank(b.area) || a.title.localeCompare(b.title)),
  };

  await writeFile(path.join(templates, CATALOG_FILE), JSON.stringify(catalog, null, 2) + '\n');

  return catalog;
}

/**
 * A title is only useful in a menu if it is unique. Four of the frozen prototypes
 * share one and cannot be edited, so the filename settles it here rather than in
 * the page.
 */
function disambiguate(pages) {
  const seen = new Map();

  for (const page of pages) {
    const key = `${page.area}/${page.title}`;
    seen.set(key, (seen.get(key) ?? 0) + 1);
  }

  for (const page of pages) {
    if (seen.get(`${page.area}/${page.title}`) > 1) {
      page.title = `${page.title} (${path.basename(page.path, '.html')})`;
    }
  }

  return pages;
}

function rank(area) {
  const index = AREA_ORDER.indexOf(area);
  return index === -1 ? AREA_ORDER.length : index;
}

function describe(relative, source) {
  const segments = relative.split('/');
  const area = segments.length > 1 ? segments[0] : 'root';

  return {
    path: `/templates/${relative}`,
    title: title(source, segments.at(-1)),
    area,
    kind: kind(relative, source),
    tags: tags(source),
    harness: source.includes('/templates/harness.css'),
    legacy: legacy(source),
  };
}

function title(source, filename) {
  const match = source.match(/<title>([^<]+)<\/title>/i);
  if (!match) return filename.replace(/\.html$/, '').replace(/[-_]/g, ' ');

  // Strip the "— adp-web-components" suffix the convention adds; the home page
  // already says which package this is.
  return match[1].split('—')[0].trim();
}

function kind(relative, source) {
  if (path.basename(relative) === 'index.html') return 'index';
  if (source.includes('host-loader')) return 'host';

  // A page with no component script is a hand-built mock, not a harness — the
  // design language treats those as frozen references.
  if (!source.includes('/build/shift-components')) return 'prototype';

  return 'demo';
}

function tags(source) {
  // Custom elements are the only dashed tags HTML has, so a dashed opening tag
  // is always a component. Script and style bodies are stripped first so a tag
  // named inside JS does not count as one being rendered.
  const markup = source.replace(/<script[\s\S]*?<\/script>/gi, '').replace(/<style[\s\S]*?<\/style>/gi, '');

  const found = new Set();

  for (const match of markup.matchAll(/<([a-z][a-z0-9]*(?:-[a-z0-9]+)+)[\s/>]/g)) found.add(match[1]);

  return [...found];
}

function legacy(source) {
  const found = [];

  if (/cdn\.jsdelivr\.net\/npm\/daisyui|@tailwindcss\/browser/.test(source)) found.push('tailwind-cdn');
  if (/unpkg\.com\/alpinejs/.test(source)) found.push('alpine-cdn');
  if (/bootstrap@3|jquery-1\./.test(source)) found.push('bootstrap3');
  if (/class="sample-button"/.test(source)) found.push('sample-button');

  return found;
}
