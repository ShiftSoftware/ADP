import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import warrantyTimelineSchema from '~locales/vehicleLookup/warrantyTimeline/type';

import CoverageTimeline, { panelVerdict, parseHiddenFields, SaleDateItem, saleDates } from './components/CoverageTimeline';

import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

/**
 * Warranty coverage as a single dated rail: the standard warranty followed by every
 * extended coverage in sequence.
 *
 * This panel shows warranty and nothing else — no campaign table, no reCAPTCHA, no
 * unauthorized campaign lookup; those belong to `vehicle-ssc`. Hosts point their warranty
 * tab at this tag and their campaign tab at that one.
 */
@Component({
  shadow: true,
  tag: 'vehicle-warranty-timeline',
  styleUrl: 'vehicle-warranty-timeline.css',
})
export class VehicleWarrantyTimeline implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof warrantyTimelineSchema> = {
    sharedLocales: sharedLocalesSchema.getDefault(),
    ...warrantyTimelineSchema.getDefault(),
  };

  async componentWillLoad() {
    this.appliedHiddenFields = parseHiddenFields(this.hiddenFields);
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.warrantyTimeline', warrantyTimelineSchema)]);
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
  /** ISO calendar date, read as UTC. Omit it to use the wall clock. */
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

  @Method()
  async clearData() {
    this.vehicleLookup = undefined;
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Sale dates

  @Prop() hiddenFields: string = '';

  @State() appliedHiddenFields: string[] = [];
  @State() datesShut = false;

  private reconfigureGeneration = 0;
  private retainedDates: SaleDateItem[] = [];
  private retainTimer?: ReturnType<typeof setTimeout>;

  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  private dates() {
    return saleDates(this.vehicleLookup, this.vehicleLookup?.isAuthorized, this.appliedHiddenFields, this.locale);
  }

  @Watch('hiddenFields')
  async onHiddenFieldsChange(newValue: string) {
    const next = parseHiddenFields(newValue);
    const current = this.appliedHiddenFields;
    if (next.length === current.length && next.every(field => current.includes(field))) return;

    const generation = ++this.reconfigureGeneration;

    if (this.dates().open) {
      this.datesShut = true;
      await new Promise(resolve => setTimeout(resolve, this.settleMs()));
      if (generation !== this.reconfigureGeneration) return;
    }

    this.appliedHiddenFields = next;
    this.datesShut = false;
  }

  private retainDates(open: boolean, items: SaleDateItem[]) {
    if (open) {
      this.retainedDates = items;
      clearTimeout(this.retainTimer);
      this.retainTimer = undefined;
      return;
    }

    if (this.retainedDates.length && !this.retainTimer) {
      this.retainTimer = setTimeout(() => {
        this.retainTimer = undefined;
        this.retainedDates = [];
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private datesShown?: boolean;

  private announceDatesHeight() {
    const shown = this.dates().open && !this.datesShut && !this.isLoading;
    const changed = this.datesShown !== undefined && this.datesShown !== shown;
    this.datesShown = shown;
    if (!changed) return;

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  disconnectedCallback() {
    clearTimeout(this.retainTimer);
    this.heightAnnouncer?.dispose();
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
    this.announceDatesHeight();
  }

  // #endregion

  render() {
    const verdict = panelVerdict({
      locale: this.locale,
      vehicleInformation: this.vehicleLookup,
      isAuthorized: this.vehicleLookup?.isAuthorized,
      today: this.today,
      error: this.isError,
    });
    this.currentVerdict = verdict;

    const dates = this.dates();
    this.retainDates(dates.open, dates.items);

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          verdict={verdict}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <CoverageTimeline
            vehicleInformation={this.vehicleLookup}
            locale={this.locale}
            isAuthorized={this.vehicleLookup?.isAuthorized}
            today={this.today}
            hiddenFields={this.appliedHiddenFields}
            retainedDates={this.retainedDates}
            datesShut={this.datesShut}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
