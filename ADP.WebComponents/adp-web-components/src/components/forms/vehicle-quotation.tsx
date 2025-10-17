import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import { Grecaptcha } from '~lib/recaptcha';

import vehicleQuotationSchema from '~locales/forms/vehicleQuotation/type';

import { vehicleQuotationElementNames, vehicleQuotationElements } from './vehicle-quotation/element-mapper';
import { VehicleQuotation, VehicleQuotationFormLocale, vehicleQuotationInputsValidation } from './vehicle-quotation/validations';

import { FormHook, FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import { fetchJson } from '~lib/fetch-json';
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

  @State() locale: VehicleQuotationFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...vehicleQuotationSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.vehicleQuotation', vehicleQuotationSchema)]);

    this.locale = { sharedFormLocales: sharedLocales, ...locale };

    this.localeLanguage = newLanguage;

    this.form?.rerender({ rerenderAll: true });
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
  @Prop({ mutable: true }) structure: FormElementStructure<vehicleQuotationElementNames> | undefined;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    this.isLoading = isLoading;
    if (this.loadingChanges) this.loadingChanges(true);
  }

  setErrorCallback(error: any) {
    const message = error.message || this.locale?.sharedFormLocales?.errors?.wildCard || '';

    if (this.errorCallback) this.errorCallback(error, message);
    else this.errorMessage = message;
  }

  setSuccessCallback(data: any) {
    if (this.successCallback) this.successCallback(data, this.locale['Form submitted successfully.'] || '');
    else this.form.openDialog();
    const formDom = this.el.shadowRoot || this.el;

    let targetElement = formDom instanceof ShadowRoot ? formDom.firstElementChild : formDom;

    targetElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  async componentWillLoad() {
    if (!this.language) this.language = getLanguageFromUrl();
  }

  async formSubmit(formValues: VehicleQuotation) {
    try {
      this.setIsLoading(true);

      const nameContactedVehicles = this.structure?.data?.nameContactedVehicles;

      const payload: any = {
        name: formValues.name,
        phone: formValues.phone,
        companyBranchId: formValues.dealer,
        vehicleQuotationType: this.structure?.data?.quotationType,
        preferredContactTime: formValues?.contactTime || 'NotSpecified',
        preferredPaymentMethod: formValues?.paymentType || 'Flexible',
      };

      if (nameContactedVehicles) {
        payload.vehicle = this['vehicleList'].find(vehicle => vehicle.value === formValues.vehicle)?.label || '';
        if (formValues?.ownVehicle === 'yes') {
          const currentBrand = this['currentVehicleBrandList'].find(brand => brand.value === formValues.currentVehicleBrand)?.meta || false;
          let currentModel;

          if (currentBrand) currentModel = currentBrand.models.find(model => `${model.id}` === formValues.currentVehicleModel) || false;
          payload.currentOrTradeInVehicle = `${currentBrand?.name || this.locale.Other} - ${currentModel?.name || this.locale.Other}`;
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

      const response = await fetch(requestEndpoint, {
        headers,
        method: 'POST',
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        const result = await response?.json();
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
    if (this.isMobileForm) return;

    if (this.structure) {
      await this.changeLanguage(this.language);
    } else {
      if (this.gistId) {
        const [newGistStructure] = await Promise.all([gistLoader(this.gistId), this.changeLanguage(this.language)]);
        this.structure = newGistStructure as FormElementStructure<vehicleQuotationElementNames>;
      } else if (this.structureUrl) {
        await this.changeLanguage(this.language);
        const [newGistStructure] = await Promise.all([fetchJson<FormElementStructure<vehicleQuotationElementNames>>(this.structureUrl), this.changeLanguage(this.language)]);
        this.structure = newGistStructure;
      }
    }
    this.localeLanguage = this.language;

    try {
      const key = this.structure?.data?.recaptchaKey;
      if (key) {
        const script = document.createElement('script');
        script.src = `https://www.google.com/recaptcha/api.js?render=${key}&hl=${this.language}`;
        script.async = true;
        script.defer = true;

        document.head.appendChild(script);
      }
    } catch (error) {
      console.log(error);
    }

    this.form = new FormHook(this, vehicleQuotationInputsValidation);
  }

  @Prop() isMobileForm: boolean = false;
  @Prop() getMobileToken?: () => string;

  @State() form: FormHook<VehicleQuotation>;

  @State() structureRendered = false;

  // #endregion
  render() {
    return (
      <Host>
        <div part={`vehicle-quotation-${this.structure?.data?.theme}`}>
          <div part="form-container" class="relative min-h-[200px]">
            <div
              part="form-loader-container"
              class={cn('form-loader-container absolute top-0 left-0 w-full h-[200px] pointer-events-none flex justify-center items-center transition-opacity duration-500', {
                'opacity-0': this.structureRendered,
              })}
            >
              <LoaderIcon part="form-loader-icon" class="img" />
            </div>
            <flexible-container onlyForMounting isOpened={this.structureRendered}>
              {!!this.form && (
                <form-structure
                  form={this.form}
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
