import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import Eye from '~assets/eye.svg';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { PaintThicknessInspectionPanelDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-panel-dto';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { closeImageViewer, ImageViewer, ImageViewerInterface, openImageViewer } from '~features/image-viewer';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { VehicleLookupComponent, VehicleLookupMock, setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-paint-thickness',
  styleUrl: 'vehicle-paint-thickness.css',
})
export class VehiclePaintThickness implements MultiLingual, VehicleInfoLayoutInterface, ImageViewerInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

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

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion

  // #region Image Viewer Logic

  @State() expandedImage?: string = '';

  originalImage: HTMLImageElement;

  // #endregion

  // #region Blazor Invokable logic
  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }
  // #endregion

  // #region Vehicle Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop() loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop() loadedResponse?: BlazorInvokableFunction<(response: VehicleLookupDTO) => void>;

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
  async fetchVin(newData: VehicleLookupDTO | string, headers: any = {}) {
    await setVehicleLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Tab Logic

  @State() activeTabIndex: number = 0;

  @Watch('vehicleLookup')
  onVehicleLookupChange() {
    this.activeTabIndex = 0;
  }

  private onActiveTabChange = ({ idx }: { label: string; idx: number }) => {
    this.activeTabIndex = idx;
  };

  // #endregion

  private groupPanels = (panels: PaintThicknessInspectionPanelDTO[]) => {
    const groups = new Map<string, { panel: string; position: string; left?: number; right?: number; center?: number }>();

    for (const p of panels) {
      const key = `${p.panelType}|${p.panelPosition || ''}`;

      if (!groups.has(key)) {
        groups.set(key, { panel: p.panelType, position: p.panelPosition || '' });
      }

      const row = groups.get(key)!;

      if (p.panelSide === 'Left') row.left = p.measuredThickness;
      else if (p.panelSide === 'Right') row.right = p.measuredThickness;
      else row.center = p.measuredThickness;
    }

    return Array.from(groups.values());
  };

  render() {
    const texts = this.locale;

    const inspections = this.vehicleLookup?.paintThicknessInspections || [];
    const tabs = inspections.map((inspection, idx) => {
      const isDuplicate = inspections.some((other, otherIdx) => otherIdx !== idx && other.source === inspection.source);
      return isDuplicate && inspection.inspectionDate ? `${inspection.source} (${inspection.inspectionDate})` : inspection.source;
    });
    const hideTabs = this.isLoading || this.isError;

    const activeInspection = inspections[this.activeTabIndex] || inspections[0];
    const panels = activeInspection?.panels || [];

    const groupedRows = this.groupPanels(panels);

    const imageGroups = panels
      .filter(panel => panel.images?.length)
      .map(panel => ({
        name: [panel.panelPosition, panel.panelSide, panel.panelType].filter(Boolean).join(' '),
        images: panel.images,
      }));

    const colWidths = { panel: 200, position: 200, side: 150 };

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
          <div class={cn('duration-300', 'py-[15px]', { hidden: hideTabs })}>
            <shift-tabs activeTabIndex={this.activeTabIndex} changeActiveTab={this.onActiveTabChange} tabs={tabs}></shift-tabs>
          </div>

          <flexible-container isOpened={!this.isLoading && !!activeInspection?.inspectionDate}>
            <div class="flex items-center justify-center gap-[6px] border-b border-slate-200 px-[16px] py-[10px] text-[16px] text-slate-700">
              <CalendarDaysIcon class="size-[18px] shrink-0" />
              <span class="font-semibold">{texts.inspectionDate}:</span>
              <span class="shift-skeleton">{activeInspection?.inspectionDate}</span>
            </div>
          </flexible-container>

          <div class="overflow-x-auto">
            <div class={cn('mx-auto w-fit', { loading: this.isLoading })}>
              {/* Header */}
              <div class="flex">
                <div class="font-semibold border-b py-[16px] px-[16px] text-center" style={{ width: `${colWidths.panel}px` }}>
                  {texts.panel}
                </div>
                <div class="font-semibold border-b py-[16px] px-[16px] text-center" style={{ width: `${colWidths.position}px` }}>
                  {texts.position}
                </div>
                <div class="font-semibold border-b py-[16px] px-[16px] text-center" style={{ width: `${colWidths.side}px` }}>
                  {texts.left}
                </div>
                <div class="font-semibold border-b py-[16px] px-[16px] text-center" style={{ width: `${colWidths.side}px` }}>
                  {texts.right}
                </div>
              </div>

              {/* Rows */}
              <flexible-container height="auto">
                {!groupedRows.length && (
                  <div class="border-b">
                    <div class="flex">
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.panel}px` }}>
                        <div class="shift-skeleton">&nbsp;</div>
                      </div>
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.position}px` }}>
                        <div class="shift-skeleton">&nbsp;</div>
                      </div>
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.side}px` }}>
                        <div class="shift-skeleton">&nbsp;</div>
                      </div>
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.side}px` }}>
                        <div class="shift-skeleton">&nbsp;</div>
                      </div>
                    </div>
                  </div>
                )}
                {groupedRows.map((row, idx) => (
                  <div key={row.panel + row.position + idx} class="border-b last:border-b-0">
                    <div class={cn('flex hover:bg-sky-100/50 transition duration-300', { 'bg-slate-100': idx % 2 === 1 })}>
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.panel}px` }}>
                        <div class="shift-skeleton">{row.panel}</div>
                      </div>
                      <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.position}px` }}>
                        <div class="shift-skeleton">{row.position || '\u00A0'}</div>
                      </div>
                      {row.center !== undefined ? (
                        <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.side * 2}px` }}>
                          <div class="shift-skeleton">{row.center}</div>
                        </div>
                      ) : (
                        [
                          <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.side}px` }}>
                            <div class="shift-skeleton">{row.left !== undefined ? row.left : '\u00A0'}</div>
                          </div>,
                          <div class="px-[16px] py-[16px] text-center my-auto" style={{ width: `${colWidths.side}px` }}>
                            <div class="shift-skeleton">{row.right !== undefined ? row.right : '\u00A0'}</div>
                          </div>,
                        ]
                      )}
                    </div>
                  </div>
                ))}
              </flexible-container>
            </div>
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
