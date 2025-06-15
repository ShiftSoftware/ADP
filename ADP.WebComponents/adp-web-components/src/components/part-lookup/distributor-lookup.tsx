import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import { LanguageKeys } from '~types/locale';
import { AppStates, MockJson } from '~types/components';
import { PartInformation } from '~types/part-information';

import { getPartInformation, PartInformationInterface } from '~api/partInformation';
import distributerSchema from '~locales/partLookup/distributor/type';

let mockData: MockJson<PartInformation> = {};

@Component({
  shadow: true,
  tag: 'distributor-lookup',
  styleUrl: 'distributor-lookup.css',
})
export class DistributorLookup implements PartInformationInterface {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() hiddenFields?: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() localizationName?: string = '';
  @Prop() errorCallback: (errorMessage: string) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: PartInformation) => void;

  @State() state: AppStates = 'idle';
  @State() errorMessage?: ErrorKeys = null;
  @State() partInformation?: PartInformation;
  @State() externalPartNumber?: string = null;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof distributerSchema> = distributerSchema.getDefault();

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'partLookup.distributor', distributerSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  private handleSettingData(response: PartInformation) {
    this.partInformation = response;
  }

  @Method()
  async setData(newData: PartInformation | string, headers: any = {}) {
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
  async setMockData(newMockData: MockJson<PartInformation>) {
    mockData = newMockData;
  }

  render() {
    const texts = this.locale;

    const localName = this.partInformation ? this.localizationName || 'russian' : 'russian';

    const hiddenFields = this.partInformation ? this.hiddenFields.split(',')?.map(field => field.trim()) || [] : [];

    const partsInformation = this.partInformation
      ? [
          { label: texts.description, key: 'partDescription', value: this.partInformation.partDescription },
          { label: texts.productGroup, key: 'group', value: this.partInformation.group },
          {
            label: texts.localDescription.replace('$', localName),
            key: 'localDescription',
            value: this.partInformation.localDescription,
          },
          {
            label: texts.dealerPurchasePrice,
            key: 'purchasePrice',
            value: null,
            values: this.partInformation.prices?.map(price => {
              return { header: price?.countryName, body: price?.purchasePrice?.formattedValue };
            }),
          },
          {
            label: texts.recommendedRetailPrice,
            key: 'retailPrice',
            value: null,
            values: this.partInformation.prices?.map(price => {
              return { header: price?.countryName, body: price?.retailPrice?.formattedValue };
            }),
          },
          {
            label: texts.supersessions,
            key: 'supersededTo',
            value: null,
            values: this.partInformation.supersededTo?.map(part => {
              return { header: null, body: part };
            }),
          },
        ]
      : [];

    const displayedFields = partsInformation.filter(part => !hiddenFields.includes(part.key));

    const displayDistributer = this.partInformation
      ? !this.partInformation.stockParts.some(
          ({ quantityLookUpResult }) => quantityLookUpResult === 'LookupIsSkipped' || quantityLookUpResult === 'QuantityNotWithinLookupThreshold',
        )
      : false;

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
                  <div class="flex mt-[12px] max-h-[70dvh] overflow-hidden rounded-[4px] flex-col border border-[#d6d8dc]">
                    <div class="w-full h-[40px] flex shrink-0 justify-center text-[18px] items-center text-[#383c43] text-center bg-[#e1e3e5]">{texts.info}</div>

                    <div class="py-[10px] px-[30px] flex flex-col gap-[15px]">
                      <div class="grid grid-cols-3 gap-[50px]">
                        {displayedFields.map(({ label, value, values, key }) => (
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

                  {displayDistributer && (
                    <div class="flex mt-[12px] max-h-[70dvh] overflow-hidden rounded-[4px] flex-col border border-[#d6d8dc]">
                      <div class="w-full h-[40px] flex shrink-0 justify-center text-[18px] items-center text-[#383c43] text-center bg-[#e1e3e5]">{texts.distributorStock}</div>

                      <div class="flex flex-col gap-[15px]">
                        <table class="w-full overflow-auto relative border-collapse">
                          <thead class="top-0 font-bold sticky bg-white">
                            <tr>
                              {[texts.location, texts.availability].map(title => (
                                <th key={title} class="px-[10px] py-[20px] text-center whitespace-nowrap border-b border-[#d6d8dc]">
                                  {title}
                                </th>
                              ))}
                            </tr>
                          </thead>

                          <tbody>
                            {this.partInformation?.stockParts?.map(stock => (
                              <tr class="transition-colors duration-100 border-b border-[#d6d8dc] last:border-none hover:bg-slate-100" key={stock.locationID}>
                                <td class={cn('px-[10px] py-[20px] text-center whitespace-nowrap')}>{stock.locationName}</td>

                                <td class={cn('px-[10px] py-[20px] text-center whitespace-nowrap')}>
                                  <div
                                    class={cn('text-[red]', {
                                      'text-[green]': stock.quantityLookUpResult === 'Available',
                                      'text-[orange]': stock.quantityLookUpResult === 'PartiallyAvailable',
                                    })}
                                  >
                                    <strong>
                                      {stock.quantityLookUpResult === 'Available'
                                        ? texts.available
                                        : stock.quantityLookUpResult === 'PartiallyAvailable'
                                          ? texts.partiallyAvailable
                                          : texts.notAvailable}
                                    </strong>
                                  </div>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
