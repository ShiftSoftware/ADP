#!/usr/bin/env node
/**
 * `npm run preview` — serves the site `npm run release` built, so it can be
 * looked at the way a host will serve it.
 *
 * Not a dev server: no watching, no reload. It exists because the built site is
 * exactly what the dev server is not — components from the CDN, depth-relative
 * asset paths, only the published pages — and none of that is exercised by
 * `npm start`.
 *
 *   npm run preview                      http://localhost:3335/
 *   npm run preview -- --port=5000       somewhere else
 *   npm run preview -- --mount=/docs     serve it under a subpath
 *                                        (a leading slash is optional — Git Bash rewrites one into a path)
 *   npm run preview -- --out=dist-site   a site built with a matching --out
 *
 * `--mount` is the interesting one. Templates used to address their assets from
 * the server root, which silently only works when the site IS the root — the
 * failure case that VS Code Live Server, a preview deployment and any subpath
 * host all hit. Serving under a prefix reproduces it in one command.
 */
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { createServer } from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

const TYPES = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
};

const argv = process.argv.slice(2);

function option(name, fallback) {
  const match = argv.find(arg => arg.startsWith(`--${name}=`));

  return match ? match.slice(name.length + 3) : fallback;
}

const outDir = path.resolve(root, option('out', 'website'));
const port = Number(option('port', '3335'));

// Normalised to `/prefix/` (or `/`) so one join works for both cases.
const mount = `/${option('mount', '').split('/').filter(Boolean).join('/')}/`.replace('//', '/');

try {
  await stat(path.join(outDir, 'index.html'));
} catch {
  console.error(`preview: no site at ${path.relative(root, outDir)} — run \`npm run release\` first`);
  process.exit(1);
}

const server = createServer(async (request, response) => {
  const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);

  if (!pathname.startsWith(mount)) {
    return send(response, 404, `not mounted here — try ${mount}`);
  }

  // `path.join` collapses `..`, and the result is re-checked against outDir so a
  // traversal cannot climb out of the site.
  let file = path.join(outDir, pathname.slice(mount.length));

  if (!file.startsWith(outDir)) return send(response, 403, 'forbidden');

  try {
    if ((await stat(file)).isDirectory()) file = path.join(file, 'index.html');

    const { size } = await stat(file);

    response.writeHead(200, {
      'content-type': TYPES[path.extname(file)] ?? 'application/octet-stream',
      'content-length': size,
      // The whole point is to see the current build, not a cached one.
      'cache-control': 'no-store',
    });

    createReadStream(file).pipe(response);
  } catch {
    // What a static host does: the 404 page, with a 404 status. Serving a
    // plain-text body here would hide every styling problem that page has.
    await notFound(response, pathname);
  }
});

server.on('error', error => {
  console.error(error.code === 'EADDRINUSE' ? `preview: port ${port} is busy — pass --port=` : `preview: ${error.message}`);
  process.exit(1);
});

server.listen(port, () => {
  console.log(`preview   http://localhost:${port}${mount}`);
  console.log(`serving   ${path.relative(root, outDir)}${mount === '/' ? '' : `  (mounted at ${mount})`}`);
  console.log('\nCtrl-C to stop.');
});

async function notFound(response, pathname) {
  const page = path.join(outDir, '404.html');

  try {
    const { size } = await stat(page);

    response.writeHead(404, { 'content-type': TYPES['.html'], 'content-length': size, 'cache-control': 'no-store' });
    createReadStream(page).pipe(response);
  } catch {
    send(response, 404, `not found: ${pathname}`);
  }
}

function send(response, status, message) {
  response.writeHead(status, { 'content-type': 'text/plain; charset=utf-8' });
  response.end(message);
}
