import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import specificationSchema from '~locales/vehicleLookup/specification/type';

import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { LookupHead, VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-specification',
  styleUrl: 'vehicle-specification.css',
})
export class VehicleSpecification implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

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
      hasRecords: !!(this.vehicleLookup?.vehicleSpecification || this.vehicleLookup?.vehicleVariantInfo || this.vehicleLookup?.identifiers),
    });
    this.currentVerdict = verdict.accent;

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
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <LookupHead title={texts.vehicleSpecification} verdict={verdict} />
          <div class="lookup-slide-clip">
            <div class="lookup-slide">
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

                  <MaterialCard desc={productionDate ? productionDate : ''} title={texts?.productionDate} minWidth="250px" />

                  <MaterialCard desc={this?.vehicleLookup?.vehicleVariantInfo?.sfx?.trim() || ''} title={texts?.sfx} minWidth="250px" />
                </div>
              </flexible-container>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
