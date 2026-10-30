import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

import {
  DEFAULT_PAGE_SIZE,
  PAGE_SIZE_STORAGE_KEY,
  PageSize,
  PanelState,
  VehicleServiceHistoryPanel,
  bodyOpen,
  clampPage,
  listed,
  pageForSize,
  pageRows,
  panelBody,
  panelVerdict,
  parsePageSize,
} from './components/VehicleServiceHistoryPanel';

import { RowVeil, tableSlots } from '~features/lookup-table';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

@Component({
  shadow: true,
  tag: 'vehicle-service-history',
  styleUrl: 'vehicle-service-history.css',
})
export class VehicleServiceHistory implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof ServiceHistorySchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...ServiceHistorySchema.getDefault() };

  async componentWillLoad() {
    this.pageSize = this.initialPageSize();
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.serviceHistory', ServiceHistorySchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

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

  // A lookup takes over a leave still in progress: bumping the generation makes the older call give up
  // instead of flipping `leaving` back, and `leaving` is cleared here because the older call will not.
  @Method()
  async fetchVin(newData: VehicleLookupDTO | string, headers: object = {}) {
    this.shutDrawer();
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

    // A slot keeps its element across lookups, so an open drawer would stay open on another visit.
    this.shutDrawer();

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

  // #region What the body held

  private retainedBody = 'none';
  private retainTimer?: ReturnType<typeof setTimeout>;

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

  // #region Drawer: one visit open at a time, keyed by its slot

  @State() openKey: string | null = null;
  /** The drawer that is shutting: its content stays rendered until it has slid shut, then is dropped. */
  @State() retainedKey: string | null = null;
  private retainedKeyTimer?: ReturnType<typeof setTimeout>;

  private toggleDrawer = (key: string) => {
    // The rows are under the skeleton or fading to another page: their visits are about to change.
    if (this.isLoading || this.leaving || this.paging) return;

    if (this.openKey === key) {
      this.shutDrawer();
      return;
    }

    if (this.openKey !== null) this.retainDrawer(this.openKey);
    this.openKey = key;
  };

  private shutDrawer() {
    if (this.openKey === null) return;
    this.retainDrawer(this.openKey);
    this.openKey = null;
  }

  private retainDrawer(key: string) {
    this.retainedKey = key;
    clearTimeout(this.retainedKeyTimer);
    this.retainedKeyTimer = setTimeout(() => {
      this.retainedKeyTimer = undefined;
      this.retainedKey = null;
    }, this.settleMs());
  }

  // #endregion

  // #region Pages

  @Prop() pagination: boolean = false;

  @Watch('pagination')
  onPaginationChange() {
    this.goTo(0, this.initialPageSize());
  }

  /** The reader's page size, kept in this browser (localStorage) so the panel opens on it next time. */
  @State() pageSize: PageSize = 'all';

  @State() page: number = 0;

  /** A page is changing: the values on screen are fading out; the next page is swapped in once they are out. */
  @State() paging: boolean = false;

  private pagingGeneration = 0;
  private pagingTimer?: ReturnType<typeof setTimeout>;
  /** The size a fading page change is on its way to; a vehicle that lands meanwhile still takes it. */
  private pendingSize?: PageSize;

  private initialPageSize(): PageSize {
    return this.pagination ? this.readPageSize() : 'all';
  }

  /** Reads the stored size. Storage can be missing or throw (private windows, blocked site data); the default stands then. */
  private readPageSize(): PageSize {
    try {
      return parsePageSize(globalThis.localStorage?.getItem(PAGE_SIZE_STORAGE_KEY));
    } catch {
      return DEFAULT_PAGE_SIZE;
    }
  }

  private storePageSize(size: PageSize) {
    try {
      globalThis.localStorage?.setItem(PAGE_SIZE_STORAGE_KEY, String(size));
    } catch {
      // Not kept: the choice still applies for as long as the panel is on the page.
    }
  }

  // A new vehicle lands at the end of a lookup, under the skeleton: its first page is what the cover lifts on.
  @Watch('vehicleLookup')
  onVehicleChange() {
    this.pagingGeneration++;
    clearTimeout(this.pagingTimer);
    if (this.pendingSize !== undefined) this.pageSize = this.pendingSize;
    this.pendingSize = undefined;
    this.paging = false;
    this.page = 0;
  }

  /**
   * Moves to `page` at `size`. Nothing is fetched, so there is no skeleton: when a row's content
   * changes, the values fade out, the page swaps while they are out, and they fade back in; rows
   * the new page adds slide open and rows it drops slide shut. When every row on screen stays (a
   * bigger page from the first one), nothing fades and the new rows simply open below. The page
   * is never scrolled, even when a shorter page lifts the controls (motion.md rule 10).
   */
  private goTo(page: number, size: PageSize) {
    // A drawer belongs to the visit in its slot, and the slot is about to hold another visit.
    this.shutDrawer();

    const rows = listed(this.panelState());
    const target = clampPage(page, rows.length, size);
    const now = pageRows(rows, this.page, this.pageSize);
    const next = pageRows(rows, target, size);

    if (size !== this.pageSize && this.pagination) this.storePageSize(size);

    const overlap = Math.min(now.length, next.length);
    const changes = now.slice(0, overlap).some((row, index) => row !== next[index]);
    const generation = ++this.pagingGeneration;

    const swap = () => {
      if (generation !== this.pagingGeneration) return;
      this.swapCause = 'page';
      this.page = target;
      this.pageSize = size;
      this.pendingSize = undefined;
      this.paging = false;
    };

    clearTimeout(this.pagingTimer);
    if (!changes) {
      swap();
      return;
    }

    this.paging = true;
    this.pendingSize = size;
    this.pagingTimer = setTimeout(swap, this.settleMs());
  }

  private onPage = (page: number) => this.goTo(page, this.pageSize);

  private onPageSize = (size: PageSize) => this.goTo(pageForSize(this.page, this.pageSize, size, listed(this.panelState()).length), size);

  // #endregion

  // #region Row slots: rows stay where they are while their values change

  /** The visits as they were last rendered, and whether the body was open on them. */
  private shownBefore: VehicleServiceHistoryDTO[] = [];
  private bodyWasOpen = false;

  /** Visits the body no longer holds, kept in their slots while those shut; dropped a settle later. */
  private outgoing: VehicleServiceHistoryDTO[] = [];
  private outgoingVeil: RowVeil = 'cover';
  private outgoingTimer?: ReturnType<typeof setTimeout>;

  /** How many of the current rows' slots are open; new slots start shut and are opened a frame later. */
  private opened = 0;
  private openPending = false;

  /** What changed the rows: a lookup (rows leave under the skeleton) or a page (rows leave faded). */
  private swapCause: 'lookup' | 'page' = 'lookup';
  private lastVeil: RowVeil = 'none';

  /**
   * Works out, once per change of the rows on screen, which slots stay, which arrive and which
   * leave. A slot that stays keeps its element: its values change under the cover (a lookup) or
   * while faded (a page). Rows past the new count leave in place, still covered or faded, as their
   * slots shut; rows past the old count arrive shut and are opened on the next frame, so they
   * slide. When the body itself opens or shuts, it carries every row and no slot moves on its own.
   */
  private updateSlots(current: VehicleServiceHistoryDTO[], open: boolean) {
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
        this.opened = Math.min(before.length, current.length);
        this.openPending = this.opened < current.length;
      } else {
        this.opened = current.length;
        this.openPending = false;
      }
    } else if (!open) {
      this.opened = current.length;
    }

    this.shownBefore = current;
    this.bodyWasOpen = open;
  }

  // #endregion

  // #region Height

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

  /** Everything that moves the panel's height, joined; a render that changes none of it announces nothing. */
  private signature(): string {
    const state = this.panelState();

    return [
      this.isLoading || this.leaving,
      this.vehicleLookup?.vin,
      panelBody(state),
      this.retainedBody,
      listed(state).length,
      this.page,
      this.pageSize,
      this.opened,
      this.outgoing.length,
      this.isError,
      this.openKey,
      this.retainedKey,
    ].join('|');
  }

  componentDidRender() {
    this.announceVerdict();

    if (this.openPending) {
      this.openPending = false;
      // The new slots are in the document, shut; laid out once in that state so opening them is a transition, not their first style.
      this.el.shadowRoot?.querySelector('.lt-rows')?.getBoundingClientRect();
      requestAnimationFrame(() => {
        this.opened = Number.MAX_SAFE_INTEGER;
        forceUpdate(this);
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
    clearTimeout(this.pagingTimer);
    clearTimeout(this.outgoingTimer);
    clearTimeout(this.retainedKeyTimer);
    this.heightAnnouncer?.dispose();
  }

  // #endregion

  private panelState(): PanelState {
    return {
      vehicleLoaded: !!this.vehicleLookup?.vin && !this.isError,
      authorized: this.vehicleLookup?.isAuthorized,
      visits: this.vehicleLookup?.serviceHistory,
      error: this.isError ? this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard : undefined,
    };
  }

  render() {
    const state = this.panelState();

    const verdict = panelVerdict(state, this.locale.sharedLocales);
    this.currentVerdict = verdict.accent;

    const busy = this.isLoading || this.leaving;
    this.retainBody(panelBody(state));

    const open = bodyOpen(state, this.leaving);
    const current = panelBody(state) === 'rows' ? pageRows(listed(state), this.page, this.pageSize) : [];
    this.updateSlots(current, open);
    const veil: RowVeil = busy ? 'cover' : this.paging ? 'fade' : 'none';
    this.lastVeil = veil;
    const slots = tableSlots({
      current: current.length,
      outgoing: this.outgoing.length,
      opened: this.opened,
      bodyOpen: open,
      currentVeil: veil,
      outgoingVeil: this.outgoingVeil,
    });

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          verdict={verdict.accent}
          isLoading={busy}
          coreOnly={this.coreOnly}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
        >
          <VehicleServiceHistoryPanel
            locale={this.locale}
            language={this.language}
            state={state}
            verdict={verdict}
            busy={busy}
            leaving={this.leaving}
            retainedBody={this.retainedBody}
            slotRows={[...current, ...this.outgoing]}
            slots={slots}
            openKey={this.openKey}
            retainedKey={this.retainedKey}
            onToggleDrawer={this.toggleDrawer}
            pagination={this.pagination}
            page={this.page}
            pageSize={this.pageSize}
            onPage={this.onPage}
            onPageSize={this.onPageSize}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
