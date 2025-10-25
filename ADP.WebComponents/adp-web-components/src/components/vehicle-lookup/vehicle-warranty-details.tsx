import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { Grecaptcha } from '~lib/recaptcha';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import warrantySchema from '~locales/vehicleLookup/warranty/type';

import CardsContainer from './components/CardsContainer';
import { InformationTableColumn } from '../components/information-table';

import XIcon from '~assets/x-mark.svg';
import CheckIcon from '~assets/check.svg';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: true,
  tag: 'vehicle-warranty-details',
  styleUrl: 'vehicle-warranty-details.css',
})
export class VehicleWarrantyDetails implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof warrantySchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...warrantySchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.warranty', warrantySchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

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
    this.recaptchaRes = null;
    clearInterval(this.recaptchaIntervalRef);
    await setVehicleLookupData(this, newData, headers, {
      beforeAssignment: async (response, { scopedTimeoutRef }) => {
        if (response?.saleInformation?.broker !== null && response?.saleInformation?.broker?.invoiceDate === null)
          this.unInvoicedByBrokerName = response.saleInformation.broker.brokerName;
        else this.unInvoicedByBrokerName = null;

        await this.handleInitializingRecaptcha(response, scopedTimeoutRef);

        return response;
      },
    });
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

  // #region Blazor Invokable logic
  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }
  // #endregion

  // #region Component Logic
  @Prop() showSsc: boolean = false;
  @Prop() recaptchaKey: string = '';
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

  @State() showRecaptcha: boolean = false;
  @State() unInvoicedByBrokerName?: string = null;
  @State() checkingUnauthorizedSSC: boolean = false;
  @State() recaptchaRes: {
    status: 'noRecall' | 'recallExists' | 'noApplicableVehicleFound'
  } | null = null;

  private recaptchaIntervalRef: ReturnType<typeof setInterval>;

  private async handleInitializingRecaptcha(newVehicleLookup: VehicleLookupDTO, scopedTimeoutRef) {
    if (newVehicleLookup?.isAuthorized === false && this.showSsc && this.recaptchaKey !== '') {
      grecaptcha.reset();
      // await new Promise(r => setTimeout(r, 400));
      this.recaptchaIntervalRef = setInterval(async () => {
        const recaptchaResponse = grecaptcha.getResponse();
        if (recaptchaResponse) {
          clearInterval(this.recaptchaIntervalRef);

          if (this.isDev) {
            this.checkingUnauthorizedSSC = true;

            //this.showRecaptcha = false;

            await new Promise(r => setTimeout(r, 3000));

            this.checkingUnauthorizedSSC = false;

            var randomValue = Math.random();

            this.recaptchaRes = {
              status:
                randomValue < 0.33 ? 'noRecall' :
                  (randomValue > 0.33 && randomValue < 0.66) ? 'noApplicableVehicleFound' :
                    'recallExists'
            };
          } else {
            this.checkingUnauthorizedSSC = true;

            //this.showRecaptcha = false;

            const response = await fetch(`${this.unauthorizedSscLookupBaseUrl}${newVehicleLookup?.vin}/${newVehicleLookup?.sscLogId}?${this.unauthorizedSscLookupQueryString}`, {
              signal: this.abortController.signal,
              headers: {
                ...this.headers,
                'Ssc-Recaptcha-Token': recaptchaResponse,
              },
            });

            const vinResponse = await response.json();

            if (vinResponse && this.networkTimeoutRef === scopedTimeoutRef) {
              this.checkingUnauthorizedSSC = false;

              this.recaptchaRes = {
                status: vinResponse.sscLookupStatus === 0 ? 'noRecall' :
                  vinResponse.sscLookupStatus === 1 ? 'recallExists' :
                  vinResponse.sscLookupStatus === 2 ? 'noApplicableVehicleFound' : null
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

  // #endregion

  render() {
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

    const rows = !this.vehicleLookup?.ssc
      ? []
      : this.vehicleLookup?.ssc.map(sscItem => ({
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
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="p-[16px]">
            {this.showWarranty && (
              <CardsContainer
                isLoading={this.isLoading}
                warrantyLocale={this.locale}
                vehicleInformation={this.vehicleLookup}
                isAuthorized={this.vehicleLookup?.isAuthorized}
                unInvoicedByBrokerName={this.unInvoicedByBrokerName}
              />
            )}

            <div class="h-[8px]" />

            <flexible-container isOpened={this.showRecaptcha} classes={cn('w-fit mx-auto shift-skeleton', { loading: !this.showRecaptcha || this.isLoading })}>
              <div style={{ height: 'auto' }} class="recaptcha-container">
                <slot></slot>
              </div>

              {this.recaptchaRes && (
                <div class={cn('recaptcha-response', this.recaptchaRes?.status === 'recallExists' ? 'reject-card ' : (this.recaptchaRes?.status === 'noRecall' ? 'success-card ' : 'warning-card '))}>{this.locale[this.recaptchaRes?.status]}</div>
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
              <information-table isLoading={this.isLoading} templateRow={templateRow} rows={rows} headers={tableHeaders}></information-table>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
