import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { Grecaptcha } from '~types/general';
import { AppStates, MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { getVehicleLookup } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';

import warrantySchema from '~locales/vehicleLookup/warranty/type';

import CardsContainer from './components/CardsContainer';
import { VehicleInfoLayout } from '../../features/vehicle-info-layout/vehicle-info-layout';
import { InformationTableColumn } from '../components/information-table';

import XIcon from './assets/x-mark.svg';
import CheckIcon from './assets/check.svg';

import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

let mockData: MockJson<VehicleLookupDTO> = {};

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: true,
  tag: 'vehicle-warranty-details',
  styleUrl: 'vehicle-warranty-details.css',
})
export class VehicleWarrantyDetails {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() showSsc: boolean = false;
  @Prop() queryString: string = '';
  @Prop() recaptchaKey: string = '';
  @Prop() coreOnly: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() showWarranty: boolean = false;
  @Prop() unauthorizedSscLookupBaseUrl: string = '';
  @Prop() unauthorizedSscLookupQueryString: string = '';

  @Prop() cityId?: string = null;
  @Prop() cityIntegrationId?: string = null;
  @Prop() companyId?: string = null;
  @Prop() companyIntegrationId?: string = null;
  @Prop() companyBranchId?: string = null;
  @Prop() companyBranchIntegrationId?: string = null;
  @Prop() userId?: string = null;
  @Prop() brandIntegrationId: string = null;

  @Prop() customerName?: string = null;
  @Prop() customerPhone?: string = null;
  @Prop() customerEmail?: string = null;

  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() state: AppStates = 'idle';
  @State() externalVin?: string = null;
  @State() showRecaptcha: boolean = false;
  @State() errorMessage?: ErrorKeys = null;
  @State() vehicleInformation?: VehicleLookupDTO;
  @State() unInvoicedByBrokerName?: string = null;
  @State() checkingUnauthorizedSSC: boolean = false;
  @State() recaptchaRes: { hasSSC: boolean; message: 'noPendingSSC' | 'pendingSSC' } | null = null;
  @State() headers: any = {};

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof warrantySchema> = warrantySchema.getDefault();

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;
  private recaptchaIntervalRef: ReturnType<typeof setInterval>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup.warranty', warrantySchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  private handleSettingData(response: VehicleLookupDTO) {
    if (response.warranty === null)
      response.warranty = {
        warrantyEndDate: '',
        warrantyStartDate: '',
        hasActiveWarranty: false,
        hasExtendedWarranty: false,
        extendedWarrantyEndDate: '',
        extendedWarrantyStartDate: '',
      };
    if (response.ssc === null) response.ssc = [];

    this.unInvoicedByBrokerName = null;

    if (response?.saleInformation?.broker !== null && response?.saleInformation?.broker?.invoiceDate === null)
      this.unInvoicedByBrokerName = response.saleInformation.broker.brokerName;

    this.vehicleInformation = response;
  }

  private async handleInitializingRecaptcha(vin, scopedTimeoutRef) {
    if (this.vehicleInformation?.isAuthorized === false && this.showSsc && this.recaptchaKey !== '') {
      grecaptcha.reset();
      // await new Promise(r => setTimeout(r, 400));
      this.recaptchaIntervalRef = setInterval(async () => {
        const recaptchaResponse = grecaptcha.getResponse();
        if (recaptchaResponse) {
          clearInterval(this.recaptchaIntervalRef);

          if (this.isDev) {
            this.checkingUnauthorizedSSC = true;

            this.showRecaptcha = false;

            await new Promise(r => setTimeout(r, 3000));

            this.checkingUnauthorizedSSC = false;

            const hasPendingSSC = Math.random() < 0.5 ? false : true;

            this.recaptchaRes = {
              hasSSC: hasPendingSSC,
              message: hasPendingSSC ? 'pendingSSC' : 'noPendingSSC',
            };
          } else {
            this.checkingUnauthorizedSSC = true;

            this.showRecaptcha = false;

            const response = await fetch(`${this.unauthorizedSscLookupBaseUrl}${vin}/${this.vehicleInformation?.sscLogId}?${this.unauthorizedSscLookupQueryString}`, {
              signal: this.abortController.signal,
              headers: {
                ...this.headers,
                'Ssc-Recaptcha-Token': recaptchaResponse,
              },
            });

            const vinResponse = await response.json();

            if (vinResponse && this.networkTimeoutRef === scopedTimeoutRef) {
              this.checkingUnauthorizedSSC = false;

              const hasPendingSSC = vinResponse.sscLookupStatus === 1;

              this.recaptchaRes = {
                hasSSC: hasPendingSSC,
                message: hasPendingSSC ? 'pendingSSC' : 'noPendingSSC',
              };
            } else throw new Error('wrongResponseFormat');
          }
        }
      }, 500);
      this.showRecaptcha = true;
    } else {
      this.showRecaptcha = false;
    }
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    this.recaptchaRes = null;
    this.headers = headers;
    clearTimeout(this.networkTimeoutRef);
    clearInterval(this.recaptchaIntervalRef);
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
        scopedTimeoutRef = setTimeout(r, 600);
        this.networkTimeoutRef = scopedTimeoutRef;
      });

      // @ts-ignore
      const vehicleResponse = isVinRequest ? await getVehicleLookup(this, { scopedTimeoutRef, vin, mockData }, headers) : newData;

      if (this.networkTimeoutRef === scopedTimeoutRef) {
        if (!vehicleResponse) throw new Error('wrongResponseFormat');

        this.handleSettingData(vehicleResponse);
        this.handleInitializingRecaptcha(vin, scopedTimeoutRef);
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

  async componentDidLoad() {
    if (this.recaptchaKey !== '') {
      const script = document.createElement('script');

      script.src = 'https://www.google.com/recaptcha/api.js';
      script.async = true;
      script.defer = true;
      script.onload = () => console.log('reCAPTCHA script loaded');

      document.head.appendChild(script);
    }
  }

  @Method()
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    mockData = newMockData;
  }

  @Watch('state')
  async loadingListener() {
    if (this.loadingStateChange) this.loadingStateChange(this.state.includes('loading'));
  }

  render() {
    const isLoading = this.state.includes('loading');
    const isError = this.state.includes('error');

    const tableHeaders: InformationTableColumn[] = [
      {
        width: 200,
        key: 'sscTableCode',
        label: this.locale.sscTableCode,
      },
      {
        width: 400,
        key: 'sscTableDescription',
        label: this.locale.sscTableDescription,
      },
      {
        width: 200,
        key: 'sscTableRepairStatus',
        label: this.locale.sscTableRepairStatus,
      },
      {
        width: 200,
        key: 'sscTableOPCode',
        label: this.locale.sscTableOPCode,
      },
      {
        width: 200,
        key: 'sscTablePartNumber',
        label: this.locale.sscTablePartNumber,
      },
    ];

    const rows = !this.vehicleInformation?.ssc
      ? []
      : this.vehicleInformation?.ssc.map(sscItem => ({
          sscTableCode: sscItem?.sscCode,
          sscTableDescription: sscItem?.description,
          sscTableRepairStatus: () => (
            <div class="table-cell-container">
              <img class="table-status-icon" src={sscItem?.repaired ? CheckIcon : XIcon} /> {sscItem?.repairDate}
            </div>
          ),
          sscTableOPCode: () => (
            <div class="table-cell-container table-cell-labors-container">
              {!!sscItem?.labors.length
                ? sscItem?.labors.map(labor => (
                    <div key={labor?.laborCode} class="success">
                      {labor?.laborCode}
                    </div>
                  ))
                : '...'}
            </div>
          ),
          sscTablePartNumber: () => (
            <div class="table-cell-container table-cell-parts-container">
              {!!sscItem?.parts.length
                ? sscItem?.parts.map(part => (
                    <div key={part?.partNumber} class={part?.isAvailable ? 'success' : 'reject'}>
                      {part?.partNumber}
                    </div>
                  ))
                : '...'}
            </div>
          ),
        }));

    const templateRow = {
      sscTableOPCode: () => <div class="h-[25px]" />,
      sscTablePartNumber: () => <div class="h-[25px]" />,
      sscTableRepairStatus: () => <div class="h-[25px]" />,
    };

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
          <div class="p-[16px]">
            {this.showWarranty && (
              <CardsContainer
                isLoading={isLoading}
                warrantyLocale={this.locale}
                vehicleInformation={this.vehicleInformation}
                isAuthorized={this.vehicleInformation?.isAuthorized}
                unInvoicedByBrokerName={this.unInvoicedByBrokerName}
              />
            )}

            <div class="h-[8px]" />

            <flexible-container isOpened={this.showRecaptcha} classes={cn('w-fit mx-auto shift-skeleton', { loading: !this.showRecaptcha })}>
              <div style={{ height: 'auto' }} class="recaptcha-container">
                <slot></slot>
              </div>

              {['data', 'data-loading'].includes(this.state) && this.recaptchaRes && (
                <div class={cn('recaptcha-response', !this.recaptchaRes?.hasSSC ? 'success-card' : 'reject-card ')}>{this.locale[this.recaptchaRes?.message]}</div>
              )}
            </flexible-container>
          </div>
          <flexible-container isOpened={this.checkingUnauthorizedSSC} classes="w-fit mx-auto">
            <div class="pt-[16px]">
              <div class="flex shift-skeleton flex-col gap-[8px]">
                <strong>{this.locale.checkingTMC}</strong>
                <div class="relative pt-[40px]">
                  <loading-spinner isLoading={this.checkingUnauthorizedSSC}></loading-spinner>
                </div>
              </div>
            </div>
          </flexible-container>
          <div class="mt-[32px] mx-auto w-fit max-w-full">
            <div class="bg-[#f6f6f6] h-[50px] flex items-center justify-center px-[16px] font-bold text-[18px]">{this.locale.sscCampings}</div>
            <div class="overflow-x-auto">
              <information-table isLoading={isLoading} templateRow={templateRow} rows={rows} headers={tableHeaders}></information-table>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
