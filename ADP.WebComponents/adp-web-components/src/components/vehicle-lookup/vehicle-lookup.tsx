import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

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
} as const;

export type ComponentMap = {
  [componentTags.vehicleAccessories]: VehicleAccessories;
  [componentTags.vehicleSpecification]: VehicleSpecification;
  [componentTags.vehicleServiceHistory]: VehicleServiceHistory;
  [componentTags.vehiclePaintThickness]: VehiclePaintThickness;
  [componentTags.vehicleClaimableItems]: VehicleClaimableItems;
  [componentTags.vehicleSaleInformation]: VehicleSaleInformation;
  [componentTags.vehicleWarrantyDetails]: VehicleWarrantyDetails;
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
  @Prop() queryString: string = '';
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

    this.componentsList = {
      [componentTags.vehicleClaimableItems]: vehicleClaim,
      [componentTags.vehicleServiceHistory]: vehicleHistory,
      [componentTags.vehicleWarrantyDetails]: vehicleDetails,
      [componentTags.vehicleAccessories]: vehicleAccessories,
      [componentTags.vehiclePaintThickness]: vehicleThickness,
      [componentTags.vehicleSpecification]: vehicleSpecification,
      [componentTags.vehicleSaleInformation]: vehicleSaleInformation,
    } as const;

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
  }

  private syncErrorAcrossComponents = (newErrorMessage: ErrorKeys) => {
    this.isError = true;
    this.errorMessage = this.locale?.errors?.[newErrorMessage] || this.locale.errors.wildCard;

    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  @Method()
  async handleLoadData(newResponse: VehicleLookupDTO, activeElement) {
    this.isError = false;
    this.errorMessage = '';
    this.currentVin = newResponse.vin || '';
    Object.values(this.componentsList).forEach(element => {
      if (element !== null && element !== activeElement && newResponse) element.fetchData(newResponse);
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

    activeElement.fetchData(vin, headers);
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

    if (!Object.values(componentTags).includes(this.activeElement as any))
      return <div class="w-full h-[200px] text-[26px] text-red-600 flex items-center justify-center">Invalid tag</div>;

    const componentList: Partial<Record<ActiveElement, Node>> = {
      'vehicle-specification': (
        <vehicle-specification
          coreOnly
          isDev={this.isDev}
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
          show-ssc="true"
          isDev={this.isDev}
          show-warranty="true"
          base-url={this.baseUrl}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleWarrantyDetails}
          {...props[componentTags.vehicleWarrantyDetails]}
        >
          <slot></slot>
        </vehicle-warranty-details>
      ),
      'vehicle-service-history': (
        <vehicle-service-history
          coreOnly
          isDev={this.isDev}
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
          base-url={this.baseUrl}
          language={this.language}
          query-string={this.queryString}
          id={componentTags.vehicleClaimableItems}
          {...props[componentTags.vehicleClaimableItems]}
        />
      ),
    };

    return (
      <Host>
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
