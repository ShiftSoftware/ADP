import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import deadStockSchema from '~locales/partLookup/deadStock/type';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { PartLookupComponent, PartLookupMock, setPartLookupData, setPartLookupErrorState } from '~features/part-lookup-components';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import { ArrowUpIcon } from '~assets/arrow-up-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';

import { DeadStockItem } from './components/dead-stock-item';

@Component({
  shadow: true,
  tag: 'dead-stock-lookup',
  styleUrl: 'dead-stock-lookup.css',
})
export class DeadStockLookup implements MultiLingual, VehicleInfoLayoutInterface, PartLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof deadStockSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...deadStockSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.deadStock', deadStockSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // ====== End Localization

  // ====== Start Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // ====== End Vehicle info layout prop

  // ====== Start Part Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: PartLookupDTO) => void;

  @State() searchString: string;
  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() partLookup?: PartLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: PartLookupMock) {
    this.mockData = newMockData;
  }

  @Method()
  async fetchData(newData: PartLookupDTO | string, headers: any = {}) {
    await setPartLookupData(this, newData, headers, {
      beforeAssignment: async response => {
        await new Promise(r => setTimeout(r, 500));
        this.openedAccordions = [];
        await new Promise(r => setTimeout(r, 500));
        return response;
      },
    });
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setPartLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  // ====== End Part Lookup Component Shared Logic

  // ====== Start Component Logic

  @State() openedAccordions: string[] = [];
  @State() newItemsHasBeenRendered: boolean = false;

  @Watch('isLoading')
  async loadingListener(newIsLoading: boolean) {
    if (newIsLoading) this.newItemsHasBeenRendered = false;
    else {
      await new Promise(r => setTimeout(r, 100));
      this.newItemsHasBeenRendered = true;
    }
  }

  private toggleAccordion = (header: string) => {
    if (this.isLoading) return;

    if (this.openedAccordions.includes(header)) {
      this.openedAccordions = this.openedAccordions.filter(item => item !== header);
    } else {
      this.openedAccordions = [...this.openedAccordions, header];
    }
  };

  render() {
    // @ts-ignore
    window.ss = this;

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          vin={this.partLookup?.partNumber}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="p-[16px]">
            <div class="pb-[16px]">
              <DeadStockItem
                locale={this.locale}
                key="first-dead-stock"
                toggleAccordion={this.toggleAccordion}
                isOpened={!this.isLoading && this.partLookup?.deadStock?.length > 0 ? this.openedAccordions.includes(this.partLookup?.deadStock[0]?.companyName) : false}
                item={
                  this.partLookup?.deadStock?.length > 0
                    ? this.partLookup?.deadStock[0]
                    : ({ companyName: !!this.partLookup && !this.partLookup?.deadStock ? this.locale.sharedLocales.noData : '' } as any)
                }
                icon={
                  (!this.partLookup && !this.partLookup?.deadStock && <div />) ||
                  (!!this.partLookup && !this.partLookup?.deadStock && <EmptyTableIcon />) ||
                  (!!this.partLookup && !!this.partLookup?.deadStock && <ArrowUpIcon />)
                }
              />
            </div>

            <flexible-container isOpened={this.newItemsHasBeenRendered}>
              <div class="flex flex-col gap-[16px]">
                {this.partLookup?.deadStock?.length > 1 &&
                  this.partLookup?.deadStock
                    .slice(1)
                    .map(deadStock => (
                      <DeadStockItem
                        item={deadStock}
                        locale={this.locale}
                        icon={<ArrowUpIcon />}
                        toggleAccordion={this.toggleAccordion}
                        isOpened={this.openedAccordions.includes(deadStock?.companyName)}
                      />
                    ))}
              </div>
            </flexible-container>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
