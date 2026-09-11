#!/usr/bin/env node
/**
 * `npm run release` — assembles the static integration site from the showcase
 * templates.
 *
 * The showcase under src/templates is written against the Stencil dev server:
 * it loads the components from `/build/`, which only exists while that server is
 * running, and it references its own assets root-absolutely. Neither survives
 * being copied onto a host. This turns that tree into something a static host
 * can serve, without the templates carrying a second set of paths for it:
 *
 *   /build/shift-components.esm.js   ->  the CDN, at a real published version
 *   /build/shift-components.js       ->  deleted (see below)
 *   ="/templates/…                   ->  depth-relative, so no mount is assumed
 *
 * There is no ES5 bundle. `dist/shift-components/shift-components.js` is not
 * emitted locally and is a 404 at every published version, so the `nomodule` tag
 * every template carries has never loaded anything; it is dropped rather than
 * repointed at another 404.
 *
 * Links and the catalog fetch are NOT rewritten here. harness.js and nav.js are
 * single shared files loaded from pages at three different depths, so one
 * build-time substitution cannot be right for all of them — those two resolve
 * their own paths against `import.meta.url` instead, which makes dev and the
 * built site take the same code path.
 *
 *   npm run release                     against the current published version
 *   npm run release -- --version=0.3.18  pin one
 *   npm run release -- --wait=600        keep checking for up to N seconds that the
 *                                        version is on the registry and the CDN
 *                                        before giving up (a release pipeline that
 *                                        runs right after `npm publish` needs this)
 *   npm run release -- --local           the working-tree version, no network
 *   npm run release -- --out=dist-site   somewhere other than ./website
 *   npm run release -- --base-url=…      fill the host template's placeholder
 *   npm run release -- --mount=/docs     the site will be served under a subpath
 *                                        (a leading slash is optional — Git Bash rewrites one into a path)
 *   npm run release -- --all             every page, published or not
 *
 * Only pages that opt in ship. A template says so itself:
 *
 *   <meta name="adp-publish" content="true" />
 *
 * …which the catalog scan reads into `page.publish`. It is an allow-list on
 * purpose: a page that says nothing stays off the public site, so a half-written
 * demo cannot leak out by being forgotten. Everything under templates/ that is
 * not site chrome is pruned with it — a directory whose pages are all
 * unpublished ships none of its mock data, fixtures or form structures either.
 */
import { spawn } from 'node:child_process';
import { cp, mkdir, readdir, readFile, rm, writeFile } from 'node:fs/promises';
import vm from 'node:vm';

import { headersFile, noscriptFallback, prerender, robotsAndSitemap, seoTags } from './prerender.mjs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

const PACKAGE_NAME = 'adp-web-components';
const DEFAULT_OUT = 'website';

const cdn = version => `https://cdn.jsdelivr.net/npm/${PACKAGE_NAME}@${version}/dist`;

/** The dead ES5 tag, matched loosely enough to survive an attribute reorder. */
const NOMODULE_TAG = /^[^\S\r\n]*<script\s+nomodule\b[^>]*><\/script>\r?\n?/gim;

/** Anything still pointing at the server root after the rewrite is a bug, not a choice. */
const ROOT_ABSOLUTE = /(?:src|href)="\/[^"]*"/g;

/**
 * A url() addressed from the server root in a stylesheet. It resolves against
 * the stylesheet, not the page, so the fix is a relative path in the source —
 * there is nothing sensible for a build-time rewrite to substitute.
 */
const ROOT_ABSOLUTE_URL = /url\(\s*['"]?\/[^)]*\)/g;

/** `{{NAME}}` slots the templates leave for deployment-specific values. */
const PLACEHOLDER = /\{\{([A-Z0-9_]+)\}\}/g;

/*
 * Everything directly inside templates/ is site chrome — the stylesheet, the
 * harness and nav modules, the catalog — and so are these two directories. They
 * ship whatever the filter decides about pages; every OTHER directory under
 * templates/ is page material and goes when its pages go.
 */
const CHROME_DIRECTORIES = new Set(['vendor', 'assets']);

const argv = process.argv.slice(2);
const flag = name => argv.includes(`--${name}`);

function option(name) {
  const match = argv.find(arg => arg.startsWith(`--${name}=`));

  return match ? match.slice(name.length + 3) : null;
}

const outDir = path.resolve(root, option('out') ?? DEFAULT_OUT);

/*
 * Where the site will be served from. Every page but one is mount-independent,
 * so this is only consulted for 404.html — see below. Normalised to `/prefix/`
 * (or `/`) so one concatenation works for both.
 */
const mount = `/${(option('mount') ?? '').split('/').filter(Boolean).join('/')}/`.replace('//', '/');

/*
 * Search-engine directives are decided here rather than in the markup, because
 * the page body is rendered by Alpine — a robots meta injected by script is one
 * no crawler is guaranteed to act on. Both are deferrable to deploy time.
 */
const noindex = flag('noindex');
const siteUrl = (option('site-url') ?? '').replace(/\/+$/, '');

/*
 * A mount has to be a URL path. Git Bash rewrites a leading slash into a Windows
 * path, and the result used to sail through: the build exited 0 and shipped a 404
 * page whose every asset pointed at "/C:/Program Files/Git/docs/…".
 */
if (/[:\s]/.test(mount)) {
  fatal(`--mount=${mount} is not a URL path — in Git Bash a leading slash becomes a Windows path, so write --mount=docs instead`);
}

// `rm -rf` on a path derived from an argument deserves a guard: refuse anything
// that would take the package or a parent of it with it.
if (outDir === root || root.startsWith(outDir + path.sep)) {
  fatal(`--out=${outDir} contains the package itself`);
}

const workingTreeVersion = JSON.parse(await readFile(path.join(root, 'package.json'), 'utf8')).version;

/*
 * The English strings, read by RUNNING the module the page loads rather than by
 * scraping it. A regex over STRINGS would be a second parser to keep in step.
 */
const locales = await (async () => {
  const source = await readFile(path.join(root, 'src', 'templates', 'site-locales.js'), 'utf8');
  const sandbox = {
    URL,
    URLSearchParams,
    navigator: { languages: ['en'] },
    document: { documentElement: {} },
    localStorage: { getItem: () => null, setItem: () => {} },
  };

  sandbox.window = sandbox;
  sandbox.window.location = { href: 'https://example.invalid/', search: '' };
  sandbox.window.history = { replaceState() {} };

  vm.runInNewContext(source, sandbox);

  return sandbox.window.siteLocales;
})();

/*
 * package.json is the version being *prepared*, which is routinely ahead of what
 * anyone can install — building against it produces a site whose every page
 * requests a 404. The registry is the only thing that knows what exists.
 */
const pinned = option('version');
const local = flag('local');
const waitSeconds = Number(option('wait') ?? 0);

let componentVersion;

try {
  componentVersion = pinned ?? (local ? workingTreeVersion : await publishedVersion());

  if (local) console.warn(`! --local: building against the working-tree version ${componentVersion}, which may not be published`);
  else await assertBundlePublished(componentVersion, waitSeconds);
} catch (error) {
  fatal(error.message);
}

/*
 * harness.css, vendor/alpine.js and catalog.json are all gitignored, so on a
 * clean checkout they simply do not exist. Skipping this step yields a site that
 * serves fine and has no stylesheet and an empty home page.
 */
await run(process.execPath, [path.join(root, 'automation', 'build-templates.mjs')]);

await rm(outDir, { recursive: true, force: true });
await mkdir(outDir, { recursive: true });

// A `*.local.*` file is a developer's private override — gitignored, so it never
// reaches CI, but it DOES sit in the working tree of the machine that runs this.
// Copying one would publish whatever it holds, which is the opposite of why it is
// kept out of the repo. Excluded here, and asserted absent from the output below.
const isLocalOverride = source => /.local.[^.]+$/.test(path.basename(source));

// harness.src.css is the Tailwind input, not an asset; harness.css is its output.
await cp(path.join(root, 'src', 'templates'), path.join(outDir, 'templates'), {
  recursive: true,
  // The READMEs beside the assets document the source tree, not the site.
  filter: source => path.basename(source) !== 'harness.src.css' && path.extname(source) !== '.md' && !isLocalOverride(source),
});

await cp(path.join(root, 'src', 'index.html'), path.join(outDir, 'index.html'));

// Cloudflare Pages serves /404.html for any unmatched path, which on this site
// is mostly a demo that exists but is not published yet.
await cp(path.join(root, 'src', '404.html'), path.join(outDir, '404.html'));

// The pages link templates/assets/favicon.svg (which the path rewrite handles like
// any other asset); this second copy answers the bare /favicon.svg a browser or a
// link-preview scraper probes for without being told.
await cp(path.join(root, 'src', 'templates', 'assets', 'favicon.svg'), path.join(outDir, 'favicon.svg'));

const dropped = flag('all') ? [] : await prune();

const entries = await readdir(outDir, { recursive: true, withFileTypes: true });
const pages = entries.filter(entry => entry.isFile() && entry.name.endsWith('.html')).map(entry => path.join(entry.parentPath ?? entry.path, entry.name));

const baseUrl = option('base-url');
const leftovers = { rooted: [], nomodule: [], placeholders: new Map() };

let repointed = 0;
let prerendered = 0;

for (const page of pages) {
  const original = await readFile(page, 'utf8');
  /*
   * 404.html is the one page whose own URL is not known: a static host serves it
   * for /a.html and for /a/b/c.html alike, so a depth-relative asset path is
   * right in exactly one of those cases. It gets absolute paths at the mount
   * instead — which is why the mount has to be declared for that page and for
   * nothing else.
   */
  let rewritten = rewrite(original, isNotFound(page) ? mount : relativePrefix(page));

  const landing = path.relative(outDir, page).split(path.sep).join('/') === 'index.html';

  rewritten = seoTags(rewritten, {
    isLanding: landing,
    noindex,
    siteUrl,
    packageName: PACKAGE_NAME,
    version: componentVersion,
    description: (rewritten.match(/<meta name="description" content="([^"]*)"/) ?? [])[1] ?? '',
  });

  /*
   * English only. lang and dir are document-level, so a file carrying two
   * languages would be worse than one carrying none.
   */
  const filled = prerender(rewritten, locales);

  rewritten = noscriptFallback(filled.output, locales);
  prerendered += filled.filled;

  if (rewritten !== original) repointed += 1;

  await writeFile(page, rewritten);

  const relative = path.relative(outDir, page).split(path.sep).join('/');

  // .match rather than .test: a /g regex carries lastIndex across calls.
  if (!isNotFound(page) && rewritten.match(ROOT_ABSOLUTE)) leftovers.rooted.push(relative);
  if (/nomodule/i.test(rewritten)) leftovers.nomodule.push(relative);

  // A Set because a slot is usually named twice in a page — once in the doc
  // comment that explains it, once where it is actually used.
  for (const [, name] of rewritten.matchAll(PLACEHOLDER)) {
    leftovers.placeholders.set(name, (leftovers.placeholders.get(name) ?? new Set()).add(relative));
  }
}

if (leftovers.rooted.length) fatal(`still server-root-absolute after the rewrite:\n  ${leftovers.rooted.join('\n  ')}`);
if (leftovers.nomodule.length) fatal(`nomodule tag survived the rewrite:\n  ${leftovers.nomodule.join('\n  ')}`);

/*
 * Stylesheets are not rewritten — a url() resolves against the stylesheet, so a
 * relative one is already correct at every mount and a root-absolute one cannot
 * be fixed here. It 404s only at a subpath, which is the case nobody checks by
 * hand, so the build refuses instead.
 */
for (const style of entries.filter(entry => entry.isFile() && entry.name.endsWith('.css'))) {
  const stylesheet = path.join(style.parentPath ?? style.path, style.name);
  const found = (await readFile(stylesheet, 'utf8')).match(ROOT_ABSOLUTE_URL);

  if (found) {
    fatal(`${path.relative(outDir, stylesheet)} has a server-root-absolute url(): ${found.join(`, `)} — make it relative to the stylesheet`);
  }
}

/*
 * A whitespace reflow between `>` and `</p>` would silently switch the whole
 * pre-render off. Better a failed build than a site that quietly goes blank to
 * every crawler again.
 */
// A filter is a promise; this is the check. If a private override ever reaches the
// output — through a new copy step, a renamed pattern, a stale directory — the build
// stops rather than publishing it.
const leaked = (await readdir(outDir, { recursive: true, withFileTypes: true }))
  .filter(entry => entry.isFile() && /.local.[^.]+$/.test(entry.name))
  .map(entry => path.relative(outDir, path.join(entry.parentPath ?? entry.path, entry.name)));

if (leaked.length) fatal(`private override files reached the site: ${leaked.join(', ')}`);

/*
 * Parse every inline module. A page is mostly markup, which the build already
 * validates by rewriting it — but an inline <script type="module"> is the one
 * part that can be syntactically broken and still copy, repoint and serve
 * cleanly. When that happens the page renders its skip link and nothing else,
 * and the only symptom is a console error nobody sees until a browser opens.
 */
for (const file of pages) {
  const html = await readFile(file, 'utf8');
  // Both tags must start their own line. A naive match also finds the literal
  // `<script type="module">` inside the landing page's own sample-code strings,
  // which is markup being displayed, not markup being run.
  for (const [, body] of html.matchAll(/^[ 	]*<script type="module">$([\s\S]*?)^[ 	]*<\/script>$/gm)) {
    try {
      // Three things a module may legitimately do that `new Function` cannot parse:
      // static imports, import.meta, and top-level await. Strip the first, stub the
      // second, and wrap in an async arrow so the third is allowed.
      const stripped = body.replace(/^\s*import[^;]+;/gm, '').replace(/\bimport\.meta\b/g, '({ url: "" })');

      new Function('return (async () => {' + stripped + '\n})');
    } catch (error) {
      fatal(`inline module in ${path.relative(outDir, file)} does not parse: ${error.message}`);
    }
  }
}

if (prerendered < 30) fatal(`pre-render resolved only ${prerendered} strings — the x-text pattern has drifted`);

const landingUrls = pages.filter(page => path.relative(outDir, page).split(path.sep).join('/') === 'index.html').map(() => '');
const { robots, sitemap } = robotsAndSitemap({ noindex, siteUrl, landingPages: landingUrls });

await writeFile(path.join(outDir, 'robots.txt'), robots);
await writeFile(path.join(outDir, '_headers'), headersFile({ noindex }));
if (sitemap) await writeFile(path.join(outDir, 'sitemap.xml'), sitemap);

await writeFile(
  path.join(outDir, 'build-info.json'),
  JSON.stringify({ package: PACKAGE_NAME, componentVersion, workingTreeVersion, builtAt: new Date().toISOString() }, null, 2) + '\n',
);

console.log(`\nsite      ${path.relative(root, outDir)}`);
console.log(`pages     ${pages.length} copied, ${repointed} repointed${dropped.length ? `, ${dropped.length} unpublished dropped` : ''}`);
console.log(`loads     ${cdn(componentVersion)}/shift-components/shift-components.esm.js`);
console.log(`copy      ${prerendered} strings pre-rendered into the HTML`);
console.log(`robots    ${noindex ? 'noindex (whole site)' : 'index the landing page, noindex everything else'}${siteUrl ? '' : '  — pass --site-url= for canonical + sitemap'}`);
if (mount !== '/') console.log(`mount     404.html built for ${mount}`);

// A page shipped with an unfilled slot renders but does not work, so say so
// rather than leaving it to be found on the deployed site.
for (const [name, files] of leftovers.placeholders) {
  console.warn(`! {{${name}}} left unfilled on ${files.size} page${files.size === 1 ? '' : 's'}: ${[...files].join(', ')}`);
}

/**
 * Removes every page the catalog does not mark `publish`, then everything left
 * under templates/ that only those pages used.
 *
 * The catalog is rewritten to match. It is what the landing page and both
 * navigations read, so leaving it whole would fill the public site with links to
 * pages that are no longer there.
 */
async function prune() {
  const catalogFile = path.join(outDir, 'templates', 'catalog.json');
  const catalog = JSON.parse(await readFile(catalogFile, 'utf8'));

  const published = catalog.pages.filter(page => page.publish);
  const removed = catalog.pages.filter(page => !page.publish);
  const areas = new Set(published.map(page => page.area));

  // `page.path` is site-absolute (`/templates/…`); drop the slash to rejoin it
  // under the output directory.
  for (const page of removed) await rm(path.join(outDir, page.path.slice(1)), { force: true });

  for (const entry of await readdir(path.join(outDir, 'templates'), { withFileTypes: true })) {
    if (!entry.isDirectory() || CHROME_DIRECTORIES.has(entry.name) || areas.has(entry.name)) continue;

    await rm(path.join(outDir, 'templates', entry.name), { recursive: true, force: true });
  }

  const counted = new Map();

  for (const page of published) counted.set(page.area, (counted.get(page.area) ?? 0) + 1);

  const trimmed = {
    ...catalog,
    areas: catalog.areas.filter(area => counted.has(area.id)).map(area => ({ ...area, count: counted.get(area.id) })),
    pages: published,
  };

  await writeFile(catalogFile, JSON.stringify(trimmed, null, 2) + '\n');

  return removed;
}

/** The not-found page, which is served at every URL rather than its own. */
function isNotFound(file) {
  return path.relative(outDir, file).split(path.sep).join('/') === '404.html';
}

/**
 * Templates address their assets from the server root, which is only true when
 * the site IS the root. Depth-relative works at any mount — and, unlike the
 * catalog links, an asset tag is per-file, so the depth is known here.
 */
function relativePrefix(file) {
  const depth = path.relative(outDir, file).split(path.sep).length - 1;

  return depth === 0 ? './' : '../'.repeat(depth);
}

function rewrite(html, prefix) {
  let output = html
    .replace(NOMODULE_TAG, '')
    .replaceAll('="/build/shift-components.esm.js"', `="${cdn(componentVersion)}/shift-components/shift-components.esm.js"`)
    .replaceAll('="/templates/', `="${prefix}templates/`)
    // A bare site-root link — the 404 page's way home. Same substitution, so it
    // follows the same rule as every asset path on the page it appears on.
    .replaceAll(`="/"`, `="${prefix}"`)
    .replaceAll('{{ADP_WEB_COMPONENTS_VERSION}}', componentVersion);

  if (baseUrl) output = output.replaceAll('{{VEHICLE_LOOKUP_BASE_URL}}', baseUrl);

  return output;
}

async function publishedVersion() {
  const url = `https://registry.npmjs.org/${PACKAGE_NAME}/latest`;

  let response;

  try {
    response = await fetch(url);
  } catch (error) {
    throw new Error(`could not reach the npm registry (${error.message}) — pass --version=x.y.z, or --local to build offline`);
  }

  if (!response.ok) throw new Error(`the npm registry answered ${response.status} for ${url}`);

  const { version } = await response.json();

  if (!version) throw new Error(`the npm registry returned no version for ${PACKAGE_NAME}`);

  return version;
}

/*
 * A pinned version has to exist in two places: on the registry, and on the CDN
 * the pages load from. Right after `npm publish` neither is guaranteed — the
 * registry takes a moment, and the CDN is asked for the file only once the
 * registry has it, so that a too-early request does not leave a cached 404 in
 * the CDN's way. With `--wait` the check is repeated every 30 seconds until the
 * deadline; without it, one failed check is fatal.
 */
async function assertBundlePublished(version, waitSeconds = 0) {
  const deadline = Date.now() + waitSeconds * 1000;

  for (;;) {
    try {
      await assertOnRegistry(version);
      await assertOnCdn(version);

      return;
    } catch (error) {
      if (Date.now() >= deadline) throw error;

      console.log(`release: ${error.message} — checking again in 30 s`);
      await new Promise(resolve => setTimeout(resolve, 30_000));
    }
  }
}

async function assertOnRegistry(version) {
  const url = `https://registry.npmjs.org/${PACKAGE_NAME}/${version}`;

  let response;

  try {
    response = await fetch(url, { method: 'HEAD' });
  } catch (error) {
    throw new Error(`could not reach the npm registry to check ${url} (${error.message})`);
  }

  if (!response.ok) throw new Error(`the npm registry answered ${response.status} for ${PACKAGE_NAME}@${version} — not published yet?`);
}

async function assertOnCdn(version) {
  const url = `${cdn(version)}/shift-components/shift-components.esm.js`;

  let response;

  try {
    response = await fetch(url, { method: 'HEAD' });
  } catch (error) {
    throw new Error(`could not reach the CDN to check ${url} (${error.message})`);
  }

  if (!response.ok) {
    throw new Error(`${url} answered ${response.status} — every page would load nothing. Publish ${PACKAGE_NAME}@${version}, or build against a version that exists.`);
  }
}

function run(command, args) {
  return new Promise((resolve, reject) => {
    spawn(command, args, { stdio: 'inherit', cwd: root }).on('exit', code => (code ? reject(new Error(`${path.basename(args[0])} exited ${code}`)) : resolve()));
  }).catch(error => fatal(error.message));
}

function fatal(message) {
  console.error(`release: ${message}`);
  process.exit(1);
}
