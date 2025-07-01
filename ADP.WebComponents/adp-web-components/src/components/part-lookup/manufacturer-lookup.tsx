import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import manufacturerSchema from '~locales/partLookup/manufacturer/type';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { PartLookupComponent, PartLookupMock, setPartLookupData, setPartLookupErrorState } from '~features/part-lookup-components';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'manufacturer-lookup',
  styleUrl: 'manufacturer-lookup.css',
})
export class ManufacturerLookup implements MultiLingual, VehicleInfoLayoutInterface, PartLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof manufacturerSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...manufacturerSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.manufacturer', manufacturerSchema)]);
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

  // ====== Start Component Logic

  @Prop() hiddenFields: string = '';
  @Prop() localizationName?: string = '';

  render() {
    const localName = this.partLookup ? this.localizationName || 'russian' : 'russian';

    const hiddenFields = this.hiddenFields?.split(',').map(field => field?.trim()) || [];

    const manufacturerData = [
      { label: this.locale.origin, key: 'origin', value: this.partLookup?.origin },
      {
        label: this.locale.warrantyPrice,
        key: 'warrantyPrice',
        values: this.partLookup?.prices.map(price => {
          return { header: price?.countryName, body: price?.warrantyPrice?.formattedValue };
        }),
      },
      { label: this.locale.specialPrice, key: 'specialPrice' },
      { label: this.locale.wholesalesPrice, key: 'salesPrice' },
      { label: this.locale.pnc, key: 'pnc', value: this.partLookup?.pnc },
      { label: this.locale.pncName.replace('$', localName), key: 'pnc', value: this.partLookup?.pnc },
      { label: this.locale.binCode, key: 'binType', value: this.partLookup?.binType },
      { label: this.locale.length, key: 'length', value: this.partLookup?.length },
      { label: this.locale.width, key: 'width', value: this.partLookup?.width },
      { label: this.locale.height, key: 'height', value: this.partLookup?.height },
      { label: this.locale.netWeight, key: 'netWeight', value: this.partLookup?.netWeight },
      { label: this.locale.grossWeight, key: 'grossWeight', value: this.partLookup?.grossWeight },
      { label: this.locale.cubicMeasure, key: 'cubicMeasure', value: this.partLookup?.cubicMeasure },
      { label: this.locale.hsCode, key: 'hsCode', value: this.partLookup?.hsCode },
      { label: this.locale.uzHsCode, key: 'hsCode', value: this.partLookup?.hsCode },
    ];

    const displayedManufacturerData = manufacturerData.filter(part => !hiddenFields.includes(part.key));

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
          <flexible-container>
            <div class="flex p-[16px] [&>div]:grow overflow-auto gap-[16px] items-stretch justify-center md:justify-between flex-wrap">
              {displayedManufacturerData.map(({ label, value, values }) =>
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
        </VehicleInfoLayout>
      </Host>
    );
  }
}
