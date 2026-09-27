import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import examples from '../../templates/forms/examples.json';
import { getFormMappers } from '../../components/forms/defaults/mappers';
import { vehicleQuotationElements } from '../../components/forms/vehicle-quotation/element-mapper';
import { renderStructure, structureAppearance } from './render-structure';

const noop = () => undefined;
const stubForm = new Proxy({ context: {} } as Record<string, unknown>, { get: (target, key) => (key in target ? target[key as string] : noop) });

type Structure = Parameters<typeof renderStructure>[0];

const mapperFor = (component: string): Parameters<typeof renderStructure>[1] => (component === 'vehicle-quotation-form' ? vehicleQuotationElements : getFormMappers());

async function renderPreset(structure: Structure, component: string, step = -2) {
  const props = { isLoading: false, form: stubForm, today: '2026-09-27' };
  const page = await newSpecPage({
    components: [],
    template: () => (
      <div id="root">{renderStructure(structure, mapperFor(component), { form: stubForm as never, isLoading: false, language: 'en' as const, locale: {}, props }, {}, step)}</div>
    ),
  });

  return page.body.querySelector('#root').outerHTML;
}

describe('form structure DOM', () => {
  for (const preset of examples.presets) {
    it(`renders ${preset.id} unchanged`, async () => {
      const steps = (preset.structure as unknown as { steps?: unknown[] }).steps?.length ?? 0;
      const rendered = [await renderPreset(preset.structure as unknown as Structure, preset.component)];

      for (let step = 1; step <= steps; step++) rendered.push(await renderPreset(preset.structure as unknown as Structure, preset.component, step));

      expect(rendered).toMatchSnapshot();
    });
  }
});

describe('form structure appearance', () => {
  const structure = {
    data: { theme: 'docs', appearance: 'material' as const, colorScheme: 'dark' as const },
    tag: 'div',
    children: [{ name: 'name' }, { name: 'email', type: 'email', appearance: 'soft' }, { name: 'phone', colorScheme: 'light' }],
  };

  it('passes the structure appearance to fields, and a field value wins', async () => {
    const date = jest.fn<boolean, [unknown]>(() => false);
    const general = { form: stubForm as never, isLoading: false, language: 'en' as const, locale: {}, props: { today: '2026-09-27', ...structureAppearance(structure.data) } };

    renderStructure({ name: 'bookingDate', type: 'date' }, { date }, general, {}, -2);
    renderStructure({ name: 'bookingDate', type: 'date', appearance: 'sharp' } as never, { date }, general, {}, -2);

    expect(date.mock.calls[0][0]).toEqual(expect.objectContaining({ props: expect.objectContaining({ appearance: 'material', colorScheme: 'dark' }) }));
    expect(date.mock.calls[1][0]).toEqual(expect.objectContaining({ props: expect.objectContaining({ appearance: 'sharp', colorScheme: 'dark' }) }));
  });

  it('renders appearance attributes on fields and leaves data.theme alone', async () => {
    const props = { isLoading: false, form: stubForm, today: '2026-09-27', ...structureAppearance(structure.data) };
    const page = await newSpecPage({
      components: [],
      template: () => (
        <div id="root">
          {renderStructure(structure as never, getFormMappers(), { form: stubForm as never, isLoading: false, language: 'en' as const, locale: {}, props }, {}, -2)}
        </div>
      ),
    });
    const [name, email, phone] = Array.from(page.body.querySelectorAll('form-input, form-phone-number'));

    expect([name.getAttribute('appearance'), email.getAttribute('appearance'), phone.getAttribute('appearance')]).toEqual(['material', 'soft', 'material']);
    expect(phone.getAttribute('colorscheme') ?? phone.getAttribute('color-scheme') ?? (phone as unknown as { colorScheme: string }).colorScheme).toBe('light');
    expect(page.body.innerHTML).not.toContain('docs');
  });

  it('adds nothing when the structure sets neither key', () => {
    expect(structureAppearance({ theme: 'docs' })).toEqual({});
    expect(structureAppearance({ size: 'lg', appearance: 'soft' })).toEqual({ size: 'lg', appearance: 'soft' });
    expect(structureAppearance(undefined)).toEqual({});
  });
});
