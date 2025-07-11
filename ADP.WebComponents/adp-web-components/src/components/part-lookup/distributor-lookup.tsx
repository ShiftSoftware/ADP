import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { InformationTableColumn } from '../components/information-table';
import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import distributerSchema from '~locales/partLookup/distributor/type';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { PartLookupComponent, PartLookupMock, setPartLookupData, setPartLookupErrorState } from '~features/part-lookup-components';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'distributor-lookup',
  styleUrl: 'distributor-lookup.css',
})
export class DistributorLookup implements MultiLingual, VehicleInfoLayoutInterface, PartLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof distributerSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...distributerSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.distributor', distributerSchema)]);
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
    await setPartLookupData(this, newData, headers);
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

  // ===== Start Component Logic

  @Prop() hiddenFields?: string = '';
  @Prop() localizationName?: string = '';

  render() {
    const localName = this.partLookup ? this.localizationName || 'russian' : 'russian';

    const hiddenFields = this.hiddenFields?.split(',')?.map(field => field.trim()) || [];

    const partsInformation = [
      { label: this.locale.description, key: 'partDescription', value: this.partLookup?.partDescription },
      { label: this.locale.productGroup, key: 'productGroup', value: this.partLookup?.productGroup },
      {
        key: 'localDescription',
        value: this.partLookup?.localDescription,
        label: this.locale.localDescription.replace('$', localName),
      },
      {
        key: 'purchasePrice',
        label: this.locale.dealerPurchasePrice,
        values: this.partLookup?.prices?.map(price => ({ header: price?.countryName, body: price?.purchasePrice?.formattedValue })),
      },
      {
        key: 'retailPrice',
        label: this.locale.recommendedRetailPrice,
        values: this.partLookup?.prices?.map(price => ({ header: price?.countryName, body: price?.retailPrice?.formattedValue })),
      },
      {
        key: 'supersededTo',
        label: this.locale.supersessions,
        values: this.partLookup?.supersededTo?.map(part => ({ header: null, body: part })),
      },
    ];

    const displayedFields = partsInformation.filter(part => !hiddenFields.includes(part.key));

    const tableHeaders: InformationTableColumn[] = [
      {
        width: 300,
        key: 'locationName',
        label: this.locale.location,
      },
      {
        width: 300,
        key: 'quantityLookUpResult',
        label: this.locale.availability,
      },
    ];

    const displayDistributer = !this.partLookup?.stockParts.some(
      ({ quantityLookUpResult }) => quantityLookUpResult === 'LookupIsSkipped' || quantityLookUpResult === 'QuantityNotWithinLookupThreshold',
    );

    const rows = displayDistributer
      ? this.partLookup?.stockParts.map(stock => ({
          locationName: stock.locationName,
          quantityLookUpResult: () => (
            <div
              class={cn('text-[red]', {
                'text-[green]': stock.quantityLookUpResult === 'Available',
                'text-[orange]': stock.quantityLookUpResult === 'PartiallyAvailable',
              })}
            >
              <strong>
                {stock.quantityLookUpResult === 'Available'
                  ? this.locale.available
                  : stock.quantityLookUpResult === 'PartiallyAvailable'
                    ? this.locale.partiallyAvailable
                    : this.locale.notAvailable}
              </strong>
            </div>
          ),
        }))
      : [];

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.partLookup?.partNumber}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="p-[16px]">
            <flexible-container>
              <div class="flex [&>div]:grow overflow-auto gap-[16px] items-stretch justify-center md:justify-between flex-wrap">
                {displayedFields.map(({ label, value, values }) =>
                  values ? (
                    <MaterialCard title={label} minWidth="250px">
                      <MaterialCardChildren class="flex flex-wrap gap-[8px] p-[2px]">
                        {values
                          .filter(x => x.body)
                          .map(x => (
                            <span class="inline-flex items-center bg-red-50 text-red-800 font-medium px-3 text-[16px] py-1 me-1 mt-2 rounded-lg border border-red-300">
                              {x.header && <span class="font-semibold">{x.header}:</span>}
                              <span class="ml-1">{x.body}</span>
                            </span>
                          ))}
                      </MaterialCardChildren>
                    </MaterialCard>
                  ) : (
                    <MaterialCard desc={value?.toString() || ''} title={label} minWidth="250px" />
                  ),
                )}
              </div>
            </flexible-container>

            <div class="mt-[32px] mx-auto w-fit max-w-full">
              <div class="bg-[#f6f6f6] h-[50px] flex items-center justify-center px-[16px] font-bold text-[18px]">{this.locale.distributorStock}</div>
              <div class="overflow-x-auto">
                <information-table isLoading={this.isLoading} rows={rows} headers={tableHeaders} />
              </div>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
