import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

import { InformationTableColumn } from '../components/information-table';

import { ServiceHistorySubRow } from './components/service-history-sub-row';

import { LookupHead, VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-service-history',
  styleUrl: 'vehicle-service-history.css',
})
export class VehicleServiceHistory implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

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
  @Prop() today?: string;
  @Prop() disableVinValidation: boolean = false;
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
  async fetchVin(newData: VehicleLookupDTO | string, headers: any = {}) {
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

  // #region Verdict

  /**
   * Fires whenever the panel's verdict changes — including back to idle when the vehicle is
   * cleared — so a composite that draws one card for several panels can colour its accent from the
   * active one.
   */
  @Event() verdictChange: EventEmitter<VerdictState>;

  private lastVerdict?: VerdictState;
  private currentVerdict: VerdictState = 'idle';

  private announceVerdict() {
    if (this.currentVerdict === this.lastVerdict) return;
    this.lastVerdict = this.currentVerdict;
    this.verdictChange.emit(this.currentVerdict);
  }

  componentDidRender() {
    this.announceVerdict();
  }

  // #endregion

  render() {
    const verdict = recordVerdict({
      locale: this.locale.sharedLocales,
      error: this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined,
      vehicleLoaded: !!this.vehicleLookup?.vin,
      authorized: this.vehicleLookup?.isAuthorized,
      hasRecords: (this.vehicleLookup?.serviceHistory?.length ?? 0) > 0,
    });
    this.currentVerdict = verdict.accent;

    const tableHeaders: InformationTableColumn[] = [
      { key: 'branchName', label: this.locale.branch },
      { key: 'companyName', label: this.locale.dealer, nowrap: true },
      { key: 'invoiceNumber', label: this.locale.invoiceNumber, nowrap: true },
      { key: 'serviceDate', label: this.locale.date, nowrap: true },
      { key: 'serviceType', label: this.locale.serviceType, maxWidth: 420 },
      { key: 'mileage', label: this.locale.odometer, nowrap: true },
    ];

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <LookupHead title={this.locale.serviceHistory} verdict={verdict} />
          <div class="lookup-slide-clip">
            <div class="lookup-slide">
              {/* The table's auto-width mode renders a bare <table>, so its height would jump on the frame the
                  rows land; the container measures the change and animates it, as the table's own flex mode does. */}
              <flexible-container>
                <div class="overflow-x-auto">
                  <information-table
                    size="small"
                    allowAutoWidth
                    scrollExpandedIntoView
                    expandUsingEntireRow
                    headers={tableHeaders}
                    isLoading={this.isLoading}
                    rows={this.vehicleLookup?.serviceHistory || []}
                    subRowRenderer={(row: any) => <ServiceHistorySubRow row={row} locale={this.locale} />}
                  />
                </div>
              </flexible-container>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
