export type AdpLanguagePackNamespace =
  | 'global'
  | 'errors'
  | 'forms'
  | 'general'
  | 'input'
  | 'calendar'
  | 'booking'
  | 'popover'
  | 'vehicleLookup.ssc'
  | 'vehicleLookup.specification'
  | 'vehicleLookup.warrantyTimeline'
  | 'vehicleLookup.serviceHistory'
  | 'vehicleLookup.saleInformation'
  | 'vehicleLookup.accessories'
  | 'vehicleLookup.paintThickness'
  | 'vehicleLookup.claimableItems';

export interface AdpLanguageMeta {
  direction?: 'ltr' | 'rtl';
  weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
  numerals?: 'latn' | 'arab';
}

export interface AdpLanguagePacks {
  version?: 1;
  languages?: Record<string, AdpLanguageMeta>;
  strings?: Partial<Record<AdpLanguagePackNamespace, Record<string, Record<string, unknown>>>>;
}

export type AdpLanguagePacksInput = AdpLanguagePacks | AdpLanguagePacks[] | string;

export type AdpRequestKind = 'submit' | 'options' | 'calendar' | 'upload' | 'lookup';

export interface AdpRequestInfo {
  kind: AdpRequestKind;
  tag: string;
  element: HTMLElement;
  field?: string;
}

export type AdpFetch = (url: string, init: RequestInit, info: AdpRequestInfo) => Promise<Response>;

export interface AdpHostOptions {
  languagePacks?: AdpLanguagePacksInput | null;
  fetch?: AdpFetch | null;
  assetsBaseUrl?: string | URL | null;
}

export interface LoadComponentOptions {
  moduleUrl: string;
  tag: string;
  packageVersion: string;
  outputFamily?: string;
  timeoutMs?: number;
}

export interface AdpIntegrationManifestComponent {
  tag: string;
  modulePath: string;
  [key: string]: unknown;
}

export interface AdpIntegrationManifest {
  schemaVersion: number;
  package: string;
  packageVersion: string;
  outputFamily: string;
  components: AdpIntegrationManifestComponent[];
  [key: string]: unknown;
}

export function loadComponent(options: LoadComponentOptions): Promise<void>;

export function loadIntegrationManifest(manifestUrl: string | URL): Promise<AdpIntegrationManifest>;

export function configureAdpHost(options?: AdpHostOptions): void;
