import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

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
import { RequestHeadersProvider, VehicleLookupComponent } from '~features/vehicle-lookup-component';
import { VehicleInfoLayout } from '~features/vehicle-info-layout/vehicle-info-layout';
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

    Object.values(this.componentsList).forEach(element => {
      if (!element) return;

      element.errorCallback = this.syncErrorAcrossComponents;
      element.loadingStateChange = this.loadingStateChangingMiddleware;
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
    Object.values(this.componentsList).forEach(element => {
      if (element) element.setMockData(mockData);
    });
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

  private loadingStateChangingMiddleware = (newState: boolean) => {
    this.isLoading = newState;
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

    if (!Object.values(componentTags).includes(this.activeElement as any) || hiddenSet.has(this.activeElement))
      return <div class="w-full h-[200px] text-[26px] text-red-600 flex items-center justify-center">Invalid tag</div>;

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
          {...props[componentTags.vehicleClaimableItems]}
        />
      ),
    };

    const componentList = Object.fromEntries(Object.entries(allComponents).filter(([key]) => !hiddenSet.has(key))) as Partial<Record<ActiveElement, Node>>;

    const claimableProps = props[componentTags.vehicleClaimableItems] as Record<string, any> | undefined;
    const showClaimableTrace =
      this.activeElement === componentTags.vehicleClaimableItems &&
      !!(claimableProps?.showTrace ?? claimableProps?.['show-trace']) &&
      !!this.currentVin &&
      !this.isError &&
      !this.isLoading;

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          header={this.currentVin}
          isLoading={this.isLoading}
          direction={this.locale.direction}
          errorMessage={this.errorMessage || this.locale.errors.wildCard}
          headerRight={
            showClaimableTrace ? (
              <button
                type="button"
                class="trace-trigger-button"
                title="View Lookup Trace"
                aria-label="View Lookup Trace"
                onClick={() => this.componentsList?.[componentTags.vehicleClaimableItems]?.openTrace()}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="6" cy="19" r="3" />
                  <path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15" />
                  <circle cx="18" cy="5" r="3" />
                </svg>
              </button>
            ) : null
          }
        >
          <shift-tab-content components={componentList} activeComponent={this.activeElement}></shift-tab-content>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
