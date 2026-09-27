import fs from 'fs';
import path from 'path';

const css = fs.readFileSync(path.resolve(__dirname, 'shift-calendar.css'), 'utf8');
const hostBlock = css.match(/:host \{([\s\S]*?)\n\}/)[1];
const privates = [...hostBlock.matchAll(/(--_[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => ({ name, value: value.replace(/\s+/g, ' ') }));

const ruleBody = (selector: string) => {
  const at = css.indexOf(`${selector} {`);
  return css.slice(at, css.indexOf('}', at));
};

describe('shift-calendar.css', () => {
  it('resolves component variable, then shared, then appearance, then baseline', () => {
    const chained = privates.filter(p => p.value.startsWith('var(--shift-calendar-'));

    expect(chained.length).toBeGreaterThan(80);
    for (const { value } of chained) {
      const order = [...value.matchAll(/var\((--[\w-]+)/g)].map(([, name]) =>
        name.startsWith('--shift-calendar-') ? 0 : name.startsWith('--shift-') ? 1 : name.startsWith('--_ap-') ? 2 : 3,
      );
      expect(order[0]).toBe(0);
      expect([...order].sort()).toEqual(order.filter(level => level < 3).concat(order.filter(level => level === 3)));
    }
  });

  it('every themable private reads a --shift-calendar-* variable', () => {
    const derived = ['--_ring', '--_weekday-row'];
    expect(privates.filter(p => !p.value.startsWith('var(--shift-calendar-') && !derived.includes(p.name)).map(p => p.name)).toEqual([]);
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

  it('today marking and colour scheme leave geometry alone', () => {
    const layout = /(^|\s)(width|height|inline-size|block-size|margin|padding|border-width|gap|flex|grid)[\w-]*:/;

    expect(ruleBody('.cal-day[data-today],\n.cal-pick[data-today]')).not.toMatch(layout);
    expect(ruleBody('.cal-day[data-today] .cal-day-number,\n.cal-pick[data-today] .cal-pick-text')).not.toMatch(layout);
    expect(ruleBody('.cal-day[data-today]::after,\n.cal-pick[data-today]::after')).toContain('position: absolute');
  });

  it('selected wins over the today marking', () => {
    const selected = ruleBody('.cal-day[data-selected],\n.cal-pick[data-selected]');

    expect(css.indexOf('.cal-day[data-selected],\n.cal-pick[data-selected] {')).toBeGreaterThan(css.indexOf('.cal-day[data-today],\n.cal-pick[data-today] {'));
    expect(selected).toContain('--_today-shadow: 0 0 transparent');
    expect(ruleBody('.cal-day[data-selected]::after,\n.cal-pick[data-selected]::after')).toContain('background: transparent');
  });

  it('the box is fixed: six rows, a fixed band, a fixed body', () => {
    expect(ruleBody('.cal-header')).toContain('block-size: var(--_nav-size)');
    expect(ruleBody('.cal-body')).toContain('block-size: calc(var(--_weekday-row) + var(--_section-gap) + var(--_cell) * 6 + var(--_gap) * 5)');
    expect(ruleBody('.cal-root')).toContain('max-inline-size: var(--_max-width)');
  });

  it('carousel and roll mirror through --_dir, instant under reduced motion', () => {
    expect(css).toMatch(/@keyframes cal-page-enter[\s\S]*?var\(--_travel, 1\) \* var\(--_dir\)/);
    expect(css).toMatch(/\.cal-root\[dir='rtl'\] \{\s*--_dir: -1;/);
    expect(css).toMatch(/prefers-reduced-motion: reduce\)[\s\S]*?animation: none/);
  });
});
