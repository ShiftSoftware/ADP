import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp, mkdir, cp, copyFile, readFile, writeFile, rm, symlink} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join, dirname, resolve, sep, basename} from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn, spawnSync} from 'node:child_process';
import {once} from 'node:events';
import {get} from 'node:http';
import {captureEvidence, checkEvidence, repositoryRoot, sourceHash} from './evidence.mjs';
import {evidenceSources} from './evidence-sources.mjs';

const explorerRoot = fileURLToPath(new URL('./', import.meta.url));

test('Explorer runs from a relocated checkout with bounded public evidence', async t => {
  // A space in this path also exercises subprocess argument handling.
  const scratch = await mkdtemp(join(tmpdir(), 'hawta-explorer verify '));
  t.after(async () => {
    const target = resolve(scratch);
    assert.ok(target.startsWith(resolve(tmpdir()) + sep));
    assert.ok(basename(target).startsWith('hawta-explorer verify '));
    await rm(target, {recursive: true, force: true});
  });
  const relocated = join(scratch, 'checkout');
  const app = join(relocated, 'ADP.Hawta', 'Explorer');
  await cp(explorerRoot, app, {recursive: true});
  for (const path of new Set(evidenceSources.map(source => source.path))) {
    await mkdir(dirname(join(relocated, path)), {recursive: true});
    await copyFile(join(repositoryRoot, path), join(relocated, path));
  }
  const evidence = await captureEvidence(relocated);
  const source = evidenceSources[0];
  const sourcePath = join(relocated, source.path);
  const sourceText = await readFile(sourcePath, 'utf8');
  const statusOf = async snapshot => (await checkEvidence(snapshot, relocated)).checks.find(check => check.id === source.id).status;

  await t.test('capture and scene verification work outside the working directory', async () => {
    for (const script of ['capture-evidence.mjs', 'verify.mjs']) {
      const result = spawnSync(process.execPath, [join(app, script)], {cwd: scratch, encoding: 'utf8', windowsHide: true});
      assert.equal(result.status, 0, result.stderr + result.stdout);
    }
    const captured = JSON.parse(await readFile(join(app, 'evidence.json'), 'utf8'));
    assert.equal(captured.entries.length, evidenceSources.length);
    assert.ok((await checkEvidence(captured, relocated)).checks.every(check => check.status === 'unchanged'));
    for (const entry of captured.entries) assert.ok(evidenceSources.some(item => item.id === entry.id && item.path === entry.path));
  });

  await t.test('freshness distinguishes source changes, missing files and line endings', async () => {
    assert.equal(await statusOf(evidence), 'unchanged');
    try {
      await writeFile(sourcePath, sourceText.replace(/\r?\n/g, '\r\n'));
      assert.equal(await statusOf(evidence), 'unchanged');
      await writeFile(sourcePath, sourceText + '\n// Changed source fixture.\n');
      assert.equal(await statusOf(evidence), 'changed');
      await rm(sourcePath);
      assert.equal(await statusOf(evidence), 'unavailable');
    } finally { await writeFile(sourcePath, sourceText); }
  });

  await t.test('captured paths and unknown IDs cannot request another file', async () => {
    const externalText = 'Outside the source catalog';
    const externalPath = join(scratch, 'external.txt');
    await writeFile(externalPath, externalText);
    for (const path of [externalPath, '../../../external.txt', 'ADP.Hawta/Explorer/server.mjs']) {
      const changed = structuredClone(evidence);
      Object.assign(changed.entries[0], {path, sha256: sourceHash(externalText)});
      assert.equal(await statusOf(changed), 'unavailable');
    }
    const changed = structuredClone(evidence);
    changed.entries[0].id = 'not-a-catalog-source';
    assert.equal((await checkEvidence(changed, relocated)).checks[0].status, 'unavailable');
    changed.hashAlgorithm = 'unknown';
    assert.ok((await checkEvidence(changed, relocated)).checks.every(check => check.status === 'unavailable'));
  });

  await t.test('a source directory junction cannot escape the relocated checkout', async () => {
    const linkedRoot = join(scratch, 'linked');
    await mkdir(join(linkedRoot, 'ADP.Hawta'), {recursive: true});
    await symlink(join(relocated, 'ADP.Hawta', 'Hawta'), join(linkedRoot, 'ADP.Hawta', 'Hawta'), process.platform === 'win32' ? 'junction' : 'dir');
    assert.ok((await checkEvidence(evidence, linkedRoot)).checks.every(check => check.status === 'unavailable'));
  });

  await t.test('the relocated server serves assets and truthful freshness on loopback', async () => {
    const child = spawn(process.execPath, [join(app, 'server.mjs')], {
      cwd: scratch, env: {...process.env, HAWTA_EXPLORER_PORT: '0'}, windowsHide: true,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    let errors = '';
    child.stderr.on('data', chunk => { errors += chunk; });
    try {
      const url = await new Promise((done, reject) => {
        const timer = setTimeout(() => reject(new Error('Server startup timed out: ' + errors)), 10000);
        let output = '';
        child.once('error', error => { clearTimeout(timer); reject(error); });
        child.once('exit', code => { clearTimeout(timer); reject(new Error('Server exited: ' + code + ' ' + errors)); });
        child.stdout.on('data', chunk => {
          output += chunk;
          const match = output.match(/http:\/\/127\.0\.0\.1:\d+\//);
          if (match) { clearTimeout(timer); done(match[0]); }
        });
      });
      const html = await (await fetch(url)).text();
      assert.match(html, /Engineering Explorer/);
      const assets = [...html.matchAll(/(?:src|href)="([^"#]+\.(?:css|js|svg))"/g)].map(match => match[1]);
      for (const asset of [...new Set(assets), 'evidence.json']) {
        const response = await fetch(new URL(asset, url));
        assert.equal(response.status, 200, asset);
        assert.equal(response.headers.get('cache-control'), 'no-store');
        assert.ok((await response.text()).length > 0);
      }
      const freshness = () => fetch(new URL('evidence-status.json', url)).then(response => response.json());
      const initialFreshness = await freshness();
      const initialCapture = JSON.parse(await readFile(join(app, 'evidence.json'), 'utf8'));
      assert.equal(initialFreshness.capturedAt, initialCapture.capturedAt);
      assert.equal(initialFreshness.hashAlgorithm, initialCapture.hashAlgorithm);
      assert.ok(initialFreshness.checks.every(check => check.status === 'unchanged'));
      assert.equal(initialFreshness.checks[0].sha256, initialCapture.entries[0].sha256);
      try {
        await writeFile(sourcePath, sourceText + '\n// Server freshness fixture.\n');
        assert.equal((await freshness()).checks.find(check => check.id === source.id).status, 'changed');
        // A consumer can reject freshness from a different capture, even when
        // source and snapshot are refreshed while that consumer is already open.
        const refreshed = await captureEvidence(relocated);
        await writeFile(join(app, 'evidence.json'), JSON.stringify(refreshed));
        const refreshedStatus = await freshness();
        assert.equal(refreshedStatus.capturedAt, refreshed.capturedAt);
        assert.notEqual(refreshedStatus.checks[0].sha256, initialCapture.entries[0].sha256);
        assert.equal(refreshedStatus.checks[0].status, 'unchanged');
        await rm(sourcePath);
        assert.equal((await freshness()).checks.find(check => check.id === source.id).status, 'unavailable');
      } finally {
        await writeFile(sourcePath, sourceText);
        await writeFile(join(app, 'evidence.json'), JSON.stringify(initialCapture));
      }
      for (const path of ['server.mjs', 'evidence.mjs', 'evidence-sources.mjs', '../Hawta/RowHash.cs', '%2e%2e%2fexternal.txt']) {
        assert.equal((await fetch(new URL(path, url))).status, 404, path);
      }
      for (const path of ['', 'evidence-status.json']) {
        const response = await fetch(new URL(path, url), {method: 'HEAD'});
        assert.equal(response.status, 200);
        assert.equal(await response.text(), '');
      }
      assert.equal((await fetch(url, {method: 'POST'})).status, 405);
      const foreignHostStatus = await new Promise((done, reject) => {
        get(url, {headers: {Host: 'example.invalid'}}, response => {
          response.resume(); done(response.statusCode);
        }).on('error', reject);
      });
      assert.equal(foreignHostStatus, 403);
      assert.equal(errors, '');
    } finally {
      if (child.exitCode === null) { const exited = once(child, 'exit'); child.kill(); await exited; }
    }
  });
});
