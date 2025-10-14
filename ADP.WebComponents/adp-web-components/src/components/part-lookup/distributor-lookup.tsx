import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { InformationTableColumn } from '../components/information-table';
import { MaterialCard, MaterialCardChildren } from '../components/material-card';

import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

import distributerSchema from '~locales/partLookup/distributor/type';

import { getMockFile } from '~features/mocks';
import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { PartLookupComponent, PartLookupMock, setPartLookupData, setPartLookupErrorState } from '~features/part-lookup-components';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';
import { FormHook, FormHookInterface, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook';
import { object, string } from 'yup';

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

const params = new URLSearchParams(window.location.search);

@Component({
  shadow: true,
  tag: 'distributor-lookup',
  styleUrl: 'distributor-lookup.css',
})
export class DistributorLookup implements MultiLingual, VehicleInfoLayoutInterface, PartLookupComponent, BlazorInvokable, FormHookInterface<any> {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof distributerSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...distributerSchema.getDefault() };

  async componentWillLoad() {
    await Promise.all([this.changeLanguage(this.language), this.onIsDevChange(this.isDev)]);
    this.setupTmcForm();
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.distributor', distributerSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion

  // #region Part Lookup Component Shared Logic

  @Prop() mockUrl = '';
  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  // @ts-ignore
  @Prop() errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop() loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop() loadedResponse?: BlazorInvokableFunction<(response: PartLookupDTO) => void>;

  @State() searchString: string;
  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() partLookup?: PartLookupDTO;
  @State() distributorLoading: boolean = false;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: PartLookupMock) {
    this.mockData = newMockData;
  }

  @Method()
  async getMockData() {
    return this.mockData;
  }

  @Watch('isDev')
  async onIsDevChange(isDev) {
    if (!isDev) return;

    const mockData = await getMockFile<PartLookupDTO>('part-lookup', this.mockUrl);

    await this.setMockData(mockData);
  }

  @Method()
  async fetchData(newData: PartLookupDTO | string, headers: any = {}) {
    await setPartLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setPartLookupErrorState(this, message);
  }

  @Watch('distributorLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Blazor Invokable logic
  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }
  // #region Component Logic

  // ===== Start Component Logic

  @Prop() tmcHeaders?: string;
  @Prop() tmcPayloads?: string;
  @Prop() showQuantity?: boolean;
  @Prop() showPartNumber?: boolean;
  @Prop() externalQuantity?: string;
  @Prop() externalPartNumber?: string;
  @Prop() hiddenFields?: string = '';
  @Prop() localizationName?: string = '';
  @State() tmcResponse = {
    isSuccess: true,
    message: '',
  };

  @State() isLoading: boolean = false;

  form;
  setupTmcForm = () => {
    let partNumber = string().meta({ label: 'Part Number', placeholder: 'Part Number' } as FormInputMeta);
    if (this.showPartNumber) partNumber = partNumber.required('This field is required.');

    let quantity = string().meta({ label: 'quantity', placeholder: 'quantity' } as FormInputMeta);
    if (this.showQuantity) quantity = quantity.required('This field is required.');

    const orderType = string()
      .meta({ label: 'Order Type', placeholder: 'Order Type' } as FormInputMeta)
      .required('This field is required.');

    const tmcLookupValidation = object({
      orderType,
      quantity,
      partNumber,
    });

    // @ts-ignore
    this.form = new FormHook(this, tmcLookupValidation);
  };

  formSubmit = async (data: { partNumber: string; quantity: string; orderType: string }) => {
    let externalHeaders = {};

    try {
      externalHeaders = JSON.parse(this.tmcHeaders);
    } catch (error) {}

    let externalPayload = {};

    try {
      externalPayload = JSON.parse(this.tmcPayloads);
    } catch (error) {}

    console.log({ ...data, externalPartNumber: this.externalPartNumber, externalQuantity: this.externalQuantity });
    console.log({ externalHeaders, externalPayload });

    await new Promise(r => setTimeout(r, 2000));

    const success = Math.random() > 0.5;

    this.form.reset();

    this.tmcResponse = {
      isSuccess: success,
      message: success ? '✅ Request submitted successfully!' : '❌ Failed to submit. Please try again.',
    };
  };

  // #endregion
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
    const { formController } = this.form;

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.distributorLoading}
          header={this.partLookup?.partNumber}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="p-[16px]">
            {!(params.get('tmcLookupOnly') === 'true') && (
              <div>
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
                    <information-table isLoading={this.distributorLoading} rows={rows} headers={tableHeaders} />
                  </div>
                </div>
              </div>
            )}

            <flexible-container
              // TODO: delete Tts ignore when showTMCPartLookup is added
              // @ts-ignore
              isOpened={(!!this?.partLookup?.showTMCPartLookup && !this.distributorLoading) || params.get('tmcLookupOnly') === 'true'}
            >
              <div class="w-full max-w-[1000px] mx-auto pt-[32px]">
                <div class="bg-[#f6f6f6] rounded-md p-[16px]">
                  <div class="flex items-center mb-[12px] justify-center px-[16px] font-bold text-[18px]">{this.locale.TMCStock}</div>
                  <form class="relative tmc-form flex justify-between items-end pt-[8px] pb-[16px]" dir={this.locale.sharedLocales.direction} {...formController}>
                    <div class="flex gap-2">
                      {this.showPartNumber && <form-input name="quantity" isLoading={this.isLoading} key={this.locale.sharedLocales.lang} form={this.form} />}
                      {this.showQuantity && <form-input name="partNumber" isLoading={this.isLoading} key={this.locale.sharedLocales.lang} form={this.form} />}
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

                  <flexible-container isOpened={!!this.tmcResponse.message && !this.isLoading}>
                    <div class="text-center font-semibold py-[12px]">{this.tmcResponse.message}</div>
                  </flexible-container>
                </div>
              </div>
            </flexible-container>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
