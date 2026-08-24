#!/usr/bin/env node
/**
 * Builds the dev-only assets the showcase templates depend on:
 *
 *   src/templates/harness.src.css  ->  src/templates/harness.css   (Tailwind 4 + daisyUI 5)
 *   node_modules/alpinejs          ->  src/templates/vendor/alpine.js
 *   src/templates/ **.html          ->  src/templates/catalog.json   (showcase index)
 *
 * All three are gitignored — they are rebuilt on every run, so tracking them
 * meant re-staging a 140KB diff constantly. The templates used to pull Tailwind,
 * daisyUI and Alpine from a CDN on every page, which meant a fresh download plus
 * a full browser-side Tailwind JIT pass on every navigation and every dev-server
 * reload. Compiling once removes both costs and makes the pages work offline.
 *
 *   npm run build:templates     one-off
 *   npm run watch:templates     rebuild while editing template markup
 *
 * Run it whenever a template gains a class it did not use before — Tailwind only
 * emits utilities it can see in the sources listed by `@source` in harness.src.css.
 *
 * The CLI is invoked by path rather than through `npx` on purpose: the v4 CLI and
 * the v3 tailwindcss the components build with both expose a `tailwindcss` binary,
 * and npx picks the wrong one.
 */
import { spawn } from 'node:child_process';
import { copyFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { writeCatalog } from './templates-catalog.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const templates = path.join(root, 'src', 'templates');
const watch = process.argv.includes('--watch');

const alpineSource = path.join(root, 'node_modules', 'alpinejs', 'dist', 'cdn.min.js');
const alpineTarget = path.join(templates, 'vendor', 'alpine.js');

await mkdir(path.dirname(alpineTarget), { recursive: true });
await copyFile(alpineSource, alpineTarget);

console.log(`vendored ${path.relative(root, alpineTarget)}`);

const catalog = await writeCatalog(root);

console.log(`catalogued ${catalog.pages.length} demo pages across ${catalog.areas.length} areas`);

const args = [
  path.join(root, 'node_modules', '@tailwindcss', 'cli', 'dist', 'index.mjs'),
  '--input',
  path.join(templates, 'harness.src.css'),
  '--output',
  path.join(templates, 'harness.css'),
  '--minify',
];

// `=always` rather than a bare `--watch`: the CLI otherwise shuts down as soon as
// stdin reaches EOF, which is immediate whenever this runs without a TTY — under
// dev.mjs, in CI, or in an IDE terminal that does not attach one.
if (watch) args.push('--watch=always');

spawn(process.execPath, args, { stdio: 'inherit' }).on('exit', code => process.exit(code ?? 0));
