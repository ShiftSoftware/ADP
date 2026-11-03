// Regenerate the checked-in single-file Explorers, or (--check) fail when they are stale.
import {readFile, writeFile} from 'node:fs/promises';
import {fileURLToPath, pathToFileURL} from 'node:url';
import {buildStandalone} from './export.mjs';

const root = fileURLToPath(new URL('../', import.meta.url));
export const checkedIn = {
  hawta: root + 'ADP.Hawta/Explorer/hawta-explorer.html',
  rastgo: root + 'ADP.Rastgo/Explorer/rastgo-explorer.html',
};
// Git may check files out with CRLF; compare content, not line endings.
const lf = text => text.replace(/\r\n/g, '\n');

export async function staleStandalones() {
  const stale = [];
  for (const [product, path] of Object.entries(checkedIn)) {
    const current = await readFile(path, 'utf8').catch(() => null);
    if (current === null || lf(current) !== lf(await buildStandalone(product))) stale.push(path);
  }
  return stale;
}

if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
  if (process.argv[2] === '--check') {
    const stale = await staleStandalones();
    for (const path of stale) console.error('Stale: ' + path);
    if (stale.length) { console.error('Run: node ADP.Explorer.Shared/standalone.mjs'); process.exitCode = 1; }
    else console.log('Checked-in standalone Explorers are current.');
  } else {
    for (const [product, path] of Object.entries(checkedIn)) {
      await writeFile(path, await buildStandalone(product));
      console.log(path);
    }
  }
}
