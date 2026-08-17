import { Config } from '@stencil/core';
import alias from '@rollup/plugin-alias';
// @ts-ignore
import path from 'path';
import { sass } from '@stencil/sass';
import tailwind, { tailwindHMR, setPluginConfigurationDefaults } from 'stencil-tailwind-plugin';
import tailwindcss from 'tailwindcss';
import tailwindConf from './tailwind.config';
import autoprefixer from 'autoprefixer';

setPluginConfigurationDefaults({
  tailwindConf,
  postcss: {
    plugins: [tailwindcss(), autoprefixer()],
  },
});

export const config: Config = {
  devMode: false,
  minifyJs: true,
  minifyCss: true,
  sourceMap: false,
  globalScript: 'src/global/lib/middleware.ts',
  namespace: 'shift-components',
  plugins: [
    sass(),
    tailwind(),
    tailwindHMR(),
    alias({
      entries: [
        { find: '~api', replacement: path.resolve('src/global/api') },
        { find: '~lib', replacement: path.resolve('src/global/lib') },
        { find: '~locales', replacement: path.resolve('src/locales') },
        { find: '~features', replacement: path.resolve('src/features') },
        { find: '~types', replacement: path.resolve('src/global/types') },
        { find: '~assets', replacement: path.resolve('src/global/assets') },
      ],
    }),
  ],
  preamble: 'Built by ShiftSoftware\nCopyright (c)',
  outputTargets: [
    {
      type: 'dist',
      polyfills: true,
      esmLoaderPath: '../loader',
    },
    {
      minify: true,
      externalRuntime: false,
      type: 'dist-custom-elements',
      copy: [
        { src: 'locales', dest: 'dist/locales' },
        { src: 'features/mocks/data', dest: 'dist/mocks' },
        { src: 'integration/integration-manifest.json', dest: 'dist/integration-manifest.json' },
        { src: 'integration/host-loader.js', dest: 'dist/host-loader.js' },
        { src: 'templates/production-host', dest: 'dist/templates/production-host' },
      ],
      customElementsExportBehavior: 'auto-define-custom-elements',
    },
    {
      type: 'docs-json',
      file: 'dist/stencil-docs.json',
    },
    {
      type: 'www',
      serviceWorker: null,
      copy: [{ src: 'index.html' }, { src: 'templates' }, { src: 'locales', dest: 'locales' }, { src: 'features/mocks/data', dest: 'mocks' }],
    },
  ],
  devServer: {
    port: 3000,
    reloadStrategy: 'pageReload',
  },
  testing: {
    // Spec tests render components that import status icons as modules. Jest has no loader for a
    // binary asset, so it resolves them to a stub — a spec asserts which icon slot is filled, never
    // what the file contains.
    moduleNameMapper: {
      '\.(svg|png|jpg|jpeg|gif|webp)$': '<rootDir>/src/tests/asset-stub.js',
    },
  },
};
