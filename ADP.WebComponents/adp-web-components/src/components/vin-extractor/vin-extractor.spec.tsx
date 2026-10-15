import { newSpecPage, SpecPage } from '@stencil/core/testing';

import vinExtractorSchema from '~locales/vinExtractor/type';
import ar from '../../locales/vinExtractor/ar.json';
import en from '../../locales/vinExtractor/en.json';
import ku from '../../locales/vinExtractor/ku.json';
import ru from '../../locales/vinExtractor/ru.json';

import { VinExtractor } from './vin-extractor';

async function mount(attributes = '') {
  const page = await newSpecPage({ components: [VinExtractor], html: `<vin-extractor ${attributes}></vin-extractor>` });
  return { page, root: page.root.shadowRoot, instance: page.rootInstance as VinExtractor };
}

const part = (root: ShadowRoot, name: string) => root.querySelector(`[part~="${name}"]`);
const label = (root: ShadowRoot, name: string) => part(root, name)?.getAttribute('aria-label');

describe('parts', () => {
  it('exposes every themeable piece, keeping the two older names', async () => {
    const { root } = await mount('manual-capture manual-capture-hint="Hold steady"');

    for (const name of ['backdrop', 'panel', 'header', 'title', 'form-input-label', 'close', 'preview', 'hint', 'capture', 'vin-extractor-capture-button', 'capture-icon']) {
      expect(part(root, name)).not.toBeNull();
    }
    expect(part(root, 'title')).toBe(part(root, 'form-input-label'));
    expect(part(root, 'capture')).toBe(part(root, 'vin-extractor-capture-button'));
  });

  it('names the label inside a labelled capture button', async () => {
    const { root } = await mount('manual-capture manual-capture-label="Scan"');

    expect(part(root, 'capture-label').textContent).toBe('Scan');
    expect(part(root, 'capture-icon')).toBeNull();
  });
});

describe('language', () => {
  it('writes its own names in English by default, left to right', async () => {
    const { page, root } = await mount('manual-capture');

    expect(label(root, 'backdrop')).toBe('VIN Scanner');
    expect(label(root, 'close')).toBe('Close scanner');
    expect(label(root, 'capture')).toBe('Capture VIN');
    expect(label(root, 'preview')).toBe('Camera preview');
    expect(root.querySelector('.vin-extractor-input').getAttribute('aria-label')).toBe('Upload VIN image');
    expect(part(root, 'backdrop').getAttribute('dir')).toBe('ltr');
    expect(page.root.getAttribute('color-scheme')).toBe('light');
  });

  it('writes them in the given language', async () => {
    const { root } = await mount('manual-capture language="ru"');

    expect(label(root, 'backdrop')).toBe(ru.dialog);
    expect(label(root, 'close')).toBe(ru.close);
    expect(label(root, 'capture')).toBe(ru.capture);
    expect(label(root, 'preview')).toBe(ru.preview);
    expect(part(root, 'backdrop').getAttribute('lang')).toBe('ru');
  });

  it('lays Arabic and Kurdish out right to left', async () => {
    const arabic = await mount('language="ar"');
    const kurdish = await mount('language="ku"');

    expect(part(arabic.root, 'backdrop').getAttribute('dir')).toBe('rtl');
    expect(part(kurdish.root, 'backdrop').getAttribute('dir')).toBe('rtl');
    expect(part(kurdish.root, 'backdrop').getAttribute('lang')).toBe('ckb');
  });

  it('keeps the host text over its own', async () => {
    const { root } = await mount('manual-capture language="ru" title="Scan the plate" manual-capture-label="Shoot"');

    expect(label(root, 'backdrop')).toBe('Scan the plate');
    expect(label(root, 'capture')).toBe('Shoot');
  });

  it('falls back to English for a language it does not ship', async () => {
    const { root } = await mount('language="xx"');

    expect(label(root, 'close')).toBe(en.close);
  });

  it('ships every string in every language', () => {
    const keys = Object.keys(vinExtractorSchema.getDefault()).sort();

    for (const strings of [en, ar, ku, ru]) {
      expect(Object.keys(strings).sort()).toEqual(keys);
      expect(vinExtractorSchema.validateSync(strings)).toEqual(strings);
      for (const value of Object.values(strings)) expect(value.trim()).not.toBe('');
    }
  });
});

describe('page scroll', () => {
  const body = (page: SpecPage) => page.doc.body;

  it('puts back the overflow the page had before it opened', async () => {
    const { page, instance } = await mount();
    body(page).style.overflow = 'clip';

    instance.lockPageScroll();
    expect(body(page).style.overflow).toBe('hidden');

    instance.closeScanner();
    expect(body(page).style.overflow).toBe('clip');
  });

  it('leaves the page alone when it closes without having opened', async () => {
    const { page, instance } = await mount();
    body(page).style.overflow = 'scroll';

    instance.closeScanner();
    expect(body(page).style.overflow).toBe('scroll');
  });

  it('records the first value only when locked twice', async () => {
    const { page, instance } = await mount();
    body(page).style.overflow = '';

    instance.lockPageScroll();
    instance.lockPageScroll();
    instance.unlockPageScroll();
    expect(body(page).style.overflow).toBe('');
  });

  it('restores it when removed while open', async () => {
    const { page, instance } = await mount();
    body(page).style.overflow = 'auto';

    instance.lockPageScroll();
    page.root.remove();
    await page.waitForChanges();
    expect(body(page).style.overflow).toBe('auto');
  });
});
