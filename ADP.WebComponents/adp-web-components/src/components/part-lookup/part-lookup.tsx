import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { DeadStockLookup } from './dead-stock-lookup';
import { DistributorLookup } from './distributor-lookup';
import { ManufacturerLookup } from './manufacturer-lookup';

import partLookupWrapperSchema from '~locales/partLookup/wrapper-type';

import { VehicleInfoLayout } from '~features/vehicle-info-layout';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

import { DotNetObjectReference } from '~types/components';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

const componentTags = {
  deadStock: 'dead-stock-lookup',
  distributor: 'distributor-lookup',
  manufacturer: 'manufacturer-lookup',
} as const;

export type ComponentMap = {
  [componentTags.deadStock]: DeadStockLookup;
  [componentTags.distributor]: DistributorLookup;
  [componentTags.manufacturer]: ManufacturerLookup;
};

export type ActiveElement = (typeof componentTags)[keyof typeof componentTags] | '';

@Component({
  shadow: false,
  tag: 'part-lookup',
  styleUrl: 'part-lookup.css',
})
export class PartLookup implements MultiLingual {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: SharedLocales = sharedLocalesSchema.getDefault();

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'partLookup', partLookupWrapperSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[1];
  }

  // ====== End Localization

  // ====== Wrapper Logic

  @Prop() activeElement?: ActiveElement = '';

  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() childrenProps?: string | Object;

  @Prop() blazorErrorStateListener = '';
  @Prop() errorStateListener?: (newError: string) => void;

  @Prop() blazorOnLoadingStateChange = '';
  @Prop() loadingStateChanged?: (isLoading: boolean) => void;

  @State() errorKey: ErrorKeys;
  @State() wrapperErrorState = '';
  @State() isError: boolean = false;
  @State() header: string = '';
  @State() isLoading: boolean = false;

  @State() blazorRef?: DotNetObjectReference;

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  async componentDidLoad() {
    const deadStockLookup = this.el.getElementsByTagName('dead-stock-lookup')[0] as unknown as DeadStockLookup;
    const distributerLookup = this.el.getElementsByTagName('distributor-lookup')[0] as unknown as DistributorLookup;
    const manufacturerLookup = this.el.getElementsByTagName('manufacturer-lookup')[0] as unknown as ManufacturerLookup;

    this.componentsList = {
      [componentTags.deadStock]: deadStockLookup,
      [componentTags.distributor]: distributerLookup,
      [componentTags.manufacturer]: manufacturerLookup,
    } as const;

    Object.values(this.componentsList).forEach(element => {
      if (!element) return;

      element.errorCallback = this.syncErrorAcrossComponents;
      element.loadingStateChange = this.loadingStateChangingMiddleware;
      element.loadedResponse = newResponse => this.handleLoadData(newResponse, element);
    });
  }

  private syncErrorAcrossComponents = (newErrorMessage: ErrorKeys) => {
    this.isError = true;
    this.errorKey = newErrorMessage;
    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  @Method()
  handleLoadData(newResponse: PartLookupDTO, activeElement) {
    this.isError = false;
    this.header = newResponse?.partNumber || '';
    Object.values(this.componentsList).forEach(element => {
      // @ts-ignore
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
  async fetchPartNumber(partNumber: string, quantity: string = '', headers: any = {}) {
    const activeElement = this.componentsList[this.activeElement] || null;

    this.wrapperErrorState = '';

    if (!activeElement) return;

    if (partNumber == '') return (this.wrapperErrorState = this.locale.errors.partNumberRequired);

    const searchingText = quantity.trim() || quantity.trim() === '0' ? `${partNumber.trim()}/${quantity.trim()}` : partNumber.trim();

    activeElement.fetchData(searchingText, headers);
  }

  render() {
    const props = {
      [componentTags.deadStock]: {},
      [componentTags.distributor]: {},
      [componentTags.manufacturer]: {},
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
      'dead-stock-lookup': (
        <dead-stock-lookup coreOnly isDev={this.isDev} base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.deadStock]} />
      ),
      'distributor-lookup': (
        <distributor-lookup coreOnly isDev={this.isDev} base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.distributor]} />
      ),
      'manufacturer-lookup': (
        <manufacturer-lookup coreOnly isDev={this.isDev} base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.manufacturer]} />
      ),
    };
    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          header={this.header}
          isLoading={this.isLoading}
          direction={this.locale.direction}
          errorMessage={this.locale.errors[this.errorKey] || this.locale.errors.wildCard}
        >
          <shift-tab-content components={componentList} activeComponent={this.activeElement}></shift-tab-content>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
