// Fails when a publishable package's `exports` / `main` / `types` point at a file the build
// did not produce. Run after the builds, before publishing.
//
// Why: the apps in this workspace alias the packages to their `src/`, so a path that only
// exists in `dist/` on paper is invisible here and breaks every consumer. 0.1.0 shipped
// `exports["./styles.css"] -> ./dist/styles.css` with no such file.

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const packagesDir = resolve(fileURLToPath(new URL('.', import.meta.url)), '../packages');

/** Every file path an `exports` value can resolve to (strings, condition maps, nested). */
function targets(value) {
  if (typeof value === 'string') return [value];
  if (value && typeof value === 'object') return Object.values(value).flatMap(targets);
  return [];
}

const missing = [];
for (const name of readdirSync(packagesDir)) {
  const dir = join(packagesDir, name);
  const manifestPath = join(dir, 'package.json');
  if (!existsSync(manifestPath)) continue;
  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
  if (manifest.private) continue;

  const paths = [manifest.main, manifest.types, ...targets(manifest.exports)].filter(Boolean);
  for (const path of new Set(paths)) {
    if (path.includes('*')) continue;
    if (!existsSync(join(dir, path))) missing.push(`${manifest.name}: ${path}`);
  }
}

if (missing.length) {
  console.error('Package entry points that do not exist after the build:\n  ' + missing.join('\n  '));
  process.exit(1);
}
console.log('All package entry points exist.');
