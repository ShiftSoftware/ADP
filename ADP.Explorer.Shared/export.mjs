// Build exports from maintained sources. Never overwrite an existing destination.
import {mkdir, copyFile, readFile, writeFile} from 'node:fs/promises';
import {resolve, join} from 'node:path';
import {fileURLToPath, pathToFileURL} from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const shared = fileURLToPath(new URL('./', import.meta.url));
const sharedAssets = ['theme.css','engineering.css','shell.js','overview.js','evidence.js'];
const usage = 'Usage: node ADP.Explorer.Shared/export.mjs <hawta|rastgo> <new-directory> OR <new-file.html> --single-file';

export async function buildStandalone(product) {
  const {html, app} = await exportDocument(product);
  const scripts = [];
  let result = html;
  for (const match of html.matchAll(/<link rel="stylesheet" href="([^"]+)">/g)) {
    const css = await readFile(assetPath(app, match[1]), 'utf8');
    if (/<\/style/i.test(css)) throw new Error('Unexpected closing style tag in ' + match[1]);
    result = result.replace(match[0], () => '<style>\n' + css + '\n</style>');
  }
  for (const match of html.matchAll(/<script src="([^"]+)" defer><\/script>/g)) {
    const code = await readFile(assetPath(app, match[1]), 'utf8');
    scripts.push('<script>\n' + code.replace(/<\/script/gi, '<\\/script') + '\n</script>');
    result = result.replace(match[0], '');
  }
  const icon = 'data:image/svg+xml;base64,' + (await readFile(join(app, product + '-icon.svg'))).toString('base64');
  result = result.replaceAll(product + '-icon.svg', icon);
  // Unlike deferred external scripts, inline classic scripts execute immediately.
  // Place them after the complete DOM, preserving the source script order.
  result = result.replace('</body>', () => scripts.join('\n') + '\n</body>');
  result = result.replace('<head>', '<head>\n<meta http-equiv="Content-Security-Policy" content="default-src \'none\'; script-src \'unsafe-inline\'; style-src \'unsafe-inline\'; img-src data:; connect-src \'none\'; base-uri \'none\'; form-action \'none\'">');
  return result;
}

function assetPath(app, asset) {
  if (asset.startsWith('shared/') && sharedAssets.includes(asset.slice(7))) return join(shared, asset.slice(7));
  if (['style.css','overview.js','mechanisms.js','explorer.js'].includes(asset)) return join(app, asset);
  throw new Error('Unexpected browser asset: ' + asset);
}

async function exportDocument(product) {
  const name = {hawta:'Hawta', rastgo:'Rastgo'}[product];
  if (!name) throw new Error(usage);
  const app = join(root, 'ADP.' + name, 'Explorer');
  const snapshot = JSON.parse(await readFile(join(app, 'evidence.json'), 'utf8'));
  const json = JSON.stringify(snapshot);
  const embedded = '<script type="application/json" id="explorer-evidence">' + json.replaceAll('<', '\\u003c') + '</script>';
  let html = await readFile(join(app, 'index.html'), 'utf8');
  html = html.replace('</head>', () => embedded + '\n</head>');
  const download = 'data:application/json;base64,' + Buffer.from(json).toString('base64');
  html = html.replace('href="evidence.json"', 'download="' + product + '-evidence.json" href="' + download + '"');
  return {html, app};
}

export async function exportExplorer(product, destination, {singleFile = false} = {}) {
  if (!destination) throw new Error(usage);
  const target = resolve(destination);
  if (singleFile) {
    const html = await buildStandalone(product);
    await writeFile(target, html, {encoding:'utf8', flag:'wx'});
    return target;
  }
  const {html, app} = await exportDocument(product);
  await mkdir(target);
  await mkdir(join(target, 'shared'));
  await writeFile(join(target, 'index.html'), html);
  const assets = ['overview.js','mechanisms.js','explorer.js','evidence.json', product + '-icon.svg'];
  if (product === 'rastgo') assets.push('style.css');
  for (const asset of assets) await copyFile(join(app, asset), join(target, asset));
  for (const asset of sharedAssets) await copyFile(join(shared, asset), join(target, 'shared', asset));
  return target;
}
if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
  const [, , product, destination, option] = process.argv;
  if (option && option !== '--single-file') throw new Error(usage);
  console.log(await exportExplorer(product, destination, {singleFile:option === '--single-file'}));
}
