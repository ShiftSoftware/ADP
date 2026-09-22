import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import specificationSchema from '~locales/vehicleLookup/specification/type';

import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { SpecificationRecord, VehicleSpecificationPanel, hasRecords } from './components/VehicleSpecificationPanel';

import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

/** The --settle token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 320;

/**
 * The build record for a VIN: what this vehicle *is* — the model, the year, the grade, and the
 * codes a parts counter or a claim form asks for.
 *
 * The panel reads five sub-objects of the response and no others (`SpecificationRecord`): the VIN,
 * `isAuthorized`, `identifiers`, `vehicleVariantInfo` and `vehicleSpecification`. Warranty,
 * campaigns, service history, sales, paint readings, accessories and service menus are other
 * panels' questions; a value that would make this panel more useful but lives outside the five is
 * not this panel's to show.
 *
 * Two facts decide what it may say. `isAuthorized === false` means the distributor has no record of
 * the vehicle, so nothing in the response is the distributor's to assert and every value reads as a
 * dash. And an *empty* record is not a record: the evaluators build `identifiers` and
 * `vehicleSpecification` for every VIN, including ones they know nothing about, so the panel's
 * `hasRecords` counts values rather than objects.
 *
 * Every state change is a movement. The head's content leaves the band while a lookup is in flight
 * and returns on its new words, so nothing in the head is ever seen changing; the identity grid is
 * a fixed structure and never shuts, its values covered where they stand.
 */
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

  /**
   * A language change is a re-layout, not a state change: it awaits the two locale files and
   * assigns `locale`, and it must never set a loading flag. Raising the covers, dropping the head
   * or shutting a region for a change of words would say a lookup had happened.
   */
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
    this.leaveGeneration++;
    await setVehicleLookupData(this, newData, headers);
  }

  /**
   * A failure raised by the panel's own lookup arrives with the head already out of the band, so
   * assigning at once *is* the arrive. A host calling this cold has a settled card on screen, so
   * the error is entered the way every other state is — through the leave. Without it the red pill
   * would mount into a summary row sitting at full opacity and the values would blank in place.
   */
  @Method()
  async setErrorMessage(message: ErrorKeys) {
    if (!this.isLoading) await this.leave();
    setVehicleLookupErrorState(this, message);
  }

  /** Back to "no vehicle": the head leaves the band and the covers come up, then the panel is emptied unseen. */
  @Method()
  async clearData() {
    await this.leave();
    this.emptyPanel();
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Leaving a state

  /** The panel is on its way to empty: the head is out of the band and the covers are up. */
  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  /** How long a state change takes to settle: the --settle token plus a margin for the frame it starts on. */
  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  /**
   * Takes the head's content out of the band, raises the covers over the values, and resolves once
   * they have settled — so the caller can swap the vehicle while nothing readable is on screen.
   * Resolves at once when there is nothing to leave. A lookup started meanwhile takes over: the
   * generation counter lets the newer call win without the older one flipping `leaving` back.
   */
  private async leave() {
    const generation = ++this.leaveGeneration;

    if (!this.vehicleLookup && !this.isError) return;

    clearTimeout(this.networkTimeoutRef);
    this.abortController?.abort();
    this.leaving = true;

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));

    if (generation === this.leaveGeneration) this.leaving = false;
  }

  private emptyPanel() {
    this.vehicleLookup = undefined;
    this.isError = false;
    this.errorMessage = undefined;
    this.isLoading = false;
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

  // #endregion

  // #region Height

  /**
   * Height changes here are CSS transitions inside this shadow root, which an enclosing
   * <flexible-container> (the wrapper's tab strip) cannot see; it would keep clipping at the old
   * height. So every render that moved something announces the change to those containers, and they
   * stop clipping until it has settled.
   */
  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

  /** Everything that can move the card's height, joined. A render that changes none of it announces nothing. */
  private signature(): string {
    return [this.isLoading || this.leaving, hasRecords(this.record()), !!this.vehicleLookup?.vehicleSpecification?.variantDescription?.trim(), this.vehicleLookup?.vin].join('|');
  }

  componentDidRender() {
    this.announceVerdict();

    const signature = this.signature();
    const changed = this.layoutSignature !== undefined && this.layoutSignature !== signature;
    this.layoutSignature = signature;

    if (!changed) return;

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  // #endregion

  /**
   * The response narrowed to the five sub-objects the panel may read. Everything downstream — the
   * render, `hasRecords`, the signature — takes this, so a field outside the barrier has no way in.
   */
  private record(): SpecificationRecord | undefined {
    const dto = this.vehicleLookup;
    if (!dto) return undefined;

    return {
      vin: dto.vin,
      isAuthorized: dto.isAuthorized,
      identifiers: dto.identifiers,
      vehicleVariantInfo: dto.vehicleVariantInfo,
      vehicleSpecification: dto.vehicleSpecification,
    };
  }

  render() {
    const record = this.record();
    const error = this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined;

    const verdict = recordVerdict({
      locale: this.locale.sharedLocales,
      error,
      vehicleLoaded: !!record?.vin,
      authorized: record?.isAuthorized,
      // By value, not by object: the evaluators hand back `{}` for a VIN they know nothing about,
      // and a green accent over a blank card is a verdict read off an absence.
      hasRecords: hasRecords(record),
    });
    this.currentVerdict = verdict.accent;

    // The wrapper's phase follows the panel's: the head leaves and the covers come up whenever a
    // lookup is in flight or the panel is on its way to empty.
    const busy = this.isLoading || this.leaving;
    const texts = this.locale;

    let productionDate: string | null = null;

    try {
      if (record?.vehicleSpecification?.productionDate) {
        const productionDateObj = new Date(record.vehicleSpecification.productionDate);

        productionDate = productionDateObj.toLocaleDateString(this.locale.sharedLocales.language, {
          year: 'numeric',
          month: 'long',
        });
      }
    } catch {
      productionDate = null;
    }

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
          <VehicleSpecificationPanel locale={this.locale} record={record} error={error} loading={busy} verdict={verdict}>
            {/* The body is still the six titled cards of the old panel; the identity grid and the
                details block replace them in the next commit, with the MaterialCard wrapper and the
                flexible-container it nests. */}
            <flexible-container>
              <div class="spec-grid">
                <MaterialCard class="spec-card-legacy" title={texts?.modelCode} minWidth="300px">
                  <MaterialCardChildren
                    class="spec-legacy-value"
                    hidden={!record?.vehicleVariantInfo?.modelCode?.trim() && !record?.vehicleSpecification?.modelDescription?.trim()}
                  >
                    {record?.vehicleVariantInfo?.modelCode?.trim() || ''} <br />
                    {record?.vehicleSpecification?.modelDescription?.trim() || ''}
                  </MaterialCardChildren>
                </MaterialCard>

                <MaterialCard class="spec-card-legacy" title={texts?.variant} minWidth="300px">
                  <MaterialCardChildren class="spec-legacy-value" hidden={!record?.identifiers?.variant?.trim() && !record?.vehicleSpecification?.variantDescription?.trim()}>
                    {record?.identifiers?.variant?.trim() || ''} <br />
                    {record?.vehicleSpecification?.variantDescription?.trim() || ''}
                  </MaterialCardChildren>
                </MaterialCard>

                <MaterialCard desc={record?.identifiers?.katashiki?.trim() || ''} title={texts?.katashiki} minWidth="250px" />

                <MaterialCard desc={record?.vehicleVariantInfo?.modelYear?.toString()?.trim() || ''} title={texts?.modelYear} minWidth="250px" />

                <MaterialCard desc={productionDate ? productionDate : ''} title={texts?.productionDate} minWidth="250px" />

                <MaterialCard desc={record?.vehicleVariantInfo?.sfx?.trim() || ''} title={texts?.sfx} minWidth="250px" />
              </div>
            </flexible-container>
          </VehicleSpecificationPanel>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
