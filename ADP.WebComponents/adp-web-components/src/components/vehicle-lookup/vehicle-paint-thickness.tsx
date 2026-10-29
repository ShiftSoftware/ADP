import { Component, Element, Event, EventEmitter, Host, Listen, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

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
  PhotoGroup,
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
  summaryOf,
  tabsOf,
} from './components/VehiclePaintThicknessPanel';

import { PlaceholderKind, RowVeil, drawerId, parseHiddenFields, retainDrawers, settleDrawer, shownDrawers, tableSlots, toggleDrawer } from '~features/lookup-table';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

const PHONE_QUERY = '(max-width: 720px)';

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
      clearTimeout(this.timer);
      this.timer = undefined;
      return;
    }

    if (this.value !== undefined && !this.timer) {
      this.timer = setTimeout(() => {
        this.timer = undefined;
        this.value = undefined;
        this.redraw();
      }, this.settleMs());
    }
  }

  release() {
    if (!this.timer) return;
    clearTimeout(this.timer);
    this.timer = undefined;
    this.value = undefined;
    this.redraw();
  }

  dispose() {
    clearTimeout(this.timer);
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
    this.applied = parseHiddenFields(this.hiddenFields);
    this.appliedSummary = this.showReadingSummary;
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

  @State() phone: boolean = false;

  private phoneQuery?: MediaQueryList;

  private onPhoneChange = (event: MediaQueryListEvent) => {
    this.phone = event.matches;
    void this.imageViewer?.hide();
  };

  connectedCallback() {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
    this.phoneQuery = window.matchMedia(PHONE_QUERY);
    this.phone = !!this.phoneQuery?.matches;
    this.phoneQuery?.addEventListener?.('change', this.onPhoneChange);
  }

  private sheetHold = new Retained<number>(
    () => this.settleMs() + 200,
    () => forceUpdate(this),
  );

  private wantedSheet(): number | null {
    const menuAvailable = this.certificateMenuOpen && !this.leaving && certificateKind(this.panelState()) === 'menu';
    if (menuAvailable) return null;
    return this.openDrawer !== null && !this.applied.has('photos') ? this.openDrawer : null;
  }

  private onCloseSheet = () => {
    if (this.phone && this.wantedSheet() !== null) this.closeDrawer();
  };

  private popupMarked = false;

  private markPhotoToggles() {
    if (!this.phone && !this.popupMarked) return;
    this.popupMarked = this.phone;
    const open = this.phone && this.openDrawer !== null ? drawerId('paint', this.openDrawer) : null;
    this.el.shadowRoot?.querySelectorAll('.lt-action-button').forEach(button => {
      if (!this.phone) return button.removeAttribute('aria-haspopup');
      button.setAttribute('aria-haspopup', 'dialog');
      button.setAttribute('aria-expanded', String(button.getAttribute('aria-controls') === open));
    });
  }

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

    if (newValue && !this.phone) this.certificateMenuOpen = false;
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

  private loadFrame?: number;
  private loadTimer?: ReturnType<typeof setTimeout>;

  componentDidLoad() {
    if (typeof requestAnimationFrame === 'function') this.loadFrame = requestAnimationFrame(() => (this.loadTimer = setTimeout(() => this.settleMs())));
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

  @State() applied: ReadonlySet<string> = new Set();
  @State() appliedSummary: boolean = false;

  @State() reconfiguring: boolean = false;

  @State() reshaping: boolean = false;

  private configGeneration = 0;

  @Watch('hiddenFields')
  @Watch('showReadingSummary')
  async reconfigure() {
    const next = parseHiddenFields(this.hiddenFields);
    const generation = ++this.configGeneration;

    if (sameConfig(next, this.applied) && this.showReadingSummary === this.appliedSummary) {
      this.reconfiguring = false;
      this.reshaping = false;
      return;
    }

    const adopt = () => {
      if (next.has('photos')) {
        this.retainedDrawers = [];
        this.openDrawer = null;
      }

      this.applied = next;
      this.appliedSummary = this.showReadingSummary;
      this.reconfiguring = false;
      this.reshaping = false;
    };

    if (this.leaving || !hasRecords(this.panelState())) {
      adopt();
      return;
    }

    this.reconfiguring = true;
    this.reshaping = reshapesTable(this.applied, next) && panelBody(this.panelState()) === 'rows';

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
    this.certificateMenuOpen = this.phone && this.certificateMenuOpen && certificateKind(this.panelState()) === 'menu';
    this.settleSelection(!this.vehicleLookup);
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

  @State() openDrawer: number | null = null;

  @State() retainedDrawers: number[] = [];

  @State() photosCollapsed: boolean = false;

  private photosMounted = false;

  private drawerTimer?: ReturnType<typeof setTimeout>;
  private drawerSnapshot: PaintRow[] = [];
  private photosGroups = new Retained<PhotoGroup[]>(
    () => this.settleMs(),
    () => {
      this.photosMounted = false;
      forceUpdate(this);
    },
  );

  private scheduleDrawerDrop() {
    clearTimeout(this.drawerTimer);
    if (!this.retainedDrawers.length) return;

    this.drawerTimer = setTimeout(() => {
      this.drawerTimer = undefined;
      this.retainedDrawers = [];
    }, this.settleMs());
  }

  private onToggleDrawer = (index: number) => {
    const next = toggleDrawer(this.openDrawer, index);
    this.retainedDrawers = retainDrawers(this.retainedDrawers, this.openDrawer, next);
    this.drawerSnapshot = this.shownBefore;
    this.openDrawer = next;
    this.scheduleDrawerDrop();
  };

  private closeDrawer = () => {
    if (this.openDrawer === null) return;
    const next = null;
    this.retainedDrawers = retainDrawers(this.retainedDrawers, this.openDrawer, next);
    this.drawerSnapshot = this.shownBefore;
    this.openDrawer = next;
    this.scheduleDrawerDrop();
  };

  private onTogglePhotos = () => {
    this.photosCollapsed = !this.photosCollapsed;
  };

  private settleSelection(cleared: boolean) {
    const state = this.panelState();
    const rows = cleared ? [] : shownRows(state);
    const next = settleDrawer({ open: this.openDrawer, hasAt: rows.map(rowHasPhotos), cleared });

    if (next !== this.openDrawer) {
      this.retainedDrawers = retainDrawers(this.retainedDrawers, this.openDrawer, next);
      this.drawerSnapshot = this.shownBefore;
      this.openDrawer = next;
      this.scheduleDrawerDrop();
    }

    if (cleared) this.photosCollapsed = false;
  }

  // #endregion

  /** Host-controlled visibility (e.g. a feature gate). The links themselves come from the lookup */
  @Prop() showCertificateButton: boolean = false;

  @State() certificateMenuOpen: boolean = false;

  private certificate = new Retained<CertificateSnapshot>(
    () => this.settleMs(),
    () => forceUpdate(this),
  );
  private summary = new Retained<ReadingSummary>(
    () => this.settleMs(),
    () => forceUpdate(this),
  );
  private tabs = new Retained<PaintTab[]>(
    () => this.settleMs(),
    () => forceUpdate(this),
  );

  private menuItems = () => Array.from(this.el.shadowRoot?.querySelectorAll<HTMLElement>('.paint-menu .paint-certificate-item') ?? []);

  private onToggleCertificate = () => {
    this.certificateMenuOpen = !this.certificateMenuOpen;
  };

  @Listen('keydown')
  onMenuKeyDown(event: KeyboardEvent) {
    const root = this.el.shadowRoot;
    const target = root?.activeElement as HTMLElement | null;
    if (!target || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;

    if (target.classList.contains('paint-certificate-button')) {
      if (event.key !== 'ArrowDown' || this.certificateMenuOpen || target.hasAttribute('disabled')) return;
      event.preventDefault();
      this.onToggleCertificate();
      return;
    }

    if (!this.certificateMenuOpen || !target.classList.contains('paint-certificate-item')) return;
    const items = this.menuItems();
    const at = items.indexOf(target);
    if (at < 0) return;
    event.preventDefault();
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : (at + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
    items[next]?.focus({ preventScroll: true });
  }

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

  private retainedBody = 'none';
  private retainTimer?: ReturnType<typeof setTimeout>;

  private retainBody(body: string) {
    if (body !== 'none') {
      this.retainedBody = body;
      clearTimeout(this.retainTimer);
      this.retainTimer = undefined;
      return;
    }

    if (this.retainedBody !== 'none' && !this.retainTimer) {
      this.retainTimer = setTimeout(() => {
        this.retainTimer = undefined;
        this.retainedBody = 'none';
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  private retainedPlaceholder: PlaceholderKind | null = null;
  private placeholderTimer?: ReturnType<typeof setTimeout>;

  private retainPlaceholder(kind: PlaceholderKind | null) {
    if (kind !== null) {
      this.retainedPlaceholder = kind;
      clearTimeout(this.placeholderTimer);
      this.placeholderTimer = undefined;
      return;
    }

    if (this.retainedPlaceholder !== null && !this.placeholderTimer) {
      this.placeholderTimer = setTimeout(() => {
        this.placeholderTimer = undefined;
        this.retainedPlaceholder = null;
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  private shownBefore: PaintRow[] = [];
  private bodyWasOpen = false;

  private outgoing: PaintRow[] = [];
  private outgoingVeil: RowVeil = 'cover';
  private outgoingTimer?: ReturnType<typeof setTimeout>;

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
        this.outgoing = before.slice(current.length);
        this.outgoingVeil = cause === 'page' ? this.lastVeil : 'cover';
        clearTimeout(this.outgoingTimer);
        this.outgoingTimer = setTimeout(() => {
          this.outgoingTimer = undefined;
          this.outgoing = [];
          forceUpdate(this);
        }, this.settleMs());
      } else if (current.length > 0) {
        this.outgoing = [];
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
      this.retainedBody,
      shownRows(state).length,
      [...this.applied].sort().join(','),
      this.appliedSummary,
      this.reconfiguring,
      this.reshaping,
      this.shownTabIndex,
      tabsOf(state, this.locale).length,
      certificateKind(state),
      this.openDrawer,
      this.retainedDrawers.join('.'),
      this.photosCollapsed,
      this.opened,
      this.outgoing.length,
      this.isError,
      panelPlaceholder(state, this.leaving),
    ].join('|');
  }

  private openFrame?: number;

  componentDidRender() {
    this.announceVerdict();
    this.markPhotoToggles();

    if (this.openPending) {
      this.openPending = false;
      this.cancelOpenFrame();
      this.openFrame = requestAnimationFrame(() => {
        this.openFrame = requestAnimationFrame(() => {
          this.openFrame = undefined;
          this.opened = Number.MAX_SAFE_INTEGER;
          forceUpdate(this);
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
    clearTimeout(this.retainTimer);
    clearTimeout(this.placeholderTimer);
    clearTimeout(this.swapTimer);
    clearTimeout(this.drawerTimer);
    clearTimeout(this.outgoingTimer);
    clearTimeout(this.loadTimer);
    this.cancelOpenFrame();
    if (this.loadFrame !== undefined) cancelAnimationFrame(this.loadFrame);
    this.phoneQuery?.removeEventListener?.('change', this.onPhoneChange);
    this.phoneQuery = undefined;
    this.sheetHold.dispose();
    this.photosGroups.dispose();
    this.certificate.dispose();
    this.summary.dispose();
    this.tabs.dispose();
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
      hidden: this.applied,
      showSummary: this.appliedSummary,
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
    this.retainBody(body);
    this.retainPlaceholder(panelPlaceholder(state, this.leaving));

    const open = bodyOpen(state, this.leaving, this.reshaping);
    const current = body === 'rows' ? shownRows(state) : [];
    this.updateSlots(current, open);

    const veil: RowVeil = busy ? 'cover' : this.swapping ? 'fade' : 'none';
    this.lastVeil = veil;
    const slots = tableSlots({ current: current.length, outgoing: this.outgoing.length, opened: this.opened, bodyOpen: open, currentVeil: veil, outgoingVeil: this.outgoingVeil });

    const tabs = tabsOf(state, this.locale);
    this.tabs.track(tabs.length ? tabs : null);
    const kind = certificateKind(state);
    this.certificate.track(kind === 'hidden' ? null : { kind, urls: state.certificateUrls ?? [] });
    this.summary.track(state.showSummary ? summaryOf(current) : null);
    const groups = allPhotoGroups(current, this.locale);
    this.photosGroups.track(groups.length ? groups : null);
    const photosBodyOpen = !this.photosCollapsed && photosSectionOpen(state, groups.length, this.leaving, this.reshaping);
    if (photosBodyOpen) this.photosMounted = true;

    const wanted = this.phone ? this.wantedSheet() : null;
    this.sheetHold.track(wanted);
    const held = wanted ?? this.sheetHold.value ?? null;
    const sheet: PaintSheet | null = held === null ? null : { index: held, open: wanted !== null };

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
            reconfiguring={this.reconfiguring}
            reshaping={this.reshaping}
            swapping={this.swapping}
            retainedBody={this.retainedBody}
            retainedPlaceholder={this.retainedPlaceholder}
            selectedTab={this.activeTabIndex}
            retainedTabs={this.tabs.value ?? []}
            retainedCertificate={this.certificate.value ?? null}
            retainedSummary={this.summary.value ?? null}
            slotRows={[...current, ...this.outgoing]}
            slots={slots}
            drawerOpen={this.openDrawer}
            drawerShown={shownDrawers(this.openDrawer, this.retainedDrawers)}
            drawerRowFor={(row, index) => ((row && rowHasPhotos(row)) || !this.retainedDrawers.includes(index) ? row : (this.drawerSnapshot[index] ?? row))}
            onToggleDrawer={this.onToggleDrawer}
            photosOpen={!this.photosCollapsed}
            photosRendered={photosBodyOpen || (this.photosMounted && !state.hidden.has('photos'))}
            retainedGroups={this.photosGroups.value ?? []}
            onTogglePhotos={this.onTogglePhotos}
            onSelectTab={this.onSelectTab}
            certificateOpen={this.certificateMenuOpen}
            onToggleCertificate={this.onToggleCertificate}
            onCloseCertificate={this.onCloseCertificate}
            onOpenPhoto={(image, src, label) => this.openImage(image, src, label)}
            phone={this.phone}
            sheet={sheet}
            onCloseSheet={this.onCloseSheet}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
