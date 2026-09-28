import fs from 'fs';
import path from 'path';

const css = fs.readFileSync(path.resolve(__dirname, 'shift-time-slots.css'), 'utf8').replace(/\r\n/g, '\n');
const hostBlock = css.match(/:host \{([\s\S]*?)\n\}/)[1];
const privates = [...hostBlock.matchAll(/(--_[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => ({ name, value: value.replace(/\s+/g, ' ') }));

const ruleBody = (selector: string) => {
  const at = css.indexOf(`${selector} {`);
  return css.slice(at, css.indexOf('}', at));
};

describe('shift-time-slots.css', () => {
  it('resolves component variable, then shared, then appearance, then baseline', () => {
    const chained = privates.filter(p => p.value.startsWith('var(--shift-time-slots-'));

    expect(chained.length).toBeGreaterThan(30);
    for (const { value } of chained) {
      const order = [...value.matchAll(/var\((--[\w-]+)/g)].map(([, name]) =>
        name.startsWith('--shift-time-slots-') ? 0 : name.startsWith('--shift-') ? 1 : name.startsWith('--_ap-') ? 2 : 3,
      );
      expect(order[0]).toBe(0);
      expect([...order].sort()).toEqual(order.filter(level => level < 3).concat(order.filter(level => level === 3)));
    }
  });

  it('every themable private reads a --shift-time-slots-* variable', () => {
    expect(privates.filter(p => !p.value.startsWith('var(--shift-time-slots-') && p.name !== '--_ring').map(p => p.name)).toEqual([]);
  });

  it('md renders as no size: every --_sz-* fallback equals md', () => {
    const sizes = fs.readFileSync(path.resolve(__dirname, '../../style/appearances.css'), 'utf8').replace(/\r\n/g, '\n');
    const md = Object.fromEntries([...sizes.match(/:host\(\[size='md'\]\) \{([^}]*)\}/)[1].matchAll(/(--_sz-[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()]));
    const fallbacks = [...css.matchAll(/var\((--_sz-[\w-]+), ([^)]+)\)/g)];

    expect(fallbacks.length).toBeGreaterThan(5);
    for (const [, name, value] of fallbacks) expect(value.trim()).toBe(md[name]);
  });

  it('vanilla baseline is the prototype chip: 44px target, 84px columns, slate-800 selected', () => {
    expect(hostBlock).toContain('--_chip-height: var(--shift-time-slots-chip-height, var(--shift-control-height, calc(var(--_ap-control-height, 44px) * var(--_sz-control, 1))));');
    expect(hostBlock).toContain('--_min-column: var(--shift-time-slots-min-column, calc(84px * var(--_sz-control, 1)));');
    expect(hostBlock).toContain('--_accent: var(--shift-time-slots-accent, var(--shift-accent, var(--_ap-accent, #1e293b)));');
    expect(ruleBody('.ts-grid')).toContain('repeat(auto-fill, minmax(min(var(--_min-column), 100%), 1fr))');
    expect(ruleBody('.ts-chip')).toContain('min-block-size: var(--_chip-height)');
  });

  it('a coarse pointer never gets a time under 44px, whatever the appearance', () => {
    expect(css).toMatch(/@media \(pointer: coarse\) \{\s*\.ts-chip \{\s*min-block-size: max\(var\(--_chip-height\), 44px\);/);
  });

  it('height moves through grid-template-rows on one clock', () => {
    expect(ruleBody('.ts-frame')).toContain('grid-template-rows: var(--_rows, auto)');
    expect(ruleBody('.ts-frame[data-measured]')).toContain('transition: grid-template-rows var(--_settle) var(--_ease)');
    expect(ruleBody('.ts-layer')).toContain('align-self: start');
  });

  it('never sets --shift-* itself, has no Tailwind, and is instant under reduced motion', () => {
    expect(css).not.toMatch(/(^|[\s;{])--shift-[\w-]+\s*:/);
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toMatch(/@apply|@tailwind/);
    expect(css).toMatch(/prefers-reduced-motion: reduce\)[\s\S]*?animation: none/);
  });
});
