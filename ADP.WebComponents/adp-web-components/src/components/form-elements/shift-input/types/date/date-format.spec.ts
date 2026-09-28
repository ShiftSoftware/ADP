import { compileFormat, formatHint, formatValue, latinDigits, parseText } from './date-format';

const standard = compileFormat('dd/MM/yyyy');
const letters = { d: 'd', M: 'm', y: 'y' };

describe('compileFormat', () => {
  it('reads tokens and separators, and falls back to dd/MM/yyyy', () => {
    expect(compileFormat('yyyy-MM-dd').tokens.map(token => token.field)).toEqual(['y', 'M', 'd']);
    expect(compileFormat('d.M.yyyy').literals).toEqual(['', '.', '.', '']);
    expect(compileFormat('MM/yyyy').pattern).toBe('dd/MM/yyyy');
    expect(compileFormat('dd MMM yyyy').pattern).toBe('dd/MM/yyyy');
    expect(compileFormat(undefined).pattern).toBe('dd/MM/yyyy');
  });
});

describe('formatValue', () => {
  it('writes the value in the format', () => {
    expect(formatValue('2026-09-05', standard)).toBe('05/09/2026');
    expect(formatValue('2026-09-05', compileFormat('d.M.yyyy'))).toBe('5.9.2026');
    expect(formatValue('2026-09-05', compileFormat('yyyy-MM-dd'))).toBe('2026-09-05');
    expect(formatValue('', standard)).toBe('');
    expect(formatValue('nonsense', standard)).toBe('');
  });

  it('hints the format with localized letters', () => {
    expect(formatHint(standard, letters)).toBe('dd/mm/yyyy');
    expect(formatHint(compileFormat('d.M.yyyy'), { d: 'д', M: 'м', y: 'г' })).toBe('дд.мм.гггг');
  });
});

describe('parseText', () => {
  const cases: [string, string][] = [
    ['05/09/2026', '2026-09-05'],
    ['5/9/2026', '2026-09-05'],
    ['05-09-2026', '2026-09-05'],
    ['05.09.2026', '2026-09-05'],
    [' 05 / 09 / 2026 ', '2026-09-05'],
    ['05092026', '2026-09-05'],
    ['2026-09-05', '2026-09-05'],
    ['29/02/2028', '2028-02-29'],
    ['٠٥/٠٩/٢٠٢٦', '2026-09-05'],
  ];

  it.each(cases)('%s is %s', (text, value) => {
    expect(parseText(text, standard)).toEqual({ kind: 'date', value });
  });

  it.each(['31/02/2026', '29/02/2027', '00/09/2026', '05/13/2026', '05/09/26', '05/09', '12', 'abc', '05/Sep/2026', '050926', '5/9/20266'])('%s is invalid', text => {
    expect(parseText(text, standard)).toEqual({ kind: 'invalid' });
  });

  it('empty text is empty', () => {
    expect(parseText('   ', standard)).toEqual({ kind: 'empty' });
  });

  it('follows the order of the format', () => {
    expect(parseText('09/05/2026', compileFormat('MM/dd/yyyy'))).toEqual({ kind: 'date', value: '2026-09-05' });
    expect(parseText('2026/9/5', compileFormat('yyyy/M/d'))).toEqual({ kind: 'date', value: '2026-09-05' });
  });

  it('reads Arabic-Indic and Persian digits as Latin', () => {
    expect(latinDigits('٠١٢٣٤٥٦٧٨٩ ۰۱۲۳۴۵۶۷۸۹')).toBe('0123456789 0123456789');
  });
});
