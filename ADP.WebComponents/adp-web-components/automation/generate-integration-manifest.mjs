import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import prettier from 'prettier';

const projectRoot = resolve(import.meta.dirname, '..');
const packageJson = readJson('package.json');
const contract = readJson('src/integration/integration-contract.json');
const stencilDocs = readJson('dist/stencil-docs.json');

function readJson(relativePath) {
  return JSON.parse(readFileSync(resolve(projectRoot, relativePath), 'utf8'));
}

function simplifyProp(prop) {
  return {
    name: prop.name,
    attribute: prop.attr,
    type: prop.complexType?.resolved ?? prop.type,
    default: prop.defaultValue ?? null,
    mutable: prop.mutable,
    required: prop.required,
  };
}

function simplifyMethod(method) {
  return {
    name: method.name,
    parameters: method.complexType?.signature ?? null,
    returns: method.returns?.complexType?.resolved ?? method.returns?.type ?? null,
  };
}

function ensureNamesExist(component, names, key, tag) {
  const availableNames = new Set(component[key].map(item => item.name));
  const missingNames = names.filter(name => !availableNames.has(name));
  if (missingNames.length) {
    throw new Error(`Integration contract for ${tag} declares missing ${key}: ${missingNames.join(', ')}`);
  }
}

const components = Object.entries(contract.components).map(([tag, contractComponent]) => {
  const stencilComponent = stencilDocs.components.find(component => component.tag === tag);
  if (!stencilComponent) {
    throw new Error(`Integration contract references a component Stencil did not emit: ${tag}`);
  }

  ensureNamesExist(stencilComponent, contractComponent.host.properties, 'props', tag);
  ensureNamesExist(stencilComponent, contractComponent.host.methods, 'methods', tag);

  return {
    tag,
    modulePath: `dist/components/${tag}.js`,
    wireContract: contractComponent.wireContract,
    supportedLocales: contractComponent.supportedLocales,
    host: {
      properties: contractComponent.host.properties,
      methods: contractComponent.host.methods,
      sequence: ['load module', 'wait for custom element definition', 'set properties', 'call a documented method'],
      owns: contractComponent.host.owns,
    },
    api: {
      props: stencilComponent.props.map(simplifyProp),
      methods: stencilComponent.methods.map(simplifyMethod),
      events: stencilComponent.events.map(event => ({ name: event.event, type: event.complexType?.resolved ?? event.type })),
      dependencies: stencilComponent.dependencies,
    },
    runtimeAssets: contractComponent.runtimeAssets,
  };
});

const manifest = {
  schemaVersion: contract.schemaVersion,
  package: packageJson.name,
  packageVersion: packageJson.version,
  outputFamily: contract.outputFamily,
  components,
};

const prettierConfig = (await prettier.resolveConfig(resolve(projectRoot, 'src/integration/integration-manifest.json'))) ?? {};
const output = await prettier.format(JSON.stringify(manifest), { ...prettierConfig, parser: 'json' });
writeFileSync(resolve(projectRoot, 'src/integration/integration-manifest.json'), output);
writeFileSync(resolve(projectRoot, 'dist/integration-manifest.json'), output);
console.log(`Generated integration manifest for ${components.length} component(s).`);
