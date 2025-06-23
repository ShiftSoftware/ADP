import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { AppStates, MockJson } from '~types/components';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import cn from '~lib/cn';

import { getPartInformation, PartInformationInterface } from '~api/partInformation';

import deadStockSchema from '~locales/partLookup/deadStock/type';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

let mockData: MockJson<PartLookupDTO> = {};

const closedAccordionHeight = '46px' as const;

@Component({
  tag: 'dead-stock-lookup',
  styleUrl: 'dead-stock-lookup.css',
  shadow: true,
})
export class DeadStockLookup implements PartInformationInterface {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: PartLookupDTO) => void;

  @State() state: AppStates = 'idle';
  @State() errorMessage?: ErrorKeys = null;
  @State() partInformation?: PartLookupDTO;
  @State() externalPartNumber?: string = null;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof deadStockSchema> = deadStockSchema.getDefault();

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'partLookup.deadStock', deadStockSchema), getSharedLocal(newLanguage)]);
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

  private toggleAccordion(event: MouseEvent) {
    const container = (event.target as HTMLButtonElement).parentElement as HTMLDivElement;
    const icon = container.getElementsByClassName('icon')[0] as HTMLSpanElement;

    const isExpanded = container.getAttribute('aria-expanded') === 'true';

    if (isExpanded) {
      container.style.height = closedAccordionHeight;
      icon.style.transform = 'rotate(0deg)';
      container.setAttribute('aria-expanded', 'false');
    } else {
      const content = container.getElementsByClassName('content')[0] as HTMLDivElement;
      const totalHeight = +closedAccordionHeight.replace('px', '') + content.getClientRects()[0].height;

      container.style.height = `${totalHeight}px`;
      icon.style.transform = 'rotate(180deg)';
      container.setAttribute('aria-expanded', 'true');
    }
  }

  render() {
    const texts = this.locale;

    return (
      <Host>
        <div dir={this.sharedLocales.direction} class="min-h-[100px] relative transition-all duration-300 overflow-hidden">
          <div>
            <loading-spinner isLoading={this.state.includes('loading')} />
            <div class={cn('transition-all !duration-700', { 'scale-0': this.state.includes('loading') || this.state === 'idle', 'opacity-0': this.state.includes('loading') })}>
              {['error', 'error-loading'].includes(this.state) && (
                <div class="py-[16px] min-h-[100px] flex items-center">
                  <div class="px-[16px] py-[8px] border reject-card text-[20px] rounded-[8px] w-fit mx-auto">
                    {this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
                  </div>
                </div>
              )}

              {['data', 'data-loading'].includes(this.state) && (
                <div>
                  <div class="flex mt-[12px] max-h-[70dvh] overflow-hidden rounded-[4px] flex-col border border-[#d6d8dc]">
                    <div class="w-full h-[40px] flex shrink-0 justify-center text-[18px] items-center text-[#383c43] text-center bg-[#e1e3e5]">{texts.deadStock}</div>

                    {!this.partInformation?.deadStock?.length && <div class="h-[80px] flex items-center justify-center text-[18px]">{this.sharedLocales.noData}</div>}
                    {this.partInformation?.deadStock?.map(deadStock => (
                      <div key={deadStock.companyIntegrationID} class="py-[10px] px-[20px]">
                        <div
                          aria-expanded="false"
                          style={{ height: closedAccordionHeight }}
                          class="border shadow transition-all duration-500 overflow-hidden rounded-md mb-[10px] border-[#d6d8dc]"
                        >
                          <button
                            onClick={this.toggleAccordion}
                            style={{ height: closedAccordionHeight }}
                            class="w-full flex px-[10px] justify-between items-center text-slate-800"
                          >
                            <strong>{deadStock.companyName}</strong>
                            <span class="icon text-slate-800 transition-transform duration-500">
                              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" class="size-[22px]">
                                <path
                                  fill-rule="evenodd"
                                  clip-rule="evenodd"
                                  d="M11.78 9.78a.75.75 0 0 1-1.06 0L8 7.06 5.28 9.78a.75.75 0 0 1-1.06-1.06l3.25-3.25a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06Z"
                                />
                              </svg>
                            </span>
                          </button>

                          <div class="content">
                            <table class="w-full overflow-auto relative border-collapse">
                              <thead>
                                <tr>
                                  <th class="py-[20px] px-[10px] text-start whitespace-nowrap border-b border-[#d6d8dc]">{texts.branch}</th>
                                  <th class="py-[20px] px-[10px] text-start whitespace-nowrap border-b border-[#d6d8dc]">{texts.availableQuantity}</th>
                                </tr>
                              </thead>

                              <tbody>
                                {deadStock?.branchDeadStock.map(branchDeadStock => (
                                  <tr
                                    class="transition-colors border-b border-[#d6d8dc] last:border-none duration-100 hover:bg-slate-100"
                                    key={branchDeadStock.companyBranchIntegrationID}
                                  >
                                    <td class={cn('py-[20px] px-[10px] text-start whitespace-nowrap')}>{branchDeadStock.companyBranchName}</td>

                                    <td class={cn('py-[20px] px-[10px] text-start whitespace-nowrap')}>
                                      <strong>{branchDeadStock.quantity}</strong>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>
                        </div>
                      </div>
                    ))}
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
