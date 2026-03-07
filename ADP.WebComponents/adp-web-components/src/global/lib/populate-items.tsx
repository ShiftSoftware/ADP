import { getNestedValue } from './get-nested-value';

type Setup = {
  'item-value': string | string[];
  'item-label': string | string[];
  'useNamedValue'?: boolean;
  'items': string | string[];
};

type Populated = { value: string; label: string; meta?: any };

const getObjectValue = (obj: any, path: string | string[]) => {
  if (typeof path === 'string') return getNestedValue(obj, path);

  return path.map(itemPath => getNestedValue(obj, itemPath)).find(value => value != null);
};

export const populateItems = (data: any, setup: Setup, hasMeta?: boolean): Populated[] => {
  const options: Populated[] = [];

  const baseOptions = getObjectValue(data, setup.items) as any[];

  baseOptions.forEach(item => {
    const value = getObjectValue(item, setup['item-value']);
    const label = getObjectValue(item, setup['item-label']);
    const newItemObject: Populated = { value: setup?.useNamedValue ? label : value, label };

    if (hasMeta) newItemObject.meta = { ...item };

    options.push(newItemObject);
  });

  return options;
};
