import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import distributerSchema from '~locales/partLookup/distributor/type';

import { ComponentLocale, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormHookInterface, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook';
import { InferType, number, object, string } from 'yup';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';
import { EndPoint } from '~types/components';
import getLanguageFromUrl from '~lib/get-language-from-url';
import { CloudAlertIcon } from '~assets/cloud-alert-icon';
import { InfoIcon } from '~assets/info-icon';
import { LaptopMinimalCheckIcon } from '~assets/laptop-minimal-check';
import { OctagonXIcon } from '~assets/octagon-x';

const tmcOrderTypesFetcher: FormSelectFetcher<any> = async ({}): Promise<FormSelectItem[]> => {
  return [
    {
      value: 'V',
      label: 'V',
    },
    {
      value: 'A',
      label: 'A ',
    },
  ] as FormSelectItem[];
};

const tmcLookupValidation = object({
  orderType: string()
    .meta({ label: 'Order Type', placeholder: 'Order Type' } as FormInputMeta)
    .required('This field is required.'),
  quantity: number()
    .meta({ label: 'quantity', placeholder: 'quantity' } as FormInputMeta)
    .required('This field is required.')
    .typeError('This field is required.')
    .min(1, 'This field is required.'),
  partNumber: string()
    .meta({ label: 'Part Number', placeholder: 'Part Number' } as FormInputMeta)
    .required('This field is required.'),
});

export type TmcLookupValidation = InferType<typeof tmcLookupValidation>;

export type Locale = ComponentLocale<typeof distributerSchema>;

const tmcResponses = ['Available', 'NotAvailable', 'PartiallyAvailable', 'RequestFailed'] as const;

export type TmcResponseMessage = (typeof tmcResponses)[number];

@Component({
  shadow: true,
  tag: 'tmc-lookup',
  styleUrl: 'tmc-lookup.css',
})
export class TmcLookup implements MultiLingual, FormHookInterface<TmcLookupValidation> {
  // #region Localization

  @Prop({ mutable: true }) language: LanguageKeys;

  @State() locale: ComponentLocale<typeof distributerSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...distributerSchema.getDefault() };

  async componentWillLoad() {
    if (!this.language) this.language = getLanguageFromUrl();
    await Promise.all([this.changeLanguage(this.language)]);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.distributor', distributerSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Form Hook logic
  @State() isLoading: boolean = false;

  @Element() el: HTMLElement;

  formSubmit = async (payload: TmcLookupValidation) => {
    try {
      if (this.isDev) {
        await new Promise(r => setTimeout(r, 2000));
        this.tmcResponse = tmcResponses[Math.floor(Math.random() * tmcResponses.length)];
        this.form.reset();
        return;
      }

      let tmcEndpoint: EndPoint = typeof this.tmcLookupEndpoint === 'string' ? JSON.parse(this.tmcLookupEndpoint) : this.tmcLookupEndpoint;

      const defaultHeaders: Record<string, string> = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      };

      const config: RequestInit = {
        method: tmcEndpoint?.method || 'POST',
        headers: { ...defaultHeaders, ...(tmcEndpoint?.headers || {}) },
        body: JSON.stringify({ ...payload, ...(tmcEndpoint?.body || {}) }),
      };

      const response = await fetch(tmcEndpoint.url, config);

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const responseType = (await response.json()) as TmcResponseMessage; // 'Available' | 'NotAvailable' | 'PartiallyAvailable' | 'RequestFailed'

      this.form.reset();

      this.tmcResponse = responseType;
    } catch (error) {
      console.error('❌ TMC Lookup request failed:', error);
      this.tmcResponse = 'RequestFailed';
    }
  };
  // #endregion

  // ===== Start Component Logic

  @Prop() isDev?: boolean;
  @Prop() standAlone?: boolean = true;
  @Prop() partLookup?: PartLookupDTO;

  @Prop() closeTmcLookup?: boolean;
  @Prop() tmcLookupEndpoint: EndPoint | string;

  @State() tmcResponse?: TmcResponseMessage;

  form = new FormHook(this, tmcLookupValidation);

  // #endregion

  render() {
    const { formController } = this.form;

    return (
      <Host>
        <flexible-container
          // @ts-ignore
          isOpened={(!!this?.partLookup?.showTMCPartLookup && !this.closeTmcLookup) || this.standAlone}
        >
          <div class="w-full max-w-[96vw] mx-auto pt-[32px]">
            <div class="bg-[#f6f6f6] rounded-md p-[16px]">
              <div class="flex items-center mb-[12px] justify-center px-[16px] font-bold text-[18px]">{this.locale.TMCLookup}</div>
              <form class="relative tmc-form flex flex-col items-center gap-[32px] pt-[8px] pb-[16px]" dir={this.locale.sharedLocales.direction} {...formController}>
                <div class="flex gap-2">
                  <form-input
                    form={this.form}
                    name="partNumber"
                    isLoading={this.isLoading}
                    key={this.locale.sharedLocales.lang}
                    staticValue={this.partLookup?.partNumber || undefined}
                  />
                  <form-input inputProps={{ type: 'number', min: 1 }} name="quantity" isLoading={this.isLoading} key={this.locale.sharedLocales.lang} form={this.form} />
                  <form-select
                    clearable
                    forceOpenUpwards
                    form={this.form}
                    name="orderType"
                    isLoading={this.isLoading}
                    fetcher={tmcOrderTypesFetcher}
                    key={this.locale.sharedLocales.lang}
                    language={this.locale.sharedLocales.language as LanguageKeys}
                  />
                </div>
                <form-submit key={this.locale.sharedLocales.lang} isLoading={this.isLoading} form={this.form} />
              </form>

              <flexible-container isOpened={!!this.tmcResponse && !this.isLoading}>
                <div class="flex flex-col items-center pt-[22px]">
                  {this.tmcResponse === 'Available' && <LaptopMinimalCheckIcon class="size-[100px] fill-transparent stroke-green-600" />}
                  {this.tmcResponse === 'PartiallyAvailable' && <InfoIcon class="size-[100px] fill-transparent stroke-blue-600" />}
                  {this.tmcResponse === 'NotAvailable' && <OctagonXIcon class="size-[100px] fill-transparent stroke-yellow-600" />}
                  {!['Available', 'NotAvailable', 'PartiallyAvailable'].includes(this.tmcResponse || '') && <CloudAlertIcon class="size-[100px] fill-transparent stroke-red-600" />}

                  <div class="text-center text-[20px] font-semibold py-[12px]">{this.locale[this.tmcResponse] || this.tmcResponse}</div>
                </div>
              </flexible-container>
            </div>
          </div>
        </flexible-container>
      </Host>
    );
  }
}
