export type FormSelectItem = {
  value: string;
  label: string;
};

export type FormSelectFetcher = (language: string, signal: AbortSignal) => Promise<FormSelectItem[]>;
