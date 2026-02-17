import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { Grecaptcha } from '~lib/recaptcha';

import { vehicleQuotationElementNames, vehicleQuotationElements } from './vehicle-quotation/element-mapper';
import { VehicleQuotation, vehicleQuotationInputsValidation } from './vehicle-quotation/validations';

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
  handleFormSubmit,
  formStructureRenderedHandler,
} from '~features/form-hook';
import { GeneralFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { LoaderIcon } from '~assets/loader-icon';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: true,
  tag: 'vehicle-quotation-form',
  styleUrl: 'vehicle-quotation/themes.css',
})
export class VehicleQuotationForm implements FormHookInterface<VehicleQuotation>, MultiLingual {
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
  @Prop({ mutable: true }) structure: FormElementStructure<vehicleQuotationElementNames> | undefined;
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

  async formSubmit(formValues: VehicleQuotation) {
    try {
      this.setIsLoading(true);

      const nameContactedVehicles = this.structure?.data?.nameContactedVehicles;

      let payload: any = {
        name: formValues.name,
        phone: formValues.phone,
        companyBranchId: formValues.dealer,
        cityId: formValues?.city,
        vehicleQuotationType: this.structure?.data?.quotationType,
        preferredContactTime: formValues?.contactTime || 'NotSpecified',
        preferredPaymentMethod: formValues?.paymentType || 'Flexible',
      };

      if (nameContactedVehicles) {
        payload.vehicle = this['vehicleList'].find(vehicle => vehicle.value === formValues.vehicle)?.label || '';
        if (formValues?.ownVehicle === 'yes') {
          const currentBrand = this['currentVehicleBrandList'].find(brand => brand.value === formValues.currentVehicleBrand)?.meta || false;
          let currentModel;

          if (currentBrand) currentModel = currentBrand.Models.find(model => `${model.ID}` === formValues.currentVehicleModel) || false;
          payload.currentOrTradeInVehicle = `${currentBrand?.Name || this.locale.Other} - ${currentModel?.Name || this.locale.Other}`;
        }
      } else {
        payload.vehicle = formValues.vehicle;
        payload.additionalData = {
          DoYouOwnAVehicle: formValues?.ownVehicle === 'yes',
        };
        if (formValues?.ownVehicle === 'yes') {
          payload.additionalData.yourCurrentVehicle = formValues?.currentVehicleBrand || this.locale.Other;
          payload.additionalData.vehicleModel = formValues?.currentVehicleModel || this.locale.Other;
        }
      }
      const headers = {
        'Brand': this.structure?.data?.brandId,
        'Accept-Language': this.localeLanguage || 'en',
        'Content-Type': 'application/json',
      };

      let requestEndpoint = '';
      if (this.isMobileForm) {
        const token = await this.getMobileToken();

        if (token.toLowerCase().startsWith('bearer')) {
          headers['Authorization'] = token;
          requestEndpoint = this.structure?.data?.requestAppUrl;
        } else {
          headers['verification-token'] = token;
          requestEndpoint = this.structure?.data?.requestAppCheckUrl;
        }
      } else {
        requestEndpoint = this.structure?.data?.requestUrl;
        const token = await grecaptcha.execute(this.structure?.data?.recaptchaKey, { action: 'submit' });
        headers['Recaptcha-Token'] = token;
      }

      if (this.isDev) requestEndpoint = requestEndpoint.replaceAll('production=true', 'production=false');

      if (this.structure?.data?.extraPayload) payload = { ...payload, ...this.structure?.data?.extraPayload };

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
            event: 'get_a_quote',
            phone: formValues.phone,
            fullname: formValues.name,
            dealer: this['dealerList'].find(d => d.value === formValues.dealer)?.label,
            vehicle: this['vehicleList'].find(v => v.value === formValues.vehicle)?.label,
          });
        }

        this.setSuccessCallback(result);

        setTimeout(() => {
          this.form.reset();
          this.form.rerender({ rerenderForm: true, rerenderAll: true });
        }, 100);
      } else {
        const errorText = await response?.text();
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
    await formDidLoadHandler<vehicleQuotationElementNames, VehicleQuotation>(this, vehicleQuotationInputsValidation);
  }

  @Method()
  async getForm() {
    return await formGetFormHandler(this);
  }

  @Method()
  async submit() {
    await handleFormSubmit(this);
  }

  @State() structureRendered = false;
  @Prop() formReadyCallback?: () => void;

  @Watch('structureRendered')
  async structureChanged(isRendered: boolean) {
    await formStructureRenderedHandler(this, isRendered);
  }

  @Prop() formId?: string;
  @Prop() isDev?: boolean = false;
  @Prop() disableScrollToTop?: boolean;
  @Prop() isMobileForm: boolean = false;
  @Prop() getMobileToken?: () => string;

  @State() form: FormHook<VehicleQuotation>;

  // #endregion
  render() {
    return (
      <Host>
        <div part={`vehicle-quotation-${this.structure?.data?.theme}`}>
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
                  formElementMapper={vehicleQuotationElements}
                  successMessage={this.locale['Form submitted successfully.']}
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
