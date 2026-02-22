import { getNestedValue } from './get-nested-value';

type Setup = {
  'item-value': string;
  'item-label': string;
  'useNamedValue'?: boolean;
  'items': 'data.attributes.Models';
};

type Populated = { value: string; label: string; meta?: any };

export const populateItems = (data: any, setup: Setup, hasMeta?: boolean): Populated[] => {
  const options: Populated[] = [];

  const baseOptions = getNestedValue(data, setup.items) as any[];

  baseOptions.forEach(item => {
    const value = getNestedValue(item, setup['item-value']);
    const label = getNestedValue(item, setup['item-label']);
    const newItemObject: Populated = { value: setup?.useNamedValue ? label : value, label };

    if (hasMeta) newItemObject.meta = { ...item };

    options.push(newItemObject);
  });

  return options;
};
