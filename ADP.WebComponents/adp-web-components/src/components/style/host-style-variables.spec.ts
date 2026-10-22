import fs from 'fs';
import path from 'path';
import postcss, { Declaration, Rule } from 'postcss';
import tailwindcss from 'tailwindcss';

import tailwindConfig from '../../../tailwind.config';

const read = (file: string) => fs.readFileSync(path.resolve(__dirname, file), 'utf8').replace(/\r\n/g, '\n');

const formVariables = [
  { name: '--shift-form-field-line', selector: '.form-input-style', property: 'border-color', fallback: 'rgb(203 213 225/var(--tw-border-opacity,1))' },
  { name: '--shift-form-field-line-width', selector: '.form-input-style', property: 'border-width', fallback: '1px' },
  { name: '--shift-form-field-radius', selector: '.form-input-style', property: 'border-radius', fallback: '.375rem' },
  { name: '--shift-form-field-padding', selector: '.form-input-style', property: 'padding', fallback: '6px 12px' },
  { name: '--shift-form-field-focus-line', selector: '.form-input-style:focus:enabled', property: 'border-color', fallback: 'rgb(71 85 105/var(--tw-border-opacity,1))' },
  { name: '--shift-form-field-focus-shadow', selector: '.form-input-style:focus:enabled', property: '--tw-shadow', fallback: '0 0 0 0.2rem rgba(71,85,105,.25)' },
  { name: '--shift-form-field-error-line', selector: '.form-input-error-style', property: 'border-color', fallback: 'rgb(239 68 68/var(--tw-border-opacity,1))', important: true },
  { name: '--shift-form-field-error-focus-shadow', selector: '.form-input-error-style:focus:enabled', property: '--tw-shadow', fallback: '0 0 0 0.2rem rgba(239,68,68,.25)' },
  { name: '--shift-form-field-disabled-opacity', selector: '.form-input-label-container.disabled .form-input-style', property: 'opacity', fallback: '.5' },
  { name: '--shift-form-container-disabled-opacity', selector: '.form-input-label-container.disabled .form-input-container', property: 'opacity', fallback: '.75' },
  { name: '--shift-form-textarea-height', selector: '.form-input-textarea-style', property: 'height', fallback: '200px' },
  { name: '--shift-form-label-gap', selector: '.form-input-label', property: 'margin-bottom', fallback: '4px' },
  { name: '--shift-form-required-ink', selector: '.form-input-label-required-star', property: 'color', fallback: 'rgb(220 38 38/var(--tw-text-opacity,1))' },
  { name: '--shift-form-error-ink', selector: '.error-message', property: 'color', fallback: 'rgb(239 68 68/var(--tw-text-opacity,1))' },
  { name: '--shift-form-error-font-size', selector: '.error-message', property: 'font-size', fallback: '12px' },
  { name: '--shift-form-select-icon-ink', selector: '.form-input-select-icon', property: 'color', fallback: 'rgb(156 163 175/var(--tw-text-opacity,1))' },
];

const lookupTokens: Record<string, string> = {
  'paper': '#ffffff',
  'paper-soft': '#f7f9fa',
  'ink': '#1a2530',
  'muted': '#5f6c78',
  'date-ink': '#46535e',
  'line': '#d5dce2',
  'line-soft': '#e8ecef',
  'blue': '#3071a9',
  'blue-dark': '#275e8f',
  'blue-tint': '#e8f1f8',
  'green': '#2c8a58',
  'green-dark': '#216b44',
  'green-tint': '#e8f4ed',
  'red': '#b42318',
  'red-dark': '#9b2c2c',
  'red-tint': '#fff1f0',
  'amber': '#b7791f',
  'amber-dark': '#8a5a12',
  'amber-tint': '#fff7e6',
  'violet': '#755aa5',
  'violet-dark': '#5f4887',
  'violet-tint': '#f1ecf8',
  'shadow': '0 18px 45px rgba(27, 45, 61, 0.1), 0 2px 7px rgba(27, 45, 61, 0.06)',
  'font-sans': "'Manrope', 'Noto Kufi Arabic', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
  'font-arabic': "'Noto Kufi Arabic', 'Manrope', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
};

const declarationsOf = (root: postcss.Root, selector: string) => {
  const found: Declaration[] = [];
  root.walkRules((rule: Rule) => {
    if (rule.selector.replace(/\s+/g, ' ').trim() === selector) rule.walkDecls(decl => void found.push(decl));
  });
  return found;
};

const winner = (declarations: Declaration[], property: string) => {
  const own = declarations.filter(decl => decl.prop === property);
  const important = own.filter(decl => decl.important);
  return (important.length ? important : own).at(-1);
};

describe('form field variables (form-inputs.css)', () => {
  const source = read('../form-elements/form-inputs.css');
  const sourceRoot = postcss.parse(source);
  let compiled: postcss.Root;

  beforeAll(async () => {
    const config = { ...tailwindConfig, content: [{ raw: '', extension: 'html' }], corePlugins: { preflight: false } };
    compiled = (await postcss([tailwindcss(config)]).process(source, { from: undefined })).root;
  });

  it('is the stylesheet of the v1 field elements', () => {
    for (const component of ['form-input', 'form-select', 'form-phone-number', 'form-file']) {
      expect(read(`../form-elements/${component}.tsx`)).toContain("styleUrl: 'form-inputs.css'");
    }
  });

  it.each(formVariables)('$name sets $property on $selector with the 0.4.17 value as fallback', ({ name, selector, property, fallback, important }) => {
    const uses = source.match(new RegExp(`var\\(${name}(?=[,)])`, 'g')) ?? [];
    expect(uses).toHaveLength(1);

    const declaration = declarationsOf(sourceRoot, selector).find(decl => decl.value.startsWith(`var(${name},`));
    expect(declaration).toBeDefined();
    expect(declaration.prop).toBe(property);
    expect(declaration.value).toBe(`var(${name}, ${fallback})`);
    expect(!!declaration.important).toBe(!!important);
  });

  it.each(formVariables)('$name is the declaration that wins for $property on $selector once Tailwind has run', ({ name, selector, property }) => {
    const declaration = winner(declarationsOf(compiled, selector), property);
    expect(declaration?.value.startsWith(`var(${name},`)).toBe(true);
  });

  it('keeps the error focus shadow after the plain focus shadow, so an invalid field still shows red', () => {
    const order: string[] = [];
    compiled.walkDecls('--tw-shadow', decl => void order.push(decl.value));
    const plain = order.findIndex(value => value.startsWith('var(--shift-form-field-focus-shadow,'));
    const error = order.findIndex(value => value.startsWith('var(--shift-form-field-error-focus-shadow,'));
    expect(plain).toBeGreaterThanOrEqual(0);
    expect(error).toBeGreaterThan(plain);
  });

  it('falls back to literals only, never to a shared --shift-* token, and adds no motion', () => {
    const declared = [...source.matchAll(/var\((--shift-form-[\w-]+),([^;]*);/g)];
    expect(declared.map(([, name]) => name).sort()).toEqual(formVariables.map(v => v.name).sort());
    for (const [, , rest] of declared) expect(rest).not.toMatch(/--shift-/);
    expect(source).not.toMatch(/(transition|animation)[\w-]*\s*:[^;]*--shift-form-/);
  });
});

describe('vehicle panel tokens (lookup-tokens.css)', () => {
  const source = read('lookup-tokens.css');
  const host = postcss.parse(source).nodes.find((node): node is Rule => node.type === 'rule' && node.selector === ':host');
  const tokens = Object.fromEntries(host.nodes.filter((node): node is Declaration => node.type === 'decl').map(decl => [decl.prop, decl.value]));

  it('is imported by the vehicle panels', () => {
    expect(read('../vehicle-lookup/vehicle-ssc.css')).toContain("@import '../style/lookup-tokens.css';");
    expect(read('../vehicle-lookup/vehicle-warranty-timeline.css')).toContain("@import '../style/lookup-tokens.css';");
  });

  it.each(Object.entries(lookupTokens))('--%s reads --shift-lookup-%s with today’s value as fallback', (name, value) => {
    expect(tokens[`--${name}`]).toBe(`var(--shift-lookup-${name}, ${value})`);
  });

  it('gives the motion tokens no public alias', () => {
    expect(tokens['--settle']).toBe('480ms');
    expect(tokens['--sheen']).toBe('1.3s');
    expect(tokens['--spin']).toBe('0.65s');
  });

  it('aliases every other token and nothing else', () => {
    const aliased = Object.keys(tokens).filter(name => tokens[name].startsWith('var(--shift-lookup-'));
    expect(aliased.sort()).toEqual(Object.keys(lookupTokens).map(name => `--${name}`).sort());
    expect(Object.keys(tokens).sort()).toEqual([...aliased, '--settle', '--sheen', '--spin'].sort());
  });
});

describe('tap highlight (--shift-tap-highlight)', () => {
  const files = [
    '../forms/defaults/style.css',
    '../forms/vehicle-quotation/themes.css',
    '../form-elements/form-inputs.css',
    '../form-elements/form-dialog.css',
    '../form-elements/branch-date-dropdown.css',
    '../form-elements/branch-slot-dropdown.css',
    '../components/shift-select-dropdown.css',
  ];

  it.each(files)('%s reads the variable with no fallback, on the elements under :host that are not hydrated hosts, and never on :host itself', file => {
    const root = postcss.parse(read(file));
    const tapped: string[] = [];
    root.walkRules(rule => {
      if (rule.nodes.some(node => node.type === 'decl' && node.prop === '-webkit-tap-highlight-color')) tapped.push(rule.selector);
    });

    expect(tapped).toEqual([':host *:not(.hydrated)']);
    expect(declarationsOf(root, ':host *:not(.hydrated)').find(decl => decl.prop === '-webkit-tap-highlight-color').value).toBe('var(--shift-tap-highlight)');
    expect(declarationsOf(root, ':host').find(decl => decl.prop === 'all').value).toBe('initial');
  });

  it('nested hosts are told apart by the default hydrated class, which the build does not rename', () => {
    expect(fs.readFileSync(path.resolve(__dirname, '../../../stencil.config.ts'), 'utf8')).not.toContain('hydratedFlag');
  });

  it('vin-extractor, which resets inheritance on its own host, reads the variable before its explicitly transparent capture button', () => {
    const root = postcss.parse(read('../vin-extractor/vin-extractor.css'));
    const tapped: string[] = [];
    root.walkRules(rule => {
      if (rule.nodes.some(node => node.type === 'decl' && node.prop === '-webkit-tap-highlight-color')) tapped.push(rule.selector);
    });

    expect(tapped).toEqual([':host *', '.vin-extractor-capture-button']);
    expect(declarationsOf(root, ':host *').find(decl => decl.prop === '-webkit-tap-highlight-color').value).toBe('var(--shift-tap-highlight)');
    expect(declarationsOf(root, '.vin-extractor-capture-button').find(decl => decl.prop === '-webkit-tap-highlight-color').value).toBe('transparent');
  });
});
