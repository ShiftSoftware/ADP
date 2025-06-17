import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { LanguageKeys } from '~types/locale';
import { AppStates, MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { getVehicleLookup } from '~api/vehicleInformation';

import cn from '~lib/cn';
import { closeImageViewer, ImageViewerInterface, openImageViewer } from '~lib/image-expansion';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import Eye from '~assets/eye.svg';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';
import { InformationTableColumn } from '../components/information-table';

let mockData: MockJson<VehicleLookupDTO> = {};

@Component({
  shadow: true,
  tag: 'vehicle-accessories',
  styleUrl: 'vehicle-accessories.css',
})
export class VehicleAccessories implements ImageViewerInterface {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() coreOnly: boolean = false;
  @Prop() queryString: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof accessoriesSchema> = accessoriesSchema.getDefault();

  @State() state: AppStates = 'idle';
  @State() externalVin?: string = null;
  @State() expandedImage?: string = null;
  @State() errorMessage?: ErrorKeys = null;
  @State() vehicleInformation?: VehicleLookupDTO;

  originalImage: HTMLImageElement;
  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup.accessories', accessoriesSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  private handleSettingData(response: VehicleLookupDTO) {
    if (!response.accessories || !Array.isArray(response.accessories)) response.accessories = [];
    this.vehicleInformation = response;
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    clearTimeout(this.networkTimeoutRef);
    if (this.abortController) this.abortController.abort();
    this.abortController = new AbortController();
    let scopedTimeoutRef: ReturnType<typeof setTimeout>;

    const isVinRequest = typeof newData === 'string';

    const vin = isVinRequest ? newData : newData?.vin;
    this.externalVin = vin;

    try {
      if (!vin || vin.trim().length === 0) {
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

      // @ts-ignore
      const vehicleResponse = isVinRequest ? await getVehicleLookup(this, { scopedTimeoutRef, vin, mockData }, headers) : newData;

      if (this.networkTimeoutRef === scopedTimeoutRef) {
        if (!vehicleResponse) throw new Error('wrongResponseFormat');

        this.handleSettingData(vehicleResponse);
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
    this.vehicleInformation = null;
    this.errorMessage = message;
  }

  @Method()
  async fetchData(requestedVin: string = this.externalVin, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }

  @Watch('state')
  async loadingListener() {
    if (this.loadingStateChange) this.loadingStateChange(this.state.includes('loading'));
  }

  @Method()
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    mockData = newMockData;
  }

  closeImageListener = (event?: KeyboardEvent) => {
    this.closeImage(event);
  };

  openImage = (target: HTMLImageElement, imageSrc: string) => {
    openImageViewer(this, target, imageSrc);
  };

  closeImage = (event?: KeyboardEvent) => {
    closeImageViewer(this, event);
  };

  render() {
    const texts = this.locale;
    const accessories = this?.vehicleInformation ? this.vehicleInformation?.accessories : [];

    const isLoading = this.state.includes('loading');
    const isError = this.state.includes('error');

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

    const rows = accessories.map(accessory => ({
      partNumber: accessory.partNumber,
      description: accessory.description,
      image: () => (
        <div class="size-[100px] flex mx-auto items-center justify-center">
          <button
            onClick={({ target }) => this.openImage(target as HTMLImageElement, accessory.image)}
            class="shrink-0 relative ring-0 outline-none w-fit mx-auto [&_img]:hover:shadow-lg [&_div]:hover:!opacity-100 cursor-pointer"
          >
            <div class="absolute flex-col justify-center gap-[4px] size-full flex items-center pointer-events-none hover:opacity-100 rounded-lg opacity-0 bg-black/40 transition-all duration-300">
              <img src={Eye} />
              <span class="text-white">{texts.expand}</span>
            </div>
            <img class="w-auto h-auto max-w-[100px] max-h-[100px] cursor-pointer shadow-sm rounded-lg transition-all duration-300" src={accessory.image} />
          </button>
        </div>
      ),
    }));

    const templateRow = {
      image: () => <div class="size-[100px] flex mx-auto items-center justify-center">&nbsp;</div>,
    };

    return (
      <Host>
        <div
          onClick={() => this.closeImage()}
          style={{ backdropFilter: this.expandedImage ? 'blur(3px)' : 'blur(0px)' }}
          class={cn('pointer-events-none w-[100dvw] h-[100dvh] fixed top-0 z-50 left-0 opacity-0 bg-black/40 transition-all duration-400', {
            'pointer-events-auto opacity-100 delay-200': this.expandedImage,
          })}
        >
          <button class="flex flex-col mt-[16px] items-center justify-center size-12 float-right mr-[16px]" onClick={() => this.closeImage()}>
            <div class="h-1 w-12 rounded-full rotate-45 absolute bg-white"></div>
            <div class="h-1 w-12 rounded-full -rotate-45 absolute bg-white"></div>
          </button>
        </div>
        <img alt="" id="expanded-image" class="fixed opacity-0 z-50 transition-all rounded-lg" />

        <VehicleInfoLayout
          isError={isError}
          isLoading={isLoading}
          coreOnly={this.coreOnly}
          vin={this.vehicleInformation?.vin}
          direction={this.sharedLocales.direction}
          errorMessage={this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
        >
          <div class="overflow-x-auto">
            <information-table templateRow={templateRow} rows={rows} headers={tableHeaders} isLoading={isLoading}></information-table>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
