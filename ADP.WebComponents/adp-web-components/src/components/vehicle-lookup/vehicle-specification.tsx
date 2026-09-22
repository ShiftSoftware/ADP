import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createResizeSettle } from '~lib/resize-settle';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { BrandSlugs, ColourCatalogue, ColourEntry, exteriorColours, loadColourCatalogue, parseBrandSlugs, slugOf } from '~features/colour-catalogue';

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
    this.loadColours();
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

  // #region The colour catalogue

  /**
   * Maps the host's own brand ids (`identifiers.brandID`, an opaque hash id from its identity
   * system) to the brand slugs the colour catalogue is keyed by — `{ "<brandID>": "<slug>" }`.
   * Accepts an object, or the JSON string of one so a Blazor or plain-HTML host can pass it as an
   * attribute.
   *
   * Without it no swatch is ever drawn and every colour cell reads code + name, which is a complete
   * state and not a degraded one. There is no default and no guess: an id the map does not carry
   * has no slug, and a brand with no slug has no catalogue.
   */
  @Prop() brandSlugs?: BrandSlugs | string;

  /**
   * The catalogue, once its chunk has arrived. It is **not** fetched: it is a dynamic import of a
   * JSON module in this same bundle, so a host that never configures `brandSlugs` pays nothing for
   * it and no colour ever depends on a third-party URL.
   */
  @State() colourCatalogue?: ColourCatalogue;

  /**
   * Started at mount, and again whenever the host changes its map — never on a lookup. A lookup
   * waits a second before it asks the server, so a chunk started at mount is long since parsed by
   * the time any vehicle lands, and the swatch comes up under its cell's cover with the rest of the
   * value rather than arriving on a beat of its own.
   */
  @Watch('brandSlugs')
  loadColours() {
    if (!parseBrandSlugs(this.brandSlugs)) return;

    loadColourCatalogue().then(catalogue => {
      this.colourCatalogue = catalogue;
    });
  }

  /**
   * This vehicle's brand's exterior table, or nothing — the one and only use of
   * `identifiers.brandID`, which is never rendered, never spoken, never put in a title or a data
   * attribute and never logged. The brand id stops here: what goes down to the render is a plain
   * code → colour table with no brand in it.
   */
  private exteriorTable(): Record<string, ColourEntry> | undefined {
    return exteriorColours(this.colourCatalogue, slugOf(this.record()?.identifiers?.brandID, parseBrandSlugs(this.brandSlugs)));
  }

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

  // #region The details block

  /**
   * The one piece of panel state that is not a function of the response: whether the reader has the
   * details expanded. Expanded on every load, so nothing is hidden from a reader who does not know
   * the control exists — and a new vehicle does **not** reset it. A reader who folded the details
   * away has said what they want to see, and re-opening the block under them on every VIN would
   * undo that choice once a minute. Only `clearData()` resets it, where the panel is back to no
   * vehicle at all.
   */
  @State() detailsOpen: boolean = true;

  private toggleDetails = () => {
    this.detailsOpen = !this.detailsOpen;
  };

  /**
   * The last vehicle's groups, kept while the block is shut so they slide away with it instead of
   * blinking out on the frame the data changed. Dropped a settle after the block shut, while
   * nothing is on screen.
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
   * Height changes here are CSS transitions inside this shadow root, which an enclosing
   * <flexible-container> (the wrapper's tab strip) cannot see; it would keep clipping at the old
   * height. So every render that moved something announces the change to those containers, and they
   * stop clipping until it has settled.
   */
  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;
  private valuesChanged = false;

  /**
   * A value block is the same slot on every vehicle but not the same size: the exterior-colour cell
   * wraps to a second line whenever a catalogue name resolves, the model-year cell whenever the
   * record note appears. Without this the block's height would change in one frame under a cover
   * that is about to lift on it.
   */
  private resizeSettle = createResizeSettle(() => this.el.shadowRoot?.querySelectorAll<HTMLElement>('.spec-value') ?? []);

  /**
   * Everything that can move the card's height, joined: the phase, the vehicle, every tier-1 value,
   * the reader's disclosure, and the groups and cells that will render. A render that changes none
   * of it announces nothing and pins no heights.
   */
  private signature(): string {
    const record = this.record();
    const readable = !!record?.vin && !this.isError && record.isAuthorized !== false;
    const cells = identityCells(record, this.locale, this.locale.sharedLocales.lang, readable, this.exteriorTable());

    return [
      this.isLoading || this.leaving,
      record?.vin,
      this.detailsOpen,
      hasRecords(record),
      !!record?.vehicleSpecification?.variantDescription?.trim(),
      // The swatch is in the signature because it changes the block's width, and so its height at a
      // narrow column: a catalogue that arrives, or a vehicle whose code resolves where the last
      // one's did not, is a resize like any other and gets the same eased settle.
      cells.map(cell => `${cell.key}=${cell.colour ? `${cell.colour.code}/${cell.colour.name}/${cell.colour.swatch?.hex ?? ''}` : cell.text}${cell.note ?? ''}`).join(','),
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
            exteriorColours={this.exteriorTable()}
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
