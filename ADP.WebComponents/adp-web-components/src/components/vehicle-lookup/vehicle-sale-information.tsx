import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import saleInformationSchema from '~locales/vehicleLookup/saleInformation/type';

import { MaterialCard } from '../components/material-card';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';
import type { VehicleSaleInformation as VehicleSaleInformationType } from '~types/generated/vehicle-lookup/vehicle-sale-information';
import cn from '~lib/cn';
import { TriangleAlertIcon } from '~assets/triangle-alert';

@Component({
  shadow: true,
  tag: 'vehicle-sale-information',
  styleUrl: 'vehicle-sale-information.css',
})
export class VehicleSaleInformation implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof saleInformationSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...saleInformationSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.saleInformation', saleInformationSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion

  // #region Blazor Invokable logic

  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }

  // #endregion

  // #region Vehicle Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop() loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop() loadedResponse?: BlazorInvokableFunction<(response: VehicleLookupDTO) => void>;

  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @Prop() hiddenFields: string = '';
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
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  private formatDate(dateString?: string | null) {
    if (!dateString) return '';
    try {
      const date = new Date(dateString);
      if (Number.isNaN(date.getTime())) return '';
      return date.toLocaleDateString(this.locale.sharedLocales.language, { year: 'numeric', month: 'long', day: '2-digit' });
    } catch {
      return '';
    }
  }

  render() {
    const hiddenFields = this.hiddenFields?.split(',')?.map(field => field.trim()) || [];

    const texts = this.locale;

    // @ts-ignore
    const sale: VehicleSaleInformationType = this.vehicleLookup?.saleInformation || {};

    const getText = (v?: unknown) => (v ?? '').toString().trim();

    const hasEndCustomerInfo = !!sale?.endCustomer || !this.vehicleLookup;

    const FIELDS: { fieldName: string; title: string; value: string }[] = [
      {
        fieldName: 'companyName',
        title: texts.companyName,
        value: getText(sale?.companyName),
      },
      {
        fieldName: 'branchName',
        title: texts.branchName,
        value: getText(sale?.branchName),
      },
      // End customer
      {
        fieldName: 'endCustomerName',
        title: texts.endCustomerName,
        value: getText(sale?.endCustomer?.name),
      },
      {
        fieldName: 'endCustomerPhone',
        title: texts.endCustomerPhone,
        value: getText(sale?.endCustomer?.phone),
      },
      {
        fieldName: 'endCustomerIdNumber',
        title: texts.endCustomerIdNumber,
        value: getText(sale?.endCustomer?.idNumber),
      },
      {
        fieldName: 'customerAccountNumber',
        title: texts.customerAccountNumber,
        value: getText(sale?.customerAccountNumber),
      },
      {
        fieldName: 'customerID',
        title: texts.customerID,
        value: getText(sale?.customerID),
      },
      // Broker
      {
        fieldName: 'brokerName',
        title: texts.brokerName,
        value: getText(sale?.broker?.brokerName),
      },
      {
        fieldName: 'brokerInvoiceNumber',
        title: texts.brokerInvoiceNumber,
        value: getText(sale?.broker?.invoiceNumber),
      },
      {
        fieldName: 'brokerInvoiceDate',
        title: texts.brokerInvoiceDate,
        value: this.formatDate(sale?.broker?.invoiceDate),
      },
      {
        fieldName: 'warrantyActivationDate',
        title: texts.warrantyActivationDate,
        value: getText(sale?.warrantyActivationDate),
      },
      {
        fieldName: 'invoiceDate',
        title: texts.invoiceDate,
        value: this.formatDate(sale?.invoiceDate),
      },
      {
        fieldName: 'invoiceNumber',
        title: texts.invoiceNumber,
        value: this.formatDate(sale?.invoiceNumber),
      },
    ];

    const filteredFields = FIELDS.filter(field => !hiddenFields.includes(field.fieldName));

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
            <flexible-container classes={cn({ loading: this.isLoading })} isOpened={!hasEndCustomerInfo}>
              <div class="p-[16px] mx-auto !pb-0 max-w-[400px]">
                <div class="shift-skeleton">
                  <div class="card warning-card">
                    <TriangleAlertIcon class="size-[22px]" />

                    <p class="ps-">{texts['Vehicle has no end customer.'] || ''}</p>
                  </div>
                </div>
              </div>
            </flexible-container>

            <div class="flex p-[16px] [&>div]:grow overflow-auto gap-[16px] items-stretch justify-center md:justify-between flex-wrap">
              {filteredFields.map(field => (
                <MaterialCard title={field.title} desc={field.value} minWidth="250px" />
              ))}
            </div>
          </flexible-container>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
