import { readFileSync } from 'fs';
import { join } from 'path';
import ts from 'typescript';

import type * as Loader from './host-loader';
import type { AdpFetch, AdpRequestInfo, AdpRequestKind } from '../features/host-bridge/adp-fetch';
import type { LanguageMeta, LanguagePackNamespace, LanguagePacks, LanguagePacksInput } from '../features/multi-lingual/language-packs';

type Same<A, B> = [A] extends [B] ? ([B] extends [A] ? true : false) : false;

const sameTypes: [
  Same<Loader.AdpFetch, AdpFetch>,
  Same<Loader.AdpRequestInfo, AdpRequestInfo>,
  Same<Loader.AdpRequestKind, AdpRequestKind>,
  Same<Loader.AdpLanguageMeta, LanguageMeta>,
  Same<Loader.AdpLanguagePackNamespace, LanguagePackNamespace>,
  Same<Loader.AdpLanguagePacks, LanguagePacks>,
  Same<Loader.AdpLanguagePacksInput, LanguagePacksInput>,
] = [true, true, true, true, true, true, true];

const read = (name: string) => readFileSync(join(__dirname, name), 'utf8');

function exportedNames(fileName: string, source: string): string[] {
  const file = ts.createSourceFile(fileName, source, ts.ScriptTarget.ES2021, true);

  return file.statements
    .filter(statement => ts.isFunctionDeclaration(statement) && statement.modifiers?.some(modifier => modifier.kind === ts.SyntaxKind.ExportKeyword))
    .map(statement => (statement as ts.FunctionDeclaration).name.text)
    .sort();
}

describe('host-loader type declarations', () => {
  it('declare every function host-loader.js exports and nothing else', () => {
    expect(sameTypes.every(Boolean)).toBe(true);
    expect(exportedNames('host-loader.d.ts', read('host-loader.d.ts'))).toEqual(exportedNames('host-loader.js', read('host-loader.js')));
    expect(exportedNames('host-loader.js', read('host-loader.js'))).toEqual(['configureAdpHost', 'loadComponent', 'loadIntegrationManifest']);
  });

  it('accept every configureAdpHost option the loader reads', () => {
    const options: Required<Parameters<typeof Loader.configureAdpHost>[0]> = {
      languagePacks: { languages: { uz: { direction: 'ltr' } }, strings: { popover: { uz: { dialog: 'Oyna' } } } },
      fetch: async (url, init) => fetch(url, init),
      assetsBaseUrl: new URL('https://example.invalid/adp/'),
    };

    expect(Object.keys(options).sort()).toEqual(['assetsBaseUrl', 'fetch', 'languagePacks']);
    for (const key of Object.keys(options)) expect(read('host-loader.js')).toContain(`options.${key}`);
  });
});
