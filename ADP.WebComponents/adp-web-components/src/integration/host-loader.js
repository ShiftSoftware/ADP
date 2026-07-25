const documentStateKey = Symbol.for('shiftsoftware.adp-web-components.integration-state');

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
