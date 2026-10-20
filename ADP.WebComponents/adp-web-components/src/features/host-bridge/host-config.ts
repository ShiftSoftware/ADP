export const HOST_CONFIG_KEY = Symbol.for('shiftsoftware.adp-web-components.host-config');

export type HostConfig = Record<string, unknown>;

type HostScope = typeof globalThis & { [HOST_CONFIG_KEY]?: HostConfig };

const isConfig = (value: unknown): value is HostConfig => typeof value === 'object' && value !== null && !Array.isArray(value);

export function peekHostConfig(): HostConfig | undefined {
  const config = (globalThis as HostScope)[HOST_CONFIG_KEY];

  return isConfig(config) ? config : undefined;
}

export function hostConfig(): HostConfig {
  const existing = peekHostConfig();
  if (existing) return existing;

  const config: HostConfig = {};
  (globalThis as HostScope)[HOST_CONFIG_KEY] = config;

  return config;
}

export const hostFlag = (name: string): boolean => peekHostConfig()?.[name] === true;
