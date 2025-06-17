import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { MockJson } from '~types/components';
import { LanguageKeys } from '~types/locale';
import MultiLingual from '~types/interfaces/multi-lingual';
import VehicleLookupComponent from '~types/interfaces/vehicle-lookup-component';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import ComponentLocale from '~lib/component-locale';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, sharedLocalesSchema } from '~lib/get-local-language';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

import { VehicleInfoLayoutInterface } from '../components/vehicle-info-layout';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~api/vehicleInformation';

@Component({
  tag: 'claim-temp',
  styleUrl: 'claim-temp.css',
  shadow: true,
})
export class ClaimTemp implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent {
  // ====== Start Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof dynamicClaimSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...dynamicClaimSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.claimableItems', dynamicClaimSchema)]);
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
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    this.mockData = newMockData;
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
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

  @Method()
  async fetchData(requestedVin: string, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }
  // ====== End Vehicle Lookup Component Shared Logic

  render() {
    return (
      <Host>
        {this.locale.claim}
        <slot></slot>
      </Host>
    );
  }
}
