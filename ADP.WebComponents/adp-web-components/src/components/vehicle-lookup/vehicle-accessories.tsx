import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import Eye from '~assets/eye.svg';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';

import { InformationTableColumn } from '../components/information-table';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { closeImageViewer, ImageViewer, ImageViewerInterface, openImageViewer } from '~features/image-viewer';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-accessories',
  styleUrl: 'vehicle-accessories.css',
})
export class VehicleAccessories implements MultiLingual, VehicleInfoLayoutInterface, ImageViewerInterface, VehicleLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof accessoriesSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...accessoriesSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.accessories', accessoriesSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // ====== End Localization

  // ====== Start Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // ====== End Vehicle info layout prop

  // ====== Start Image Viewer Logic

  @State() expandedImage?: string = '';

  originalImage: HTMLImageElement;

  // ====== End Image Viewer Logic

  // ====== Start Vehicle Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() vehicleLookup?: VehicleLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: VehicleLookupMock) {
    this.mockData = newMockData;
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    await setVehicleLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  @Method()
  async fetchData(requestedVin: string, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }

  // ====== End Vehicle Lookup Component Shared Logic

  render() {
    const texts = this.locale;
    const accessories = this?.vehicleLookup?.accessories ? this.vehicleLookup?.accessories : [];

    const tableHeaders: InformationTableColumn[] = [
      {
        width: 300,
        key: 'partNumber',
        label: texts.partNumber,
      },
      {
        width: 500,
        key: 'description',
        label: texts.description,
      },
      {
        width: 200,
        key: 'image',
        label: texts.image,
      },
    ];

    const rows = accessories?.map(accessory => ({
      partNumber: accessory?.partNumber,
      description: accessory?.description,
      image: () => (
        <div class="size-[100px] flex mx-auto items-center justify-center">
          <button
            onClick={({ target }) => openImageViewer.bind(this)(target as HTMLImageElement, accessory?.image)}
            class="shrink-0 relative ring-0 outline-none w-fit mx-auto [&_img]:hover:shadow-lg [&_div]:hover:!opacity-100 cursor-pointer"
          >
            <div class="absolute flex-col justify-center gap-[4px] size-full flex items-center pointer-events-none hover:opacity-100 rounded-lg opacity-0 bg-black/40 transition-all duration-300">
              <img src={Eye} />
              <span class="text-white">{texts.expand}</span>
            </div>
            <img class="w-auto h-auto max-w-[100px] max-h-[100px] cursor-pointer shadow-sm rounded-lg transition-all duration-300" src={accessory?.image} />
          </button>
        </div>
      ),
    }));

    const templateRow = {
      image: () => <div class="size-[100px] flex mx-auto items-center justify-center">&nbsp;</div>,
    };

    return (
      <Host>
        <ImageViewer closeImageViewer={() => closeImageViewer.bind(this)()} expandedImage={this.expandedImage} />

        <VehicleInfoLayout
          isError={this.isError}
          isLoading={this.isLoading}
          coreOnly={this.coreOnly}
          vin={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="overflow-x-auto">
            <information-table templateRow={templateRow} rows={rows} headers={tableHeaders} isLoading={this.isLoading}></information-table>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
