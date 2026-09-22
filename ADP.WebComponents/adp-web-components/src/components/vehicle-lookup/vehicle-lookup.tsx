import { Component, Element, Host, Listen, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { getMockFile } from '~features/mocks';

import vehicleLookupWrapperSchema from '~locales/vehicleLookup/wrapper-type';

import { VehicleSsc } from './vehicle-ssc';
import { VehicleAccessories } from './vehicle-accessories';
import { VehicleSpecification } from './vehicle-specification';
import { VehicleClaimableItems } from './vehicle-claimable-items';
import { VehiclePaintThickness } from './vehicle-paint-thickness';
import { VehicleServiceHistory } from './vehicle-service-history';
import { VehicleWarrantyTimeline } from './vehicle-warranty-timeline';
import { VehicleSaleInformation } from './vehicle-sale-information';

import { DotNetObjectReference } from '~features/blazor-ref';
import { RequestHeadersProvider, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { LookupTabs, VehicleInfoLayout, VerdictState, createTabRegion, tabPark } from '~features/vehicle-info-layout';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

const componentTags = {
  vehicleAccessories: 'vehicle-accessories',
  vehicleSpecification: 'vehicle-specification',
  vehiclePaintThickness: 'vehicle-paint-thickness',
  vehicleServiceHistory: 'vehicle-service-history',
  vehicleClaimableItems: 'vehicle-claimable-items',
  vehicleSaleInformation: 'vehicle-sale-information',
  vehicleWarrantyTimeline: 'vehicle-warranty-timeline',
  vehicleSsc: 'vehicle-ssc',
} as const;

export type ComponentMap = {
  [componentTags.vehicleAccessories]: VehicleAccessories;
  [componentTags.vehicleSpecification]: VehicleSpecification;
  [componentTags.vehicleServiceHistory]: VehicleServiceHistory;
  [componentTags.vehiclePaintThickness]: VehiclePaintThickness;
  [componentTags.vehicleClaimableItems]: VehicleClaimableItems;
  [componentTags.vehicleSaleInformation]: VehicleSaleInformation;
  [componentTags.vehicleWarrantyTimeline]: VehicleWarrantyTimeline;
  [componentTags.vehicleSsc]: VehicleSsc;
};

export type ActiveElement = (typeof componentTags)[keyof typeof componentTags] | '';

const hasEntries = (value?: object | null) => !!value && typeof value === 'object' && Object.keys(value).length > 0;

@Component({
  shadow: true,
  tag: 'vehicle-lookup',
  styleUrl: 'vehicle-lookup.css',
})
export class VehicleLookup implements MultiLingual {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: SharedLocales = sharedLocalesSchema.getDefault();

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup', vehicleLookupWrapperSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[1];
  }

  // #endregion

  // #region Wrapper Logic

  @Prop() activeElement?: ActiveElement = '';

  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  /** ISO calendar date forwarded to every panel. Omit it to use the wall clock. */
  @Prop() today?: string;
  @Prop() mockUrl: string = '';
  @Prop() mockRecaptcha: boolean = false;
  @Prop() disableVinValidation: boolean = false;
  @Prop() queryString: string = '';
  /**
   * Appended to the SSC tab's own lookup request only — e.g. a logging flag — so that only a search
   * made from the SSC tab counts as a campaign check. A search from any other tab leaves the SSC tab
   * in its "check required" state, which names the VIN and offers to run the check itself. Never
   * joins the panel's trace request: a trace re-reads a lookup that was already logged.
   */
  @Prop() sscQueryString: string = '';
  @Prop() hiddenTabs: string = '';
  /**
   * The host's tab strip order, as a comma-separated list of tags, so a switch travels the way the
   * strip reads: content enters from the side the new tab is on and the old content leaves to the
   * other. Defaults to this component's own order; a host whose strip differs passes its own.
   */
  @Prop() tabOrder: string = '';
  @Prop() childrenProps?: string | object;

  /**
   * Asked for the current request headers before every request a child makes on its own — a
   * trace, a claim, the unauthorized campaign lookup — so a host can refresh its token on demand
   * instead of the children reusing the headers captured at the last search.
   */
  @Prop() requestHeadersProvider?: RequestHeadersProvider;
  /** Name of a [JSInvokable] method on the Blazor reference that answers with the current request headers. */
  @Prop() blazorRequestHeadersProvider = '';

  @Prop() blazorErrorStateListener = '';
  @Prop() errorStateListener?: (newError: string) => void;

  @Prop() blazorOnLoadingStateChange = '';
  @Prop() loadingStateChanged?: (isLoading: boolean) => void;

  @Prop() dynamicClaimActivate?: (vehicleInformation: VehicleLookupDTO) => void;
  @Prop() blazorDynamicClaimActivate = '';

  @State() errorMessage?: string;
  @State() currentVin: string = '';
  @State() isError: boolean = false;
  @State() isLoading: boolean = false;

  private searchGeneration = 0;

  /** The headers the host passed with the most recent search; the fallback when it supplies no provider. */
  private lastRequestHeaders?: object;

  @State() blazorRef?: DotNetObjectReference;

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  // #region One card, the active panel's verdict, the tab region

  /** Each panel's last announced verdict, by tag; the card's accent reads the active one's. */
  @State() verdicts: Partial<Record<ActiveElement, VerdictState>> = {};

  @Listen('verdictChange')
  onVerdictChange(event: CustomEvent<VerdictState>) {
    // By the time the event reaches this host it has crossed the shadow boundary and its target has
    // been retargeted to the host itself; the panel that fired it is the first node of the path.
    const source = (event.composedPath?.()[0] ?? event.target) as HTMLElement | null;
    const tag = source?.id as ActiveElement | undefined;
    if (!tag || !Object.values(componentTags).includes(tag as any)) return;
    this.verdicts = { ...this.verdicts, [tag]: event.detail };
  }

  private regionEl?: HTMLDivElement;
  private tabRegion = createTabRegion(() => this.regionEl);
  /** Set by the activeElement watcher, consumed by the render that switches the tab. */
  private switchingTo?: ActiveElement;

  /** The tab that just left, until the switch has settled: its head content rests up and small, not below. */
  @State() leaving?: ActiveElement;
  private leavingTimer?: ReturnType<typeof setTimeout>;

  @Watch('activeElement')
  onActiveElementChange(next: ActiveElement, previous: ActiveElement) {
    this.switchingTo = next;
    this.leaving = previous || undefined;
    clearTimeout(this.leavingTimer);
    // Re-parked once out of the band, unseen: the head's movement plus the frame it starts on.
    this.leavingTimer = setTimeout(() => (this.leaving = undefined), this.headSettleMs() + 80);
  }

  /** The --settle token — the switch's one clock — read from the stylesheet; its default when it cannot be read, as in tests. */
  private headSettleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 820;
  }

  private tabOf(tag: string | undefined) {
    return tag ? ((this.el.shadowRoot?.querySelector(`.lookup-tab[data-tab="${tag}"]`) as HTMLElement | null) ?? undefined) : undefined;
  }

  componentWillRender() {
    if (this.switchingTo === undefined) return;
    // Read once, before the patch: the region as it shows now, the incoming tab as laid out, and
    // both panels' head bands.
    const host = (tag: string | undefined) => (tag ? ((this.el.shadowRoot?.getElementById(tag) as HTMLElement | null) ?? undefined) : undefined);
    this.tabRegion.beforeSwitch(this.tabOf(this.switchingTo), { incoming: host(this.switchingTo), outgoing: host(this.leaving) });
  }

  componentDidRender() {
    if (this.switchingTo !== undefined) {
      const incoming = (this.el.shadowRoot?.getElementById(this.switchingTo) as HTMLElement | null) ?? undefined;
      this.switchingTo = undefined;
      this.tabRegion.afterSwitch(incoming);
    }
    // Follow the active panel's own growth (a drawer opening) with the same transition.
    this.tabRegion.observe(this.el.shadowRoot?.getElementById(this.activeElement) ?? undefined);
  }

  disconnectedCallback() {
    clearTimeout(this.leavingTimer);
    this.tabRegion.dispose();
  }

  /** The strip order the travel direction follows: the host's, or this component's own. */
  private get order(): string[] {
    const own = Object.values(componentTags) as string[];
    const given = this.tabOrder
      .split(',')
      .map(tag => tag.trim())
      .filter(tag => own.includes(tag));
    return given.length ? [...given, ...own.filter(tag => !given.includes(tag))] : own;
  }

  // #endregion

  async componentDidLoad() {
    const vehicleAccessories = this.el.shadowRoot.getElementById('vehicle-accessories') as unknown as VehicleAccessories;
    const vehicleClaim = this.el.shadowRoot.getElementById('vehicle-claimable-items') as unknown as VehicleClaimableItems;
    const vehicleHistory = this.el.shadowRoot.getElementById('vehicle-service-history') as unknown as VehicleServiceHistory;
    const vehicleTimeline = this.el.shadowRoot.getElementById('vehicle-warranty-timeline') as unknown as VehicleWarrantyTimeline;
    const vehicleThickness = this.el.shadowRoot.getElementById('vehicle-paint-thickness') as unknown as VehiclePaintThickness;
    const vehicleSpecification = this.el.shadowRoot.getElementById('vehicle-specification') as unknown as VehicleSpecification;
    const vehicleSaleInformation = this.el.shadowRoot.getElementById('vehicle-sale-information') as unknown as VehicleSaleInformation;
    const vehicleSsc = this.el.shadowRoot.getElementById('vehicle-ssc') as unknown as VehicleSsc;

    this.componentsList = {
      [componentTags.vehicleClaimableItems]: vehicleClaim,
      [componentTags.vehicleServiceHistory]: vehicleHistory,
      [componentTags.vehicleWarrantyTimeline]: vehicleTimeline,
      [componentTags.vehicleAccessories]: vehicleAccessories,
      [componentTags.vehiclePaintThickness]: vehicleThickness,
      [componentTags.vehicleSpecification]: vehicleSpecification,
      [componentTags.vehicleSaleInformation]: vehicleSaleInformation,
      [componentTags.vehicleSsc]: vehicleSsc,
    };

    Object.entries(this.componentsList).forEach(([tag, element]) => {
      if (!element) return;

      element.errorCallback = this.syncErrorAcrossComponents;
      element.loadingStateChange = (newState: boolean) => this.loadingStateChangingMiddleware(newState, tag as ActiveElement);
      element.loadedResponse = newResponse => this.handleLoadData(newResponse, element);
      // Every child asks the wrapper, and the wrapper asks the host, so a token refreshed for one
      // request is refreshed for all of them. Only the children that make follow-up requests of
      // their own declare the prop; on the others this is an inert expando.
      (element as unknown as VehicleLookupComponent).requestHeadersProvider = this.resolveRequestHeaders;
    });

    if (vehicleClaim && this.dynamicClaimActivate) {
      vehicleClaim.activate = this.dynamicClaimActivate;
    }

    if (vehicleClaim) {
      vehicleClaim.activate = vehicleInformation => {
        if (this.blazorRef && this.blazorDynamicClaimActivate) {
          this.blazorRef.invokeMethodAsync(this.blazorDynamicClaimActivate, vehicleInformation);
        }
      };
    }

    if (this.isDev) await this.loadMockData();
  }

  @Watch('isDev')
  async onIsDevChange(isDev: boolean) {
    if (isDev) await this.loadMockData();
  }

  private async loadMockData() {
    if (!this.componentsList) return;
    const mockData = await getMockFile<VehicleLookupDTO>('vehicle-lookup', this.mockUrl);
    await this.setMockData(mockData);
  }

  /** Replace every panel's mock map as one operation when a host changes environment. */
  @Method()
  async setMockData(newMockData: VehicleLookupMock) {
    if (!this.componentsList) return;

    await Promise.all(Object.values(this.componentsList).map(element => element?.setMockData(newMockData)));
  }

  private syncErrorAcrossComponents = (newErrorMessage: ErrorKeys) => {
    this.isError = true;
    this.errorMessage = this.locale?.errors?.[newErrorMessage] || this.locale.errors.wildCard;

    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  /**
   * The current headers for a request made on the host's behalf. The host's own provider wins —
   * it can refresh a token before answering — and the headers it passed with the last search are
   * the fallback for hosts that only ever push headers at search time.
   */
  private resolveRequestHeaders: RequestHeadersProvider = async () => {
    if (this.requestHeadersProvider) return await this.requestHeadersProvider();
    if (this.blazorRef && this.blazorRequestHeadersProvider) return await this.blazorRef.invokeMethodAsync(this.blazorRequestHeadersProvider);
    return this.lastRequestHeaders;
  };

  @Method()
  async handleLoadData(newResponse: VehicleLookupDTO, activeElement) {
    const generation = this.searchGeneration;
    this.isError = false;
    this.errorMessage = '';
    this.currentVin = newResponse.vin || '';

    // Skip distributing to non-active components if a new search has started
    if (generation !== this.searchGeneration) return;

    const sscElement = this.sscQueryString ? this.componentsList[componentTags.vehicleSsc] || null : null;
    // Only skip SSC when we know the search came from a specific non-SSC tab.
    // When activeElement is null (programmatic data injection), distribute to all components.
    const shouldSkipSsc = sscElement && activeElement !== null && activeElement !== sscElement;

    Object.values(this.componentsList).forEach(element => {
      if (element === null || element === activeElement || !newResponse) return;

      // When sscQueryString is set, the SSC tab's own request is the logged one. A search from any
      // other tab must not show campaigns that were never logged as looked up — but a blank SSC tab
      // would read as "no campaigns", so the tab is told the check was skipped and offers to run it.
      if (shouldSkipSsc && element === sscElement) {
        void (element as VehicleSsc).skipLookup(newResponse.vin || '');
        return;
      }

      element.fetchVin(newResponse);
    });
  }

  /**
   * Each panel's own loading flag, by tag. The card's phase follows the active panel only: after
   * a search the other panels are hydrated in turn and each is busy for the helper's second, and
   * the accent must not stay out of the card for as long as the last of them takes. The host's
   * callback keeps the old meaning — the composite as a whole is loading while any panel is.
   */
  @State() loadingByTag: Partial<Record<ActiveElement, boolean>> = {};

  private loadingStateChangingMiddleware = (newState: boolean, tag?: ActiveElement) => {
    this.isLoading = newState;
    if (tag) this.loadingByTag = { ...this.loadingByTag, [tag]: newState };
    if (this.loadingStateChanged) this.loadingStateChanged(newState);
    if (this.blazorRef && this.blazorOnLoadingStateChange) this.blazorRef.invokeMethodAsync(this.blazorOnLoadingStateChange, newState);
  };

  @Watch('errorMessage')
  async errorListener(newState) {
    if (this.errorStateListener) this.errorStateListener(newState);
    if (this.blazorRef && this.blazorErrorStateListener) this.blazorRef.invokeMethodAsync(this.blazorErrorStateListener, newState);
  }

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }

  @Method()
  async fetchVin(vin: string, headers: any = {}) {
    const activeElement = this.componentsList[this.activeElement] || null;

    // A host that supplies a provider may search without passing headers at all; the child still
    // receives explicit headers, so every panel behaves the same whichever way the host works.
    const resolvedHeaders = hasEntries(headers) ? headers : await this.resolveRequestHeaders();

    if (hasEntries(resolvedHeaders)) this.lastRequestHeaders = resolvedHeaders;

    if (!activeElement) return;

    this.searchGeneration++;

    activeElement.fetchVin(vin, resolvedHeaders || {});
  }
  // #endregion
  render() {
    const props = {
      [componentTags.vehicleAccessories]: {},
      [componentTags.vehicleSpecification]: {},
      [componentTags.vehicleClaimableItems]: {},
      [componentTags.vehiclePaintThickness]: {},
      [componentTags.vehicleServiceHistory]: {},
      [componentTags.vehicleWarrantyTimeline]: {},
      [componentTags.vehicleSaleInformation]: {},
      [componentTags.vehicleSsc]: {},
    };

    try {
      if (this.childrenProps) {
        let parsedProps = {};
        if (typeof this.childrenProps === 'string') parsedProps = JSON.parse(this.childrenProps);
        else if (typeof this.childrenProps === 'object') parsedProps = this.childrenProps;

        Object.keys(props).forEach(key => {
          if (typeof parsedProps[key] === 'object') props[key] = parsedProps[key];
        });
      }
    } catch (error) {
      console.error(error);
    }

    const hiddenSet = new Set(
      this.hiddenTabs
        .split(',')
        .map(t => t.trim())
        .filter(Boolean),
    );

    // An active tag that is unknown or hidden leaves every tab inactive: an empty region, not a message.
    const active = Object.values(componentTags).includes(this.activeElement as any) && !hiddenSet.has(this.activeElement) ? this.activeElement : '';
    const order = this.order;
    const direction = this.locale.direction;
    /** The side an inactive panel's content parks on, on the panel's host, for its own .lookup-slide rule. */
    const park = (tag: string) => (tag === active ? null : tabPark(tag, active, order, direction));
    const leaving = (tag: string) => (tag !== active && tag === this.leaving ? 'true' : null);

    const allComponents: Partial<Record<ActiveElement, Node>> = {
      'vehicle-specification': (
        <vehicle-specification
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleSpecification}
          data-tab-park={park(componentTags.vehicleSpecification)}
          data-tab-leaving={leaving(componentTags.vehicleSpecification)}
          {...props[componentTags.vehicleSpecification]}
        />
      ),
      'vehicle-accessories': (
        <vehicle-accessories
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleAccessories}
          data-tab-park={park(componentTags.vehicleAccessories)}
          data-tab-leaving={leaving(componentTags.vehicleAccessories)}
          {...props[componentTags.vehicleAccessories]}
        />
      ),
      'vehicle-sale-information': (
        <vehicle-sale-information
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleSaleInformation}
          data-tab-park={park(componentTags.vehicleSaleInformation)}
          data-tab-leaving={leaving(componentTags.vehicleSaleInformation)}
          {...props[componentTags.vehicleSaleInformation]}
        />
      ),
      'vehicle-warranty-timeline': (
        <vehicle-warranty-timeline
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleWarrantyTimeline}
          data-tab-park={park(componentTags.vehicleWarrantyTimeline)}
          data-tab-leaving={leaving(componentTags.vehicleWarrantyTimeline)}
          {...props[componentTags.vehicleWarrantyTimeline]}
        />
      ),
      'vehicle-ssc': (
        <vehicle-ssc
          coreOnly
          isDev={this.isDev}
          mockRecaptcha={this.mockRecaptcha}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          lookup-query-string={this.sscQueryString}
          id={componentTags.vehicleSsc}
          data-tab-park={park(componentTags.vehicleSsc)}
          data-tab-leaving={leaving(componentTags.vehicleSsc)}
          {...props[componentTags.vehicleSsc]}
        />
      ),
      'vehicle-service-history': (
        <vehicle-service-history
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleServiceHistory}
          data-tab-park={park(componentTags.vehicleServiceHistory)}
          data-tab-leaving={leaving(componentTags.vehicleServiceHistory)}
          {...props[componentTags.vehicleServiceHistory]}
        />
      ),
      'vehicle-paint-thickness': (
        <vehicle-paint-thickness
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehiclePaintThickness}
          data-tab-park={park(componentTags.vehiclePaintThickness)}
          data-tab-leaving={leaving(componentTags.vehiclePaintThickness)}
          {...props[componentTags.vehiclePaintThickness]}
        />
      ),
      'vehicle-claimable-items': (
        <vehicle-claimable-items
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
          today={this.today}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleClaimableItems}
          data-tab-park={park(componentTags.vehicleClaimableItems)}
          data-tab-leaving={leaving(componentTags.vehicleClaimableItems)}
          {...props[componentTags.vehicleClaimableItems]}
        />
      ),
    };

    const panels = Object.entries(allComponents).filter(([key]) => !hiddenSet.has(key)) as [string, Node][];

    // One card for every panel: its own loading and error state, the active panel's verdict on the
    // accent — which cross-fades to the incoming panel's colour on a switch and to the new verdict
    // on a lookup. The panels render coreOnly inside it.
    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          header={this.currentVin}
          isLoading={!!(active && this.loadingByTag[active])}
          direction={direction}
          verdict={(active && this.verdicts[active]) || 'idle'}
        >
          <LookupTabs active={active} panels={panels} regionRef={el => (this.regionEl = el)} />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
