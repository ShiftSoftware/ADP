import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import globals from 'globals';

export default [
  {
    ignores: [
      'node_modules/**',
      'dist/**',
      'loader/**',
      'www/**',
      'build/**',
      '.stencil/**',
      '*.config.*',
      'src/components.d.ts',
      'src/locale-mapper.ts',
      'src/global/types/generated/**',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{js,mjs,ts,tsx}'],
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
        ...globals.jest,
      },
    },
    rules: {
      '@typescript-eslint/no-unused-vars': 'warn',
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-require-imports': 'off',
      '@typescript-eslint/no-empty-object-type': 'warn',
      '@typescript-eslint/ban-ts-comment': 'warn',
      '@typescript-eslint/no-unused-expressions': 'warn',
      'no-undef': 'off',
      'no-empty': 'warn',
      'no-empty-pattern': 'warn',
      'no-extra-boolean-cast': 'warn',
      'no-constant-binary-expression': 'warn',
    },
  },
  {
    // Stencil compiles TSX with the `h` pragma; decorators are the framework contract.
    files: ['**/*.tsx'],
    rules: {
      '@typescript-eslint/no-unused-vars': [
        'warn',
        { varsIgnorePattern: '^(h|Host|Fragment)$' },
      ],
    },
  },
  {
    files: ['automation/**/*.mjs'],
    languageOptions: {
      globals: { ...globals.node },
    },
  },
];
