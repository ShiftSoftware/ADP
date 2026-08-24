#!/usr/bin/env node
/**
 * `npm start` — the Stencil dev server plus the template stylesheet watcher.
 *
 * The showcase pages under src/templates are styled by src/templates/harness.css,
 * which Tailwind compiles from harness.src.css. Stencil's watch knows nothing
 * about that step, so on its own, adding a class to a template does nothing at
 * all until someone remembers to run `build:templates` — a silent failure that
 * looks like a broken class name. Running the watcher alongside removes the
 * habit entirely.
 *
 * There is no loop between the two: Tailwind watches only *.html and harness.js
 * under src/templates and writes harness.css; Stencil sees that write, re-copies
 * templates into www, and reloads the page. Stencil never writes into src.
 *
 * Use `npm run start:stencil` for the dev server on its own.
 */
import { spawn } from 'node:child_process';
import { watch } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { CATALOG_FILE, writeCatalog } from './templates-catalog.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

const buildTemplates = path.join(root, 'automation', 'build-templates.mjs');
const stencil = path.join(root, 'node_modules', '@stencil', 'core', 'bin', 'stencil');

const children = [];
let shuttingDown = false;

function shutdown(code) {
  if (shuttingDown) return;
  shuttingDown = true;

  for (const child of children) {
    if (!child.killed) child.kill();
  }

  process.exit(code);
}

function start(args) {
  // Spawned through node directly rather than the .bin shim so this works the
  // same on Windows, where the shim is a .cmd wrapper.
  const child = spawn(process.execPath, args, { stdio: 'inherit', cwd: root });

  children.push(child);
  child.on('exit', code => shutdown(code ?? 0));

  return child;
}

for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => shutdown(0));

// Build once up front so the first copy into www is never stale, then watch.
await new Promise(resolve => spawn(process.execPath, [buildTemplates], { stdio: 'inherit', cwd: root }).on('exit', resolve));

start([buildTemplates, '--watch']);
start([stencil, 'build', '--dev', '--watch', '--serve']);

/*
 * The Tailwind watcher rebuilds the stylesheet but knows nothing about the
 * showcase index, so adding or renaming a page would not appear on the home page
 * until a restart. Watching for that separately keeps the catalog live.
 *
 * Writing catalog.json fires this watcher again, so that filename is skipped —
 * otherwise it would rewrite itself forever.
 */
let pending;

watch(path.join(root, 'src', 'templates'), { recursive: true }, (_event, filename) => {
  if (!filename || path.basename(filename) === CATALOG_FILE) return;
  if (!filename.endsWith('.html')) return;

  clearTimeout(pending);
  pending = setTimeout(async () => {
    try {
      const catalog = await writeCatalog(root);
      console.log(`catalogued ${catalog.pages.length} demo pages`);
    } catch (error) {
      console.error(`catalog rebuild failed — ${error.message}`);
    }
  }, 150);
});
