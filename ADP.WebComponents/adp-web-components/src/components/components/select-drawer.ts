import type { LanguageCode } from '~features/multi-lingual';

export type SelectDrawerMode = 'mobile' | 'always';
export type SelectDrawerSetting = SelectDrawerMode | 'off';

const isMode = (value: unknown): value is SelectDrawerMode => value === 'mobile' || value === 'always';

export const selectDrawerMode = (value: unknown): SelectDrawerMode | undefined => (isMode(value) ? value : undefined);

export function resolveSelectDrawer(field: unknown, form: unknown): SelectDrawerMode | undefined {
  if (field === 'off') return undefined;

  return selectDrawerMode(field) ?? selectDrawerMode(form);
}

// Empty unless opted in, so the select renders exactly as before for every other host.
export function selectDrawerProps(
  field: unknown,
  form: unknown,
  language: LanguageCode,
  label: string,
): { selectDrawer?: SelectDrawerMode; language?: LanguageCode; drawerLabel?: string } {
  const selectDrawer = resolveSelectDrawer(field, form);

  return selectDrawer ? { selectDrawer, language, drawerLabel: label } : {};
}
