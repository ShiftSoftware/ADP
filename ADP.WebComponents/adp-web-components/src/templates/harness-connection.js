const HTTP_PROTOCOLS = new Set(['http:', 'https:']);
const HEADER_NAME = /^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$/;
const STORAGE_VERSION = 1;

const clone = value => {
  if (value === undefined || value === null || typeof value !== 'object') return value;

  return JSON.parse(JSON.stringify(value));
};

const asObject = value => {
  if (!value) return {};

  if (typeof value === 'object' && !Array.isArray(value)) return clone(value);

  if (typeof value !== 'string') return {};

  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
  } catch {
    return {};
  }
};

const urlField = (name, label, required = true) => ({
  name,
  label,
  kind: 'url',
  inputType: 'url',
  required,
  placeholder: 'https://api.example.invalid/',
});

const secretField = (name, label, kind, placeholder) => ({
  name,
  label,
  kind,
  inputType: 'password',
  required: true,
  placeholder,
  secret: true,
});

export function connectionFields(profile, options = {}) {
  if (profile === 'part') {
    return [urlField('endpointUrl', 'Endpoint URL'), secretField('queryJson', 'Query JSON', 'query', '{"code":""}')];
  }

  if (profile === 'form') return [urlField('structureUrl', 'Structure URL')];
  if (profile === 'vin-extractor') return [urlField('ocrEndpoint', 'OCR endpoint')];

  const fields = [urlField('baseUrl', 'Base URL'), secretField('headersJson', 'Request headers JSON', 'headers', '{}')];

  if (options.recaptcha) fields.push(secretField('recaptchaSiteKey', 'reCAPTCHA site key', 'text', 'Site key'));
  if (options.claim) fields.push(urlField('claimEndpoint', 'Claim endpoint'));
  if (options.unauthorizedSsc) fields.push(urlField('unauthorizedSscEndpoint', 'Manufacturer-check endpoint'));

  return fields;
}

export function emptyConnectionDraft(profile, options = {}) {
  return Object.fromEntries(connectionFields(profile, options).map(field => [field.name, field.kind === 'headers' || field.kind === 'query' ? '{}' : '']));
}

const validateUrl = value => {
  try {
    const url = new URL(value);

    return HTTP_PROTOCOLS.has(url.protocol) && !url.username && !url.password;
  } catch {
    return false;
  }
};

const parseJsonObject = (value, field, errors) => {
  try {
    const parsed = JSON.parse(value);

    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) throw new Error();

    return parsed;
  } catch {
    errors[field] = 'Enter one JSON object.';
    return null;
  }
};

export function validateConnection(profile, draft, options = {}) {
  const errors = {};
  const settings = {};

  for (const field of connectionFields(profile, options)) {
    const value = String(draft?.[field.name] ?? '').trim();

    if (!value) {
      if (field.required) errors[field.name] = `${field.label} is required.`;
      continue;
    }

    if (field.kind === 'url') {
      if (!validateUrl(value)) errors[field.name] = 'Enter an absolute HTTP or HTTPS URL without embedded credentials.';
      else settings[field.name] = value;
      continue;
    }

    if (field.kind === 'headers') {
      const parsed = parseJsonObject(value, field.name, errors);

      if (parsed) {
        const invalidName = Object.keys(parsed).some(name => !HEADER_NAME.test(name));
        const invalidValue = Object.values(parsed).some(header => !['string', 'number', 'boolean'].includes(typeof header));

        if (invalidName || invalidValue) errors[field.name] = 'Use valid header names with string, number, or boolean values.';
        else settings.headers = Object.fromEntries(Object.entries(parsed).map(([name, header]) => [name, String(header)]));
      }
      continue;
    }

    if (field.kind === 'query') {
      const parsed = parseJsonObject(value, field.name, errors);
      if (parsed) settings.query = parsed;
      continue;
    }

    settings[field.name] = value;
  }

  return { ok: Object.keys(errors).length === 0, errors, settings };
}

const settingsToDraft = (profile, settings, options) => ({
  ...emptyConnectionDraft(profile, options),
  ...settings,
  headersJson: JSON.stringify(settings?.headers ?? {}),
  queryJson: JSON.stringify(settings?.query ?? {}),
});

export function connectionStorageKey(profile, pathname) {
  return `adp-harness-connection:v${STORAGE_VERSION}:${profile}:${pathname || '/'}`;
}

export function saveConnection(storage, key, settings) {
  storage.setItem(key, JSON.stringify({ version: STORAGE_VERSION, settings }));
}

export function loadConnection(storage, key, profile, options = {}) {
  try {
    const stored = JSON.parse(storage.getItem(key));
    if (stored?.version !== STORAGE_VERSION) return null;

    const validated = validateConnection(profile, settingsToDraft(profile, stored.settings, options), options);
    return validated.ok ? validated.settings : null;
  } catch {
    return null;
  }
}

export function clearConnection(storage, key) {
  storage.removeItem(key);
}

export function connectionOptions(profile, configured) {
  const options = configured && typeof configured === 'object' ? configured : {};

  return {
    recaptcha: profile === 'composite' || options.recaptcha === true,
    claim: profile === 'composite' || options.claim === true,
    unauthorizedSsc: profile === 'composite' || options.unauthorizedSsc === true,
    targets: options.targets ?? null,
  };
}

export function captureConnectionState(profile, subject, options = {}) {
  if (profile === 'part') return { endpoint: clone(subject.endpoint) };
  if (profile === 'form') return { structureUrl: subject.structureUrl || subject.getAttribute?.('structure-url') || '', fields: clone(subject.fields) };
  if (profile === 'vin-extractor') return { endpoints: (options.targets ?? [subject]).map(target => target.ocrEndpoint || target.getAttribute?.('ocr-endpoint') || '') };

  return {
    baseUrl: subject.baseUrl ?? '',
    headers: clone(subject.headers ?? {}),
    requestHeadersProvider: subject.requestHeadersProvider,
    recaptchaKey: subject.recaptchaKey ?? '',
    claimEndPoint: subject.claimEndPoint ?? '',
    unauthorizedSscLookupBaseUrl: subject.unauthorizedSscLookupBaseUrl ?? '',
    childrenProps: clone(subject.childrenProps),
  };
}

const applyCompositeSettings = (subject, settings, options) => {
  const props = asObject(subject.childrenProps);

  if (options.recaptcha || options.unauthorizedSsc) {
    props['vehicle-ssc'] = {
      ...(props['vehicle-ssc'] ?? {}),
      ...(options.recaptcha ? { recaptchaKey: settings.recaptchaSiteKey } : {}),
      ...(options.unauthorizedSsc ? { unauthorizedSscLookupBaseUrl: settings.unauthorizedSscEndpoint } : {}),
    };
  }

  if (options.claim) {
    props['vehicle-claimable-items'] = {
      ...(props['vehicle-claimable-items'] ?? {}),
      claimEndPoint: settings.claimEndpoint,
    };
  }

  subject.childrenProps = props;
};

export function connectProfile(profile, subject, settings, options = {}) {
  if (profile === 'part') {
    subject.endpoint = { url: settings.endpointUrl, query: clone(settings.query) };
    subject.isDev = false;
    return;
  }

  if (profile === 'form') {
    subject.structureUrl = settings.structureUrl;
    subject.isDev = false;
    return;
  }

  if (profile === 'vin-extractor') {
    for (const target of options.targets ?? [subject]) target.ocrEndpoint = settings.ocrEndpoint;
    return;
  }

  subject.baseUrl = settings.baseUrl;
  subject.headers = clone(settings.headers);
  subject.requestHeadersProvider = () => clone(settings.headers);

  if (profile === 'composite') applyCompositeSettings(subject, settings, options);
  else {
    if (options.recaptcha) subject.recaptchaKey = settings.recaptchaSiteKey;
    if (options.claim) subject.claimEndPoint = settings.claimEndpoint;
    if (options.unauthorizedSsc) subject.unauthorizedSscLookupBaseUrl = settings.unauthorizedSscEndpoint;
  }

  subject.isDev = false;
}

export function disconnectProfile(profile, subject, baseline, options = {}) {
  if (profile === 'part') {
    subject.endpoint = clone(baseline.endpoint);
    subject.isDev = true;
    return;
  }

  if (profile === 'form') {
    subject.structureUrl = baseline.structureUrl;
    subject.fields = clone(baseline.fields);
    subject.isDev = true;
    return;
  }

  if (profile === 'vin-extractor') {
    (options.targets ?? [subject]).forEach((target, index) => {
      target.ocrEndpoint = baseline.endpoints[index] ?? '';
    });
    return;
  }

  subject.baseUrl = baseline.baseUrl;
  subject.headers = clone(baseline.headers);
  subject.requestHeadersProvider = baseline.requestHeadersProvider;
  subject.recaptchaKey = baseline.recaptchaKey;
  subject.claimEndPoint = baseline.claimEndPoint;
  subject.unauthorizedSscLookupBaseUrl = baseline.unauthorizedSscLookupBaseUrl;
  subject.childrenProps = clone(baseline.childrenProps);
  subject.isDev = true;
}

export function todayChoiceAvailable(mode, connected, hasAnchor) {
  if (connected) return mode === 'live';
  return mode !== 'anchor' || hasAnchor;
}

export function connectionLog(action, profile) {
  const label = profile === 'vin-extractor' ? 'VIN extractor' : profile === 'form' ? 'form' : profile === 'part' ? 'part lookup' : 'vehicle lookup';
  return action === 'connect' ? `${label} · live settings applied · credentials withheld` : `${label} · generated settings restored`;
}
