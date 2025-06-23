import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import validateVin from '~lib/validate-vin';
import { DotNetObjectReference } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import vehicleLookupWrapperSchema from '~locales/vehicleLookup/wrapper-type';

import { VehicleAccessories } from './vehicle-accessories';
import { VehicleSpecification } from './vehicle-specification';
import { VehicleClaimableItems } from './vehicle-claimable-items';
import { VehiclePaintThickness } from './vehicle-paint-thickness';
import { VehicleServiceHistory } from './vehicle-service-history';
import { VehicleWarrantyDetails } from './vehicle-warranty-details';

import { VehicleInfoLayout } from '../../features/vehicle-info-layout/vehicle-info-layout';

import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

const componentTags = {
  vehicleAccessories: 'vehicle-accessories',
  vehicleSpecification: 'vehicle-specification',
  vehiclePaintThickness: 'vehicle-paint-thickness',
  vehicleServiceHistory: 'vehicle-service-history',
  vehicleClaimableItems: 'vehicle-claimable-items',
  vehicleWarrantyDetails: 'vehicle-warranty-details',
} as const;

export type ComponentMap = {
  [componentTags.vehicleAccessories]: VehicleAccessories;
  [componentTags.vehicleSpecification]: VehicleSpecification;
  [componentTags.vehicleServiceHistory]: VehicleServiceHistory;
  [componentTags.vehiclePaintThickness]: VehiclePaintThickness;
  [componentTags.vehicleClaimableItems]: VehicleClaimableItems;
  [componentTags.vehicleWarrantyDetails]: VehicleWarrantyDetails;
};

export type ActiveElement = (typeof componentTags)[keyof typeof componentTags] | '';

@Component({
  shadow: false,
  tag: 'vehicle-lookup',
  styleUrl: 'vehicle-lookup.css',
})
export class VehicleLookup {
  @Prop() activeElement?: ActiveElement = '';

  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() childrenProps?: string | Object;

  @Prop() errorStateListener?: (newError: string) => void;
  @Prop() blazorErrorStateListener = '';

  @Prop() loadingStateChanged?: (isLoading: boolean) => void;
  @Prop() blazorOnLoadingStateChange = '';

  @Prop() dynamicClaimActivate?: (vehicleInformation: VehicleLookupDTO) => void;
  @Prop() blazorDynamicClaimActivate = '';

  @State() errorKey: ErrorKeys;
  @State() wrapperErrorState = '';
  @State() currentVin: string = '';
  @State() isError: boolean = false;
  @State() isLoading: boolean = false;
  @State() blazorRef?: DotNetObjectReference;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup', vehicleLookupWrapperSchema), getSharedLocal(newLanguage)]);
    this.sharedLocales = localeResponses[1];
  }

  async componentDidLoad() {
    const vehicleAccessories = this.el.getElementsByTagName('vehicle-accessories')[0] as unknown as VehicleAccessories;
    const vehicleClaim = this.el.getElementsByTagName('vehicle-claimable-items')[0] as unknown as VehicleClaimableItems;
    const vehicleHistory = this.el.getElementsByTagName('vehicle-service-history')[0] as unknown as VehicleServiceHistory;
    const vehicleDetails = this.el.getElementsByTagName('vehicle-warranty-details')[0] as unknown as VehicleWarrantyDetails;
    const vehicleThickness = this.el.getElementsByTagName('vehicle-paint-thickness')[0] as unknown as VehiclePaintThickness;
    const vehicleSpecification = this.el.getElementsByTagName('vehicle-specification')[0] as unknown as VehicleSpecification;

    this.componentsList = {
      [componentTags.vehicleClaimableItems]: vehicleClaim,
      [componentTags.vehicleServiceHistory]: vehicleHistory,
      [componentTags.vehicleWarrantyDetails]: vehicleDetails,
      [componentTags.vehicleAccessories]: vehicleAccessories,
      [componentTags.vehiclePaintThickness]: vehicleThickness,
      [componentTags.vehicleSpecification]: vehicleSpecification,
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
    this.errorKey = newErrorMessage;
    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  @Method()
  handleLoadData(newResponse: VehicleLookupDTO, activeElement) {
    this.isError = false;
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

  @Watch('wrapperErrorState')
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

    this.wrapperErrorState = '';

    this.componentsList[componentTags.vehicleClaimableItems].headers = headers;

    if (!activeElement) return;

    if (vin == '') return (this.wrapperErrorState = this.sharedLocales.errors.vinNumberRequired);

    if (!validateVin(vin)) return (this.wrapperErrorState = this.sharedLocales.errors.invalidVin);

    activeElement.fetchData(vin, headers);
  }

  render() {
    const props = {
      [componentTags.vehicleAccessories]: {},
      [componentTags.vehicleSpecification]: {},
      [componentTags.vehicleClaimableItems]: {},
      [componentTags.vehiclePaintThickness]: {},
      [componentTags.vehicleServiceHistory]: {},
      [componentTags.vehicleWarrantyDetails]: {},
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
          {...props[componentTags.vehicleSpecification]}
        ></vehicle-specification>
      ),
      'vehicle-accessories': (
        <vehicle-accessories
          coreOnly
          isDev={this.isDev}
          base-url={this.baseUrl}
          language={this.language}
          query-string={this.queryString}
          {...props[componentTags.vehicleAccessories]}
        ></vehicle-accessories>
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
          {...props[componentTags.vehicleServiceHistory]}
        ></vehicle-service-history>
      ),
      'vehicle-paint-thickness': (
        <vehicle-paint-thickness
          coreOnly
          isDev={this.isDev}
          base-url={this.baseUrl}
          language={this.language}
          query-string={this.queryString}
          {...props[componentTags.vehiclePaintThickness]}
        ></vehicle-paint-thickness>
      ),
      'vehicle-claimable-items': (
        <vehicle-claimable-items
          coreOnly
          isDev={this.isDev}
          base-url={this.baseUrl}
          language={this.language}
          query-string={this.queryString}
          {...props[componentTags.vehicleClaimableItems]}
        ></vehicle-claimable-items>
      ),
    };

    return (
      <Host>
        <VehicleInfoLayout
          vin={this.currentVin}
          isError={this.isError}
          isLoading={this.isLoading}
          direction={this.sharedLocales.direction}
          errorMessage={this.sharedLocales.errors[this.errorKey] || this.sharedLocales.errors.wildCard}
        >
          <shift-tab-content components={componentList} activeComponent={this.activeElement}></shift-tab-content>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
