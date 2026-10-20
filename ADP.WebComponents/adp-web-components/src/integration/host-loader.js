const documentStateKey = Symbol.for('shiftsoftware.adp-web-components.integration-state');
const hostConfigKey = Symbol.for('shiftsoftware.adp-web-components.host-config');

function getDocumentState() {
  const global = globalThis;
  if (!global[documentStateKey]) {
    global[documentStateKey] = {
      modules: new Set(),
      outputFamily: undefined,
      packageVersion: undefined,
    };
  }

  return global[documentStateKey];
}

function ensureDocumentContract(packageVersion, outputFamily) {
  const state = getDocumentState();

  if (state.packageVersion && state.packageVersion !== packageVersion) {
    throw new Error(`ADP web components version mismatch. This document already uses ${state.packageVersion}; cannot load ${packageVersion}.`);
  }

  if (state.outputFamily && state.outputFamily !== outputFamily) {
    throw new Error(`ADP web components output family mismatch. This document already uses ${state.outputFamily}; cannot load ${outputFamily}.`);
  }

  state.packageVersion = packageVersion;
  state.outputFamily = outputFamily;
  return state;
}

function waitForDefinition(tag, timeoutMs) {
  if (customElements.get(tag)) {
    return Promise.resolve();
  }

  return Promise.race([
    customElements.whenDefined(tag),
    new Promise((_, reject) => {
      setTimeout(() => reject(new Error(`Timed out while defining custom element ${tag}.`)), timeoutMs);
    }),
  ]);
}

function loadModuleScript(moduleUrl) {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.type = 'module';
    script.src = moduleUrl;
    script.onload = resolve;
    script.onerror = () => reject(new Error(`Could not load ADP web component module: ${moduleUrl}`));
    document.head.appendChild(script);
  });
}

/**
 * Loads one ADP per-component module and waits until its custom element is available.
 * All calls in one browser document must use the same package version and output family.
 */
export async function loadComponent({ moduleUrl, tag, packageVersion, outputFamily = 'per-component', timeoutMs = 10000 }) {
  if (!moduleUrl || !tag || !packageVersion) {
    throw new Error('moduleUrl, tag, and packageVersion are required to load an ADP web component.');
  }

  const state = ensureDocumentContract(packageVersion, outputFamily);
  if (!state.modules.has(moduleUrl)) {
    await loadModuleScript(moduleUrl);
    state.modules.add(moduleUrl);
  }

  await waitForDefinition(tag, timeoutMs);
}

/** Fetches a published integration manifest from a host-selected URL. */
export async function loadIntegrationManifest(manifestUrl) {
  const response = await fetch(manifestUrl);
  if (!response.ok) {
    throw new Error(`Could not load ADP integration manifest: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

function getHostConfig() {
  const global = globalThis;
  const config = global[hostConfigKey];
  if (config && typeof config === 'object' && !Array.isArray(config)) return config;

  global[hostConfigKey] = {};
  return global[hostConfigKey];
}

function isLanguagePacksStore(value) {
  return !!value && typeof value === 'object' && !Array.isArray(value) && typeof value.revision === 'number' && !!value.layers && typeof value.layers === 'object';
}

// Same store shape as src/features/multi-lingual/language-packs.ts; this file cannot import it.
function registerGlobalLanguagePacks(config, input) {
  if (input === undefined || input === null || input === '') return;

  let store = config.languagePacks;
  if (!isLanguagePacksStore(store)) {
    const previous = store;
    store = { revision: 0, layers: { 1: [], 2: [], 3: [] } };
    if (previous !== undefined && previous !== null) store.layers[1].push({ input: previous });
    config.languagePacks = store;
  }

  if (!Array.isArray(store.layers[1])) store.layers[1] = [];
  const entries = store.layers[1];
  const existing = entries.findIndex(entry => entry.input === input);
  if (existing > -1) entries.splice(existing, 1);
  entries.push({ input });
  store.revision += 1;
}

/** Document-wide settings read by every ADP tag; unknown keys are ignored so older builds accept newer options. */
export function configureAdpHost(options = {}) {
  const config = getHostConfig();

  if (options && Object.prototype.hasOwnProperty.call(options, 'languagePacks')) registerGlobalLanguagePacks(config, options.languagePacks);
}
