import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { DeadStockLookup } from './dead-stock-lookup';
import { DistributorLookup } from './distributor-lookup';
import { ManufacturerLookup } from './manufacturer-lookup';

import partLookupWrapperSchema from '~locales/partLookup/wrapper-type';

import { VehicleInfoLayout } from '~features/vehicle-info-layout';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';
import { BlazorInvokableFunction, DotNetObjectReference, smartInvokable } from '~features/blazor-ref';
import { Endpoint } from '~lib/fetch-from';

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
  shadow: true,
  tag: 'part-lookup',
  styleUrl: 'part-lookup.css',
})
export class PartLookup implements MultiLingual {
  // #region Localization

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

  // #endregion

  // ====== Wrapper Logic

  @Prop() activeElement?: ActiveElement = '';

  @Prop() mockUrl = '';
  @Prop() endpoint: Endpoint;
  @Prop() isDev: boolean = false;
  @Prop() childrenProps?: string | Object;

  @Prop({ mutable: true }) loadedMockDatas?: BlazorInvokableFunction<(response: any) => void>;
  @Prop({ mutable: true }) errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop({ mutable: true }) loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop({ mutable: true }) loadedResponse?: BlazorInvokableFunction<(response: PartLookupDTO) => void>;

  @State() errorKey: ErrorKeys;
  @State() wrapperErrorState = '';
  @State() isError: boolean = false;
  @State() header: string = '';
  @State() isLoading: boolean = false;

  @State() blazorRef?: DotNetObjectReference;

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  async componentDidLoad() {
    const deadStockLookup = this.el.shadowRoot.getElementById(componentTags.deadStock) as unknown as DeadStockLookup;
    const distributerLookup = this.el.shadowRoot.getElementById(componentTags.distributor) as unknown as DistributorLookup;
    const manufacturerLookup = this.el.shadowRoot.getElementById(componentTags.manufacturer) as unknown as ManufacturerLookup;

    this.componentsList = {
      [componentTags.deadStock]: deadStockLookup,
      [componentTags.distributor]: distributerLookup,
      [componentTags.manufacturer]: manufacturerLookup,
    } as const;

    Object.values(this.componentsList).forEach(element => {
      if (!element) return;

      element.loadedMockDatas = this.onMockDataSetMiddleware;
      element.errorCallback = this.syncErrorAcrossComponents;
      element.loadingStateChange = this.loadingStateChangingMiddleware;
      element.loadedResponse = newResponse => this.handleLoadData(newResponse, element);
    });
  }

  private syncErrorAcrossComponents = (newErrorMessage: ErrorKeys) => {
    this.isError = true;
    this.errorKey = newErrorMessage;
    smartInvokable.bind(this)(this.errorCallback, newErrorMessage);
    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  @Method()
  handleLoadData(newResponse: PartLookupDTO, activeElement) {
    this.isError = false;
    this.header = newResponse?.partNumber || '';
    smartInvokable.bind(this)(this.loadedResponse, newResponse);
    Object.values(this.componentsList).forEach(element => {
      // @ts-ignore
      if (element !== null && element !== activeElement && newResponse) element.fetchData(newResponse);
    });
  }

  private loadingStateChangingMiddleware = (newState: boolean) => {
    this.isLoading = newState;
    smartInvokable.bind(this)(this.loadingStateChange, newState);
  };

  private onMockDataSetMiddleware = (newMockData: any) => {
    smartInvokable.bind(this)(this.loadedMockDatas, newMockData);
  };

  @Watch('wrapperErrorState')
  async errorListener(newState) {
    smartInvokable.bind(this)(this.errorCallback, newState);
  }

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }

  @Method()
  async fetchData(partNumber: string, quantity: string = '') {
    const activeElement = this.componentsList[this.activeElement] || null;

    this.wrapperErrorState = '';

    if (!activeElement) return;

    if (partNumber == '') return (this.wrapperErrorState = this.locale.errors.partNumberRequired);

    const searchingText = quantity.trim() || quantity.trim() === '0' ? `${partNumber.trim()}/${quantity.trim()}` : partNumber.trim();

    activeElement.fetchData(searchingText);
  }

  @Method()
  async getMockData() {
    const activeElement = this.componentsList[this.activeElement] || null;
    const mockData = await activeElement.getMockData();
    return mockData;
  }

  // #endregion
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
        <dead-stock-lookup
          coreOnly
          isDev={this.isDev}
          mock-url={this.mockUrl}
          language={this.language}
          endpoint={this.endpoint}
          id={componentTags.deadStock}
          {...props[componentTags.deadStock]}
        />
      ),
      'distributor-lookup': (
        <distributor-lookup
          coreOnly
          isDev={this.isDev}
          mock-url={this.mockUrl}
          language={this.language}
          endpoint={this.endpoint}
          id={componentTags.distributor}
          {...props[componentTags.distributor]}
        />
      ),
      'manufacturer-lookup': (
        <manufacturer-lookup
          coreOnly
          isDev={this.isDev}
          mock-url={this.mockUrl}
          language={this.language}
          endpoint={this.endpoint}
          id={componentTags.manufacturer}
          {...props[componentTags.manufacturer]}
        />
      ),
    };
    return (
      <Host class="part-lookup">
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
