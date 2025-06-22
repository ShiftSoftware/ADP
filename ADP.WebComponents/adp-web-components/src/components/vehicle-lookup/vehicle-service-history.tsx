import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

import { InformationTableColumn } from '../components/information-table';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-service-history',
  styleUrl: 'vehicle-service-history.css',
})
export class VehicleServiceHistory implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof ServiceHistorySchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...ServiceHistorySchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.serviceHistory', ServiceHistorySchema)]);
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
    const tableHeaders: InformationTableColumn[] = [
      {
        width: 400,
        key: 'branchName',
        label: this.locale.branch,
      },
      {
        width: 200,
        key: 'companyName',
        label: this.locale.dealer,
      },
      {
        width: 200,
        key: 'invoiceNumber',
        label: this.locale.invoiceNumber,
      },
      {
        width: 200,
        key: 'serviceDate',
        label: this.locale.date,
      },
      {
        width: 400,
        key: 'serviceType',
        label: this.locale.serviceType,
      },
      {
        width: 200,
        key: 'mileage',
        label: this.locale.odometer,
      },
    ];

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          isLoading={this.isLoading}
          coreOnly={this.coreOnly}
          vin={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="overflow-x-auto">
            <information-table rows={this.vehicleLookup?.serviceHistory || []} headers={tableHeaders} isLoading={this.isLoading}></information-table>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
