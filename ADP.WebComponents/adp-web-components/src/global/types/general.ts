export type Grecaptcha = {
  getResponse(): string;
  reset(widgetId?: number): void;
  ready(callback: () => void): void;
  execute(siteKey: string, options: { action: string }): Promise<string>;
  render(
    container: HTMLElement | string,
    parameters: { 'sitekey': string; 'callback'?: (token: string) => void; 'error-callback'?: () => void; 'expired-callback'?: () => void },
  ): number;
};

export type InputParams = {
  id?: string;
  name: string;
  type?: string;
  disabled?: boolean;
  placeholder?: string;
  defaultValue?: string;
  onInput?: (event: InputEvent) => void;
};
