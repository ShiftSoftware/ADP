import { Component, Element, Event, EventEmitter, Host, Listen, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import Eye from '~assets/eye.svg';
import { ArrowIcon } from '~assets/arrow-icon';
import { PrintIcon } from '~assets/print-icon';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { PaintThicknessInspectionPanelDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-panel-dto';

import { LookupHead, VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { VehicleLookupComponent, VehicleLookupMock, setVehicleLookupData, setVehicleLookupErrorState } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: true,
  tag: 'vehicle-paint-thickness',
  styleUrl: 'vehicle-paint-thickness.css',
})
export class VehiclePaintThickness implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
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

  // #region Image viewer (<shift-image>, portaled onto <body>)

  /** The picture open in the viewer, or empty. */
  @State() expandedImage?: string = '';

  private imageViewer?: HTMLShiftImageElement;

  /** Opens the viewer on `src`, growing out of the thumbnail `origin`. The same picture again while open is ignored. */
  private openImage(origin: HTMLImageElement, src: string, label?: string) {
    if (this.expandedImage === src) return;
    this.expandedImage = src;
    void this.imageViewer?.show(src, { origin, label });
  }

  /** The viewer closed (overlay, key or hide()): the panel no longer has a picture open. */
  private onViewerChange = (event: CustomEvent<{ open: boolean }>) => {
    if (!event.detail.open) this.expandedImage = '';
  };

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
  @Prop() today?: string;
  @Prop() disableVinValidation: boolean = false;
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
    this.certificateMenuOpen = false;
  }

  private onActiveTabChange = ({ idx }: { label: string; idx: number }) => {
    this.activeTabIndex = idx;
  };

  // #endregion

  // #region Certificate Print Logic

  /** Host-controlled visibility (e.g. a feature gate). The links themselves come from the lookup
   *  response (`paintThicknessCertificateUrls`, one signed URL per print language with its menu
   *  label) — the server only issues them to callers allowed to print, so no endpoint, auth
   *  headers, fetch or language knowledge live on the client. */
  @Prop() showCertificateButton: boolean = false;

  @State() certificateMenuOpen: boolean = false;

  // Shadow DOM retargets inner clicks to the host element, so any click whose composed
  // path includes the host happened inside the component; everything else closes the menu.
  @Listen('click', { target: 'document' })
  onDocumentClick(event: MouseEvent) {
    if (this.certificateMenuOpen && !event.composedPath().includes(this.el)) this.certificateMenuOpen = false;
  }

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

  // #region Verdict

  /**
   * Fires whenever the panel's verdict changes — including back to idle when the vehicle is
   * cleared — so a composite that draws one card for several panels can colour its accent from the
   * active one.
   */
  @Event() verdictChange: EventEmitter<VerdictState>;

  private lastVerdict?: VerdictState;
  private currentVerdict: VerdictState = 'idle';

  private announceVerdict() {
    if (this.currentVerdict === this.lastVerdict) return;
    this.lastVerdict = this.currentVerdict;
    this.verdictChange.emit(this.currentVerdict);
  }

  componentDidRender() {
    this.announceVerdict();
  }

  // #endregion

  render() {
    const verdict = recordVerdict({
      locale: this.locale.sharedLocales,
      error: this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined,
      vehicleLoaded: !!this.vehicleLookup?.vin,
      authorized: this.vehicleLookup?.isAuthorized,
      hasRecords: (this.vehicleLookup?.paintThicknessInspections?.length ?? 0) > 0,
    });
    this.currentVerdict = verdict.accent;

    const texts = this.locale;

    const inspections = this.vehicleLookup?.paintThicknessInspections || [];
    const tabs = inspections.map((inspection, idx) => {
      const isDuplicate = inspections.some((other, otherIdx) => otherIdx !== idx && other.source === inspection.source);
      return isDuplicate && inspection.inspectionDate ? `${inspection.source} (${inspection.inspectionDate})` : inspection.source;
    });
    // No inspections means no tab strip: shift-tabs draws its baseline even with zero tabs, which
    // would read as a second rule under the head's own.
    const hideTabs = this.isLoading || this.isError || !tabs.length;

    const activeInspection = inspections[this.activeTabIndex] || inspections[0];
    const panels = activeInspection?.panels || [];

    const groupedRows = this.groupPanels(panels);

    const imageGroups = panels
      .filter(panel => panel.images?.length)
      .map(panel => ({
        name: [panel.panelPosition, panel.panelSide, panel.panelType].filter(Boolean).join(' '),
        images: panel.images,
      }));

    const certificateUrls = this.vehicleLookup?.paintThicknessCertificateUrls || [];

    const colWidths = { panel: 200, position: 200, side: 150 };

    return (
      <Host translate="no">
        <shift-image ref={el => (this.imageViewer = el)} language={this.language} onViewerChange={this.onViewerChange} />

        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <LookupHead title={texts.paintThickness} verdict={verdict} />
          <div class="lookup-slide-clip">
            <div class="lookup-slide">
              <div class={{ 'paint-toolbar': true, 'paint-toolbar-hidden': hideTabs }}>
                <div class="paint-tabs">
                  <shift-tabs activeTabIndex={this.activeTabIndex} changeActiveTab={this.onActiveTabChange} tabs={tabs}></shift-tabs>
                </div>

                {/* Print menu sized to mirror the shift-tabs tab buttons (px-16 / py-6 / 17px).
                    The button opens a language menu; each item is a plain anchor to that
                    language's server-issued signed URL — opens in a new tab with no fetch, so
                    no auth plumbing and no popup-blocker interception. */}
                {this.showCertificateButton && !!certificateUrls.length && (
                  <div class="paint-certificate">
                    <button
                      aria-haspopup="menu"
                      aria-expanded={this.certificateMenuOpen ? 'true' : 'false'}
                      onClick={() => (this.certificateMenuOpen = !this.certificateMenuOpen)}
                      class="paint-certificate-button"
                    >
                      <PrintIcon fill="currentColor" viewBox="0 0 24 24" class="paint-certificate-print" />
                      <span class="paint-certificate-label">{texts.printCertificate}</span>
                      <ArrowIcon class={this.certificateMenuOpen ? 'paint-certificate-arrow paint-certificate-arrow-open' : 'paint-certificate-arrow'} />
                    </button>

                    {this.certificateMenuOpen && (
                      <div role="menu" class="paint-certificate-menu">
                        {certificateUrls.map(({ language, name, url }) => (
                          <a
                            key={language}
                            role="menuitem"
                            href={url}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={() => (this.certificateMenuOpen = false)}
                            class="paint-certificate-item"
                          >
                            {name || language}
                          </a>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>

              <flexible-container isOpened={!this.isLoading && !!activeInspection?.inspectionDate}>
                <div class="paint-date">
                  <CalendarDaysIcon class="paint-date-icon" />
                  <span class="paint-date-label">{texts.inspectionDate}:</span>
                  <span class="shift-skeleton">{activeInspection?.inspectionDate}</span>
                </div>
              </flexible-container>

              <div class="paint-table-scroll">
                <div class={{ 'paint-table': true, 'loading': this.isLoading }}>
                  {/* Header */}
                  <div class="paint-table-head">
                    <div class="paint-table-head-cell" style={{ width: `${colWidths.panel}px` }}>
                      {texts.panel}
                    </div>
                    <div class="paint-table-head-cell" style={{ width: `${colWidths.position}px` }}>
                      {texts.position}
                    </div>
                    <div class="paint-table-head-cell" style={{ width: `${colWidths.side}px` }}>
                      {texts.left}
                    </div>
                    <div class="paint-table-head-cell" style={{ width: `${colWidths.side}px` }}>
                      {texts.right}
                    </div>
                  </div>

                  {/* Rows */}
                  <flexible-container height="auto">
                    {!groupedRows.length && (
                      <div class="paint-table-row-empty">
                        <div class="paint-table-row-line">
                          <div class="paint-table-cell" style={{ width: `${colWidths.panel}px` }}>
                            <div class="shift-skeleton">&nbsp;</div>
                          </div>
                          <div class="paint-table-cell" style={{ width: `${colWidths.position}px` }}>
                            <div class="shift-skeleton">&nbsp;</div>
                          </div>
                          <div class="paint-table-cell" style={{ width: `${colWidths.side}px` }}>
                            <div class="shift-skeleton">&nbsp;</div>
                          </div>
                          <div class="paint-table-cell" style={{ width: `${colWidths.side}px` }}>
                            <div class="shift-skeleton">&nbsp;</div>
                          </div>
                        </div>
                      </div>
                    )}
                    {groupedRows.map((row, idx) => (
                      <div key={row.panel + row.position + idx} class="paint-table-row">
                        <div class={{ 'paint-table-row-line': true, 'paint-table-row-line-data': true, 'paint-table-row-line-odd': idx % 2 === 1 }}>
                          <div class="paint-table-cell" style={{ width: `${colWidths.panel}px` }}>
                            <div class="shift-skeleton">{row.panel}</div>
                          </div>
                          <div class="paint-table-cell" style={{ width: `${colWidths.position}px` }}>
                            <div class="shift-skeleton">{row.position || '\u00A0'}</div>
                          </div>
                          {row.center !== undefined ? (
                            <div class="paint-table-cell" style={{ width: `${colWidths.side * 2}px` }}>
                              <div class="shift-skeleton">{row.center}</div>
                            </div>
                          ) : (
                            [
                              <div class="paint-table-cell" style={{ width: `${colWidths.side}px` }}>
                                <div class="shift-skeleton">{row.left !== undefined ? row.left : '\u00A0'}</div>
                              </div>,
                              <div class="paint-table-cell" style={{ width: `${colWidths.side}px` }}>
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
                <div class="paint-images">
                  {imageGroups?.map((imageGroup, index) => (
                    <div class="paint-image-group" key={imageGroup?.name + index}>
                      <h1 class="paint-image-group-title">
                        <span class="shift-skeleton">{imageGroup?.name}</span>
                      </h1>

                      <div class="paint-image-list">
                        {imageGroup?.images.map((image, idx) => (
                          <div class={{ 'paint-image-cell': true, 'shift-skeleton': true, 'loading': !image }} key={image + idx}>
                            <button onClick={({ target }) => this.openImage(target as HTMLImageElement, image)} class="paint-image-button">
                              <div class="paint-image-overlay">
                                <img src={Eye} />
                                <span class="paint-image-overlay-label">{texts.expand}</span>
                              </div>
                              <img src={image} class="paint-image" />
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </flexible-container>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
