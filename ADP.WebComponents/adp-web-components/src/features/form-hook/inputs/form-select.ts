export type FormSelectItem = {
  value: string;
  label: string;
  meta?: Record<string, string>;
};

type FormSelectFetcherProps<T> = {
  locale: T;
  language: string;
  signal: AbortSignal;
};

export type FormSelectFetcher<T> = (props: FormSelectFetcherProps<T>) => Promise<FormSelectItem[]>;
