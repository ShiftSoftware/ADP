import { FormSelect } from '../../../components/form-elements/form-select';

export type FormSelectItem = {
  value: string;
  label: string;
  meta?: Record<string, string>;
};

type FormSelectFetcherProps = {
  language: string;
  signal: AbortSignal;
  context: FormSelect;
  locale: Record<string, any>;
};

export type FormSelectFetcher = (props: FormSelectFetcherProps) => Promise<FormSelectItem[]>;
