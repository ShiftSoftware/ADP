import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { AppStates, MockJson } from '~types/components';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import { getPartInformation, PartInformationInterface } from '~api/partInformation';

import manufacturerSchema from '~locales/partLookup/manufacturer/type';

import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

let mockData: MockJson<PartLookupDTO> = {};

@Component({
  shadow: true,
  tag: 'manufacturer-lookup',
  styleUrl: 'manufacturer-lookup.css',
})
export class ManufacturerLookup implements PartInformationInterface {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() hiddenFields: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() localizationName?: string = '';
  @Prop() headerTitle: string = 'Manufacturer';
  @Prop() errorCallback: (errorMessage: string) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: PartLookupDTO) => void;

  @State() state: AppStates = 'idle';
  @State() errorMessage?: ErrorKeys = null;
  @State() partInformation?: PartLookupDTO;
  @State() externalPartNumber?: string = null;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof manufacturerSchema> = manufacturerSchema.getDefault();

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'partLookup.manufacturer', manufacturerSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  private handleSettingData(response: PartLookupDTO) {
    this.partInformation = response;
  }

  @Method()
  async setData(newData: PartLookupDTO | string, headers: any = {}) {
    clearTimeout(this.networkTimeoutRef);
    if (this.abortController) this.abortController.abort();
    this.abortController = new AbortController();
    let scopedTimeoutRef: ReturnType<typeof setTimeout>;

    const isPartNumberRequest = typeof newData === 'string';

    const partNumber = isPartNumberRequest ? newData : newData?.partNumber;
    this.externalPartNumber = partNumber;

    try {
      if (!partNumber || partNumber.trim().length === 0) {
        this.state = 'idle';
        return;
      }

      if (this.state === 'data' || this.state === 'error') {
        this.state = (this.state + '-loading') as 'data-loading' | 'error-loading';
      } else this.state = 'loading';

      await new Promise(r => {
        scopedTimeoutRef = setTimeout(r, 700);
        this.networkTimeoutRef = scopedTimeoutRef;
      });

      const partResponse = isPartNumberRequest ? await getPartInformation(this, { scopedTimeoutRef, partNumber, mockData }, headers) : newData;

      if (this.networkTimeoutRef === scopedTimeoutRef) {
        if (!partResponse) throw new Error('wrongResponseFormat');

        this.handleSettingData(partResponse);
      }

      this.errorMessage = null;
      this.state = 'data';
    } catch (error) {
      if (error && error?.name === 'AbortError') return;
      if (this.errorCallback) this.errorCallback(error.message);
      console.error(error);
      this.setErrorMessage(error.message);
    }
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    this.state = 'error';
    this.partInformation = null;
    this.errorMessage = message;
  }

  @Method()
  async fetchData(partNumber: string = this.externalPartNumber, headers: any = {}) {
    await this.setData(partNumber, headers);
  }

  @Watch('state')
  async loadingListener() {
    if (this.loadingStateChange) this.loadingStateChange(this.state.includes('loading'));
  }

  @Method()
  async setMockData(newMockData: MockJson<PartLookupDTO>) {
    mockData = newMockData;
  }

  render() {
    const texts = this.locale;

    const localName = this.partInformation ? this.localizationName || 'russian' : 'russian';

    const hiddenFields = this.partInformation ? this.hiddenFields.split(',').map(field => field.trim()) || [] : [];

    const manufacturerData = this.partInformation
      ? [
          { label: texts.origin, key: 'origin', value: this.partInformation.origin },
          {
            label: texts.warrantyPrice,
            key: 'warrantyPrice',
            value: null,
            values: this.partInformation.prices.map(price => {
              return { header: price?.countryName, body: price?.warrantyPrice?.formattedValue };
            }),
          },
          { label: texts.specialPrice, key: 'specialPrice', value: null },
          { label: texts.wholesalesPrice, key: 'salesPrice', value: null },
          { label: texts.pnc, key: 'pnc', value: this.partInformation.pnc },
          { label: texts.pncName.replace('$', localName), key: 'pnc', value: this.partInformation.pnc },
          { label: texts.binCode, key: 'binType', value: this.partInformation.binType },
          { label: texts.length, key: 'length', value: this.partInformation.length },
          { label: texts.width, key: 'width', value: this.partInformation.width },
          { label: texts.height, key: 'height', value: this.partInformation.height },
          { label: texts.netWeight, key: 'netWeight', value: this.partInformation.netWeight },
          { label: texts.grossWeight, key: 'grossWeight', value: this.partInformation.grossWeight },
          { label: texts.cubicMeasure, key: 'cubicMeasure', value: this.partInformation.cubicMeasure },
          { label: texts.hsCode, key: 'hsCode', value: this.partInformation.hsCode },
          { label: texts.uzHsCode, key: 'hsCode', value: this.partInformation.hsCode },
        ]
      : [];

    const displayedManufacturerData = manufacturerData.filter(part => !hiddenFields.includes(part.key));

    return (
      <Host>
        <div dir={this.sharedLocales.direction} class="min-h-[100px] relative transition-all duration-300 overflow-hidden">
          <div>
            <loading-spinner isLoading={this.state.includes('loading')} />
            <div class={cn('transition-all !duration-700', { 'scale-0': this.state.includes('loading') || this.state === 'idle', 'opacity-0': this.state.includes('loading') })}>
              {['error', 'error-loading'].includes(this.state) && (
                <div class="py-[16px] min-h-[100px] flex items-center">
                  <div class=" px-[16px] py-[8px] border reject-card text-[20px] rounded-[8px] w-fit mx-auto">
                    {this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
                  </div>
                </div>
              )}

              {['data', 'data-loading'].includes(this.state) && (
                <div>
                  <div class="flex mt-[12px] overflow-hidden rounded-[4px] flex-col border border-[#d6d8dc]">
                    <div class="w-full h-[40px] flex shrink-0 justify-center text-[18px] items-center text-[#383c43] text-center bg-[#e1e3e5]">{this.headerTitle}</div>

                    <div class="px-[30px] py-[10px] flex flex-col gap-[15px]">
                      <div class="grid grid-cols-3 gap-[50px]">
                        {displayedManufacturerData.map(({ label, value, values, key }) => (
                          <div key={key} class="flex flex-col flex-1">
                            <strong class="py-[10px] px-0 border-b-[gray] border-b">{label}</strong>
                            {values ? (
                              <div>
                                {values
                                  .filter(x => x.body)
                                  .map(x => (
                                    <span
                                      key={x.header + x.body}
                                      class="inline-flex items-center bg-red-50 text-red-800 text-sm font-medium px-3 py-1 me-1 mt-2 rounded-lg border border-red-300"
                                    >
                                      {x.header && <span class="font-semibold">{x.header}:</span>}
                                      <span class="ml-1">{x.body}</span>
                                    </span>
                                  ))}
                              </div>
                            ) : (
                              <div class="py-[10px] px-0">{value}</div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
