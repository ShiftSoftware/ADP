import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createResizeSettle } from '~lib/resize-settle';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import specificationSchema from '~locales/vehicleLookup/specification/type';

import { DetailGroup, SpecificationRecord, VehicleSpecificationPanel, detailGroups, hasRecords, identityCells } from './components/VehicleSpecificationPanel';

import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component/vehicle-lookup-api-integration';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

/** The --settle token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 480;

/**
 * The build record for a VIN: what this vehicle *is* — model, year, grade, and the codes a parts
 * counter or a claim form asks for.
 *
 * It reads five sub-objects and no others (`SpecificationRecord`). Two facts decide what it may
 * say: `isAuthorized === false` means the distributor has no record, so every value dashes; and an
 * *empty* record is not a record, so `hasRecords` counts values rather than objects.
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
   * A re-layout, not a state change: never sets a loading flag. Covers or a dropped head would
   * say a lookup had happened.
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
   * The panel's own failure arrives with the head already out of the band, so assigning at once
   * *is* the arrive. Called cold, the card is settled, so the error enters through the leave —
   * otherwise the red pill mounts into a summary row at full opacity.
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
    // The panel is back to no vehicle, so it is back to its default. It resets while the details
    // block is shut, so nothing on screen moves for it and the next vehicle arrives expanded.
    this.detailsOpen = true;
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
   * Takes the head out of the band and raises the covers, resolving once settled, so the caller
   * can swap the vehicle while nothing readable is on screen. The generation counter lets a newer
   * lookup win without the older one flipping `leaving` back.
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
   * Fires on every verdict change, idle included, so a composite can colour its accent from the
   * active panel.
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

  // #region The details block

  /**
   * The one piece of state that is not a function of the response. Expanded on load, so nothing is
   * hidden from a reader who does not know the control exists, and a new vehicle does **not** reset
   * it — that would undo the reader's choice once a minute. Only `clearData()` resets it.
   */
  @State() detailsOpen: boolean = true;

  private toggleDetails = () => {
    this.detailsOpen = !this.detailsOpen;
  };

  /**
   * Kept while the block is shut so the groups slide away rather than blink out; dropped a settle
   * later, while nothing is on screen.
   */
  private retainedGroups: DetailGroup[] = [];
  private retainTimer?: ReturnType<typeof setTimeout>;

  private groups(): DetailGroup[] {
    const record = this.record();
    const readable = !!record?.vin && !this.isError && record.isAuthorized !== false;
    return detailGroups(record, this.locale, readable);
  }

  private retainDetails(groups: DetailGroup[]) {
    if (groups.length) {
      this.retainedGroups = groups;
      clearTimeout(this.retainTimer);
      this.retainTimer = undefined;
      return;
    }

    if (this.retainedGroups.length && !this.retainTimer) {
      this.retainTimer = setTimeout(() => {
        this.retainTimer = undefined;
        this.retainedGroups = [];
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  // #endregion

  // #region Height

  /**
   * An enclosing <flexible-container> cannot see transitions inside this shadow root and would
   * keep clipping at the old height, so every render that moved something announces it.
   */
  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;
  private valuesChanged = false;

  /**
   * A value block is the same slot on every vehicle but not the same size — a name or a year note
   * wraps to a second line. Without this the height changes in one frame under a lifting cover.
   */
  private resizeSettle = createResizeSettle(() => this.el.shadowRoot?.querySelectorAll<HTMLElement>('.spec-value') ?? []);

  /**
   * Everything that can move the card's height, joined. A render that changes none of it announces
   * nothing and pins no heights.
   */
  private signature(): string {
    const record = this.record();
    const readable = !!record?.vin && !this.isError && record.isAuthorized !== false;
    const cells = identityCells(record, this.locale, this.locale.sharedLocales.lang, readable);

    return [
      this.isLoading || this.leaving,
      record?.vin,
      this.detailsOpen,
      hasRecords(record),
      !!record?.vehicleSpecification?.variantDescription?.trim(),
      // narrow column: a catalogue that arrives, or a vehicle whose code resolves where the last
      // one's did not, is a resize like any other and gets the same eased settle.
      cells.map(cell => `${cell.key}=${cell.colour ? `${cell.colour.code}/${cell.colour.name}` : cell.text}${cell.note ?? ''}`).join(','),
      this.groups()
        .map(group => `${group.key}:${group.cells.map(cell => cell.key).join('+')}`)
        .join(','),
    ].join('|');
  }

  componentWillRender() {
    const signature = this.signature();
    this.valuesChanged = this.layoutSignature !== undefined && this.layoutSignature !== signature;
    // Pin the live heights before the patch; the new measurements go on in componentDidRender.
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
    const groups = this.groups();
    this.retainDetails(groups);

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
          <VehicleSpecificationPanel
            locale={this.locale}
            record={record}
            error={error}
            loading={busy}
            verdict={verdict}
            lang={this.locale.sharedLocales.lang}
            groups={groups}
            retainedGroups={this.retainedGroups}
            detailsOpen={this.detailsOpen}
            onToggleDetails={this.toggleDetails}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
