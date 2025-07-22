import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import Eye from '~assets/eye.svg';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { InformationTableColumn } from '../components/information-table';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { closeImageViewer, ImageViewer, ImageViewerInterface, openImageViewer } from '~features/image-viewer';
import { VehicleLookupComponent, VehicleLookupMock, setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-paint-thickness',
  styleUrl: 'vehicle-paint-thickness.css',
})
export class VehiclePaintThickness implements MultiLingual, VehicleInfoLayoutInterface, ImageViewerInterface, VehicleLookupComponent {
  // ====== Start Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof paintThicknessSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...paintThicknessSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.paintThickness', paintThicknessSchema)]);
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
  async fetchData(newData: VehicleLookupDTO | string, headers: any = {}) {
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

  // ====== End Vehicle Lookup Component Shared Logic

  render() {
    const texts = this.locale;

    const { imageGroups, parts } = this?.vehicleLookup?.paintThickness ? this?.vehicleLookup?.paintThickness : { imageGroups: [], parts: [] };

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
        <ImageViewer closeImageViewer={() => closeImageViewer.bind(this)()} expandedImage={this.expandedImage} />

        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="overflow-x-auto">
            <information-table rows={parts} headers={tableHeaders} isLoading={this.isLoading}></information-table>
          </div>

          <flexible-container isOpened={!this.isLoading && !!imageGroups?.length}>
            <div class="py-[16px] gap-[16px] justify-center flex flex-wrap px-[24px] w-full">
              {imageGroups?.map((imageGroup, index) => (
                <div class="shrink-0 rounded-lg shadow-sm border overflow-hidden flex flex-col" key={imageGroup?.name + index}>
                  <h1 class="text-center border-b bg-slate-50 font-semibold p-[6px]">
                    <span class="shift-skeleton">{imageGroup?.name}</span>
                  </h1>

                  <div class="flex max-w-full flex-wrap p-[12px] gap-[8px]">
                    {imageGroup?.images.map((image, idx) => (
                      <div class={cn('flex shift-skeleton gap-[8px]', { loading: !image })} key={image + idx}>
                        <button
                          onClick={({ target }) => openImageViewer.bind(this)(target as HTMLImageElement, image)}
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
