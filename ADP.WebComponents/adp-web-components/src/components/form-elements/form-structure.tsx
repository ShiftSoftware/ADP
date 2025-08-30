import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import generalSchema from '~locales/general/type';

import { ComponentLocale, getLocaleLanguage, getSharedLocal, LanguageKeys, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormElementMapper, FormElementMapperFunctionProps, FormElementStructure, renderStructure } from '~features/form-hook';

@Component({
  shadow: false,
  tag: 'form-structure',
  styleUrl: 'form-inputs.css',
})
export class FormStructure {
  // ====== Start Localization
  @Prop() formLocale: any = {};
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof generalSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...generalSchema.getDefault() };

  async componentWillLoad() {
    this.form.formStructure = this;
    this.form.setSuccessAnimation(() => {
      this.showSuccess = true;
      setTimeout(() => {
        this.showSuccess = false;
      }, 4000);
    });

    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'general', generalSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // ====== End Localization

  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() errorMessage: string;
  @Prop() structure: FormElementStructure<any>;
  @Prop() formElementMapper: FormElementMapper<any, any>;

  @State() showSuccess: boolean = false;

  render() {
    const [locale] = this.form.getFormLocale();

    const generalProps: FormElementMapperFunctionProps<any> = { form: this.form, isLoading: this.isLoading, language: this.language, locale: this.formLocale, props: {} };

    if (!this.structure) return <form-structure-error language={this.language} />;

    const { formController, resetFormErrorMessage } = this.form;

    return (
      <Host>
        <form class="relative" dir={this.locale.sharedLocales.direction} {...formController}>
          <div
            class={cn('absolute pointer-events-none transition duration-1000 flex items-center justify-center size-full opacity-0', {
              'opacity-100': this.showSuccess,
            })}
          >
            <div class="flex flex-col gap-[16px] items-center">
              <svg
                fill="none"
                stroke-width="2"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                xmlns="http://www.w3.org/2000/svg"
                class="size-[70px] stroke-green-700"
              >
                <path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z" />
                <path d="m9 12 2 2 4-4" />
              </svg>

              <div class="text-[20px]">{this.locale.formSubmittedSuccessfully}</div>
            </div>
          </div>
          <form-dialog dialogClosed={resetFormErrorMessage} closeText={locale.close} form={this.form} errorMessage={this.errorMessage} />
          <div class={cn('transition duration-1000', { 'opacity-0': this.showSuccess })}>{renderStructure(this.structure, this.formElementMapper, generalProps)}</div>
        </form>
      </Host>
    );
  }
}
