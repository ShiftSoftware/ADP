import assert from 'node:assert/strict';
import {mkdtemp, readFile, realpath, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join, dirname, basename} from 'node:path';
import vm from 'node:vm';
import {exportExplorer, buildStandalone} from './export.mjs';

const evidenceLoader = await readFile(new URL('evidence.js', import.meta.url), 'utf8');
async function checkOfflineLoader(snapshot, protocol) {
  const sandbox = {window:{}, location:{protocol}, document:{getElementById:() => ({textContent:JSON.stringify(snapshot)})},
    fetch:() => { throw new Error('Offline exports must never fetch'); }};
  vm.runInNewContext(evidenceLoader, sandbox);
  assert.equal(sandbox.window.ExplorerEvidence.offline, true);
  assert.equal(JSON.stringify(await sandbox.window.ExplorerEvidence.load()), JSON.stringify(snapshot));
  assert.equal(await sandbox.window.ExplorerEvidence.freshness(), null);
}

const scratch = await mkdtemp(join(tmpdir(), 'adp-explorer-export-'));
try {
  for (const product of ['hawta', 'rastgo']) {
    const destination = await exportExplorer(product, join(scratch, product));
    const html = await readFile(join(destination, 'index.html'), 'utf8');
    const assets = [...html.matchAll(/(?:src|href)="([^"#]+\.(?:css|js|svg))"/g)].map(match => match[1]);
    assert(assets.includes('shared/shell.js'));
    assert(assets.includes('shared/overview.js'));
    assert(assets.includes('shared/theme.css'));
    assert(assets.includes('shared/engineering.css'));
    for (const asset of assets) {
      assert(!asset.includes('..') && !asset.includes(':'));
      const content = await readFile(join(destination, asset), 'utf8');
      assert(content.length > 0, asset);
      if (asset.endsWith('.js')) new vm.Script(content, {filename: asset});
    }
    const evidence = JSON.parse(await readFile(join(destination, 'evidence.json'), 'utf8'));
    assert(evidence.entries.length > 0);
    await assert.rejects(exportExplorer(product, destination), {code:'EEXIST'});
    assert.equal(await readFile(join(destination, 'index.html'), 'utf8'), html);
    const standalone = await exportExplorer(product, join(scratch, product + '.html'), {singleFile:true});
    const single = await readFile(standalone, 'utf8');
    assert.equal(single, await buildStandalone(product), 'Export must be reproducible');
    assert(!/<script\b[^>]*\bsrc=|<link\b[^>]*rel="stylesheet"/i.test(single), 'No external script or stylesheet');
    const staticMarkup = single.replace(/<script(?:\s[^>]*)?>[\s\S]*?<\/script>/g, '');
    assert(!/(?:src|href|data-explorer-icon)="(?!data:|#)[^"]+"/.test(staticMarkup), 'No relative asset or network links');
    assert.match(single, /connect-src 'none'/);
    const embedded = JSON.parse(single.match(/<script type="application\/json" id="explorer-evidence">([\s\S]*?)<\/script>/)[1]);
    assert.deepEqual(embedded, evidence, 'Every captured excerpt and hash is preserved');
    for (const script of single.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)) {
      if (!script[0].startsWith('<script type="application/json"')) new vm.Script(script[1]);
    }
    assert(single.indexOf('window.ExplorerOverview =') > single.indexOf('</main>'), 'Inlined controllers execute after the DOM');
    await checkOfflineLoader(embedded, 'file:');
    await checkOfflineLoader(embedded, 'http:');
    await assert.rejects(exportExplorer(product, standalone, {singleFile:true}), {code:'EEXIST'});
    assert.equal(await readFile(standalone, 'utf8'), single);
  }
  await assert.rejects(exportExplorer('unknown', join(scratch, 'unknown')), /Usage:/);
  // The evidence loader returns code excerpts as data, without executing them.
  await checkOfflineLoader({entries:[{excerpt:'</script><script>throw new Error("data")</script> $& $`'}]}, 'file:');
  let requests = [];
  const live = {window:{},location:{protocol:'http:'},document:{getElementById:()=>null},fetch:async url => {
    requests.push(url); return {ok:true,json:async()=>({url})};
  }};
  vm.runInNewContext(evidenceLoader, live);
  assert.equal(live.window.ExplorerEvidence.offline, false);
  await live.window.ExplorerEvidence.load(); await live.window.ExplorerEvidence.freshness();
  assert.deepEqual(requests, ['evidence.json','evidence-status.json']);
  const rawFile = {window:{},location:{protocol:'file:'},document:{getElementById:()=>null},fetch:()=>{throw new Error('Unexpected file fetch');}};
  vm.runInNewContext(evidenceLoader, rawFile);
  await assert.rejects(rawFile.window.ExplorerEvidence.load(), /exported Explorer/);
  assert.equal(await rawFile.window.ExplorerEvidence.freshness(), null);
  console.log('PASS: both folder and single-file exports preserve assets/evidence, inline scripts parse, builds reproduce, offline evidence never fetches, live previews still check sources, and outputs are never overwritten.');
} finally {
  const canonical = await realpath(scratch);
  assert.equal(dirname(canonical), await realpath(tmpdir()));
  assert(basename(canonical).startsWith('adp-explorer-export-'));
  await rm(canonical, {recursive:true, force:true});
}
