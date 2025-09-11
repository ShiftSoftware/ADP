import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function setNestedKey(obj, keyPath, value) {
  const keys = keyPath.split('.');
  let current = obj;

  keys.forEach((key, i) => {
    if (i === keys.length - 1) {
      current[key] = value;
    } else {
      if (!current[key] || typeof current[key] !== 'object') {
        current[key] = {};
      }
      current = current[key];
    }
  });
}

const args = process.argv.slice(2);

function getArg(flag) {
  const index = args.indexOf(flag);
  return index !== -1 ? args[index + 1] : null;
}

const targetFile = args[0]; // e.g. vehicle-quotation
if (!targetFile) console.error('❌ Missing required arguments: target file (e.g. vehicle-quotation)');

const targetFolderPath = args[1]; // e.g. vehicle-quotation
if (!targetFolderPath) console.error('❌ Missing required arguments: target folder path relative to locales folder (e.g. ./forms)');

const keyPath = getArg('-key'); // e.g. form.phone
if (!keyPath) console.error('❌ Missing required arguments: -key');

const enValue = getArg('-en'); // required english language
if (!enValue) console.error('❌ Missing required arguments: -en');

if (!keyPath || !enValue || !targetFile || !targetFolderPath) {
  process.exit(1);
}

const camelName = targetFile.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
const relativePath = path.join(__dirname, '../src/locales', targetFolderPath, camelName);

const languages = ['ku', 'ar', 'en', 'ru'];

for (const lang of languages) {
  const targetFileLang = path.join(relativePath, `${lang}.json`);
  if (!fs.existsSync(targetFileLang)) {
    console.error(`❌ Target folder does not exist: ${targetFileLang} \nPlease create it first using the create:locale script.`);
    process.exit(1);
  }

  const json = JSON.parse(fs.readFileSync(targetFileLang, 'utf-8'));

  const value = getArg(`-${lang}`) || enValue;

  setNestedKey(json, keyPath, value);

  fs.writeFileSync(targetFileLang, JSON.stringify(json, null, 2), 'utf-8');
  console.log(`✅ Updated ${targetFileLang} with ${keyPath} = "${value}"`);
}

execSync(`yarn create:type ${targetFolderPath}/${targetFile}`, { stdio: 'inherit' });
