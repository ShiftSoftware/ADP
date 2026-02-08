import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { serviceBookingElementNames, serviceBookingElements } from './service-booking/element-mapper';
import { ServiceBooking, serviceBookingInputsValidation } from './service-booking/validations';

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
  formSubmittHandler,
  formStructureRenderedHandler,
} from '~features/form-hook';
import { GeneralFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { LoaderIcon } from '~assets/loader-icon';

@Component({
  shadow: true,
  tag: 'service-booking-form',
  styleUrl: 'service-booking/themes.css',
})
export class ServiceBookingForm implements FormHookInterface<ServiceBooking>, MultiLingual {
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
  @Prop() extraHeader: object;
  @Prop() extraPayload: object;
  @Prop() structureUrl?: string;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() errorCallback: (error: any, message: string) => void;
  @Prop() successCallback: (data: any, message?: string) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<serviceBookingElementNames> | undefined;
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

  async formSubmit(formValues: ServiceBooking) {
    await formSubmittHandler<ServiceBooking>({ context: this, formValues });
  }
  // #endregion

  // #region Component Logic
  async componentDidLoad() {
    await formDidLoadHandler<serviceBookingElementNames, ServiceBooking>(this, serviceBookingInputsValidation);
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
  @Prop() isDev?: boolean = false;
  @Prop() disableScrollToTop?: boolean;
  @Prop() isMobileForm: boolean = false;
  @Prop() getMobileToken?: () => string;

  @State() form: FormHook<ServiceBooking>;

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
                  formElementMapper={serviceBookingElements}
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
