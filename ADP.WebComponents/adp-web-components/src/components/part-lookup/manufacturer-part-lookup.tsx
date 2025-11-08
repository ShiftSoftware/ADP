import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import distributerSchema from '~locales/partLookup/distributor/type';
import { ComponentLocale, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormHookInterface, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook';
import { InferType, number, object, string } from 'yup';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';
import getLanguageFromUrl from '~lib/get-language-from-url';
import cn from '~lib/cn';
import { Endpoint, fetchFrom } from '~lib/fetch-from';
import { ManufacturerPartLookupRequestDTO } from '~types/generated/part/manufacturer-part-lookup-request-dto';
import { ManufacturerPartLookupResponseDTO } from '~types/generated/part/manufacturer-part-lookup-response-dto';

const manufacturerOrderTypesFetcher: FormSelectFetcher<any> = async ({}): Promise<FormSelectItem[]> => {
  return [
    {
      value: '0',
      label: 'V',
    },
    {
      value: '1',
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
          if (0.5 < Math.random())
            this.manufacturerResponse = { message: '', quantity: 2, id: '2', orderType: 'Airplane', partNumber: 'T110715002102', status: 'Pending', manufacturerResult: [] };
          else this.manufacturerResponse = { message: '', quantity: 2, id: '2', orderType: 'Airplane', partNumber: 'T110715002102', status: 'UnResolved', manufacturerResult: [] };
        } else {
          this.manufacturerResponse = {
            id: '2',
            message: '',
            quantity: 2,
            orderType: 'Airplane',
            partNumber: 'T110715002102',
            status: 'Pending',
            manufacturerResult: [
              { key: 'item 1', value: `${Math.floor(Math.random() * 100)}` },
              { key: 'item 2', value: 'Available' },
              { key: 'item 3', value: `${Math.floor(Math.random() * 100)}` },
              { key: 'item 4', value: 'Not Available' },
            ],
          };
        }

        this.form.reset();
        return;
      }

      const requestPayload: ManufacturerPartLookupRequestDTO = {
        logId: this.logId,
        quantity: +payload.quantity,
        partNumber: payload.partNumber,
        // @ts-ignore
        orderType: +payload.orderType as 'Sea' | 'Airplane',
      };
      const response = await fetchFrom(this.manufacturerPartLookupEndpoint, { language: this.language, body: requestPayload, method: 'POST' });

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const responseType = (await response.json()) as typeof this.manufacturerResponse;

      this.form.reset();

      this.manufacturerResponse = responseType;
    } catch (error) {
      console.error('❌ Manufacturer Lookup request failed:', error);
      // @ts-ignore
      this.manufacturerResponse = { message: this.locale.RequestFailed };
    }
  };
  // #endregion

  // ===== Start Component Logic

  @Prop() isDev?: boolean;
  @Prop() logId?: string = '';
  @Prop() partLookup?: PartLookupDTO;
  @Prop() standAlone?: boolean = true;
  @Prop() manufacturerPartLookupTitle?: string = 'Manufacture lookup';

  @Prop() closeManufacturerPartLookup?: boolean;
  @Prop() manufacturerPartLookupEndpoint: Endpoint;

  @State() manufacturerResponse?: ManufacturerPartLookupResponseDTO;

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
                  <div class={cn('text-center text-[20px] font-semibold py-[12px]')}>
                    {!this.manufacturerResponse?.manufacturerResult?.length && (this.manufacturerResponse?.message || this.locale.RequestFailed)}
                    {!!this.manufacturerResponse?.manufacturerResult?.length && (
                      <div class="bg-white rounded-lg border w-[500px]">
                        {this.manufacturerResponse?.manufacturerResult.map(({ key, value }) => (
                          <div key={`${key}-${value}`} class="flex even:bg-slate-100 hover:bg-sky-100/50 border-b w-full text-xl text-center">
                            <div class="text-center flex items-center flex-1 max-w-[250px] shrink-0 justify-center py-[10px] px-[16px] border-r">{key}</div>
                            <div class="text-center flex items-center flex-1 shrink-0 justify-center py-[10px] px-[16px] border-l">{value}</div>
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
