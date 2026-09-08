import http from 'node:http';
import {readFile} from 'node:fs/promises';
import {fileURLToPath, pathToFileURL} from 'node:url';
import {join} from 'node:path';
import {checkEvidence, repositoryRoot} from './evidence.mjs';

const explorerRoot = fileURLToPath(new URL('./', import.meta.url));
const publicFiles = new Map([
  ['/', ['index.html', 'text/html; charset=utf-8']],
  ['/index.html', ['index.html', 'text/html; charset=utf-8']],
  ['/style.css', ['style.css', 'text/css; charset=utf-8']],
  ['/animation.js', ['animation.js', 'text/javascript; charset=utf-8']],
  ['/mechanisms.js', ['mechanisms.js', 'text/javascript; charset=utf-8']],
  ['/explorer.js', ['explorer.js', 'text/javascript; charset=utf-8']],
  ['/explorer.css', ['explorer.css', 'text/css; charset=utf-8']],
  ['/evidence.json', ['evidence.json', 'application/json; charset=utf-8']],
  ['/hawta-icon.svg', ['hawta-icon.svg', 'image/svg+xml']],
]);

export function createExplorerServer({assetRoot = explorerRoot, sourceRoot = repositoryRoot} = {}) {
  return http.createServer(async (request, response) => {
    const send = (status, type, body, headers = {}) => {
      response.writeHead(status, {'Content-Type': type, 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff', ...headers});
      response.end(request.method === 'HEAD' ? undefined : body);
    };
    if (!['GET', 'HEAD'].includes(request.method)) {
      send(405, 'text/plain', 'Method not allowed', {Allow: 'GET, HEAD'});
      return;
    }
    let url;
    try { url = new URL(request.url, 'http://' + request.headers.host); }
    catch { send(400, 'text/plain', 'Invalid request'); return; }
    if (!['127.0.0.1', 'localhost'].includes(url.hostname)) {
      send(403, 'text/plain', 'Loopback host required');
      return;
    }
    if (url.pathname === '/evidence-status.json') {
      try {
        const evidence = JSON.parse(await readFile(join(assetRoot, 'evidence.json'), 'utf8'));
        send(200, 'application/json; charset=utf-8', JSON.stringify(await checkEvidence(evidence, sourceRoot)));
      } catch { send(503, 'text/plain', 'Evidence check unavailable'); }
      return;
    }
    const entry = publicFiles.get(url.pathname);
    if (!entry) { send(404, 'text/plain', 'Not found'); return; }
    try { send(200, entry[1], await readFile(join(assetRoot, entry[0]))); }
    catch { send(500, 'text/plain', 'Unable to read Explorer asset'); }
  });
}

if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
  const port = Number(process.env.HAWTA_EXPLORER_PORT || 4178);
  if (!Number.isInteger(port) || port < 0 || port > 65535) throw new Error('Invalid HAWTA_EXPLORER_PORT');
  const server = createExplorerServer();
  server.on('error', error => { process.stderr.write(`Hawta Explorer: ${error.message}\n`); process.exitCode = 1; });
  server.listen(port, '127.0.0.1', () => process.stdout.write(`Hawta Explorer: http://127.0.0.1:${server.address().port}/\n`));
}
