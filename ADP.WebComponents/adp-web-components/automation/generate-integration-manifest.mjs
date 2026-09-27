import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import prettier from 'prettier';

const projectRoot = resolve(import.meta.dirname, '..');
const packageJson = readJson('package.json');
const contract = readJson('src/integration/integration-contract.json');
const stencilDocs = readJson('dist/stencil-docs.json');

function readJson(relativePath) {
  return JSON.parse(readFileSync(resolve(projectRoot, relativePath), 'utf8'));
}

function simplifyProp(prop, reference) {
  return {
    name: prop.name,
    attribute: prop.attr,
    type: prop.complexType?.resolved ?? prop.type,
    default: prop.defaultValue ?? null,
    mutable: prop.mutable,
    required: prop.required,
    ...(reference && { default: prop.default ?? null, reflect: prop.reflectToAttr, description: reference.props?.[prop.name] ?? null }),
  };
}

function simplifyMethod(method, reference) {
  return {
    name: method.name,
    parameters: method.complexType?.signature ?? null,
    returns: method.returns?.complexType?.resolved ?? method.returns?.type ?? null,
    ...(reference && { description: reference.methods?.[method.name] ?? null }),
  };
}

// The value after the first top-level comma inside var( … ), with nested parentheses kept whole.
function fallbackOf(css, start) {
  let depth = 1;
  let comma = -1;

  for (let index = start; index < css.length; index++) {
    const char = css[index];

    if (char === '(') depth++;
    else if (char === ')' && --depth === 0) return comma === -1 ? null : css.slice(comma + 1, index).trim();
    else if (char === ',' && depth === 1 && comma === -1) comma = index;
  }

  return null;
}

// Every --<tag>-* custom property the component's stylesheet reads, with the default it falls back to.
function cssPropertiesOf(stencilComponent, reference) {
  const stylesheet = resolve(projectRoot, stencilComponent.filePath.replace(/\.tsx$/, '.css'));
  if (!existsSync(stylesheet)) return [];

  const css = readFileSync(stylesheet, 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');
  const found = new Map();

  for (const match of css.matchAll(new RegExp(`var\\(\\s*(--${stencilComponent.tag}-[\\w-]+)`, 'g'))) {
    if (!found.has(match[1])) found.set(match[1], fallbackOf(css, match.index + match[0].length));
  }

  return [...found]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([name, fallback]) => ({ name, default: fallback, ...(reference && { description: reference.cssProperties?.[name] ?? null }) }));
}

function ensureReferenceComplete(stencilComponent, reference, tag) {
  const source = readFileSync(resolve(projectRoot, stencilComponent.filePath), 'utf8');
  const problems = [];

  for (const [key, names] of [
    ['props', stencilComponent.props.map(prop => prop.name)],
    ['events', stencilComponent.events.map(event => event.event)],
    ['methods', stencilComponent.methods.map(method => method.name)],
  ]) {
    const described = Object.keys(reference[key] ?? {});

    problems.push(...names.filter(name => !described.includes(name)).map(name => `undescribed ${key}: ${name}`));
    problems.push(...described.filter(name => !names.includes(name)).map(name => `described ${key} that Stencil did not emit: ${name}`));
  }

  const variables = cssPropertiesOf(stencilComponent).map(variable => variable.name);
  const describedVariables = Object.keys(reference.cssProperties ?? {});

  problems.push(...variables.filter(name => !describedVariables.includes(name)).map(name => `undescribed CSS variable: ${name}`));
  problems.push(...describedVariables.filter(name => !variables.includes(name)).map(name => `described CSS variable the stylesheet never reads: ${name}`));

  for (const part of Object.keys(reference.parts ?? {})) {
    const emitted = stencilComponent.parts.some(entry => entry.name === part) || new RegExp(`['"\\s]${part}['"\\s]`).test(source);
    if (!emitted) problems.push(`described part the component never renders: ${part}`);
  }

  if (problems.length) throw new Error(`Integration contract reference for ${tag} is out of date:\n  ${problems.join('\n  ')}`);
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

  const reference = contractComponent.reference;
  if (reference) ensureReferenceComplete(stencilComponent, reference, tag);

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
      props: stencilComponent.props.map(prop => simplifyProp(prop, reference)),
      methods: stencilComponent.methods.map(method => simplifyMethod(method, reference)),
      events: stencilComponent.events.map(event => ({
        name: event.event,
        type: event.complexType?.resolved ?? event.type,
        ...(reference && { description: reference.events?.[event.event] ?? null }),
      })),
      dependencies: stencilComponent.dependencies,
      ...(reference && {
        parts: Object.entries(reference.parts ?? {}).map(([name, description]) => ({ name, description })),
        cssProperties: cssPropertiesOf(stencilComponent, reference),
      }),
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
