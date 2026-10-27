import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';

type Environment = { name: string; label: string; anchor: string; files: Record<string, string[]> };
type EnvironmentModule = {
  chooseEnvironment: (environments: Environment[], requested: string | null, stored: string | null) => Environment | null;
  environmentFileUrl: (siteRoot: URL, environment: string, file: string) => URL;
  environmentsFrom: (index: unknown) => Environment[];
};

// Jest runs CommonJS while the showcase module is deliberately browser-native.
// Evaluate that exact source with its export keywords removed instead of keeping
// a second test-only implementation of the selection rules.
const moduleSource = readFileSync(path.resolve(process.cwd(), 'src', 'templates', 'harness-environment.js'), 'utf8').replaceAll('export ', '');
const { chooseEnvironment, environmentFileUrl, environmentsFrom } = new Function(
  `${moduleSource}\nreturn { chooseEnvironment, environmentFileUrl, environmentsFrom };`,
)() as EnvironmentModule;

describe('generated demo environments', () => {
  const index = {
    environments: [
      { name: 'alpha-market', anchor: '2026-01-01', files: { 'vehicle-lookup': ['VIN-A'] } },
      { name: 'beta-market', anchor: '2026-02-02', files: { 'vehicle-lookup': ['VIN-B'] } },
    ],
  };

  it('takes its choices only from valid generated index entries', () => {
    const environments = environmentsFrom({
      environments: [...index.environments, { name: '../private', files: { 'vehicle-lookup': ['NO'] } }, { name: 'broken-market', files: null }],
    });

    expect(environments.map(environment => environment.name)).toEqual(['alpha-market', 'beta-market']);
    expect(environments.map(environment => environment.label)).toEqual(['Alpha Market', 'Beta Market']);
  });

  it('gives the query string precedence over per-tab memory and has an index-derived fallback', () => {
    const environments = environmentsFrom(index);

    expect(chooseEnvironment(environments, 'beta-market', 'alpha-market').name).toBe('beta-market');
    expect(chooseEnvironment(environments, 'missing', 'beta-market').name).toBe('beta-market');
    expect(chooseEnvironment(environments, null, 'missing').name).toBe('alpha-market');
  });

  it('resolves fixture paths inside a site sub-mount', () => {
    expect(environmentFileUrl(new URL('http://localhost:3335/public-demo/'), 'beta-market', 'vehicle-lookup').href).toBe(
      'http://localhost:3335/public-demo/mocks/generated/beta-market/vehicle-lookup.json',
    );
  });

  it('keeps every vehicle and part demo on the generated family', () => {
    const templates = path.resolve(process.cwd(), 'src', 'templates');
    const pages = ['vehicle-lookup', 'part-lookup'].flatMap(area =>
      readdirSync(path.join(templates, area))
        .filter(file => file.endsWith('.html'))
        .map(file => readFileSync(path.join(templates, area, file), 'utf8')),
    );

    expect(pages).toHaveLength(13);
    for (const page of pages) {
      expect(page).toMatch(/mocks:\s*'(vehicle|part)-lookup'/);
      expect(page).not.toMatch(/(?:mock-data|warranty-mock-data|mock-loader)\.js/);
      expect(page).not.toContain('samples:');
    }
  });
});
