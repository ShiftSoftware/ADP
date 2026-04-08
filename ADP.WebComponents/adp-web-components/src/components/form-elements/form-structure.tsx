import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import generalSchema from '~locales/general/type';

import { ComponentLocale, getLocaleLanguage, getSharedLocal, LanguageKeys, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormElementMapper, FormElementMapperFunctionProps, FormElementStructure, renderStructure } from '~features/form-hook';
import cn from '~lib/cn';
import getCustomClassesForPortal from '~lib/get-custom-classes-for-portal';

@Component({
  shadow: false,
  tag: 'form-structure',
  styleUrl: 'form-inputs.css',
})
export class FormStructure {
  // #region Localization
  @Prop({ reflect: true }) formLocale: any = {};
  @Prop({ reflect: true }) language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof generalSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...generalSchema.getDefault() };

  async componentWillLoad() {
    this.form.formStructure = this;

    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'general', generalSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  @Prop() formId?: string;
  @Prop() isLoading: boolean = false;
  @Prop() form!: FormHook<any>;
  @Prop() errorMessage: string = '';
  @Prop() successMessage: string = '';
  @Prop({ mutable: true }) fields: object = {};
  @Prop() structure!: FormElementStructure<any>;
  @Prop() formElementMapper!: FormElementMapper<any, any>;

  @State() currentStep: number = 1;
  @State() dialogClasses: string = '';

  @Element() el!: HTMLElement;

  async componentDidLoad() {
    this.dialogClasses = getCustomClassesForPortal(this.el);
    setTimeout(() => {
      if (typeof this.form.context?.structureRendered === 'boolean') this.form.context.structureRendered = true;
    }, 300);
  }

  render() {
    const [locale] = this.form.getFormLocale();

    const generalProps: FormElementMapperFunctionProps<any> = {
      form: this.form,
      isLoading: this.isLoading,
      language: this.language,
      locale: this.formLocale,
      props: { isLoading: this.isLoading, form: this.form },
    };

    if (!this.structure) return <form-structure-error language={this.language} />;

    const { formController, resetFormErrorMessage } = this.form;

    const dialogParams = {
      form: this.form,
      message: this.errorMessage,
      isError: !!this.errorMessage,
      dialogClosed: resetFormErrorMessage,
      successMessage: this.successMessage,
      closeText: locale?.sharedFormLocales?.close,
    };

    return (
      <Host translate="no">
        {/* @ts-ignore */}
        <form id={this.formId} class="relative" dir={this?.locale?.sharedLocales?.direction} {...formController}>
          <div class="fixed top-10 left-10 size-20 overflow-hidden translate-x-5 translate-y-7">
            {
              // @ts-ignore
              false && <form-dialog />
            }
            <shift-portal tag="form-dialog" inheritedClasses={this.dialogClasses} componentProps={dialogParams} />
          </div>
          <div part="form-structure-form-container">
            {!this?.structure?.steps && renderStructure(this.structure, this.formElementMapper, generalProps, this.fields, -2)}

            {!!this?.structure?.steps && (
              <div part="form-stepper-container" class="overflow-hidden">
                {renderStructure(this.structure, this.formElementMapper, generalProps, this.fields, -1)}

                <div class="grid grid-cols-1 grid-rows-1">
                  {this?.structure?.steps.map((_, i) => (
                    <div
                      class={cn('col-start-1 row-start-1 transition-all !duration-700', {
                        'pointer-events-none! *:pointer-events-none!': this.currentStep !== i + 1,
                        'translate-x-full rtl:-translate-x-full opacity-0': this.currentStep < i + 1,
                        'translate-x-0 opacity-100': this.currentStep === i + 1,
                        '-translate-x-full rtl:translate-x-full  opacity-0': this.currentStep > i + 1,
                      })}
                    >
                      {renderStructure(this.structure, this.formElementMapper, generalProps, this.fields, i + 1)}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
          <button formnovalidate type="submit" class="hidden" />
        </form>
      </Host>
    );
  }
}
