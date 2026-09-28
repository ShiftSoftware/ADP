import fs from 'fs';
import path from 'path';

// A Windows checkout has CRLF; the rules below are matched line by line.
const read = (file: string) => fs.readFileSync(path.resolve(__dirname, file), 'utf8').replace(/\r\n/g, '\n');
const sizes = read('../../style/appearances.css');
const md = Object.fromEntries([...sizes.match(/:host\(\[size='md'\]\) \{([^}]*)\}/)[1].matchAll(/(--_sz-[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()]));

const sheets = [
  { file: 'shift-input.css', prefix: '--shift-input-', derived: ['--_inset', '--_underline-focus'] },
  { file: '../../components/shift-popover/shift-popover.css', prefix: '--shift-popover-', derived: [] },
];

const rule = (css: string, selector: string) => {
  const at = css.indexOf(`${selector} {`);
  return css.slice(at, css.indexOf('}', at));
};

describe.each(sheets)('$file', ({ file, prefix, derived }) => {
  const css = read(file);
  const hostBlock = css.match(/:host \{([\s\S]*?)\n\}/)[1];
  const privates = [...hostBlock.matchAll(/(--_[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => ({ name, value: value.replace(/\s+/g, ' ') }));

  it('resolves component variable, then shared, then appearance, then baseline', () => {
    const chained = privates.filter(p => p.value.startsWith(`var(${prefix}`));

    expect(chained.length).toBeGreaterThan(10);
    for (const { value } of chained) {
      const order = [...value.matchAll(/var\((--[\w-]+)/g)].map(([, name]) => (name.startsWith(prefix) ? 0 : name.startsWith('--shift-') ? 1 : name.startsWith('--_ap-') ? 2 : 3));
      expect(order[0]).toBe(0);
      expect([...order].sort()).toEqual(order.filter(level => level < 3).concat(order.filter(level => level === 3)));
    }
  });

  it('every themable private reads its own component variable', () => {
    expect(privates.filter(p => !p.value.startsWith(`var(${prefix}`) && !derived.includes(p.name)).map(p => p.name)).toEqual([]);
  });

  it('md renders as no size: every --_sz-* fallback equals md', () => {
    for (const [, name, value] of css.matchAll(/var\((--_sz-[\w-]+), ([^)]+)\)/g)) expect(value.trim()).toBe(md[name]);
  });

  it('never sets --shift-* on its host, and has no Tailwind', () => {
    // ::slotted declarations lose to the page's own, so the host still wins there.
    const outsideSlotted = css.replace(/::slotted\([^)]*\) \{[^}]*\}/g, '');

    expect(outsideSlotted).not.toMatch(/(^|[\s;{])--shift-[\w-]+\s*:/);
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toMatch(/@apply|@tailwind/);
  });

  it('instant under reduced motion', () => {
    expect(css).toMatch(/prefers-reduced-motion: reduce\)[\s\S]*?transition: none/);
  });
});

describe('layout never shifts', () => {
  const input = read('shift-input.css');
  const popover = read('../../components/shift-popover/shift-popover.css');

  it('the support line is reserved; hint and error only fade in it', () => {
    expect(rule(input, '.in-support')).toContain('min-block-size');
    expect(rule(input, '.in-hint[data-shown],\n.in-error[data-shown]')).not.toMatch(/block-size|height|margin|padding|display/);
  });

  it('the hidden clear button keeps its room', () => {
    expect(rule(input, '.in-clear[data-hidden]')).not.toMatch(/display|(^|\s)inline-size:|width:/);
  });

  it('the control is one fixed height', () => {
    expect(rule(input, '.in-control')).toContain('block-size: var(--_height)');
  });

  it('the panel is fixed, so it never takes room on the page', () => {
    expect(rule(popover, '.pop-panel')).toContain('position: fixed');
    expect(rule(popover, '.pop-panel[data-open]')).not.toMatch(/display|position/);
  });
});
