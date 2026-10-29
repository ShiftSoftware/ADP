import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import {
  CertificateSnapshot,
  DEFAULT_HIDDEN_FIELDS,
  PaintRow,
  PaintTab,
  PaintSheet,
  PanelState,
  PhotoActions,
  PhotoGroup,
  PhotoState,
  ReadingSummary,
  VehiclePaintThicknessPanel,
  allPhotoGroups,
  bodyOpen,
  certificateKind,
  hasRecords,
  panelBody,
  panelPlaceholder,
  photosSectionOpen,
  reshapesTable,
  rowHasPhotos,
  sameConfig,
  shownRows,
  slotIndex,
  slotKey,
  summaryOf,
  tabsOf,
} from './components/VehiclePaintThicknessPanel';

import { RowVeil, parseHiddenFields, tableSlots } from '~features/lookup-table';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

// The --settle token's value, for when it cannot be read (a spec page has no computed styles).
const DEFAULT_SETTLE_MS = 480;

class Retained<T> {
  value: T | undefined;
  private timer?: ReturnType<typeof setTimeout>;

  constructor(
    private settleMs: () => number,
    private redraw: () => void,
  ) {}

  track(content: T | null) {
    if (content !== null) {
      this.value = content;
      this.cancel();
    } else if (this.value !== undefined && !this.timer) {
      this.dropLater();
    }
  }

  hold(content: T) {
    this.value = content;
    this.cancel();
    this.dropLater();
  }

  clear() {
    this.cancel();
    this.value = undefined;
  }

  dispose() {
    this.cancel();
  }

  private dropLater() {
    this.timer = setTimeout(() => {
      this.timer = undefined;
      this.value = undefined;
      this.redraw();
    }, this.settleMs());
  }

  private cancel() {
    clearTimeout(this.timer);
    this.timer = undefined;
  }
}

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
    this.applied = { hidden: parseHiddenFields(this.hiddenFields), summary: this.showReadingSummary };
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

  private expandedImage = '';

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

  private photoStates = new Map<string, Exclude<PhotoState, 'loading'>>();

  private settlePhoto = (src: string, state: Exclude<PhotoState, 'loading'>) => {
    if (this.photoStates.get(src) === state) return;
    this.photoStates.set(src, state);
    forceUpdate(this);
  };

  // #endregion

  /** Whether the popovers are bottom sheets, as shift-popover decides it for its own breakpoint. */
  @State() sheetMode: boolean = false;

  private onSheetChange = (sheet: boolean) => {
    this.sheetMode = sheet;
  };

  private redraw = () => forceUpdate(this);

  private retain<T>() {
    return new Retained<T>(() => this.settleMs(), this.redraw);
  }

  // The sheet slides down over --settle; its row is kept a little longer so it never empties in view.
  private sheetHold = new Retained<string>(() => this.settleMs() + 200, this.redraw);

  private wantedSheet(): string | null {
    const menuAvailable = this.certificateMenuOpen && !this.leaving && certificateKind(this.panelState()) === 'menu';
    if (menuAvailable) return null;
    return this.openKey !== null && !this.applied.hidden.has('photos') ? this.openKey : null;
  }

  private onCloseSheet = () => {
    if (this.sheetMode && this.wantedSheet() !== null) this.closeDrawer();
  };

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

  @Prop() hiddenFields: string = DEFAULT_HIDDEN_FIELDS;

  @Prop() showReadingSummary: boolean = false;

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
  async fetchVin(newData: VehicleLookupDTO | string, headers: object = {}) {
    this.leaveGeneration++;
    this.leaving = false;
    await setVehicleLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    if (!this.isLoading && !(await this.leave())) return;
    setVehicleLookupErrorState(this, message);
  }

  @Method()
  async clearData() {
    if (!(await this.leave())) return;
    this.emptyPanel();
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  private settleCache?: number;

  private settleMs(): number {
    if (this.settleCache === undefined) {
      const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
      const parsed = parseFloat(raw);
      this.settleCache = (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
    }

    return this.settleCache;
  }

  private async leave(): Promise<boolean> {
    const generation = ++this.leaveGeneration;

    if (!this.vehicleLookup && !this.isError) return true;

    clearTimeout(this.networkTimeoutRef);
    this.abortController?.abort();
    this.leaving = true;
    this.certificateMenuOpen = false;
    this.settleSelection(true);

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));

    if (generation !== this.leaveGeneration) return false;
    this.leaving = false;
    return true;
  }

  private emptyPanel() {
    this.vehicleLookup = undefined;
    this.isError = false;
    this.errorMessage = undefined;
    this.isLoading = false;
  }

  @State() applied: { hidden: ReadonlySet<string>; summary: boolean } = { hidden: new Set(), summary: false };

  @State() reconfiguring: false | 'values' | 'shape' = false;

  private get reshaping() {
    return this.reconfiguring === 'shape';
  }

  private configGeneration = 0;

  @Watch('hiddenFields')
  @Watch('showReadingSummary')
  async reconfigure() {
    const next = parseHiddenFields(this.hiddenFields);
    const generation = ++this.configGeneration;

    if (sameConfig(next, this.applied.hidden) && this.showReadingSummary === this.applied.summary) {
      this.reconfiguring = false;
      return;
    }

    const adopt = () => {
      if (next.has('photos')) {
        this.retainedDrawer.clear();
        this.openKey = null;
      }

      this.applied = { hidden: next, summary: this.showReadingSummary };
      this.reconfiguring = false;
    };

    if (this.leaving || !hasRecords(this.panelState())) {
      adopt();
      return;
    }

    this.reconfiguring = reshapesTable(this.applied.hidden, next) && panelBody(this.panelState()) === 'rows' ? 'shape' : 'values';

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));
    if (generation !== this.configGeneration) return;

    adopt();
  }

  // #endregion

  @State() activeTabIndex: number = 0;

  @State() shownTabIndex: number = 0;

  @State() swapping: boolean = false;

  private swapGeneration = 0;
  private swapTimer?: ReturnType<typeof setTimeout>;

  @Watch('vehicleLookup')
  onVehicleLookupChange() {
    this.swapGeneration++;
    clearTimeout(this.swapTimer);
    this.swapping = false;
    this.activeTabIndex = 0;
    this.shownTabIndex = 0;
    this.certificateMenuOpen = this.certificateMenuOpen && certificateKind(this.panelState()) === 'menu';
    this.settleSelection(!this.vehicleLookup);
    this.keepPhotoStates();
  }

  // Only the new answer's photos: one that stays keeps its element, which will not fire `load` again.
  private keepPhotoStates() {
    const kept = new Set((this.vehicleLookup?.paintThicknessInspections ?? []).flatMap(inspection => (inspection.panels ?? []).flatMap(panel => panel.images ?? [])));
    for (const src of this.photoStates.keys()) if (!kept.has(src)) this.photoStates.delete(src);
  }

  private onSelectTab = (index: number) => {
    if (index === this.activeTabIndex || this.isLoading || this.leaving) return;

    this.activeTabIndex = index;
    const generation = ++this.swapGeneration;

    clearTimeout(this.swapTimer);
    this.swapping = true;
    this.swapTimer = setTimeout(() => {
      if (generation !== this.swapGeneration) return;
      this.swapCause = 'page';
      this.shownTabIndex = index;
      this.settleSelection(false);
      this.swapping = false;
    }, this.settleMs());
  };

  // #endregion

  @State() openKey: string | null = null;

  @State() photosCollapsed: boolean = false;

  private photosMounted = false;

  private retainedDrawer = this.retain<string>();
  private drawerSnapshot: PaintRow[] = [];
  private photosGroups = new Retained<PhotoGroup[]>(
    () => this.settleMs(),
    () => {
      this.photosMounted = false;
      this.redraw();
    },
  );

  private moveDrawer(next: string | null) {
    if (this.openKey !== null && this.openKey !== next) this.retainedDrawer.hold(this.openKey);
    else if (this.retainedDrawer.value === next) this.retainedDrawer.clear();
    this.drawerSnapshot = this.shownBefore;
    this.openKey = next;
  }

  private onToggleDrawer = (key: string) => this.moveDrawer(this.openKey === key ? null : key);

  private closeDrawer = () => {
    if (this.openKey !== null) this.moveDrawer(null);
  };

  private onTogglePhotos = () => {
    this.photosCollapsed = !this.photosCollapsed;
  };

  private settleSelection(cleared: boolean) {
    const state = this.panelState();
    const rows = cleared ? [] : shownRows(state);
    const kept = rows.some((row, index) => slotKey(index) === this.openKey && rowHasPhotos(row));

    if (this.openKey !== null && !kept) this.moveDrawer(null);

    if (cleared) this.photosCollapsed = false;
  }

  // #endregion

  /** Host-controlled visibility (e.g. a feature gate). The links themselves come from the lookup */
  @Prop() showCertificateButton: boolean = false;

  @State() certificateMenuOpen: boolean = false;

  private certificate = this.retain<CertificateSnapshot>();
  private summary = this.retain<ReadingSummary>();
  private tabs = this.retain<PaintTab[]>();

  private onToggleCertificate = () => {
    this.certificateMenuOpen = !this.certificateMenuOpen;
  };

  private onCloseCertificate = () => {
    this.certificateMenuOpen = false;
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

  private body = this.retain<'rows'>();
  private placeholder = this.retain<'idle' | 'empty'>();

  private shownBefore: PaintRow[] = [];
  private bodyWasOpen = false;

  private outgoing = this.retain<PaintRow[]>();
  private outgoingVeil: RowVeil = 'cover';

  private opened = 0;
  private openPending = false;

  private swapCause: 'lookup' | 'page' = 'lookup';
  private lastVeil: RowVeil = 'none';

  private cancelOpenFrame() {
    if (this.openFrame !== undefined) cancelAnimationFrame(this.openFrame);
    this.openFrame = undefined;
  }

  private updateSlots(current: PaintRow[], open: boolean) {
    const before = this.shownBefore;
    const changed = current.length !== before.length || current.some((row, index) => row !== before[index]);

    if (changed) {
      const cause = this.swapCause;
      this.swapCause = 'lookup';

      if (this.bodyWasOpen && before.length > current.length) {
        this.outgoing.hold(before.slice(current.length));
        this.outgoingVeil = cause === 'page' ? this.lastVeil : 'cover';
      } else if (current.length > 0) {
        this.outgoing.clear();
      }

      if (this.bodyWasOpen && open) {
        this.cancelOpenFrame();
        this.opened = Math.min(before.length, current.length);
        this.openPending = this.opened < current.length;
      } else {
        this.cancelOpenFrame();
        this.opened = current.length;
        this.openPending = false;
      }
    } else if (!open) {
      this.cancelOpenFrame();
      this.opened = current.length;
    }

    this.shownBefore = current;
    this.bodyWasOpen = open;
  }

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

  private signature(): string {
    const state = this.panelState();

    return [
      this.isLoading || this.leaving,
      this.vehicleLookup?.vin,
      panelBody(state),
      this.body.value,
      shownRows(state).length,
      [...this.applied.hidden].sort().join(','),
      this.applied.summary,
      this.reconfiguring,
      this.shownTabIndex,
      tabsOf(state, this.locale).length,
      certificateKind(state),
      this.openKey,
      this.retainedDrawer.value,
      this.photosCollapsed,
      this.sheetMode,
      this.opened,
      this.outgoing.value?.length,
      this.isError,
      panelPlaceholder(state, this.leaving),
    ].join('|');
  }

  private openFrame?: number;

  componentDidRender() {
    this.announceVerdict();

    if (this.openPending) {
      this.openPending = false;
      this.cancelOpenFrame();
      // Two frames, so the new slots are styled shut once and open as a transition; MAX_SAFE_INTEGER opens every slot there is.
      this.openFrame = requestAnimationFrame(() => {
        this.openFrame = requestAnimationFrame(() => {
          this.openFrame = undefined;
          this.opened = Number.MAX_SAFE_INTEGER;
          this.redraw();
        });
      });
    }

    const signature = this.signature();
    const changed = this.layoutSignature !== undefined && this.layoutSignature !== signature;
    this.layoutSignature = signature;

    if (!changed) return;

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  disconnectedCallback() {
    clearTimeout(this.swapTimer);
    this.cancelOpenFrame();
    [this.body, this.placeholder, this.outgoing, this.retainedDrawer, this.sheetHold, this.photosGroups, this.certificate, this.summary, this.tabs].forEach(retained =>
      retained.dispose(),
    );
    this.heightAnnouncer?.dispose();
  }

  // #endregion

  private errorText(): string | undefined {
    return this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined;
  }

  private panelState(): PanelState {
    return {
      vehicleLoaded: !!this.vehicleLookup?.vin && !this.isError,
      authorized: this.vehicleLookup?.isAuthorized,
      inspections: this.vehicleLookup?.paintThicknessInspections,
      shownIndex: this.shownTabIndex,
      hidden: this.applied.hidden,
      showSummary: this.applied.summary,
      certificateAvailable: this.vehicleLookup?.paintThicknessCertificateAvailable,
      certificateUrls: this.vehicleLookup?.paintThicknessCertificateUrls,
      showCertificateButton: this.showCertificateButton,
      error: this.errorText(),
      model: this.vehicleLookup?.vehicleSpecification?.modelDescription,
      modelYear: this.vehicleLookup?.vehicleVariantInfo?.modelYear ?? this.vehicleLookup?.vehicleSpecification?.modelYear,
    };
  }

  render() {
    const state = this.panelState();

    const verdict = recordVerdict({
      locale: this.locale.sharedLocales,
      error: this.errorText(),
      vehicleLoaded: state.vehicleLoaded,
      authorized: state.authorized,
      hasRecords: hasRecords(state),
    });
    this.currentVerdict = verdict.accent;

    const busy = this.isLoading || this.leaving;
    const body = panelBody(state);
    this.body.track(body === 'rows' ? body : null);
    this.placeholder.track(panelPlaceholder(state, this.leaving));

    const open = bodyOpen(state, this.leaving, this.reshaping);
    const current = body === 'rows' ? shownRows(state) : [];
    this.updateSlots(current, open);

    const veil: RowVeil = busy ? 'cover' : this.swapping ? 'fade' : 'none';
    this.lastVeil = veil;
    const outgoing = this.outgoing.value ?? [];
    const retainedKey = this.retainedDrawer.value ?? null;
    const slots = tableSlots({ current: current.length, outgoing: outgoing.length, opened: this.opened, bodyOpen: open, currentVeil: veil, outgoingVeil: this.outgoingVeil });

    const tabs = tabsOf(state, this.locale);
    this.tabs.track(tabs.length ? tabs : null);
    const kind = certificateKind(state);
    this.certificate.track(kind === 'hidden' ? null : { kind, urls: state.certificateUrls ?? [] });
    this.summary.track(state.showSummary ? summaryOf(current) : null);
    const groups = allPhotoGroups(current, this.locale);
    this.photosGroups.track(groups.length ? groups : null);
    const photosBodyOpen = !this.photosCollapsed && photosSectionOpen(state, groups.length, this.leaving, this.reshaping);
    if (photosBodyOpen) this.photosMounted = true;

    const wanted = this.sheetMode ? this.wantedSheet() : null;
    this.sheetHold.track(wanted);
    const held = wanted ?? this.sheetHold.value ?? null;
    const sheet: PaintSheet | null = held === null ? null : { key: held, open: wanted !== null };
    const photos: PhotoActions = { open: (image, src, label) => this.openImage(image, src, label), state: src => this.photoStates.get(src) ?? 'loading', settle: this.settlePhoto };

    return (
      <Host translate="no">
        <shift-image ref={el => (this.imageViewer = el)} language={this.language} onViewerChange={this.onViewerChange} />

        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          isLoading={busy}
          coreOnly={this.coreOnly}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <VehiclePaintThicknessPanel
            locale={this.locale}
            language={this.language}
            direction={this.locale.sharedLocales.direction}
            state={state}
            verdict={verdict}
            busy={busy}
            leaving={this.leaving}
            reconfiguring={!!this.reconfiguring}
            reshaping={this.reshaping}
            swapping={this.swapping}
            retainedBody={this.body.value ?? 'none'}
            retainedPlaceholder={this.placeholder.value ?? null}
            selectedTab={this.activeTabIndex}
            retainedTabs={this.tabs.value ?? []}
            retainedCertificate={this.certificate.value ?? null}
            retainedSummary={this.summary.value ?? null}
            slotRows={[...current, ...outgoing]}
            slots={slots}
            openKey={this.openKey}
            retainedKey={retainedKey}
            drawerRowFor={(row, key) => ((row && rowHasPhotos(row)) || key !== retainedKey ? row : (this.drawerSnapshot[slotIndex(key)] ?? row))}
            onToggleDrawer={this.onToggleDrawer}
            photosOpen={!this.photosCollapsed}
            photosRendered={photosBodyOpen || (this.photosMounted && !state.hidden.has('photos'))}
            retainedGroups={this.photosGroups.value ?? []}
            onTogglePhotos={this.onTogglePhotos}
            onSelectTab={this.onSelectTab}
            certificateOpen={this.certificateMenuOpen}
            onToggleCertificate={this.onToggleCertificate}
            onCloseCertificate={this.onCloseCertificate}
            photos={photos}
            sheetMode={this.sheetMode}
            sheet={sheet}
            onCloseSheet={this.onCloseSheet}
            onSheetChange={this.onSheetChange}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
