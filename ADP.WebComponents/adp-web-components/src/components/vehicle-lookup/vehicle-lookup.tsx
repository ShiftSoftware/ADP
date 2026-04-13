import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { getMockFile } from '~features/mocks';

import vehicleLookupWrapperSchema from '~locales/vehicleLookup/wrapper-type';

import { VehicleAccessories } from './vehicle-accessories';
import { VehicleSpecification } from './vehicle-specification';
import { VehicleClaimableItems } from './vehicle-claimable-items';
import { VehiclePaintThickness } from './vehicle-paint-thickness';
import { VehicleServiceHistory } from './vehicle-service-history';
import { VehicleWarrantyDetails } from './vehicle-warranty-details';
import { VehicleSaleInformation } from './vehicle-sale-information';

import { DotNetObjectReference } from '~features/blazor-ref';
import { VehicleInfoLayout } from '~features/vehicle-info-layout/vehicle-info-layout';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

const componentTags = {
  vehicleAccessories: 'vehicle-accessories',
  vehicleSpecification: 'vehicle-specification',
  vehiclePaintThickness: 'vehicle-paint-thickness',
  vehicleServiceHistory: 'vehicle-service-history',
  vehicleClaimableItems: 'vehicle-claimable-items',
  vehicleSaleInformation: 'vehicle-sale-information',
  vehicleWarrantyDetails: 'vehicle-warranty-details',
  vehicleSsc: 'vehicle-ssc',
} as const;

export type ComponentMap = {
  [componentTags.vehicleAccessories]: VehicleAccessories;
  [componentTags.vehicleSpecification]: VehicleSpecification;
  [componentTags.vehicleServiceHistory]: VehicleServiceHistory;
  [componentTags.vehiclePaintThickness]: VehiclePaintThickness;
  [componentTags.vehicleClaimableItems]: VehicleClaimableItems;
  [componentTags.vehicleSaleInformation]: VehicleSaleInformation;
  [componentTags.vehicleWarrantyDetails]: VehicleWarrantyDetails;
  [componentTags.vehicleSsc]?: VehicleWarrantyDetails;
};

export type ActiveElement = (typeof componentTags)[keyof typeof componentTags] | '';

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
  @Prop() mockUrl: string = '';
  @Prop() disableVinValidation: boolean = false;
  @Prop() queryString: string = '';
  @Prop() sscQueryString: string = '';
  @Prop() separateSsc: boolean = false;
  @Prop() hiddenTabs: string = '';
  @Prop() childrenProps?: string | Object;

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

  @State() blazorRef?: DotNetObjectReference;

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  async componentDidLoad() {
    const vehicleAccessories = this.el.shadowRoot.getElementById('vehicle-accessories') as unknown as VehicleAccessories;
    const vehicleClaim = this.el.shadowRoot.getElementById('vehicle-claimable-items') as unknown as VehicleClaimableItems;
    const vehicleHistory = this.el.shadowRoot.getElementById('vehicle-service-history') as unknown as VehicleServiceHistory;
    const vehicleDetails = this.el.shadowRoot.getElementById('vehicle-warranty-details') as unknown as VehicleWarrantyDetails;
    const vehicleThickness = this.el.shadowRoot.getElementById('vehicle-paint-thickness') as unknown as VehiclePaintThickness;
    const vehicleSpecification = this.el.shadowRoot.getElementById('vehicle-specification') as unknown as VehicleSpecification;
    const vehicleSaleInformation = this.el.shadowRoot.getElementById('vehicle-sale-information') as unknown as VehicleSaleInformation;
    const vehicleSsc = this.separateSsc ? (this.el.shadowRoot.getElementById('vehicle-ssc') as unknown as VehicleWarrantyDetails) : null;

    this.componentsList = {
      [componentTags.vehicleClaimableItems]: vehicleClaim,
      [componentTags.vehicleServiceHistory]: vehicleHistory,
      [componentTags.vehicleWarrantyDetails]: vehicleDetails,
      [componentTags.vehicleAccessories]: vehicleAccessories,
      [componentTags.vehiclePaintThickness]: vehicleThickness,
      [componentTags.vehicleSpecification]: vehicleSpecification,
      [componentTags.vehicleSaleInformation]: vehicleSaleInformation,
    };

    if (vehicleSsc) {
      this.componentsList[componentTags.vehicleSsc] = vehicleSsc;
    }

    Object.values(this.componentsList).forEach(element => {
      if (!element) return;

      element.errorCallback = this.syncErrorAcrossComponents;
      element.loadingStateChange = this.loadingStateChangingMiddleware;
      element.loadedResponse = newResponse => this.handleLoadData(newResponse, element);
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

  private getSscElement(): VehicleWarrantyDetails | null {
    if (this.separateSsc) return this.componentsList[componentTags.vehicleSsc] || null;
    return this.componentsList[componentTags.vehicleWarrantyDetails] || null;
  }

  @Method()
  async handleLoadData(newResponse: VehicleLookupDTO, activeElement) {
    const generation = this.searchGeneration;
    this.isError = false;
    this.errorMessage = '';
    this.currentVin = newResponse.vin || '';

    // Skip distributing to non-active components if a new search has started
    if (generation !== this.searchGeneration) return;

    const sscElement = this.sscQueryString ? this.getSscElement() : null;
    // Only clear SSC when we know the search came from a specific non-SSC tab.
    // When activeElement is null (programmatic data injection), distribute to all components.
    const shouldClearSsc = sscElement && activeElement !== null && activeElement !== sscElement;

    Object.values(this.componentsList).forEach(element => {
      if (element === null || element === activeElement || !newResponse) return;

      // When sscQueryString is set, clear SSC data on the SSC-showing component
      // if the search was triggered from a non-SSC tab (prevents viewing SSC without logging)
      if (shouldClearSsc && element === sscElement) {
        if (this.separateSsc) {
          // SSC-only component: reset to empty state
          (element as VehicleWarrantyDetails).clearData();
        } else {
          // Combined warranty+SSC: distribute data with SSC stripped
          element.fetchVin({ ...newResponse, ssc: [], sscLogId: null });
        }
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

    this.componentsList[componentTags.vehicleClaimableItems].headers = headers;

    if (!activeElement) return;

    this.searchGeneration++;

    activeElement.fetchVin(vin, headers);
  }
  // #endregion
  render() {
    const props = {
      [componentTags.vehicleAccessories]: {},
      [componentTags.vehicleSpecification]: {},
      [componentTags.vehicleClaimableItems]: {},
      [componentTags.vehiclePaintThickness]: {},
      [componentTags.vehicleServiceHistory]: {},
      [componentTags.vehicleWarrantyDetails]: {},
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

    const hiddenSet = new Set(this.hiddenTabs.split(',').map(t => t.trim()).filter(Boolean));

    if (!Object.values(componentTags).includes(this.activeElement as any) || hiddenSet.has(this.activeElement))
      return <div class="w-full h-[200px] text-[26px] text-red-600 flex items-center justify-center">Invalid tag</div>;

    const allComponents: Partial<Record<ActiveElement, Node>> = {
      'vehicle-specification': (
        <vehicle-specification
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
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
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleSaleInformation}
          {...props[componentTags.vehicleSaleInformation]}
        />
      ),
      'vehicle-warranty-details': (
        <vehicle-warranty-details
          coreOnly
          show-ssc={!this.separateSsc}
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          show-warranty="true"
          base-url={this.baseUrl}
          language={this.language}
          query-string={!this.separateSsc && this.sscQueryString ? [this.queryString, this.sscQueryString].filter(Boolean).join('&') : this.queryString}
          id={componentTags.vehicleWarrantyDetails}
          {...props[componentTags.vehicleWarrantyDetails]}
        >
        </vehicle-warranty-details>
      ),
      ...(this.separateSsc
        ? {
            'vehicle-ssc': (
              <vehicle-warranty-details
                coreOnly
                show-ssc="true"
                isDev={this.isDev}
                disableVinValidation={this.disableVinValidation}
                show-warranty="false"
                base-url={this.baseUrl}
                language={this.language}
                query-string={[this.queryString, this.sscQueryString].filter(Boolean).join('&')}
                id={componentTags.vehicleSsc}
                {...props[componentTags.vehicleSsc]}
              />
            ),
          }
        : {}),
      'vehicle-service-history': (
        <vehicle-service-history
          coreOnly
          isDev={this.isDev}
          disableVinValidation={this.disableVinValidation}
          base-url={this.baseUrl}
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
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleClaimableItems}
          {...props[componentTags.vehicleClaimableItems]}
        />
      ),
    };

    const componentList = Object.fromEntries(
      Object.entries(allComponents).filter(([key]) => !hiddenSet.has(key))
    ) as Partial<Record<ActiveElement, Node>>;

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          header={this.currentVin}
          isLoading={this.isLoading}
          direction={this.locale.direction}
          errorMessage={this.errorMessage || this.locale.errors.wildCard}
        >
          <shift-tab-content components={componentList} activeComponent={this.activeElement}></shift-tab-content>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
