import { spawnSync } from 'child_process';
import path from 'path';

// Jest's sandboxed process.env cannot change the running zone, so each zone gets its own Node child.
const SOURCE = path.resolve(__dirname, 'calendar-date.ts');
const ROOT = path.resolve(__dirname, '../../..');

const PROBE = `
const fs = require('fs');
const ts = require('typescript');
const code = ts.transpileModule(fs.readFileSync(process.argv[1], 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 } }).outputText;
const lib = {};
new Function('exports', code)(lib);
const months = ['2024-02', '2026-03', '2026-09', '2026-10', '2026-11'];
process.stdout.write(JSON.stringify({
  offset: new Date(2026, 0, 15).getTimezoneOffset(),
  grids: months.map(month => lib.monthGrid(month, 6).map(cell => cell.date + (cell.inMonth ? '' : '*')).join(',')),
  weekdays: ['2026-03-08', '2026-03-29', '2026-11-01', '2026-10-25'].map(lib.weekdayOf),
  steps: [lib.addDays('2026-03-07', 1), lib.addDays('2026-10-31', 1), lib.addMonths('2026-03-31', -1)],
  localToday: lib.localDate(new Date(Date.UTC(2026, 8, 27, 22, 30))),
}));
`;

function probe(zone: string) {
  const result = spawnSync(process.execPath, ['-e', PROBE, SOURCE], { cwd: ROOT, env: { ...process.env, TZ: zone }, encoding: 'utf8' });

  if (result.status !== 0) throw new Error(result.stderr);

  return JSON.parse(result.stdout);
}

describe('real time zones', () => {
  const newYork = probe('America/New_York');
  const baghdad = probe('Asia/Baghdad');
  const utc = probe('UTC');

  it('zones applied', () => {
    expect(newYork.offset).toBe(300);
    expect(baghdad.offset).toBe(-180);
    expect(utc.offset).toBe(0);
  });

  it('same results in every zone', () => {
    expect(newYork.grids).toEqual(utc.grids);
    expect(baghdad.grids).toEqual(utc.grids);
    expect(newYork.weekdays).toEqual([0, 0, 0, 0]);
    expect(baghdad.weekdays).toEqual(newYork.weekdays);
    expect(newYork.steps).toEqual(['2026-03-08', '2026-11-01', '2026-02-28']);
    expect(baghdad.steps).toEqual(newYork.steps);
  });

  it('local today per zone', () => {
    expect(newYork.localToday).toBe('2026-09-27');
    expect(baghdad.localToday).toBe('2026-09-28');
  });
});
