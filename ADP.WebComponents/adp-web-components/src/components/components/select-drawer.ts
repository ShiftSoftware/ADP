import { popoverStrings } from './shift-popover/popover-locale';

export type SelectDrawerMode = 'mobile' | 'always';
export type SelectDrawerSetting = SelectDrawerMode | 'off';

const isMode = (value: unknown): value is SelectDrawerMode => value === 'mobile' || value === 'always';

export const selectDrawerMode = (value: unknown): SelectDrawerMode | undefined => (isMode(value) ? value : undefined);

export function resolveSelectDrawer(field: unknown, form: unknown): SelectDrawerMode | undefined {
  if (field === 'off') return undefined;

  return selectDrawerMode(field) ?? selectDrawerMode(form);
}

export function selectDrawerProps(
  field: unknown,
  form: unknown,
  language: string,
  label: string,
): { selectDrawer?: SelectDrawerMode; drawerLabel?: string; drawerSearchLabel?: string } {
  const selectDrawer = resolveSelectDrawer(field, form);

  return selectDrawer ? { selectDrawer, drawerLabel: label, drawerSearchLabel: popoverStrings(language).search } : {};
}
