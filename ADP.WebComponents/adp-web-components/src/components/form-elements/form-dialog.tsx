import { InferType } from 'yup';
import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import generalSchema from '~locales/general/type';

import { getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: false,
  tag: 'form-dialog',
  styleUrl: 'form-dialog.css',
})
export class FormInput {
  @Prop() errorMessage: string;
  @Prop() dialogClosed: () => void;
  @Prop() language: LanguageKeys = 'en';

  @State() internalMessage: string;
  @State() isOpened: boolean = false;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() generalLocale: InferType<typeof generalSchema> = generalSchema.getDefault();

  async componentWillLoad() {
    if (this.internalMessage) {
      this.internalMessage = this.errorMessage;
      setTimeout(() => (this.isOpened = true), 100);
    }
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'general', generalSchema), getSharedLocal(newLanguage)]);
    this.generalLocale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  @Watch('errorMessage')
  async changeErrorMiddleware(newError: string) {
    if (newError && newError !== this.internalMessage) {
      this.internalMessage = newError;
      setTimeout(() => (this.isOpened = true), 100);
    } else this.isOpened = false;
  }

  render() {
    const closeDialog = () => {
      this.isOpened = false;
      setTimeout(() => {
        this.internalMessage = '';
        this.dialogClosed();
      }, 310);
    };

    return (
      <Host>
        <div
          dir={this.sharedLocales.direction}
          class={cn('fixed pointer-events-none flex items-center justify-center top-0 left-0 z-[9999999999] w-[100dvw] h-[100dvh] transition duration-300', {
            'bg-black/50 dialog-blur pointer-events-auto': this.isOpened,
          })}
        >
          <div
            class={cn('bg-white min-w-[322px] max-w-[80dvw] gap-[16px] flex flex-col transition duration-300 opacity-0 scale-50 rounded shadow p-[20px]', {
              'scale-100 opacity-100': this.isOpened,
            })}
          >
            <div class="flex justify-end">
              <button onClick={closeDialog} type="button" class="form-dialog-button transition duration-300 opacity-40 hover:opacity-100">
                <svg
                  width="20"
                  height="20"
                  fill="none"
                  stroke-width="2"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path d="M18 6 6 18" />
                  <path d="m6 6 12 12" />
                </svg>
              </button>
            </div>
            <div class="text-justify">{this.sharedLocales.errors[this.internalMessage] || this.internalMessage}</div>
            <div class="flex justify-start">
              <button
                type="button"
                onClick={closeDialog}
                class="h-[32px] text-white relative overflow-hidden px-[14px] hover:bg-red-400 transition-colors duration-300 bg-red-500 active:bg-red-700 rounded flex items-center"
              >
                {this.generalLocale.close}
              </button>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
