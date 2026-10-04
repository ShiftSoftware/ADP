import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createResizeSettle } from '~lib/resize-settle';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import saleInformationSchema from '~locales/vehicleLookup/saleInformation/type';

import {
  EndCustomerStatement,
  RetainedLegs,
  SaleConfig,
  SaleLeg,
  SaleRecord,
  VehicleSaleInformationPanel,
  chainEnd,
  endCustomerCells,
  endCustomerStatement,
  hasRecords,
  headStatement,
  parseHiddenFields,
  readable,
  saleCells,
  saleLegs,
  sameConfig,
  sameHead,
} from './components/VehicleSaleInformationPanel';

import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

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
    this.applied = this.requestedConfig();
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
  @Prop() hiddenFields: string =
    'customerAccountNumber,customerID,brokerInvoiceNumber,brokerInvoiceDate,distributorInvoiceNumber,distributorInvoiceDate,warrantyActivationDate,invoiceDate,invoiceNumber,intermediaryInvoiceNumber,intermediaryInvoiceDate';

  @Prop() showSupplyChain: boolean = false;

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
    this.leaveGeneration++;
    this.leaving = false;
    await setVehicleLookupData(this, newData, headers);
  }

  // Called on a settled card, the error has to enter through the leave or the red pill mounts at full opacity.
  @Method()
  async setErrorMessage(message: ErrorKeys) {
    if (!this.isLoading && !(await this.leave())) return;
    setVehicleLookupErrorState(this, message);
  }

  @Method()
  async clearData() {
    if (!(await this.leave())) return;
    this.emptyPanel();
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Leaving a state

  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  private async leave(): Promise<boolean> {
    const generation = ++this.leaveGeneration;

    if (!this.vehicleLookup && !this.isError) return true;

    clearTimeout(this.networkTimeoutRef);
    this.abortController?.abort();
    this.leaving = true;

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));

    if (generation !== this.leaveGeneration) return false;
    this.leaving = false;
    return true;
  }

  private emptyPanel() {
    this.vehicleLookup = undefined;
    this.isError = false;
    this.errorMessage = undefined;
    this.isLoading = false;
  }

  // #endregion

  // #region Configuration

  @State() applied: SaleConfig = { chain: false, hidden: parseHiddenFields(this.hiddenFields) };

  @State() reconfiguring: boolean = false;

  @State() headLeaving: boolean = false;

  private configGeneration = 0;

  private requestedConfig(): SaleConfig {
    return { chain: !!this.showSupplyChain, hidden: parseHiddenFields(this.hiddenFields) };
  }

  // Not a lookup: the body shuts over the old layout, swaps unseen and reopens; the head leaves only if its words change.
  @Watch('showSupplyChain')
  @Watch('hiddenFields')
  async reconfigure() {
    const next = this.requestedConfig();
    const generation = ++this.configGeneration;

    if (sameConfig(next, this.applied)) {
      this.reconfiguring = false;
      this.headLeaving = false;
      return;
    }

    const record = this.record();
    const canRead = readable(record, this.errorText());
    this.headLeaving = !sameHead(headStatement(record, this.applied, canRead), headStatement(record, next, canRead));
    this.reconfiguring = true;

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));
    if (generation !== this.configGeneration) return;

    this.applied = next;
    this.reconfiguring = false;
    this.headLeaving = false;
  }

  // #endregion

  // #region Verdict

  @Event() verdictChange: EventEmitter<VerdictState>;

  private lastVerdict?: VerdictState;
  private currentVerdict: VerdictState = 'idle';

  private announceVerdict() {
    if (this.currentVerdict === this.lastVerdict) return;
    this.lastVerdict = this.currentVerdict;
    this.verdictChange.emit(this.currentVerdict);
  }

  // #endregion

  // #region The legs band

  private retained: RetainedLegs = { legs: [] };
  private retainTimer?: ReturnType<typeof setTimeout>;

  private legs(): { legs: SaleLeg[]; chainEnd?: EndCustomerStatement } {
    const record = this.record();
    const canRead = readable(record, this.errorText());
    return { legs: saleLegs(record, this.applied, this.locale, canRead), chainEnd: chainEnd(record, this.applied, this.locale, canRead) };
  }

  private retainLegs(current: { legs: SaleLeg[]; chainEnd?: EndCustomerStatement }) {
    if (current.legs.length || current.chainEnd) {
      this.retained = current;
      clearTimeout(this.retainTimer);
      this.retainTimer = undefined;
      return;
    }

    if ((this.retained.legs.length || this.retained.chainEnd) && !this.retainTimer) {
      this.retainTimer = setTimeout(() => {
        this.retainTimer = undefined;
        this.retained = { legs: [] };
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  // #endregion

  // #region Height

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;
  private valuesChanged = false;

  private resizeSettle = createResizeSettle(() => this.el.shadowRoot?.querySelectorAll<HTMLElement>('.sale-value.resize-settle') ?? []);

  private signature(): string {
    const record = this.record();
    const canRead = readable(record, this.errorText());
    const cellText = (cells: { key: string; text: string; note?: string }[] | null) => (cells ?? []).map(cell => `${cell.key}=${cell.text}${cell.note ?? ''}`).join(',');

    return [
      this.isLoading || this.leaving,
      record?.vin,
      this.applied.chain,
      [...this.applied.hidden].sort().join(','),
      this.reconfiguring,
      hasRecords(record),
      cellText(saleCells(record, this.applied, this.locale, canRead)),
      cellText(endCustomerCells(record, this.applied, this.locale, canRead)),
      endCustomerStatement(record, this.applied, this.locale, canRead).kind,
      this.legs()
        .legs.map(leg => `${leg.key}:${leg.party?.text ?? ''}:${leg.cells.map(cell => cell.key).join('+')}`)
        .join(','),
    ].join('|');
  }

  componentWillRender() {
    const signature = this.signature();
    this.valuesChanged = this.layoutSignature !== undefined && this.layoutSignature !== signature;
    if (this.valuesChanged) this.resizeSettle.beforeSwap();
  }

  componentDidRender() {
    this.announceVerdict();

    this.layoutSignature = this.signature();

    if (!this.valuesChanged) return;
    this.valuesChanged = false;

    this.resizeSettle.afterSwap();

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  disconnectedCallback() {
    clearTimeout(this.retainTimer);
    this.resizeSettle.dispose();
    this.heightAnnouncer?.dispose();
  }

  // #endregion

  private errorText(): string | undefined {
    return this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined;
  }

  private record(): SaleRecord | undefined {
    const dto = this.vehicleLookup;
    if (!dto) return undefined;

    return {
      vin: dto.vin,
      isAuthorized: dto.isAuthorized,
      saleInformation: dto.saleInformation,
    };
  }

  render() {
    const record = this.record();
    const error = this.errorText();

    const verdict = recordVerdict({
      locale: this.locale.sharedLocales,
      error,
      vehicleLoaded: !!record?.vin,
      authorized: record?.isAuthorized,
      hasRecords: hasRecords(record),
    });
    this.currentVerdict = verdict.accent;

    const busy = this.isLoading || this.leaving;
    const current = this.legs();
    this.retainLegs(current);

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          coreOnly={this.coreOnly}
          isLoading={busy}
          header={record?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <VehicleSaleInformationPanel
            locale={this.locale}
            record={record}
            error={error}
            loading={busy}
            reconfiguring={this.reconfiguring}
            headLeaving={this.headLeaving}
            verdict={verdict}
            config={this.applied}
            legs={current.legs}
            chainEnd={current.chainEnd}
            retained={this.retained}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
