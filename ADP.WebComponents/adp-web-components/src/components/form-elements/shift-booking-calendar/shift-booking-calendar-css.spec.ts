import fs from 'fs';
import path from 'path';

const css = fs.readFileSync(path.resolve(__dirname, 'shift-booking-calendar.css'), 'utf8').replace(/\r\n/g, '\n');
const hostBlock = css.match(/:host \{([\s\S]*?)\n\}/)[1];
const privates = [...hostBlock.matchAll(/(--_[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => ({ name, value: value.replace(/\s+/g, ' ') }));

const ruleBody = (selector: string) => {
  const at = css.indexOf(`${selector} {`);
  expect(at).toBeGreaterThan(-1);
  return css.slice(at, css.indexOf('}', at));
};

const motionFree = css.slice(css.indexOf('@media (prefers-reduced-motion: reduce)'));

describe('shift-booking-calendar.css', () => {
  it('resolves component variable, then shared, then appearance, then baseline', () => {
    const chained = privates.filter(p => p.value.startsWith('var(--shift-booking-calendar-'));

    expect(chained.length).toBe(privates.length);
    for (const { value } of chained) {
      const order = [...value.matchAll(/var\((--[\w-]+)/g)].map(([, name]) =>
        name.startsWith('--shift-booking-calendar-') ? 0 : name.startsWith('--shift-') ? 1 : name.startsWith('--_ap-') ? 2 : 3,
      );
      expect(order[0]).toBe(0);
      expect([...order].sort()).toEqual(order.filter(level => level < 3).concat(order.filter(level => level === 3)));
    }
  });

  it('a coarse pointer gets a 44px retry button, and the status row grows with it', () => {
    expect(css).toMatch(/@media \(pointer: coarse\) \{\s*\.bc-status,\s*\.bc-retry \{\s*min-block-size: max\(var\(--_status-height\), 44px\);/);
  });

  it('md renders as no size', () => {
    const sizes = fs.readFileSync(path.resolve(__dirname, '../../style/appearances.css'), 'utf8').replace(/\r\n/g, '\n');
    const md = Object.fromEntries([...sizes.match(/:host\(\[size='md'\]\) \{([^}]*)\}/)[1].matchAll(/(--_sz-[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()]));

    for (const [, name, value] of css.matchAll(/var\((--_sz-[\w-]+), ([^)]+)\)/g)) expect(value.trim()).toBe(md[name]);
  });

  it('the times view lies on the calendar: out of flow, on the same box, clipped at its padding', () => {
    const times = ruleBody('.bc-times');

    expect(ruleBody('.bc-box')).toContain('position: relative');
    expect(times).toContain('position: absolute');
    expect(times).toContain('inset: 0');
    expect(times).toContain('overflow: clip');
    expect(ruleBody('.bc-box shift-calendar::part(root)')).toContain('overflow: clip');
    expect(ruleBody('.bc-times-scroll')).toContain('overflow-y: auto');
  });

  it('nothing in the field changes height: no collapsible, one fixed status row', () => {
    expect(css).not.toMatch(/grid-template-rows/);
    expect(css).not.toMatch(/\.bc-collapsible|\.bc-slots/);
    expect(ruleBody('.bc-message')).toContain('grid-area: 1 / 1');
    expect(ruleBody('.bc-status')).toContain('min-block-size: var(--_status-height)');
  });

  it('the header rolls vertically: the month name up and out, the day up from below', () => {
    const leaving = ruleBody(".bc-box[data-view='times'] shift-calendar::part(heading),\n.bc-box[data-view='times'] shift-calendar::part(nav)");

    expect(leaving).toContain('transform: translateY(-100%)');
    expect(leaving).toContain('opacity: 0.9');
    expect(ruleBody('.bc-title')).toContain('transform: translateY(100%)');
    expect(ruleBody('.bc-back')).toContain('transform: translateY(100%)');
    expect(ruleBody(".bc-box[data-view='times'] .bc-title,\n.bc-box[data-view='times'] .bc-back")).toContain('transform: none');
  });

  it('the content slides horizontally: days toward the start, times from the end, mirrored in RTL', () => {
    expect(ruleBody('.bc-root')).toContain('--_bc-dir: 1');
    expect(ruleBody(".bc-root[dir='rtl']")).toContain('--_bc-dir: -1');
    expect(ruleBody(".bc-box[data-view='times'] shift-calendar::part(grid)")).toContain('transform: translateX(calc((100% + var(--_f-travel, 12px)) * var(--_bc-dir) * -1))');
    expect(ruleBody('.bc-times-body')).toContain('transform: translateX(calc((100% + var(--_f-travel, 12px)) * var(--_bc-dir)))');
    expect(ruleBody(".bc-box[data-view='times'] .bc-times-body")).toContain('transform: none');
  });

  it('one clock: every moving piece uses the same duration and easing', () => {
    const moving = [
      ruleBody('.bc-box shift-calendar::part(heading),\n.bc-box shift-calendar::part(nav)'),
      ruleBody('.bc-box shift-calendar::part(grid)'),
      ruleBody('.bc-title'),
      ruleBody('.bc-back'),
      ruleBody('.bc-times-body'),
    ];

    expect(ruleBody('.bc-root')).toContain('--_bc-settle: var(--_settle)');
    expect(ruleBody('.bc-root')).toContain('--_bc-ease: var(--_ease)');
    for (const rule of moving) expect(rule).toMatch(/transform var\(--_(bc-)?settle\) var\(--_(bc-)?ease\)/);
  });

  it('never sets --shift-* itself, has no Tailwind, and is instant under reduced motion', () => {
    expect(css).not.toMatch(/(^|[\s;{])--shift-[\w-]+\s*:/);
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toMatch(/@apply|@tailwind/);
    for (const selector of ['shift-calendar::part(heading)', 'shift-calendar::part(grid)', '.bc-title', '.bc-back', '.bc-times-body']) expect(motionFree).toContain(selector);
    expect(motionFree).toMatch(/transition: none/);
    expect(motionFree).toMatch(/\.bc-slot-layer\[data-phase\][\s\S]*?animation: none/);
  });
});
