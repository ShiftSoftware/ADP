import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { Grecaptcha } from '~lib/recaptcha';

import { generalInquiryElements } from './general-inquiry/element-mapper';
import { GeneralInquiry, generalInquiryInputsValidation } from './general-inquiry/validations';
import type { generalInquiryElementNames } from './general-inquiry/element-mapper';

import {
  FormHook,
  FormHookInterface,
  FormElementStructure,
  formLanguageChange,
  formErrorHandler,
  formLoadingHandler,
  formSuccessHandler,
  formDidLoadHandler,
  formGetFormHandler,
  formSubmitHandler,
  formStructureRenderedHandler,
} from '~features/form-hook';
import { GeneralFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';

import cn from '~lib/cn';
import { LoaderIcon } from '~assets/loader-icon';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: true,
  tag: 'general-inquiry-form',
  styleUrl: 'general-inquiry/themes.css',
})
export class GeneralInquiryForm implements FormHookInterface<GeneralInquiry>, MultiLingual {
  // #region Localization
  @Prop({ mutable: true, reflect: true }) language: LanguageKeys;

  @State() locale: GeneralFormLocal = { sharedFormLocales: sharedFormLocalesSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    await formLanguageChange(this, newLanguage);
  }
  // #endregion

  // #region Form Hook logic
  @State() errorMessage: string;
  @State() isLoading: boolean = false;
  @State() localeLanguage: LanguageKeys;

  @Prop() gistId?: string;
  @Prop() structureUrl?: string;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() errorCallback: (error: any, message: string) => void;
  @Prop() successCallback: (data: any, message?: string) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<generalInquiryElementNames> | undefined;
  @Prop({ mutable: true }) fields?: object;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    formLoadingHandler(this, isLoading);
  }

  setErrorCallback(error: any) {
    formErrorHandler(this, error);
  }

  setSuccessCallback(data: any) {
    formSuccessHandler(this, data);
  }

  async componentWillLoad() {
    if (!this.language) this.language = getLanguageFromUrl();
  }

  async formSubmit(formValues: GeneralInquiry) {
    try {
      this.setIsLoading(true);

      const hasAdditionalData = !!this.structure?.data?.truncatedFields && !!Object.keys(this.structure?.data?.truncatedFields)?.length;

      let additionalData: Record<string, string> = {};

      if (hasAdditionalData) {
        Object.entries(this.structure?.data?.truncatedFields as Record<string, string>).forEach(([oldKey, newKey]) => {
          if (formValues[oldKey]) additionalData[newKey] = formValues[oldKey];

          delete formValues[oldKey];
        });
      }

      let payload: any = { ...formValues };

      if (this.structure?.data?.extraPayload) payload = { ...payload, ...this.structure?.data?.extraPayload };

      if (hasAdditionalData) payload.additionalData = additionalData;

      if (!!this?.branchId) payload.companyBranchId = this.branchId;

      if (!!this.customMessage) payload.message = this.customMessage;

      const headers = {
        'Content-Type': 'application/json',
        'Brand': this.structure?.data?.brandId,
        'Accept-Language': this.localeLanguage || 'en',
      };

      let requestEndpoint = this.structure?.data?.requestUrl;
      const token = await grecaptcha.execute(this.structure?.data?.recaptchaKey, { action: 'submit' });
      headers['Recaptcha-Token'] = token;

      if (this.isDev && requestEndpoint) {
        requestEndpoint = requestEndpoint.replaceAll('production=true', 'production=false');
      }

      if (!requestEndpoint) {
        throw new Error('Request endpoint is not configured');
      }

      const response = await fetch(requestEndpoint, {
        headers,
        method: 'POST',
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        const result = await response?.json();

        const eventHolder = this.structure?.data?.pushAnalyticsEventTo as string | undefined;

        if (eventHolder) {
          window[eventHolder] = window[eventHolder] || [];
          window[eventHolder].push({
            name: formValues.name,
            phone: formValues.phone,
            event: this.structure?.data?.analyticEventKey || 'general_inquiry',
          });
        }

        this.setSuccessCallback(result);

        setTimeout(() => {
          this.form.reset();
          this.form.rerender({ rerenderForm: true, rerenderAll: true });
        }, 100);
      } else {
        const contentType = response.headers.get('content-type');

        const errorText = contentType?.includes('application/json') ? (await response.json())?.message?.body : await response.text();

        throw new Error(errorText);
      }
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // #endregion

  // #region Component Logic
  async componentDidLoad() {
    await formDidLoadHandler<generalInquiryElementNames, GeneralInquiry>(this, generalInquiryInputsValidation);
  }

  @Method()
  async getForm() {
    return await formGetFormHandler(this);
  }

  @Method()
  async submit() {
    await formSubmitHandler(this);
  }

  @State() structureRendered = false;
  @Prop() formReadyCallback?: () => void;

  @Watch('structureRendered')
  async structureChanged(isRendered: boolean) {
    await formStructureRenderedHandler(this, isRendered);
  }

  @Prop() theme?: string;
  @Prop() formId?: string;
  @Prop() branchId?: string;
  @Prop() customMessage?: string;
  @Prop() isDev?: boolean = false;
  @Prop() hideBranch: boolean = false;
  @Prop() disableScrollToTop?: boolean;
  @Prop() getMobileToken?: () => string;
  @Prop() isMobileForm: boolean = false;

  @State() form: FormHook<GeneralInquiry>;

  getBranchId() {
    return this?.branchId;
  }

  getHideBranch() {
    return !!this?.hideBranch;
  }

  // #endregion
  render() {
    const formPart = `general-inquiry${this?.theme ? `-${this.theme}` : ''}`;

    return (
      <Host>
        <div part={formPart}>
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
                  formElementMapper={generalInquiryElements}
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
