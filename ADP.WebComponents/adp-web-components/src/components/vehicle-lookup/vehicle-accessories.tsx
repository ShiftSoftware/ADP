import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { revealBelow } from '~lib/reveal-below';
import { createResizeSettle } from '~lib/resize-settle';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';

import {
  COLLAPSED_ROWS,
  DEFAULT_HIDDEN_FIELDS,
  PanelNotice,
  PanelState,
  VehicleAccessoriesPanel,
  collapseNeedsReveal,
  hasRecords,
  leadNotice,
  listed,
  panelBody,
  sameColumns,
} from './components/VehicleAccessoriesPanel';

import { parseHiddenFields } from '~features/lookup-table';
import { closeImageViewer, ImageViewer, ImageViewerInterface, openImageViewer } from '~features/image-viewer';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

@Component({
  shadow: true,
  tag: 'vehicle-accessories',
  styleUrl: 'vehicle-accessories.css',
})
export class VehicleAccessories implements MultiLingual, VehicleInfoLayoutInterface, ImageViewerInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof accessoriesSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...accessoriesSchema.getDefault() };

  async componentWillLoad() {
    this.applied = parseHiddenFields(this.hiddenFields);
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.accessories', accessoriesSchema)]);
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
  @Prop() today?: string;
  @Prop() disableVinValidation: boolean = false;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  /**
   * Comma-separated column names to leave out: `partNumber`, `description`, `image`, `jobNumber`,
   * `invoiceNumber`, `companyName`. A shown column with no value reads as a dash; a hidden one has
   * no heading, no cell and no caption. The default hides the three columns this panel did not
   * always have, so a host that configures nothing sees what it saw before.
   */
  @Prop() hiddenFields: string = DEFAULT_HIDDEN_FIELDS;

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

  // A lookup takes over a leave still in progress: bumping the generation makes the older call give up
  // instead of flipping `leaving` back, and `leaving` is cleared here because the older call will not.
  @Method()
  async fetchVin(newData: VehicleLookupDTO | string, headers: object = {}) {
    this.leaveGeneration++;
    this.leaving = false;
    await setVehicleLookupData(this, newData, headers);
  }

  // Called on a settled card, the error has to enter through the leave or the red pill mounts at full opacity.
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

  // #endregion

  // #region Leaving a state

  /** The panel is on its way to empty or to an error: the head leaves and the body is shutting over its rows. */
  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  /** How long a state change takes to settle: the --settle token plus a margin for the frame it starts on. */
  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  /**
   * Shuts the body over whatever it holds and resolves once it has settled, so the caller can swap
   * the content while nothing is on screen. True when the caller may go on; false when a newer call
   * took over meanwhile. Resolves at once when there is nothing to leave.
   */
  private async leave(): Promise<boolean> {
    const generation = ++this.leaveGeneration;

    if (!this.vehicleLookup && !this.isError) return true;

    clearTimeout(this.networkTimeoutRef);
    this.abortController?.abort();
    this.leaving = true;

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

  // #endregion

  // #region Configuration (hiddenFields)

  /** The hidden set the render uses. It trails `hiddenFields` by a settle: the body shuts, this swaps, the body reopens. */
  @State() applied: ReadonlySet<string> = new Set();

  @State() reconfiguring: boolean = false;

  private configGeneration = 0;

  // Not a lookup: nothing is fetched, so there is no skeleton and no spinner. The body shuts over the old
  // columns, the layout swaps while it is shut, and it reopens. The head's words never change here.
  @Watch('hiddenFields')
  async reconfigure() {
    const next = parseHiddenFields(this.hiddenFields);
    const generation = ++this.configGeneration;

    if (sameColumns(next, this.applied)) {
      this.reconfiguring = false;
      return;
    }

    // Nothing is open to shut (idle, loading, a notice, an empty list): swap at once.
    if (this.isLoading || this.leaving || panelBody(this.panelState()) !== 'rows') {
      this.applied = next;
      this.reconfiguring = false;
      return;
    }

    this.reconfiguring = true;

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));
    if (generation !== this.configGeneration) return;

    this.applied = next;
    this.reconfiguring = false;
  }

  // #endregion

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

  // #endregion

  // #region What the body held, and what the strip said

  private retainedBody = 'none';
  private retainTimer?: ReturnType<typeof setTimeout>;
  private retainedNotice: PanelNotice | null = null;

  /** Remembers the body's kind and forgets it a settle after the body shut, so rows slide away with it. */
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

  // #endregion

  // #region The disclosure: "Show all 23 accessories"

  /** The reader asked for every row. Reset to collapsed whenever a new lookup lands. */
  @State() expanded: boolean = false;

  /** Collapsing: the extra rows stay rendered until the fold has shut, so they leave by the fold closing, not by vanishing. */
  @State() collapsing: boolean = false;

  private collapseTimer?: ReturnType<typeof setTimeout>;
  private stopFollowing?: () => void;
  private toggled = false;
  private revealPending = false;
  private releaseFoldAfterRender = false;

  private fold(): HTMLElement | null {
    return this.el.shadowRoot?.querySelector<HTMLElement>('.accessories-fold') ?? null;
  }

  /** Back to `auto`; called only once nothing is left to disagree with the pin. */
  private releaseFold() {
    const fold = this.fold();
    if (!fold) return;
    fold.style.transition = '';
    fold.style.removeProperty('--resize-settle-height');
  }

  /** Pins the fold at `from` without a transition and sends it to `to` on the stylesheet's, as lib/resize-settle does for a render. */
  private pinFold(from: number, to: number) {
    const fold = this.fold();
    if (!fold) return;

    fold.style.transition = 'none';
    fold.style.setProperty('--resize-settle-height', `${from}px`);
    // A call, never a bare read: the production minifier drops `void el.offsetHeight` as dead code.
    fold.getBoundingClientRect();
    fold.style.transition = '';
    fold.style.setProperty('--resize-settle-height', `${to}px`);
  }

  private resetDisclosure() {
    clearTimeout(this.collapseTimer);
    this.collapseTimer = undefined;
    this.stopFollowing?.();
    this.stopFollowing = undefined;
    this.revealPending = false;
    this.toggled = false;
    this.expanded = false;
    this.collapsing = false;
    this.releaseFoldAfterRender = true;
  }

  // A new vehicle lands only after the body has shut (the shared helper's one-second wait), so the
  // reset is never seen: the rows that go are not on screen.
  @Watch('vehicleLookup')
  onVehicleChange() {
    this.resetDisclosure();
  }

  private expand() {
    clearTimeout(this.collapseTimer);
    this.collapseTimer = undefined;
    this.collapsing = false;
    this.toggled = true;
    this.expanded = true;
  }

  private collapse() {
    const fold = this.fold();
    const button = this.el.shadowRoot?.querySelector<HTMLElement>('.accessories-more-button');

    let removed = 0;
    if (fold) {
      const from = fold.getBoundingClientRect().height;
      removed = Array.from(fold.querySelectorAll<HTMLElement>('.lt-row'))
        .slice(COLLAPSED_ROWS)
        .reduce((sum, row) => sum + row.getBoundingClientRect().height, 0);
      this.pinFold(from, Math.max(0, from - removed));
    }

    this.revealPending = !!button && collapseNeedsReveal(button.getBoundingClientRect().top, removed);

    this.collapsing = true;
    this.expanded = false;

    clearTimeout(this.collapseTimer);
    this.collapseTimer = setTimeout(() => {
      this.collapseTimer = undefined;
      this.releaseFoldAfterRender = true;
      this.collapsing = false;
    }, this.settleMs());
  }

  private toggle = () => {
    if (this.expanded) this.collapse();
    else this.expand();
  };

  // #endregion

  // #region Height

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

  private resizeSettle = createResizeSettle(() => this.el.shadowRoot?.querySelectorAll<HTMLElement>('.accessories-fold') ?? []);

  /** Everything that moves the panel's height, joined; a render that changes none of it announces nothing. */
  private signature(): string {
    const state = this.panelState();

    return [
      this.isLoading || this.leaving,
      this.vehicleLookup?.vin,
      panelBody(state),
      this.retainedBody,
      listed(state).length,
      [...this.applied].sort().join(','),
      this.reconfiguring,
      this.expanded,
      this.collapsing,
      this.isError,
    ].join('|');
  }

  componentWillRender() {
    if (this.toggled) this.resizeSettle.beforeSwap();
  }

  componentDidRender() {
    this.announceVerdict();

    if (this.toggled) {
      this.toggled = false;
      this.resizeSettle.afterSwap();
    }

    if (this.releaseFoldAfterRender && !this.collapsing) {
      this.releaseFoldAfterRender = false;
      this.releaseFold();
    }

    if (this.revealPending) {
      this.revealPending = false;
      const anchor = this.el.shadowRoot?.querySelector('.accessories-more');
      this.stopFollowing?.();
      if (anchor) this.stopFollowing = revealBelow(anchor, this.settleMs() + 80);
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
    clearTimeout(this.collapseTimer);
    this.stopFollowing?.();
    this.resizeSettle.dispose();
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
      accessories: this.vehicleLookup?.accessories,
      hidden: this.applied,
      error: this.errorText(),
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
    this.retainBody(panelBody(state));

    // Keeps the last sentence the strip said, so a notice that is fading out still has its words.
    this.retainedNotice = leadNotice(state, this.locale) ?? this.retainedNotice;

    return (
      <Host translate="no">
        <ImageViewer closeImageViewer={() => closeImageViewer.bind(this)()} expandedImage={this.expandedImage} />

        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          isLoading={busy}
          coreOnly={this.coreOnly}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <VehicleAccessoriesPanel
            locale={this.locale}
            language={this.language}
            state={state}
            verdict={verdict}
            busy={busy}
            reconfiguring={this.reconfiguring}
            retainedBody={this.retainedBody}
            retainedNotice={this.retainedNotice}
            expanded={this.expanded}
            showingAll={this.expanded || this.collapsing}
            onToggle={this.toggle}
            onOpenMedia={(image, src) => openImageViewer.bind(this)(image, src)}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
