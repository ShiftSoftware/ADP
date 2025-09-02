import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { Grecaptcha } from '~lib/recaptcha';

import vehicleQuotationSchema from '~locales/forms/vehicleQuotation/type';

import { VehicleQuotationStructures } from './vehicle-quotation/structure';
import { vehicleQuotationElementNames, vehicleQuotationElements } from './vehicle-quotation/element-mapper';
import { VehicleQuotation, VehicleQuotationFormLocale, vehicleQuotationInputsValidation } from './vehicle-quotation/validations';

import { FormHook } from '~features/form-hook/form-hook';
import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: false,
  tag: 'vehicle-quotation-form',
  styleUrl: 'vehicle-quotation/themes.css',
})
export class VehicleQuotationForm implements FormHookInterface<VehicleQuotation>, MultiLingual {
  // ====== Start Localization
  @Prop({ mutable: true, reflect: true }) language: LanguageKeys = 'en';

  @State() locale: VehicleQuotationFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...vehicleQuotationSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.vehicleQuotation', vehicleQuotationSchema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.localeLanguage = newLanguage;

    this.form.rerender({ rerenderAll: true });
  }
  // ====== End Localization

  // ====== Start Form Hook logic
  @State() errorMessage: string;
  @State() isLoading: boolean = false;
  @State() localeLanguage: LanguageKeys;

  @Prop() theme: string;
  @Prop() gistId?: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<vehicleQuotationElementNames> | undefined;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    this.isLoading = isLoading;
    if (this.loadingChanges) this.loadingChanges(true);
  }

  setErrorCallback(error: any) {
    if (error?.message) this.errorMessage = error.message;
    if (this.errorCallback) this.errorCallback(error);
  }

  setSuccessCallback(data: any) {
    if (this.successCallback) this.successCallback(data);
  }

  async componentWillLoad() {
    this.language = getLanguageFromUrl();

    if (this.structure) {
      await this.changeLanguage(this.language);
    } else {
      if (this.gistId) {
        const [newGistStructure] = await Promise.all([gistLoader(this.gistId), this.changeLanguage(this.language)]);
        this.structure = newGistStructure as FormElementStructure<vehicleQuotationElementNames>;
      } else {
        await this.changeLanguage(this.language);
        if (this.theme === 'tiq') this.structure = VehicleQuotationStructures.tiq;
      }
    }
    this.localeLanguage = this.language;
  }

  async formSubmit(formValues: VehicleQuotation) {
    try {
      this.setIsLoading(true);

      const nameContactedVehicles = this.structure.data?.nameContactedVehicles;

      const payload: any = {
        name: formValues.name,
        phone: formValues.phone,
        companyBranchId: formValues.dealer,
        preferredPaymentMethod: formValues.paymentType,
        vehicleQuotationType: this.structure.data?.quotationType,
      };

      if (nameContactedVehicles) {
        payload.vehicle = this['vehicleList'].find(vehicle => `${vehicle.ID}` === formValues.vehicle)?.Title || '';

        if (formValues?.ownVehicle === 'yes') {
          const currentBrand = this['currentVehiclesList'].find(brand => `${brand.id}` === formValues.currentVehicleBrand) || false;

          let currentModel;

          if (currentBrand) currentModel = currentBrand.models.find(model => `${model.id}` === formValues.currentVehicleModel) || false;

          payload.currentOrTradeInVehicle = `${currentBrand?.name || this.locale.Others} - ${currentModel?.name || this.locale.Others}`;
        }
      } else {
        payload.vehicle = formValues.vehicle;
        payload.additionalData = {
          DoYouOwnAVehicle: formValues?.ownVehicle === 'yes',
        };
        if (formValues?.ownVehicle === 'yes') {
          payload.additionalData.youCurrentVehicle = formValues.currentVehicleBrand || this.locale.Others;
          payload.additionalData.vehicleModel = formValues.currentVehicleModel || this.locale.Others;
        }
      }

      const token = await grecaptcha.execute(this.structure.data?.recaptchaKey, { action: 'submit' });

      const response = await fetch(this.structure.data?.requestUrl, {
        method: 'POST',
        headers: {
          'Recaptcha-Token': token,
          'Brand': this.structure.data?.brandId,
          'Accept-Language': this.localeLanguage || 'en',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      console.log(response);

      if (response.ok) {
        const result = await response?.json();
        this.setSuccessCallback(result);
        this.form.openDialog();
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
  // ====== End Form Hook logic

  // ====== component logic

  async componentDidLoad() {
    try {
      const key = this.structure.data?.recaptchaKey;
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
  }

  form = new FormHook(this, vehicleQuotationInputsValidation);

  render() {
    // @ts-ignore
    window.aa = this;
    return (
      <Host
        class={cn({
          [`vehicle-quotation-${this.theme}`]: this.theme,
        })}
      >
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
      </Host>
    );
  }
}
