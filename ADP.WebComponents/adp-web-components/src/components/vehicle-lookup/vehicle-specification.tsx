import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import specificationSchema from '~locales/vehicleLookup/specification/type';

import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-specification',
  styleUrl: 'vehicle-specification.css',
})
export class VehicleSpecification implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof specificationSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...specificationSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.specification', specificationSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // ====== End Localization

  // ====== Start Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // ====== End Vehicle info layout prop

  // ====== Start Vehicle Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() vehicleLookup?: VehicleLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: VehicleLookupMock) {
    this.mockData = newMockData;
  }

  @Method()
  async fetchData(newData: VehicleLookupDTO | string, headers: any = {}) {
    await setVehicleLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  // ====== End Vehicle Lookup Component Shared Logic

  render() {
    const texts = this.locale;

    let productionDate: string | null = null;

    try {
      if (this.vehicleLookup?.vehicleSpecification?.productionDate) {
        const productionDateObj = new Date(this.vehicleLookup?.vehicleSpecification?.productionDate);

        productionDate = productionDateObj.toLocaleDateString(this.locale.sharedLocales.language, {
          year: 'numeric',
          month: 'long',
        });
      }
    } catch (error) {
      productionDate = null;
    }

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <flexible-container>
            <div class="flex p-[16px] [&>div]:grow overflow-auto gap-[16px] items-stretch justify-center md:justify-between flex-wrap">
              <MaterialCard class="grow" title={texts?.model} minWidth="300px">
                <MaterialCardChildren
                  class="text-center"
                  hidden={!this?.vehicleLookup?.vehicleVariantInfo?.modelCode?.trim() && !this?.vehicleLookup?.vehicleSpecification?.modelDescription?.trim()}
                >
                  {this?.vehicleLookup?.vehicleVariantInfo?.modelCode?.trim() || ''} <br class="my-2" />
                  {this?.vehicleLookup?.vehicleSpecification?.modelDescription?.trim() || ''}
                </MaterialCardChildren>
              </MaterialCard>

              <MaterialCard class="grow" title={texts?.variant} minWidth="300px">
                <MaterialCardChildren
                  class="text-center"
                  hidden={!this?.vehicleLookup?.identifiers?.variant?.trim() && !this?.vehicleLookup?.vehicleSpecification?.variantDescription?.trim()}
                >
                  {this?.vehicleLookup?.identifiers?.variant?.trim() || ''} <br />
                  {this?.vehicleLookup?.vehicleSpecification?.variantDescription?.trim() || ''}
                </MaterialCardChildren>
              </MaterialCard>

              <MaterialCard desc={this?.vehicleLookup?.identifiers?.katashiki?.trim() || ''} title={texts?.katashiki} minWidth="250px" />

              <MaterialCard desc={this?.vehicleLookup?.vehicleVariantInfo?.modelYear?.toString()?.trim() || ''} title={texts?.modelYear} minWidth="250px" />

              <MaterialCard desc={!!productionDate ? productionDate : ''} title={texts?.productionDate} minWidth="250px" />

              <MaterialCard desc={this?.vehicleLookup?.vehicleVariantInfo?.sfx?.trim() || ''} title={texts?.sfx} minWidth="250px" />
            </div>
          </flexible-container>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
