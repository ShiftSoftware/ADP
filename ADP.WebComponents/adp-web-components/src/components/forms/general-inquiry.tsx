import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { FormElementMapper, FormElementStructure, FormHook, FormHookInterface, FormInputMeta, FormSelectFetcher, FormSelectItem, functionHooks } from '~features/form-hook';
import { GeneralFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { LoaderIcon } from '~assets/loader-icon';
import { object, string } from 'yup';
import { getDefaultValidaations, y } from './defaults/validation';
import { getDefaultMappers } from './defaults/mappers';
import { getDefaultStateObject } from './defaults/state-object';

let stateObject = getDefaultStateObject();

const validation = object({
  ...getDefaultValidaations(stateObject),

  companyBranchId: string()
    .meta({ label: y.label('companyBranchId'), placeholder: y.placeholder('companyBranchId') } as FormInputMeta)
    .when(y.condition('companyBranchId'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(y.require('companyBranchId')),
    }),
});

const elementMapper: FormElementMapper<any, any> = {
  ...getDefaultMappers(stateObject),

  companyBranchId: ({ form, language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const branchEndpoint = form.context.structure?.data.branchApi as string;

      const response = await fetch(branchEndpoint, { signal, headers: { 'Accept-Language': language } });

      const branches = (await response.json()) as { ID?: string; Name?: string }[];

      const parsedOptions = branches.map(branch => ({ label: branch?.Name, value: branch?.ID })) as FormSelectItem[];

      return parsedOptions;
    };

    return <form-select {...props} searchable fetcher={fetcher} language={language} />;
  },
};

@Component({
  shadow: true,
  tag: 'general-inquiry-form',
  styleUrl: './defaults/styles/general-inquiry.css',
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
    await functionHooks.formSubmittHandler<any>({ context: this, formValues });
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
    await functionHooks.formSubmitHandler(this);
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
        <div part={cn(this.structure?.data?.theme, this.theme)}>
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
