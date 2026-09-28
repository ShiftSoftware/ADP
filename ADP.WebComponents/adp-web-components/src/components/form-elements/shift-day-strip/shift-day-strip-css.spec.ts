import fs from 'fs';
import path from 'path';

const css = fs.readFileSync(path.resolve(__dirname, 'shift-day-strip.css'), 'utf8');
const hostBlock = css.match(/:host \{([\s\S]*?)\n\}/)[1];
const privates = [...hostBlock.matchAll(/(--_[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => ({ name, value: value.replace(/\s+/g, ' ') }));

const ruleBody = (selector: string) => {
  const at = css.indexOf(`${selector} {`);
  return css.slice(at, css.indexOf('}', at));
};

describe('shift-day-strip.css', () => {
  it('resolves component variable, then shared, then appearance, then baseline', () => {
    const chained = privates.filter(p => p.value.startsWith('var(--shift-day-strip-'));

    expect(chained.length).toBeGreaterThan(60);
    for (const { value } of chained) {
      const order = [...value.matchAll(/var\((--[\w-]+)/g)].map(([, name]) =>
        name.startsWith('--shift-day-strip-') ? 0 : name.startsWith('--shift-') ? 1 : name.startsWith('--_ap-') ? 2 : 3,
      );
      expect(order[0]).toBe(0);
      expect([...order].sort()).toEqual(order.filter(level => level < 3).concat(order.filter(level => level === 3)));
    }
  });

  it('every themable private reads a --shift-day-strip-* variable', () => {
    const derived = ['--_day-height', '--_lift-block', '--_lift-inline'];
    expect(privates.filter(p => !p.value.startsWith('var(--shift-day-strip-') && !derived.includes(p.name)).map(p => p.name)).toEqual([]);
  });

  it('md renders as no size: every --_sz-* fallback equals md', () => {
    const sizes = fs.readFileSync(path.resolve(__dirname, '../../style/appearances.css'), 'utf8');
    const md = Object.fromEntries([...sizes.match(/:host\(\[size='md'\]\) \{([^}]*)\}/)[1].matchAll(/(--_sz-[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()]));
    const fallbacks = [...css.matchAll(/var\((--_sz-[\w-]+), ([^)]+)\)/g)];

    expect(fallbacks.length).toBeGreaterThan(10);
    for (const [, name, value] of fallbacks) expect(value.trim()).toBe(md[name]);
  });

  it('never sets --shift-* itself', () => {
    expect(css).not.toMatch(/(^|[\s;{])--shift-[\w-]+\s*:/);
  });

  it('has no Tailwind', () => {
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toMatch(/@apply|@tailwind/);
  });

  it('the card box is fixed: states and marks never touch geometry', () => {
    const layout = /(^|\s)(width|height|inline-size|block-size|margin|padding|border-width|gap|flex|grid)[\w-]*:/;

    expect(ruleBody('.ds-day')).toContain('block-size: var(--_day-height)');
    expect(ruleBody('.ds-day')).toContain('inline-size: var(--_day-width)');
    expect(ruleBody('.ds-foot')).toContain('block-size: calc(var(--_small-size) * var(--_small-line))');
    for (const selector of ['.ds-day[data-today]', '.ds-day[data-selected]', ".ds-day[aria-disabled='true']", ".ds-day[data-kind='closed'] .ds-number"]) {
      expect(ruleBody(selector)).not.toMatch(layout);
    }
    expect(ruleBody('.ds-day[data-today] .ds-number::after')).toContain('position: absolute');
  });

  it('selected wins over the today marking', () => {
    expect(css.indexOf('.ds-day[data-selected] {')).toBeGreaterThan(css.indexOf('.ds-day[data-today] {'));
    expect(ruleBody('.ds-day[data-selected]')).toContain('--_today-shadow: 0 0 transparent');
  });

  it('scrolls only itself, snaps per card, and is instant under reduced motion', () => {
    expect(ruleBody('.ds-viewport')).toContain('scroll-snap-type: x mandatory');
    expect(ruleBody('.ds-day')).toContain('scroll-snap-align: start');
    expect(css).toMatch(/prefers-reduced-motion: reduce\)[\s\S]*?animation: none[\s\S]*?transition: none/);
  });
});
