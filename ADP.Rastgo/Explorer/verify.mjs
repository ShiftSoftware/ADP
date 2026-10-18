import assert from 'node:assert/strict';
import {readFile, mkdtemp, cp, rm, realpath} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join, dirname, basename} from 'node:path';
import {request} from 'node:http';
import {fileURLToPath} from 'node:url';
import './mechanisms.js';
import {captureEvidence, checkEvidence} from './evidence.mjs';
import {createExplorerServer} from './server.mjs';

const root = fileURLToPath(new URL('./', import.meta.url));
const evidence = JSON.parse(await readFile(new URL('evidence.json', import.meta.url), 'utf8'));
const fresh = await captureEvidence();
assert.deepEqual(evidence.entries, fresh.entries, 'Review explanations and recapture changed evidence');
const checks = await checkEvidence(evidence);
assert(checks.checks.every(c => c.status === 'unchanged'));
const ids = new Set(evidence.entries.map(e => e.id));
const topics = globalThis.RastgoExplorer.mechanisms;
assert.equal(new Set(topics.map(m => m.id)).size, topics.length);
let scenarios = 0;
for (const m of topics) {
  assert(m.evidence.length > 0 && m.evidence.every(id => ids.has(id)), m.id + ': missing evidence');
  assert.equal(new Set(m.scenarios.map(s => s.id)).size, m.scenarios.length);
  for (const s of m.scenarios) {
    scenarios++;
    assert.equal(s.values.length, m.steps.length, m.id + '/' + s.id);
    assert(['Pass', 'Warn', 'Fail', 'Error', 'Skipped', 'Context'].includes(s.status));
  }
}

async function listen(server) {
  await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
  return 'http://127.0.0.1:' + server.address().port;
}
async function close(server) { await new Promise((resolve, reject) => server.close(e => e ? reject(e) : resolve())); }
const assets = ['index.html','style.css','overview.js','mechanisms.js','explorer.js','rastgo-icon.svg','evidence.json'];
const sharedRoot = fileURLToPath(new URL('../../ADP.Explorer.Shared/', import.meta.url));
const sharedAssets = ['theme.css','engineering.css','shell.js','overview.js','evidence.js'];
const server = createExplorerServer();
try {
  const base = await listen(server);
  for (const asset of assets) {
    const response = await fetch(base + '/' + asset);
    assert.equal(response.status, 200, asset);
    assert.equal(response.headers.get('x-content-type-options'), 'nosniff');
    assert.deepEqual(Buffer.from(await response.arrayBuffer()), await readFile(join(root, asset)));
  }
  for (const asset of sharedAssets) {
    const response = await fetch(base + '/shared/' + asset);
    assert.equal(response.status, 200, asset);
    assert.deepEqual(Buffer.from(await response.arrayBuffer()), await readFile(join(sharedRoot, asset)));
  }
  for (const path of ['/shared/server.mjs','/shared/../shell.js','/server.mjs', '/README.md', '/../../CLAUDE.md', '/%2e%2e%2fCLAUDE.md']) {
    assert.equal((await fetch(base + path)).status, 404, path);
  }
  assert.equal((await fetch(base, {method: 'POST', body: 'no mutation'})).status, 405);
  const rejectedHost = await new Promise((resolve, reject) => {
    const req = request(base, {headers: {Host: 'example.com'}}, response => {
      response.resume();
      response.on('end', () => resolve(response.statusCode));
    });
    req.on('error', reject); req.end();
  });
  assert.equal(rejectedHost, 403);
  const head = await fetch(base, {method: 'HEAD'});
  assert.equal(head.status, 200);
  assert.equal((await head.text()).length, 0);
  const status = await (await fetch(base + '/evidence-status.json')).json();
  assert(status.checks.every(c => c.status === 'unchanged'));
} finally { await close(server); }

const scratch = await mkdtemp(join(tmpdir(), 'rastgo-explorer-verify-'));
const relocated = createExplorerServer({assetRoot: scratch, sourceRoot: join(scratch, 'no-checkout'), sharedRoot: join(scratch, 'shared')});
try {
  for (const asset of assets) await cp(join(root, asset), join(scratch, asset));
  await cp(sharedRoot, join(scratch, 'shared'), {recursive:true});
  const base = await listen(relocated);
  for (const asset of sharedAssets) {
    const response = await fetch(base + '/shared/' + asset);
    assert.equal(response.status, 200);
    assert.deepEqual(Buffer.from(await response.arrayBuffer()), await readFile(join(scratch, 'shared', asset)));
  }
  assert.equal((await fetch(base)).status, 200);
  const snapshot = await (await fetch(base + '/evidence.json')).json();
  assert.equal(snapshot.entries.length, evidence.entries.length);
  const status = await (await fetch(base + '/evidence-status.json')).json();
  assert(status.checks.every(c => c.status === 'unavailable'));
} finally {
  await close(relocated);
  // Delete only this verifier's newly created, canonical temporary directory.
  const canonicalScratch = await realpath(scratch);
  assert.equal(dirname(canonicalScratch), await realpath(tmpdir()));
  assert(basename(canonicalScratch).startsWith('rastgo-explorer-verify-'));
  await rm(scratch, {recursive: true, force: true});
}
console.log('Verified ' + topics.length + ' mechanisms, ' + scenarios + ' scenarios, ' + ids.size + ' evidence excerpts; HTTP boundaries and relocated copy passed.');
