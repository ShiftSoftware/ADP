import '../../form-elements/shift-booking-calendar/validation.mock';
import { FormHook } from '~features/form-hook';
import { getFormMappers } from './mappers';

type VNode = { $tag$: string; $attrs$: Record<string, any> };

describe('modelYear', () => {
  const formWith = (vehicle: string) => ({ addWatcher: () => undefined, getValue: () => vehicle }) as unknown as FormHook<unknown>;
  const render = (vehicle: string) => getFormMappers().modelYear({ form: formWith(vehicle), props: { name: 'modelYear' } }) as unknown as VNode;

  it('a digits-only input, disabled until a model is chosen', () => {
    expect(render('').$tag$).toBe('form-input');
    expect(render('').$attrs$).toMatchObject({ name: 'modelYear', isDisabled: true });
    expect(render('Land Cruiser 300').$attrs$).toMatchObject({ isDisabled: false, inputProps: { inputMode: 'numeric' } });
    expect(render('x').$attrs$.formatter('20a2-25')).toBe('2022');
  });
});
