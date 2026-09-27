import fs from 'fs';
import path from 'path';

const css = fs.readFileSync(path.resolve(__dirname, 'appearances.css'), 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');

const APPEARANCES = ['vanilla', 'material', 'soft', 'sharp', 'minimal'];
const COLOUR_TOKENS = [
  'surface',
  'surface-muted',
  'panel-surface',
  'ink',
  'ink-muted',
  'ink-disabled',
  'line',
  'line-strong',
  'panel-line',
  'cell-line',
  'accent',
  'on-accent',
  'accent-tint',
  'focus',
  'focus-halo',
  'focus-inner',
  'danger',
  'shadow-panel',
  'shadow-control',
  'hover-bg',
  'hover-line',
  'hover-ink',
  'today-ring',
  'today-dot',
];
const PAGE = { light: '#ffffff', dark: '#0f172a' };

function blocks(): { selector: string; tokens: Record<string, string> }[] {
  return [...css.matchAll(/([^{}]*?)\{([^{}]*)\}/g)].map(([, selector, body]) => ({
    selector: selector.replace(/\s+/g, ' ').trim(),
    tokens: Object.fromEntries([...body.matchAll(/--_ap-([\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()])),
  }));
}

function tokensFor(appearance: string, scheme: 'light' | 'dark' | 'auto'): Record<string, string> {
  const all = blocks();
  const light = all.find(b => b.selector === `:host([appearance='${appearance}'])`)?.tokens ?? {};
  if (scheme === 'light') return light;

  const dark = all.find(b => b.selector.includes(`:host([appearance='${appearance}'][color-scheme='${scheme}'])`))?.tokens ?? {};
  return { ...light, ...dark };
}

type Rgba = [number, number, number, number];

function parse(colour: string): Rgba {
  if (colour === 'transparent') return [0, 0, 0, 0];
  if (colour.startsWith('#')) {
    const hex = colour.slice(1);
    return [0, 2, 4].map(i => parseInt(hex.slice(i, i + 2), 16)).concat(1) as Rgba;
  }
  const [r, g, b, a = 1] = colour.match(/[\d.]+/g).map(Number);
  return [r, g, b, a];
}

const over = (top: Rgba, bottom: Rgba): Rgba => [0, 1, 2].map(i => top[i] * top[3] + bottom[i] * (1 - top[3])).concat(1) as Rgba;

function luminance([r, g, b]: Rgba): number {
  const channel = (c: number) => ((c /= 255) <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

export function contrast(foreground: string, background: string, page: string): number {
  const base = over(parse(background), parse(page));
  const [a, b] = [luminance(over(parse(foreground), base)), luminance(base)].sort((x, y) => y - x);
  return (a + 0.05) / (b + 0.05);
}

describe('appearances.css', () => {
  it('defines every appearance in light, dark and auto', () => {
    for (const appearance of APPEARANCES) {
      for (const scheme of ['light', 'dark', 'auto'] as const) {
        const tokens = tokensFor(appearance, scheme);
        expect(COLOUR_TOKENS.filter(token => !(token in tokens))).toEqual([]);
      }
    }
  });

  it('dark and auto change colour and shadow only', () => {
    for (const block of blocks().filter(b => /color-scheme='(dark|auto)'/.test(b.selector))) {
      expect(Object.keys(block.tokens).filter(token => !COLOUR_TOKENS.includes(token) && !token.startsWith('tone-'))).toEqual([]);
    }
  });

  it('auto repeats dark', () => {
    for (const appearance of APPEARANCES) expect(tokensFor(appearance, 'auto')).toEqual(tokensFor(appearance, 'dark'));
  });

  it('shares geometry within a density', () => {
    const geometry = (appearance: string) => {
      const t = tokensFor(appearance, 'light');
      return [t['cell-size'], t.gap, t['panel-border-width']];
    };
    expect(geometry('minimal')).toEqual(geometry('vanilla'));
  });

  it('every appearance but minimal has an edge in both schemes', () => {
    for (const appearance of APPEARANCES.filter(name => name !== 'minimal')) {
      for (const scheme of ['light', 'dark'] as const) {
        const t = tokensFor(appearance, scheme);
        const border = parseFloat(t['panel-border-width']) > 0 && t['panel-line'] !== 'transparent';
        expect(border || t['shadow-control'] !== 'none').toBe(true);
      }
    }
  });

  it('never sets --shift-*', () => {
    expect(css).not.toMatch(/(^|[\s;{])--shift-[\w-]+\s*:/);
  });

  it('with no appearance, dark is vanilla dark', () => {
    expect(blocks().some(b => b.selector.includes(":host(:not([appearance])[color-scheme='dark'])") && b.selector.includes("[appearance='vanilla'][color-scheme='dark']"))).toBe(
      true,
    );
  });

  describe('contrast (AA)', () => {
    for (const appearance of APPEARANCES) {
      for (const scheme of ['light', 'dark'] as const) {
        it(`${appearance} ${scheme}`, () => {
          const t = tokensFor(appearance, scheme);
          const page = PAGE[scheme];
          const surface = t['panel-surface'];

          expect(contrast(t.ink, surface, page)).toBeGreaterThanOrEqual(4.5);
          expect(contrast(t['ink-muted'], surface, page)).toBeGreaterThanOrEqual(4.5);
          expect(contrast(t['on-accent'], t.accent, page)).toBeGreaterThanOrEqual(4.5);
          expect(contrast(t.accent, surface, page)).toBeGreaterThanOrEqual(3);
          expect(contrast(t.focus, surface, page)).toBeGreaterThanOrEqual(3);

          const marks = [t['today-ring'], t['today-dot']].filter(mark => mark !== 'transparent');
          for (const mark of marks) expect(contrast(mark, surface, page)).toBeGreaterThanOrEqual(3);
        });
      }
    }
  });
});
