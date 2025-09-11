import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function deleteNestedKey(obj, keyPath) {
  const keys = keyPath.split('.');

  function helper(current, i) {
    const key = keys[i];

    if (i === keys.length - 1) {
      // Final key → delete it
      delete current[key];
    } else {
      if (!current[key] || typeof current[key] !== 'object') {
        return; // Path doesn’t exist → nothing to delete
      }
      helper(current[key], i + 1);

      // After recursion, if child is empty, clean it up
      if (Object.keys(current[key]).length === 0) {
        delete current[key];
      }
    }
  }

  helper(obj, 0);
}

const args = process.argv.slice(2);

const targetFile = args[0]; // e.g. vehicle-quotation
if (!targetFile) console.error('❌ Missing required arguments: target file (e.g. vehicle-quotation)');

const targetFolderPath = args[1]; // e.g. vehicle-quotation
if (!targetFolderPath) console.error('❌ Missing required arguments: target folder path relative to locales folder (e.g. ./forms)');

const keyPath = args[2]; // e.g. form.phone
if (!keyPath) console.error('❌ Missing required arguments: field key path');

if (!keyPath || !targetFile || !targetFolderPath) {
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

  deleteNestedKey(json, keyPath);

  fs.writeFileSync(targetFileLang, JSON.stringify(json, null, 2), 'utf-8');
  console.log(`✅ Updated ${targetFileLang} with deletion of ${keyPath}"`);
}

execSync(`yarn create:type ${targetFolderPath}/${targetFile}`, { stdio: 'inherit' });
