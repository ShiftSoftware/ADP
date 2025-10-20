import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import distributerSchema from '~locales/partLookup/distributor/type';
import { ComponentLocale, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormHookInterface, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook';
import { InferType, number, object, string } from 'yup';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';
import { EndPoint } from '~types/components';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { ManufacturerPartLookupDTO } from '~types/generated/part/manufacturer-part-lookup-dto';

const manufacturerOrderTypesFetcher: FormSelectFetcher<any> = async ({}): Promise<FormSelectItem[]> => {
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

const manufacturerPartLookupValidation = object({
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

export type ManufacturerPartLookupValidation = InferType<typeof manufacturerPartLookupValidation>;

export type Locale = ComponentLocale<typeof distributerSchema>;

@Component({
  shadow: true,
  tag: 'manufacturer-part-lookup',
  styleUrl: 'manufacturer-part-lookup.css',
})
export class ManufacturerPartLookup implements MultiLingual, FormHookInterface<ManufacturerPartLookupValidation> {
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

  formSubmit = async (payload: ManufacturerPartLookupValidation) => {
    try {
      if (this.isDev) {
        await new Promise(r => setTimeout(r, 2000));
        if (0.5 < Math.random()) {
          if (0.5 < Math.random()) this.manufacturerResponse = { isSuccess: true, message: 'This is sample f success message' };
          else this.manufacturerResponse = { isSuccess: false, message: 'This is sample of failed message' };
        } else {
          this.manufacturerResponse = {
            isSuccess: true,

            data: [
              { fieldName: 'item 1', fieldValue: `${Math.floor(Math.random() * 100)}` },
              { fieldName: 'item 2', fieldValue: 'Available' },
              { fieldName: 'item 3', fieldValue: `${Math.floor(Math.random() * 100)}` },
              { fieldName: 'item 4', fieldValue: 'Not Available' },
            ],
          };
        }

        this.form.reset();
        return;
      }

      let manufacturerEndpoint: EndPoint =
        typeof this.manufacturerPartLookupEndpoint === 'string' ? JSON.parse(this.manufacturerPartLookupEndpoint) : this.manufacturerPartLookupEndpoint;

      const defaultHeaders: Record<string, string> = {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'Accept-Language': this.language || 'en',
      };

      const config: RequestInit = {
        method: manufacturerEndpoint?.method || 'POST',
        headers: { ...defaultHeaders, ...(manufacturerEndpoint?.headers || {}) },
        body: JSON.stringify({ ...payload, ...(manufacturerEndpoint?.body || {}) }),
      };

      const response = await fetch(manufacturerEndpoint.url, config);

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const responseType = (await response.json()) as (typeof this.manufacturerResponse)['data'];

      this.form.reset();

      this.manufacturerResponse = { isSuccess: true, data: responseType };
    } catch (error) {
      console.error('❌ Manufacturer Lookup request failed:', error);
      this.manufacturerResponse = { isSuccess: false, message: ' Manufacturer Lookup request failed' };
    }
  };
  // #endregion

  // ===== Start Component Logic

  @Prop() isDev?: boolean;
  @Prop() standAlone?: boolean = true;
  @Prop() partLookup?: PartLookupDTO;
  @Prop() manufacturerPartLookupTitle?: string = 'Manufacture lookup';

  @Prop() closeManufacturerPartLookup?: boolean;
  @Prop() manufacturerPartLookupEndpoint: EndPoint | string;

  @State() manufacturerResponse?: ManufacturerPartLookupDTO;

  form = new FormHook(this, manufacturerPartLookupValidation);

  // #endregion

  render() {
    const { formController } = this.form;

    const isComponentOpened = (!!this?.partLookup?.showManufacturerPartLookup && !this.closeManufacturerPartLookup) || this.standAlone;

    return (
      <Host>
        <flexible-container isOpened={isComponentOpened}>
          <div class="w-full max-w-[96vw] mx-auto pt-[32px]">
            <div class="bg-[#f6f6f6] rounded-md p-[16px]">
              <div class="flex items-center mb-[12px] justify-center px-[16px] font-bold text-[18px]">{this.manufacturerPartLookupTitle}</div>
              <form class="relative manufacturer-form flex flex-col items-center gap-[32px] pt-[8px] pb-[16px]" dir={this.locale.sharedLocales.direction} {...formController}>
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
                    form={this.form}
                    name="orderType"
                    isLoading={this.isLoading}
                    fetcher={manufacturerOrderTypesFetcher}
                    key={this.locale.sharedLocales.lang}
                    language={this.locale.sharedLocales.language as LanguageKeys}
                  />
                </div>
                <form-submit key={this.locale.sharedLocales.lang} isLoading={this.isLoading} form={this.form} />
              </form>

              <flexible-container isOpened={!!this.manufacturerResponse && !this.isLoading}>
                <div class="flex flex-col items-center pt-[12px]">
                  <div
                    class={cn('text-center text-[20px] font-semibold py-[12px]', {
                      'text-green-600': this.manufacturerResponse?.isSuccess && !this.manufacturerResponse?.data?.length,
                      'text-red-600': !this.manufacturerResponse?.isSuccess,
                    })}
                  >
                    {!this.manufacturerResponse?.data?.length && (this.manufacturerResponse?.message || this.locale.RequestFailed)}
                    {!!this.manufacturerResponse?.data?.length && (
                      <div class="bg-white rounded-lg border w-[500px]">
                        {this.manufacturerResponse?.data.map(({ fieldName, fieldValue }) => (
                          <div key={`${fieldName}-${fieldValue}`} class="flex even:bg-slate-100 hover:bg-sky-100/50 border-b w-full text-xl text-center">
                            <div class="text-center flex items-center flex-1 max-w-[250px] shrink-0 justify-center py-[10px] px-[16px] border-r">{fieldName}</div>
                            <div class="text-center flex items-center flex-1 shrink-0 justify-center py-[10px] px-[16px] border-l">{fieldValue}</div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </flexible-container>
            </div>
          </div>
        </flexible-container>
      </Host>
    );
  }
}
