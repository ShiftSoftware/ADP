// scripts/create-form.mjs
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const args = process.argv.slice(2);
if (args.length < 2) {
  console.error('❌ Please provide a tag name and destination. \nExample: \nyarn create:locale vehicle-quotation ./forms');
  process.exit(1);
}

const tagName = args[0];
const destination = args[1];
const camelName = tagName.replace(/-([a-z])/g, (_, c) => c.toUpperCase());

const relativePath = path.join('./locales', destination, camelName);

const baseDir = path.join(__dirname, '../src/', relativePath);
if (!fs.existsSync(baseDir)) {
  fs.mkdirSync(baseDir, { recursive: true });
  console.log(`📁 Created folder: ${baseDir}`);
}

const locales = ['ku', 'ar', 'en', 'ru'];
locales.forEach(locale => {
  const filePath = path.join(baseDir, `${locale}.json`);
  const relPath = path.join(relativePath, `${locale}.json`);
  if (!fs.existsSync(filePath)) {
    fs.writeFileSync(filePath, '{}\n');
    console.log(`📝 Created: ${relPath}`);
  }
});

const typeFile = path.join(baseDir, 'type.ts');
const relTypeFile = path.join(relativePath, 'type.ts');
if (!fs.existsSync(typeFile)) {
  const content = `import yupTypeMapper from '~lib/yup-type-mapper';

const ${camelName}Schema = yupTypeMapper([]);

export default ${camelName}Schema;
`;
  fs.writeFileSync(typeFile, content);
  console.log(`📝 Created: ${relTypeFile}`);
}

console.log('✅ Done!');

execSync('yarn create:locale-mapper', { stdio: 'inherit' });
