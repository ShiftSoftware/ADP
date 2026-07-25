import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const projectRoot = resolve(import.meta.dirname, '..');
const packageJson = JSON.parse(readFileSync(resolve(projectRoot, 'package.json'), 'utf8'));
const manifestPath = resolve(projectRoot, 'dist/integration-manifest.json');
const templatePath = resolve(projectRoot, 'dist/templates/production-host/vehicle-service-history.html');
const componentPath = resolve(projectRoot, 'dist/components/vehicle-service-history.js');
const loaderPath = resolve(projectRoot, 'dist/host-loader.js');

for (const file of [manifestPath, templatePath, componentPath, loaderPath]) {
  if (!existsSync(file)) {
    throw new Error(`Published integration artifact is missing: ${file}`);
  }
}

const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
if (manifest.packageVersion !== packageJson.version) {
  throw new Error(`Manifest version ${manifest.packageVersion} does not match package version ${packageJson.version}.`);
}

const serviceHistory = manifest.components.find(component => component.tag === 'vehicle-service-history');
if (!serviceHistory || serviceHistory.modulePath !== 'dist/components/vehicle-service-history.js') {
  throw new Error('The vehicle-service-history manifest entry does not advertise its published flat module path.');
}

if (!serviceHistory.api?.props?.some(prop => prop.name === 'baseUrl') || !serviceHistory.api?.methods?.some(method => method.name === 'fetchVin')) {
  throw new Error('The service-history manifest is missing Stencil-generated API metadata.');
}

const template = readFileSync(templatePath, 'utf8');
for (const disallowedValue of ['is-dev', 'setMockData', 'mock-data.js', 'loadedResponse', 'JTM']) {
  if (template.includes(disallowedValue)) {
    throw new Error(`Production host template contains development-only content: ${disallowedValue}`);
  }
}

console.log('Integration artifact verified.');
