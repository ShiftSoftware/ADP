import { LanguageKeys } from '~features/multi-lingual';

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

export const setupRecaptcha = (recaptchaKey: string, language: LanguageKeys = 'en', callback: () => void = () => {}) => {
  console.log(language);

  if (!recaptchaKey) return;

  const oldScript = document.querySelector("script[src*='recaptcha/api.js']");
  if (oldScript) {
    oldScript.remove();
    console.log('Old reCAPTCHA script removed');
  }

  const recaptchaContainers = document.getElementsByClassName('g-recaptcha');
  Array.from(recaptchaContainers).forEach(container => container.remove());

  const script = document.createElement('script');
  script.src = `https://www.google.com/recaptcha/api.js?render=explicit&hl=${language}`;
  script.async = true;
  script.defer = true;

  script.onload = () => {
    console.log(`reCAPTCHA script loaded with language: ${language}`);
    callback();
  };

  document.head.appendChild(script);
};
