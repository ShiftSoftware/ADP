import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { ShiftPortal } from '../shift-portal';
import { ShiftImage } from './shift-image';
import { ShiftImageLayer } from './shift-image-layer';

import english from '../../../locales/imageViewer/en.json';
import arabic from '../../../locales/imageViewer/ar.json';

const render = async (attributes = '', inner = ''): Promise<SpecPage> =>
  newSpecPage({
    components: [ShiftImage, ShiftImageLayer, ShiftPortal],
    html: `<img id="thumb" src="https://example.invalid/thumb.png" /><shift-image ${attributes}>${inner}</shift-image>`,
  });

const viewer = (page: SpecPage) => page.doc.querySelector('shift-image') as unknown as HTMLShiftImageElement;
const layer = (page: SpecPage) => page.doc.body.querySelector('shift-image-layer') as HTMLElement & { open: boolean; src: string; origin: unknown };
const scrim = (page: SpecPage) => layer(page).shadowRoot.querySelector('.scrim') as HTMLElement;

const wait = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

describe('shift-image', () => {
  it('renders nothing where it stands and portals its layer onto <body>, closed', async () => {
    const page = await render();

    expect(layer(page)).not.toBeNull();
    expect(layer(page).parentElement).toBe(page.doc.body);
    // Stencil keeps a component hidden until it carries `hydrated`; the portal writes the class list, so it must pass it on.
    expect(layer(page).classList.contains('hydrated')).toBe(true);
    expect(layer(page).open).toBe(false);
    expect(layer(page).getAttribute('aria-hidden')).toBe('true');
    expect(layer(page).hasAttribute('data-state')).toBe(false);
  });

  it('draws a scrim, the picture card (thumbnail pixels and the full picture), a caption and a close button', async () => {
    const page = await render();
    const root = layer(page).shadowRoot;

    expect(root.querySelector('.scrim')).not.toBeNull();
    expect(root.querySelectorAll('.frame > .clip > img.picture')).toHaveLength(2);
    expect(root.querySelector('.caption')).not.toBeNull();
    expect(root.querySelector('button.close svg')).not.toBeNull();
  });

  it('show() opens on the picture from its thumbnail, with its label and caption', async () => {
    const page = await render();
    const thumb = page.doc.getElementById('thumb');
    const changes: { open: boolean; src: string }[] = [];
    viewer(page).addEventListener('viewerChange', (event: CustomEvent) => changes.push(event.detail));

    await viewer(page).show('https://example.invalid/full.png', { origin: '#thumb', label: 'Open picture of TST-0001', caption: 'TST-0001 · Side steps' });
    await page.waitForChanges();

    expect(layer(page).open).toBe(true);
    expect(layer(page).src).toBe('https://example.invalid/full.png');
    expect(layer(page).origin).toBe(thumb);
    expect(layer(page).getAttribute('aria-label')).toBe('Open picture of TST-0001');
    expect(layer(page).getAttribute('data-state')).toBe('open');
    expect(layer(page).shadowRoot.querySelector('.caption').textContent).toBe('TST-0001 · Side steps');
    expect(changes).toEqual([{ open: true, src: 'https://example.invalid/full.png' }]);
  });

  it('does not lock the page by hiding its scrollbar (no sideways jump)', async () => {
    const page = await render();
    await viewer(page).show('https://example.invalid/a.png');
    await page.waitForChanges();
    expect(page.doc.body.style.overflow).toBe('');
  });

  it('closes on a click outside the picture, on Escape, and on hide(), and tells the host each time', async () => {
    const page = await render();
    const changes: boolean[] = [];
    viewer(page).addEventListener('viewerChange', (event: CustomEvent) => changes.push(event.detail.open));

    await viewer(page).show('https://example.invalid/a.png');
    await page.waitForChanges();
    scrim(page).click();
    await page.waitForChanges();
    expect(layer(page).open).toBe(false);

    await viewer(page).show('https://example.invalid/a.png');
    await page.waitForChanges();
    // Any other key leaves it open; Escape closes it.
    page.doc.dispatchEvent(new KeyboardEvent('keydown', { key: 'x' }));
    await page.waitForChanges();
    expect(layer(page).open).toBe(true);
    page.doc.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await page.waitForChanges();
    expect(layer(page).open).toBe(false);

    await viewer(page).show('https://example.invalid/a.png');
    await page.waitForChanges();
    await viewer(page).hide();
    await page.waitForChanges();
    expect(layer(page).open).toBe(false);

    expect(changes).toEqual([true, false, true, false, true, false]);
  });

  it('ignores the same picture again while open, as before', async () => {
    const page = await render();
    const changes: boolean[] = [];
    viewer(page).addEventListener('viewerChange', (event: CustomEvent) => changes.push(event.detail.open));

    await viewer(page).show('https://example.invalid/a.png');
    await viewer(page).show('https://example.invalid/a.png');
    expect(changes).toEqual([true]);
    expect(await viewer(page).opened()).toBe(true);
  });

  it('names its close button and dialog in the language it is given, and takes overrides', async () => {
    const page = await render('language="ar"');
    const close = () => layer(page).shadowRoot.querySelector('.close');
    expect(close().getAttribute('aria-label')).toBe(arabic.close);

    await viewer(page).show('https://example.invalid/a.png');
    await page.waitForChanges();
    expect(layer(page).getAttribute('aria-label')).toBe(arabic.dialog);

    const english_ = await render('close-label="Done"');
    expect(english_.doc.body.querySelector('shift-image-layer').shadowRoot.querySelector('.close').getAttribute('aria-label')).toBe('Done');
    expect(english.close).toBe('Close picture');
  });

  it('is untranslatable, the element and its layer', async () => {
    const page = await render();
    expect(viewer(page).getAttribute('translate')).toBe('no');
    expect(layer(page).getAttribute('translate')).toBe('no');
  });

  describe('automatic use', () => {
    const trigger = (page: SpecPage) => viewer(page).shadowRoot.querySelector<HTMLButtonElement>('button.trigger');
    const clickAndSettle = async (page: SpecPage) => {
      trigger(page).click();
      await wait(10);
      await page.waitForChanges();
    };

    it('draws the thumbnail in a named button and opens on full-src when clicked', async () => {
      const page = await render('src="https://example.invalid/t.png" full-src="https://example.invalid/full.png" alt="Side steps"');

      const image = viewer(page).shadowRoot.querySelector('img.image');
      expect(image.getAttribute('src')).toBe('https://example.invalid/t.png');
      expect(image.getAttribute('alt')).toBe('Side steps');
      expect(trigger(page).getAttribute('aria-label')).toBe(`${english.open}: Side steps`);

      await clickAndSettle(page);
      expect(layer(page).open).toBe(true);
      expect(layer(page).src).toBe('https://example.invalid/full.png');
      expect(layer(page).origin).toBe(image);
    });

    it('opens on src when there is no full-src', async () => {
      const page = await render('src="https://example.invalid/t.png"');
      await clickAndSettle(page);
      expect(layer(page).src).toBe('https://example.invalid/t.png');
    });

    it('does not open on a click the page prevented: the page handles the click itself', async () => {
      const page = await render('src="https://example.invalid/t.png"');
      viewer(page).addEventListener('click', event => event.preventDefault());
      await clickAndSettle(page);
      expect(layer(page).open).toBe(false);
    });

    it('manual: a click does nothing, show() opens it', async () => {
      const page = await render('src="https://example.invalid/t.png" manual');
      await clickAndSettle(page);
      expect(layer(page).open).toBe(false);

      expect(await viewer(page).show()).toBe(true);
      await page.waitForChanges();
      expect(layer(page).open).toBe(true);
    });

    it('disabled: neither a click nor show() opens it, and the button is disabled', async () => {
      const page = await render('src="https://example.invalid/t.png" disabled');
      expect(trigger(page).hasAttribute('disabled')).toBe(true);
      await clickAndSettle(page);
      expect(await viewer(page).show()).toBe(false);
      expect(layer(page).open).toBe(false);
    });

    it('without src it draws nothing: a viewer for the page to drive', async () => {
      const page = await render();
      expect(trigger(page)).toBeNull();
    });
  });

  describe('hooks', () => {
    it('beforeOpen and beforeClose can be cancelled', async () => {
      const page = await render();
      const cancelOpen = (event: Event) => event.preventDefault();
      viewer(page).addEventListener('beforeOpen', cancelOpen);
      expect(await viewer(page).show('https://example.invalid/a.png')).toBe(false);
      expect(await viewer(page).opened()).toBe(false);

      viewer(page).removeEventListener('beforeOpen', cancelOpen);
      await viewer(page).show('https://example.invalid/a.png');
      viewer(page).addEventListener('beforeClose', event => event.preventDefault());
      expect(await viewer(page).hide()).toBe(false);
      expect(await viewer(page).opened()).toBe(true);
    });

    it('fires before and after, in order, the after ones once the movement has finished', async () => {
      const page = await render();
      const heard: string[] = [];
      for (const name of ['beforeOpen', 'afterOpen', 'beforeClose', 'afterClose']) viewer(page).addEventListener(name, () => heard.push(name));

      await viewer(page).show('https://example.invalid/a.png');
      await page.waitForChanges();
      expect(heard).toEqual(['beforeOpen']);
      await wait(520);
      expect(heard).toEqual(['beforeOpen', 'afterOpen']);

      await viewer(page).hide();
      await page.waitForChanges();
      expect(heard).toEqual(['beforeOpen', 'afterOpen', 'beforeClose']);
      await wait(520);
      expect(heard).toEqual(['beforeOpen', 'afterOpen', 'beforeClose', 'afterClose']);
    });
  });
});
