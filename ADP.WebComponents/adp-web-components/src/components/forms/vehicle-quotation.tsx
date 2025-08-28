import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { FormHook } from '~features/form-hook/form-hook';

import vehicleQuotationSchema from '~locales/forms/vehicleQuotation/type';

import { VehicleQuotationStructures } from './vehicle-quotation/structure';
import { vehicleQuotationElementNames, vehicleQuotationElements } from './vehicle-quotation/element-mapper';
import { VehicleQuotation, VehicleQuotationFormLocale, vehicleQuotationInputsValidation } from './vehicle-quotation/validations';

import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: false,
  tag: 'vehicle-quotation-form',
  styleUrl: 'vehicle-quotation/themes.css',
})
export class VehicleQuotationForm implements FormHookInterface<VehicleQuotation>, MultiLingual {
  // ====== Start Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: VehicleQuotationFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...vehicleQuotationSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.vehicleQuotation', vehicleQuotationSchema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.form.rerender({ rerenderAll: true });
  }
  // ====== End Localization

  // ====== Start Form Hook logic
  @State() isLoading: boolean;
  @State() errorMessage: string;

  @Prop() theme: string;
  @Prop() gistId?: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() structure: FormElementStructure<vehicleQuotationElementNames> | undefined;

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
    await this.changeLanguage(this.language);

    if (this.structure) return;

    if (this.gistId) this.structure = await gistLoader(this.gistId);
    else if (this.theme === 'tiq') this.structure = VehicleQuotationStructures.tiq;
  }

  async formSubmit(formValues: VehicleQuotation) {
    try {
      console.log(formValues);

      this.setIsLoading(true);

      await new Promise(r => setTimeout(r, 3000));

      this.setSuccessCallback({});

      this.form.successAnimation();
      setTimeout(() => {
        this.form.reset();
      }, 1000);
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // ====== End Form Hook logic

  // ====== component logic

  form = new FormHook(this, vehicleQuotationInputsValidation);

  render() {
    return (
      <Host
        class={cn({
          [`vehicle-quotation-${this.theme}`]: this.theme,
        })}
      >
        <form-structure
          form={this.form}
          language={this.language}
          formLocale={this.locale}
          structure={this.structure}
          isLoading={this.isLoading}
          errorMessage={this.errorMessage}
          formElementMapper={vehicleQuotationElements}
        >
          <slot></slot>
        </form-structure>
      </Host>
    );
  }
}
