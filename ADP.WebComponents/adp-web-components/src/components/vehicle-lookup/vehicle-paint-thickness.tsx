import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { closeImageViewer, ImageViewerInterface, openImageViewer } from '~lib/image-expansion';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import { getVehicleInformation } from '~api/vehicleInformation';

import { LanguageKeys } from '~types/locale';
import { AppStates, MockJson } from '~types/components';
import { vehicleLookupDTO } from '~types/vehicleLookup/vehicleLookupDTO';

import Eye from '~assets/eye.svg';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';
import { InformationTableColumn } from '../components/information-table';

let mockData: MockJson<vehicleLookupDTO> = {};

@Component({
  shadow: true,
  tag: 'vehicle-paint-thickness',
  styleUrl: 'vehicle-paint-thickness.css',
})
export class VehiclePaintThickness implements ImageViewerInterface {
  @Prop() baseUrl: string = '';
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() coreOnly: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: vehicleLookupDTO) => void;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof paintThicknessSchema> = paintThicknessSchema.getDefault();

  @State() state: AppStates = 'idle';
  @State() externalVin?: string = null;
  @State() expandedImage?: string = null;
  @State() errorMessage?: ErrorKeys = null;
  @State() vehicleInformation?: vehicleLookupDTO;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  originalImage: HTMLImageElement;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup.paintThickness', paintThicknessSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  private handleSettingData(response: vehicleLookupDTO) {
    if (!response.paintThickness) response.paintThickness = { imageGroups: [], parts: [] };
    if (!response.paintThickness.parts || !Array.isArray(response.paintThickness.parts)) response.paintThickness.parts = [];
    if (!response.paintThickness.imageGroups || !Array.isArray(response.paintThickness.imageGroups)) response.paintThickness.imageGroups = [];

    this.vehicleInformation = response;
  }

  @Method()
  async setData(newData: vehicleLookupDTO | string, headers: any = {}) {
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

      const vehicleResponse = isVinRequest ? await getVehicleInformation(this, { scopedTimeoutRef, vin, mockData }, headers) : newData;

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

  @Watch('vehicleInformation')
  async onInformationUpdate() {}

  @Watch('state')
  async loadingListener() {
    if (this.loadingStateChange) this.loadingStateChange(this.state.includes('loading'));
  }

  @Method()
  async setMockData(newMockData: MockJson<vehicleLookupDTO>) {
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

    const { imageGroups, parts } = this?.vehicleInformation ? this?.vehicleInformation?.paintThickness : { imageGroups: [], parts: [] };

    const isLoading = this.state.includes('loading');
    const isError = this.state.includes('error');

    const tableHeaders: InformationTableColumn[] = [
      {
        width: 250,
        key: 'part',
        label: texts.part,
      },
      {
        width: 250,
        key: 'left',
        label: texts.left,
      },
      {
        width: 250,
        key: 'right',
        label: texts.right,
      },
    ];

    return (
      <Host>
        <div
          onClick={() => this.closeImage()}
          style={{ backdropFilter: this.expandedImage ? 'blur(3px)' : 'blur(0px)' }}
          class={cn('pointer-events-none w-[100dvw] h-[100dvh] fixed top-0 z-10 left-0 opacity-0 bg-black/40 transition-all duration-400', {
            'pointer-events-auto opacity-100 delay-200': this.expandedImage,
          })}
        >
          <button class="flex flex-col mt-[16px] items-center justify-center size-12 float-right mr-[16px]" onClick={() => this.closeImage()}>
            <div class="h-1 w-12 rounded-full rotate-45 absolute bg-white"></div>
            <div class="h-1 w-12 rounded-full -rotate-45 absolute bg-white"></div>
          </button>
        </div>
        <img alt="" id="expanded-image" class="fixed opacity-0 z-40 transition-all rounded-lg" />

        <VehicleInfoLayout
          isError={isError}
          isLoading={isLoading}
          coreOnly={this.coreOnly}
          vin={this.vehicleInformation?.vin}
          direction={this.sharedLocales.direction}
          errorMessage={this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
        >
          <div class="overflow-x-auto">
            <information-table rows={parts} headers={tableHeaders} isLoading={isLoading}></information-table>
          </div>

          <flexible-container isOpened={!isLoading && !!imageGroups.length}>
            <div class="py-[16px] gap-[16px] justify-center flex flex-wrap px-[24px] w-full">
              {imageGroups.map((imageGroup, index) => (
                <div class="shrink-0 rounded-lg shadow-sm border overflow-hidden flex flex-col" key={imageGroup.name + index}>
                  <h1 class="text-center border-b bg-slate-50 font-semibold p-[6px]">
                    <span class="shift-skeleton">{imageGroup.name}</span>
                  </h1>

                  <div class="flex max-w-full flex-wrap p-[12px] gap-[8px]">
                    {imageGroup.images.map((image, idx) => (
                      <div class={cn('flex shift-skeleton gap-[8px]', { loading: !image })} key={image + idx}>
                        <button
                          onClick={({ target }) => this.openImage(target as HTMLImageElement, image)}
                          class="shrink-0 relative ring-0 outline-none w-fit mx-auto [&_img]:hover:shadow-lg [&_div]:hover:!opacity-100 cursor-pointer"
                        >
                          <div class="absolute flex-col justify-center gap-[4px] size-full flex items-center pointer-events-none hover:opacity-100 rounded-lg opacity-0 bg-black/40 transition-all duration-300">
                            <img src={Eye} />
                            <span class="text-white">{texts.expand}</span>
                          </div>
                          <img src={image} class="h-[150px] cursor-pointer shadow-sm rounded-lg w-[84px] transition-all duration-300" />
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </flexible-container>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
