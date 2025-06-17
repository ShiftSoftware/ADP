import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { LanguageKeys } from '~types/locale';
import { AppStates, MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ErrorKeys, getLocaleLanguage, getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import { getVehicleLookup } from '~api/vehicleInformation';

import specificationSchema from '~locales/vehicleLookup/specification/type';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';
import { MaterialCard, MaterialCardChildren } from '../components/material-card';

let mockData: MockJson<VehicleLookupDTO> = {};

@Component({
  shadow: true,
  tag: 'vehicle-specification',
  styleUrl: 'vehicle-specification.css',
})
export class VehicleSpecification {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() coreOnly: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof specificationSchema> = specificationSchema.getDefault();

  @State() state: AppStates = 'idle';
  @State() externalVin?: string = null;
  @State() errorMessage?: ErrorKeys = null;
  @State() vehicleInformation?: VehicleLookupDTO;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup.specification', specificationSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    clearTimeout(this.networkTimeoutRef);
    if (this.abortController) this.abortController.abort();
    this.abortController = new AbortController();
    let scopedTimeoutRef: ReturnType<typeof setTimeout>;

    const isVinRequest = typeof newData === 'string';

    const vin = isVinRequest ? newData : newData?.vin;
    this.externalVin = vin;

    try {
      if (!vin || vin.trim().length === 0) {
        this.state = 'idle';
        return;
      }

      if (this.state === 'data' || this.state === 'error') {
        this.state = (this.state + '-loading') as 'data-loading' | 'error-loading';
      } else this.state = 'loading';

      await new Promise(r => {
        scopedTimeoutRef = setTimeout(r, 700);
        this.networkTimeoutRef = scopedTimeoutRef;
      });

      // @ts-ignore
      const vehicleResponse = isVinRequest ? await getVehicleLookup(this, { scopedTimeoutRef, vin, mockData }, headers) : newData;

      if (this.networkTimeoutRef === scopedTimeoutRef) {
        if (!vehicleResponse) throw new Error('wrongResponseFormat');
        this.vehicleInformation = vehicleResponse;
      }

      this.errorMessage = null;
      this.state = 'data';
    } catch (error) {
      if (error && error?.name === 'AbortError') return;
      if (this.errorCallback) this.errorCallback(error.message);
      console.error(error);
      this.setErrorMessage(error.message);
    }
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    this.state = 'error';
    this.vehicleInformation = null;
    this.errorMessage = message;
  }

  @Method()
  async fetchData(requestedVin: string = this.externalVin, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }

  @Watch('state')
  async loadingListener() {
    if (this.loadingStateChange) this.loadingStateChange(this.state.includes('loading'));
  }

  @Method()
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    mockData = newMockData;
  }

  render() {
    const texts = this.locale;

    let productionDate: string | null = null;

    try {
      if (this.vehicleInformation?.vehicleSpecification?.productionDate) {
        const productionDateObj = new Date(this.vehicleInformation?.vehicleSpecification?.productionDate);

        productionDate = productionDateObj.toLocaleDateString(this.sharedLocales.language, {
          year: 'numeric',
          month: 'long',
        });
      }
    } catch (error) {
      productionDate = null;
    }

    const isLoading = this.state.includes('loading');
    const isError = this.state.includes('error');

    return (
      <Host>
        <VehicleInfoLayout
          isError={isError}
          isLoading={isLoading}
          coreOnly={this.coreOnly}
          vin={this.vehicleInformation?.vin}
          direction={this.sharedLocales.direction}
          errorMessage={this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
        >
          <flexible-container>
            <div class="flex p-[16px] [&>div]:grow overflow-auto gap-[16px] items-stretch justify-center md:justify-between flex-wrap">
              <MaterialCard class="grow" title={texts?.model} minWidth="300px">
                <MaterialCardChildren
                  class="text-center"
                  hidden={!this?.vehicleInformation?.vehicleVariantInfo?.modelCode?.trim() && !this?.vehicleInformation?.vehicleSpecification?.modelDescription?.trim()}
                >
                  {this?.vehicleInformation?.vehicleVariantInfo?.modelCode?.trim() || '...'} <br class="my-2" />
                  {this?.vehicleInformation?.vehicleSpecification?.modelDescription?.trim() || '...'}
                </MaterialCardChildren>
              </MaterialCard>

              <MaterialCard class="grow" title={texts?.variant} minWidth="300px">
                <MaterialCardChildren
                  class="text-center"
                  hidden={!this?.vehicleInformation?.identifiers?.variant?.trim() && !this?.vehicleInformation?.vehicleSpecification?.variantDescription?.trim()}
                >
                  {this?.vehicleInformation?.identifiers?.variant?.trim() || '...'} <br />
                  {this?.vehicleInformation?.vehicleSpecification?.variantDescription?.trim() || '...'}
                </MaterialCardChildren>
              </MaterialCard>

              <MaterialCard desc={this?.vehicleInformation?.identifiers?.katashiki?.trim() || '...'} title={texts?.katashiki} minWidth="250px" />

              <MaterialCard desc={this?.vehicleInformation?.vehicleVariantInfo?.modelYear?.toString()?.trim() || '...'} title={texts?.modelYear} minWidth="250px" />

              <MaterialCard desc={!!productionDate ? productionDate : '...'} title={texts?.productionDate} minWidth="250px" />

              <MaterialCard desc={this?.vehicleInformation?.vehicleVariantInfo?.sfx?.trim() || '...'} title={texts?.sfx} minWidth="250px" />
            </div>
          </flexible-container>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
