import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const rootDir = path.join(__dirname, '..');
const pkgPath = path.join(rootDir, 'package.json');
const distComponentsDir = path.join(rootDir, 'dist', 'components');

if (!fs.existsSync(distComponentsDir)) {
  console.error(`❌ Missing dist components directory: ${distComponentsDir}`);
  process.exit(1);
}

const pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf-8'));
const packageName = pkg.name;

const entries = fs.readdirSync(distComponentsDir, { withFileTypes: true });
const componentFiles = entries.filter(entry => entry.isFile() && entry.name.endsWith('.d.ts')).map(entry => entry.name);

if (!componentFiles.length) {
  console.error(`❌ No component type files found in: ${distComponentsDir}`);
  process.exit(1);
}

const urls = componentFiles.map(fileName => {
  const baseName = fileName.replace(/\.d\.ts$/, '.js');
  const relativePath = ['dist', 'components', baseName].join('/');
  return `https://purge.jsdelivr.net/npm/${packageName}@latest/${relativePath}`;
});

urls.push(`https://purge.jsdelivr.net/npm/${packageName}@latest/dist/shift-components/shift-components.esm.js`);

async function purgeAll() {
  const results = await Promise.all(
    urls.map(async url => {
      try {
        const response = await fetch(url, { method: 'GET' });
        return { url, status: response.status };
      } catch (error) {
        return { url, status: 'error', error };
      }
    }),
  );

  const failed = results.filter(result => result.status !== 200);
  for (const result of results) {
    if (result.status === 200) {
      console.log(`✅ Purged: ${result.url}`);
    } else {
      console.error(`❌ Failed: ${result.url} (${result.status})`);
    }
  }

  if (failed.length) {
    process.exit(1);
  }
}

await purgeAll();
