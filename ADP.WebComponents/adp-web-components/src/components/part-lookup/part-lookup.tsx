import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { DotNetObjectReference } from '~types/components';

import { DeadStockLookup } from './dead-stock-lookup';
import { DistributorLookup } from './distributor-lookup';
import { ManufacturerLookup } from './manufacturer-lookup';

import cn from '~lib/cn';
import partLookupWrapperSchema from '~locales/partLookup/wrapper-type';

import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

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
export class PartLookup {
  @Prop() baseUrl: string = '';
  @Prop() queryString: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() blazorErrorStateListener = '';
  @Prop() childrenProps?: string | Object;
  @Prop() blazorOnLoadingStateChange = '';
  @Prop() activeElement?: ActiveElement = '';
  @Prop() errorStateListener?: (newError: string) => void;
  @Prop() loadingStateChanged?: (isLoading: boolean) => void;

  @State() wrapperErrorState = '';
  @State() blazorRef?: DotNetObjectReference;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();

  @Element() el: HTMLElement;

  private componentsList: ComponentMap;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'partLookup', partLookupWrapperSchema), getSharedLocal(newLanguage)]);
    this.sharedLocales = localeResponses[1];
  }

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
    Object.values(this.componentsList).forEach(element => {
      if (element) element.setErrorMessage(newErrorMessage);
    });
  };

  private handleLoadData(newResponse, activeElement) {
    Object.values(this.componentsList).forEach(element => {
      if (element !== null && element !== activeElement && newResponse) element.setData(newResponse);
    });
  }

  private loadingStateChangingMiddleware = (newState: boolean) => {
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

    if (partNumber == '') return (this.wrapperErrorState = this.sharedLocales.errors.partNumberRequired);

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

    return (
      <Host>
        <div class={cn('w-full', { hidden: this.activeElement !== componentTags.deadStock })}>
          <dead-stock-lookup base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.deadStock]}></dead-stock-lookup>
        </div>

        <div class={cn('w-full', { hidden: this.activeElement !== componentTags.distributor })}>
          <distributor-lookup base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.distributor]}></distributor-lookup>
        </div>

        <div class={cn('w-full', { hidden: this.activeElement !== componentTags.manufacturer })}>
          <manufacturer-lookup base-url={this.baseUrl} language={this.language} query-string={this.queryString} {...props[componentTags.manufacturer]}></manufacturer-lookup>
        </div>

        <slot></slot>
      </Host>
    );
  }
}
