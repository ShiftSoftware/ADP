import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { FormElementMapper, FormElementStructure, FormHook, FormHookInterface, FormSelectFetcher, FormSelectItem, functionHooks } from '~features/form-hook';
import { GeneralFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { LoaderIcon } from '~assets/loader-icon';
import { string } from 'yup';
import { getFormValidations, y } from './defaults/validation';
import { getFormMappers } from './defaults/mappers';
import { getDefaultStateObject } from './defaults/state-object';

let stateObject = getDefaultStateObject();

const validation = getFormValidations(stateObject, {
  generalTicketType: string()
    .meta(y.meta('generalTicketType'))
    .when(y.condition('generalTicketType'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(y.require('generalTicketType')),
    }),
});

const elementMapper: FormElementMapper<any, any> = getFormMappers(stateObject, {
  generalTicketType: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async ({}): Promise<FormSelectItem[]> => {
      const options = Array.isArray(props?.options) ? props?.options : [];

      return options.map(option => ({ value: option?.value || '', label: option?.[language] || '' })) as FormSelectItem[];
    };

    return <form-select {...props} clearable fetcher={fetcher} language={language} />;
  },
});

@Component({
  shadow: true,
  tag: 'general-inquiry-form',
  styleUrl: './defaults/style.css',
})
export class GeneralInquiryForm implements FormHookInterface<any>, MultiLingual {
  // #region Localization
  @Prop({ mutable: true, reflect: true }) language: LanguageKeys;

  @State() locale: GeneralFormLocal = { sharedFormLocales: sharedFormLocalesSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    await functionHooks.formLanguageChange(this, newLanguage);
  }
  // #endregion

  // #region Form Hook logic
  @State() errorMessage: string;
  @State() isLoading: boolean = false;
  @State() localeLanguage: LanguageKeys;

  @Prop() gistId?: string;
  @Prop() extraHeader: object;
  @Prop() extraPayload: object;
  @Prop() structureUrl?: string;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() errorCallback: (error: any, message: string) => void;
  @Prop() successCallback: (data: any, message?: string) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<any> | undefined;
  @Prop({ mutable: true }) fields?: object;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    functionHooks.formLoadingHandler(this, isLoading);
  }

  setErrorCallback(error: any) {
    functionHooks.formErrorHandler(this, error);
  }

  setSuccessCallback(data: any) {
    functionHooks.formSuccessHandler(this, data);
  }

  async componentWillLoad() {
    if (!this.language) this.language = getLanguageFromUrl();
  }

  async formSubmit(formValues: any) {
    await functionHooks.onFormSubmit<any>({ context: this, formValues });
  }
  // #endregion

  // #region Component Logic
  async componentDidLoad() {
    await functionHooks.formDidLoadHandler<any, any>(this, validation);
  }

  @Method()
  async getForm() {
    return await functionHooks.formGetFormHandler(this);
  }

  @Method()
  async submit() {
    await functionHooks.handleFormSubmit(this);
  }

  @State() structureRendered = false;
  @Prop() formReadyCallback?: () => void;

  @Watch('structureRendered')
  async structureChanged(isRendered: boolean) {
    await functionHooks.formStructureRenderedHandler(this, isRendered);
  }

  @Prop() theme?: string;
  @Prop() formId?: string;
  @Prop() isDev?: boolean = false;
  @Prop() disableScrollToTop?: boolean;
  @Prop() isMobileForm: boolean = false;
  @Prop() getMobileToken?: () => string;

  @State() form: FormHook<any>;

  // #endregion
  render() {
    return (
      <Host>
        <div part={cn('shift-form', this.structure?.data?.theme, this.theme)}>
          <div part="form-container" class="relative min-h-[150px]">
            <div
              part="form-loader-container"
              class={cn('form-loader-container absolute top-0 left-0 w-full h-full pointer-events-none flex justify-center items-center transition-opacity duration-500', {
                'opacity-0': this.structureRendered,
              })}
            >
              <LoaderIcon part="form-loader-icon" class="img" />
            </div>
            <flexible-container onlyForMounting isOpened={this.structureRendered}>
              {!!this.form && (
                <form-structure
                  form={this.form}
                  fields={this.fields}
                  formId={this.formId}
                  formLocale={this.locale}
                  structure={this.structure}
                  isLoading={this.isLoading}
                  language={this.localeLanguage}
                  errorMessage={this.errorMessage}
                  formElementMapper={elementMapper}
                  successMessage={this.locale['Form submitted successfully.'] || 'Form submitted successfully.'}
                >
                  <slot></slot>
                </form-structure>
              )}
            </flexible-container>
          </div>
        </div>
      </Host>
    );
  }
}
