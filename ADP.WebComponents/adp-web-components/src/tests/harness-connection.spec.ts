/* eslint-disable @typescript-eslint/no-explicit-any -- the test exercises browser-native JavaScript against custom-element-shaped doubles. */
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';

type StorageLike = { getItem: (key: string) => string | null; setItem: (key: string, value: string) => void; removeItem: (key: string) => void };

type ConnectionModule = {
  captureConnectionState: (profile: string, subject: any, options?: any) => any;
  clearConnection: (storage: StorageLike, key: string) => void;
  connectProfile: (profile: string, subject: any, settings: any, options?: any) => void;
  connectionFields: (profile: string, options?: any) => any[];
  connectionLog: (action: 'connect' | 'disconnect', profile: string) => string;
  connectionOptions: (profile: string, configured?: any) => any;
  connectionStorageKey: (profile: string, pathname: string) => string;
  disconnectProfile: (profile: string, subject: any, baseline: any, options?: any) => void;
  emptyConnectionDraft: (profile: string, options?: any) => Record<string, string>;
  loadConnection: (storage: StorageLike, key: string, profile: string, options?: any) => any;
  saveConnection: (storage: StorageLike, key: string, settings: any) => void;
  todayChoiceAvailable: (mode: string, connected: boolean, hasAnchor: boolean) => boolean;
  validateConnection: (profile: string, draft: Record<string, string>, options?: any) => { ok: boolean; errors: Record<string, string>; settings: any };
};

const connectionSource = readFileSync(path.resolve(process.cwd(), 'src', 'templates', 'harness-connection.js'), 'utf8').replaceAll('export ', '');
const connection = new Function(
  `${connectionSource}
  return {
    captureConnectionState, clearConnection, connectProfile, connectionFields,
    connectionLog, connectionOptions, connectionStorageKey, disconnectProfile,
    emptyConnectionDraft, loadConnection, saveConnection, todayChoiceAvailable,
    validateConnection
  };`,
)() as ConnectionModule;

const memoryStorage = (): StorageLike & { values: Map<string, string> } => {
  const values = new Map<string, string>();

  return {
    values,
    getItem: key => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: key => values.delete(key),
  };
};

const liveSettings = {
  baseUrl: 'https://api.example.invalid/vehicle/',
  headers: { Authorization: 'Bearer synthetic-test-token' },
  recaptchaSiteKey: 'synthetic-site-key',
  claimEndpoint: 'https://api.example.invalid/claim',
  unauthorizedSscEndpoint: 'https://api.example.invalid/campaign/',
};

describe('public-demo Connection panel', () => {
  it('validates absolute URLs and request-header JSON without echoing rejected values', () => {
    const options = connection.connectionOptions('composite');
    const draft = {
      baseUrl: liveSettings.baseUrl,
      headersJson: JSON.stringify(liveSettings.headers),
      recaptchaSiteKey: liveSettings.recaptchaSiteKey,
      claimEndpoint: liveSettings.claimEndpoint,
      unauthorizedSscEndpoint: liveSettings.unauthorizedSscEndpoint,
    };

    const valid = connection.validateConnection('composite', draft, options);
    expect(valid).toEqual({ ok: true, errors: {}, settings: liveSettings });

    const sentinel = 'DO_NOT_DISCLOSE_SYNTHETIC_VALUE';
    const invalid = connection.validateConnection('lookup', { baseUrl: `ftp://${sentinel}/`, headersJson: `{"bad header":"${sentinel}"}` });

    expect(invalid.ok).toBe(false);
    expect(JSON.stringify(invalid.errors)).not.toContain(sentinel);
  });

  it('masks every credential-bearing field', () => {
    const lookup = connection.connectionFields('composite', connection.connectionOptions('composite'));
    const part = connection.connectionFields('part');

    expect(lookup.find(field => field.name === 'headersJson').inputType).toBe('password');
    expect(lookup.find(field => field.name === 'recaptchaSiteKey').inputType).toBe('password');
    expect(part.find(field => field.name === 'queryJson').inputType).toBe('password');
  });

  it('persists only in the supplied per-tab session store and clears on disconnect', () => {
    const firstTab = memoryStorage();
    const secondTab = memoryStorage();
    const key = connection.connectionStorageKey('lookup', '/templates/vehicle-lookup/vehicle-accessories.html');

    connection.saveConnection(firstTab, key, { baseUrl: liveSettings.baseUrl, headers: liveSettings.headers });

    expect(connection.loadConnection(firstTab, key, 'lookup')).toEqual({ baseUrl: liveSettings.baseUrl, headers: liveSettings.headers });
    expect(connection.loadConnection(secondTab, key, 'lookup')).toBeNull();

    connection.clearConnection(firstTab, key);
    expect(connection.loadConnection(firstTab, key, 'lookup')).toBeNull();
    expect(connectionSource).not.toContain('localStorage');
    expect(connectionSource).not.toContain('history.');
  });

  it('connects and disconnects a single vehicle panel without losing its generated setup', () => {
    const subject = { baseUrl: '', headers: {}, isDev: true, requestHeadersProvider: undefined };
    const baseline = connection.captureConnectionState('lookup', subject);

    connection.connectProfile('lookup', subject, { baseUrl: liveSettings.baseUrl, headers: liveSettings.headers });

    expect(subject.isDev).toBe(false);
    expect(subject.baseUrl).toBe(liveSettings.baseUrl);
    expect(subject.requestHeadersProvider()).toEqual(liveSettings.headers);

    connection.disconnectProfile('lookup', subject, baseline);

    expect(subject).toMatchObject({ baseUrl: '', headers: {}, isDev: true });
    expect(subject.requestHeadersProvider).toBeUndefined();
  });

  it('applies composite settings to the children that actually own each endpoint', () => {
    const options = connection.connectionOptions('composite');
    const subject = {
      baseUrl: '',
      headers: {},
      isDev: true,
      childrenProps: {
        'vehicle-claimable-items': { showTrace: true },
        'vehicle-ssc': { showTrace: true },
        'vehicle-paint-thickness': { showCertificateButton: true },
      },
    };
    const baseline = connection.captureConnectionState('composite', subject, options);

    connection.connectProfile('composite', subject, liveSettings, options);

    expect(subject.isDev).toBe(false);
    expect(subject.childrenProps['vehicle-claimable-items']).toEqual({ showTrace: true, claimEndPoint: liveSettings.claimEndpoint });
    expect(subject.childrenProps['vehicle-ssc']).toEqual({
      showTrace: true,
      recaptchaKey: liveSettings.recaptchaSiteKey,
      unauthorizedSscLookupBaseUrl: liveSettings.unauthorizedSscEndpoint,
    });
    expect(subject.childrenProps['vehicle-paint-thickness']).toEqual({ showCertificateButton: true });
    expect(subject.childrenProps['dynamic-claim']).toBeUndefined();

    connection.disconnectProfile('composite', subject, baseline, options);
    expect(subject.isDev).toBe(true);
    expect(subject.childrenProps).toEqual(baseline.childrenProps);
  });

  it('applies part, form, and VIN-extractor profiles and restores their baselines', () => {
    const part = { endpoint: undefined, isDev: true };
    const partBaseline = connection.captureConnectionState('part', part);
    connection.connectProfile('part', part, { endpointUrl: 'https://api.example.invalid/parts', query: { code: '' } });
    expect(part).toEqual({ endpoint: { url: 'https://api.example.invalid/parts', query: { code: '' } }, isDev: false });
    connection.disconnectProfile('part', part, partBaseline);
    expect(part).toEqual({ endpoint: undefined, isDev: true });

    const form = { structureUrl: '/templates/forms/generated.json', fields: { bookingDate: { disabledWeekdays: [5, 6] } }, isDev: true };
    const formBaseline = connection.captureConnectionState('form', form);
    connection.connectProfile('form', form, { structureUrl: 'https://api.example.invalid/form-structure' });
    expect(form).toMatchObject({ structureUrl: 'https://api.example.invalid/form-structure', isDev: false });
    connection.disconnectProfile('form', form, formBaseline);
    expect(form).toEqual({ ...formBaseline, isDev: true });

    const targets = [{ ocrEndpoint: '' }, { ocrEndpoint: '' }];
    const vinBaseline = connection.captureConnectionState('vin-extractor', targets[0], { targets });
    connection.connectProfile('vin-extractor', targets[0], { ocrEndpoint: 'https://api.example.invalid/ocr' }, { targets });
    expect(targets.map(target => target.ocrEndpoint)).toEqual(['https://api.example.invalid/ocr', 'https://api.example.invalid/ocr']);
    connection.disconnectProfile('vin-extractor', targets[0], vinBaseline, { targets });
    expect(targets.map(target => target.ocrEndpoint)).toEqual(['', '']);
  });

  it('allows only Live Today while connected and restores generated choices after disconnect', () => {
    expect(connection.todayChoiceAvailable('anchor', true, true)).toBe(false);
    expect(connection.todayChoiceAvailable('custom', true, true)).toBe(false);
    expect(connection.todayChoiceAvailable('live', true, true)).toBe(true);
    expect(connection.todayChoiceAvailable('anchor', false, true)).toBe(true);
    expect(connection.todayChoiceAvailable('anchor', false, false)).toBe(false);
  });

  it('never includes connection values in connect or disconnect log details', () => {
    const rendered = ['connect', 'disconnect'].map(action => connection.connectionLog(action as 'connect' | 'disconnect', 'composite')).join(' ');

    for (const value of Object.values(liveSettings).flatMap(setting => (typeof setting === 'object' ? Object.values(setting) : setting))) {
      expect(rendered).not.toContain(value);
    }
    expect(rendered).toContain('credentials withheld');
  });

  it('removes the mode switch and client wiring from ordinary templates while preserving the host placeholders', () => {
    const templates = path.resolve(process.cwd(), 'src', 'templates');
    const ordinaryPages = ['vehicle-lookup', 'part-lookup'].flatMap(area =>
      readdirSync(path.join(templates, area))
        .filter(file => file.endsWith('.html'))
        .map(file => readFileSync(path.join(templates, area, file), 'utf8')),
    );
    ordinaryPages.push(readFileSync(path.join(templates, 'vin-extractor.html'), 'utf8'));

    const ordinary = ordinaryPages.join('\n');
    const harness = readFileSync(path.join(templates, 'harness.js'), 'utf8');
    const structure = readFileSync(path.join(templates, 'prototypes', 'structures', 'tiq-test-drive-slots.json'), 'utf8');
    const productionHost = readFileSync(path.join(templates, 'production-host', 'vehicle-service-history.html'), 'utf8');

    expect(harness).not.toMatch(/setMode|show\.mode|>Mode</);
    expect(harness).toContain("key === '' ? clear?.(this.subject, this) : select?.(this.subject, key, this)");
    expect(ordinary).not.toMatch(/azurewebsites\.net|6Le[A-Za-z0-9_-]{10,}/);
    expect(ordinary).not.toMatch(/(?:base-url|ocr-endpoint|claim-end-point)="https?:\/\//);
    expect(structure).not.toMatch(/azurewebsites\.net|6Le[A-Za-z0-9_-]{10,}/);
    expect(productionHost).toContain('{{VEHICLE_LOOKUP_BASE_URL}}');
    expect(productionHost).toContain('{{ADP_WEB_COMPONENTS_VERSION}}');
  });
});
