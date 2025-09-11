import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function getArg(flag) {
  const index = args.indexOf(flag);
  return index !== -1 ? args[index + 1] : null;
}

const args = process.argv.slice(2);

const targetFile = args[0]; // e.g. ./forms/vehicle-quotation
if (!targetFile) console.error('❌ Missing required arguments: target file (e.g. ./forms/vehicle-quotation)');

const fileName = path.basename(targetFile);

const jsonPath = path.join(__dirname, '../src/locales', targetFile, 'en.json');
const typePath = path.join(__dirname, '../src/locales', targetFile, 'type.ts');

if (!fs.existsSync(jsonPath)) {
  console.error(`❌ Target file does not exist: ${jsonPath}`);
  process.exit(1);
}

function generateSchema(camelName, json) {
  let nestedDeclarations = [];

  function walk(obj, varName) {
    let localObjectFields = [];
    let localConcatKeys = [];

    for (const key in obj) {
      if (typeof obj[key] === 'object' && obj[key] !== null) {
        // Nested object → recurse
        const childVar = `${varName}_${key}`;
        const { code, fields } = walk(obj[key], childVar);

        // Add child declaration
        nestedDeclarations.push(code);

        // Add this key as an object reference
        localObjectFields.push(`${key}: ${childVar}`);
      } else {
        // String → goes into concat
        localConcatKeys.push(key);
      }
    }

    let code = '';
    if (localConcatKeys.length || localObjectFields.length) {
      code += `const ${varName} = object({\n`;
      code += localObjectFields.map(f => `  ${f},`).join('\n');
      code += `\n}).concat(\n  yupTypeMapper(${JSON.stringify(localConcatKeys, null, 2)})\n);\n`;
    }

    return { code, fields: localObjectFields };
  }

  // Start recursion
  const { code } = walk(json, camelName);
  nestedDeclarations.push(code);

  return `import { object } from 'yup';\nimport yupTypeMapper from '~lib/yup-type-mapper';\n\n${nestedDeclarations.join('\n')}\nexport default ${camelName};`;
}

const json = JSON.parse(fs.readFileSync(jsonPath, 'utf-8'));

const typeFileContent = generateSchema(fileName, json);

fs.writeFileSync(typePath, typeFileContent, 'utf-8');

console.log(`✅ Type file generated at: ${typePath}`);
