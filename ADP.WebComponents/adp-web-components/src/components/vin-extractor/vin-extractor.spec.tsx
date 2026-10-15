import { newSpecPage } from '@stencil/core/testing';

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
  it('writes its own names in English by default and leaves direction and scheme to the page', async () => {
    const { page, root } = await mount('manual-capture');

    expect(label(root, 'backdrop')).toBe('VIN Scanner');
    expect(label(root, 'close')).toBe('Close scanner');
    expect(label(root, 'capture')).toBe('Capture VIN');
    expect(label(root, 'preview')).toBe('Camera preview');
    expect(root.querySelector('.vin-extractor-input').getAttribute('aria-label')).toBe('Upload VIN image');
    expect(part(root, 'backdrop').hasAttribute('dir')).toBe(false);
    expect(part(root, 'backdrop').hasAttribute('lang')).toBe(false);
    expect(
      Array.from(page.root.attributes)
        .map(attribute => attribute.name)
        .sort(),
    ).toEqual(['manual-capture', 'translate']);
  });

  it('writes them in the given language', async () => {
    const { root } = await mount('manual-capture language="ru"');

    expect(part(root, 'backdrop').getAttribute('dir')).toBe('ltr');

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

describe('colour scheme', () => {
  it('reflects only a scheme the host passed', async () => {
    const { page } = await mount('color-scheme="dark"');

    expect(page.root.getAttribute('color-scheme')).toBe('dark');
  });
});

describe('page scroll', () => {
  it('sets the body overflow to auto on close, as it always has', async () => {
    const { page, instance } = await mount();
    page.doc.body.style.overflow = 'hidden';

    instance.closeScanner();
    expect(page.doc.body.style.overflow).toBe('auto');
  });
});

describe('host labels', () => {
  const allLabels = async (attributes: string) => {
    const { page, root, instance } = await mount(`manual-capture ${attributes}`);
    instance.videoInputs = [{} as MediaDeviceInfo, {} as MediaDeviceInfo];
    await page.waitForChanges();
    const idle = {
      dialog: label(root, 'backdrop'),
      switchCamera: label(root, 'switch-camera'),
      close: label(root, 'close'),
      capture: label(root, 'capture'),
      preview: label(root, 'preview'),
      upload: root.querySelector('.vin-extractor-input').getAttribute('aria-label'),
    };
    instance.manualCaptureLoading = true;
    await page.waitForChanges();
    return { ...idle, capturing: label(root, 'capture') };
  };

  const HOST = 'dialog-label="D" switch-camera-label="S" close-label="C" capturing-label="W" preview-label="P" upload-label="U"';

  it('keeps the English names when nothing is passed', async () => {
    expect(await allLabels('')).toEqual({
      dialog: 'VIN Scanner',
      switchCamera: 'Switch camera',
      close: 'Close scanner',
      capture: 'Capture VIN',
      preview: 'Camera preview',
      upload: 'Upload VIN image',
      capturing: 'Capturing...',
    });
  });

  it('uses the locale when only language is passed', async () => {
    expect(await allLabels('language="ru"')).toEqual({
      dialog: ru.dialog,
      switchCamera: ru.switchCamera,
      close: ru.close,
      capture: ru.capture,
      preview: ru.preview,
      upload: ru.upload,
      capturing: ru.capturing,
    });
  });

  it('puts the host text before the locale and the English default', async () => {
    const expected = { dialog: 'D', switchCamera: 'S', close: 'C', capture: 'Capture VIN', preview: 'P', upload: 'U', capturing: 'W' };

    expect(await allLabels(HOST)).toEqual(expected);
    expect(await allLabels(`${HOST} language="ar"`)).toEqual({ ...expected, capture: ar.capture });
  });

  it('treats an empty host text as not passed', async () => {
    expect((await allLabels('close-label="" language="ru"')).close).toBe(ru.close);
  });

  it('keeps title as the dialog name over dialogLabel', async () => {
    expect((await allLabels('title="T" dialog-label="D"')).dialog).toBe('T');
  });
});

describe('icon slots', () => {
  it('wraps each icon in a named slot whose fallback is the icon, with its part', async () => {
    const { page, root, instance } = await mount('manual-capture');
    instance.videoInputs = [{} as MediaDeviceInfo, {} as MediaDeviceInfo];
    await page.waitForChanges();

    for (const name of ['switch-camera-icon', 'close-icon', 'capture-icon']) {
      const slot = root.querySelector(`slot[name="${name}"]`);
      expect(slot).not.toBeNull();
      expect(slot.querySelector(`svg[part="${name}"]`)).not.toBeNull();
    }
  });

  it('keeps one unnamed slot for the other children', async () => {
    const { root } = await mount();

    expect(root.querySelectorAll('slot:not([name])').length).toBe(1);
  });
});

describe('permissions fallback', () => {
  const run = async (attributes: string, permissions: unknown) => {
    const { page, instance } = await mount(attributes);
    const getUserMedia = jest.fn(async () => ({ getTracks: () => [{ stop: () => undefined }] }));
    for (const navigator of new Set([page.win.navigator, globalThis.navigator])) {
      Object.defineProperty(navigator, 'permissions', { value: permissions, configurable: true });
      Object.defineProperty(navigator, 'mediaDevices', { value: { getUserMedia, enumerateDevices: async () => [] }, configurable: true });
    }
    const errors: string[] = [];
    instance.onError = (error: Error) => errors.push(error.message);

    await instance.openScanner();
    return { getUserMedia, errors };
  };

  it('is off by default: a missing Permissions API fails before asking for the camera', async () => {
    const { getUserMedia, errors } = await run('', undefined);

    expect(getUserMedia).not.toHaveBeenCalled();
    expect(errors).toHaveLength(1);
    expect(errors[0]).not.toBe('No Camera Found');
  });

  it('asks for the camera directly when the Permissions API is missing', async () => {
    const { getUserMedia, errors } = await run('permissions-fallback', undefined);

    expect(getUserMedia).toHaveBeenCalledWith({ video: true });
    expect(errors).toEqual(['No Camera Found']);
  });

  it('asks for the camera directly when the query rejects camera', async () => {
    const { getUserMedia, errors } = await run('permissions-fallback', { query: async () => Promise.reject(new TypeError('camera is not a valid permission name')) });

    expect(getUserMedia).toHaveBeenCalledWith({ video: true });
    expect(errors).toEqual(['No Camera Found']);
  });

  it('still honours an answer the Permissions API gives', async () => {
    const denied = await run('permissions-fallback', { query: async () => ({ state: 'denied' }) });
    const granted = await run('permissions-fallback', { query: async () => ({ state: 'granted' }) });

    expect(denied.getUserMedia).not.toHaveBeenCalled();
    expect(denied.errors).toEqual(['no camera access']);
    expect(granted.getUserMedia).not.toHaveBeenCalled();
    expect(granted.errors).toEqual(['No Camera Found']);
  });
});
