import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { revealBelow } from '~lib/reveal-below';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import type { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

import {
  DEFAULT_HIDDEN_FIELDS,
  PAGE_SIZE_STORAGE_KEY,
  PanelState,
  VehicleServiceHistoryPanel,
  bodyOpen,
  hasRecords,
  historyKey,
  listed,
  panelBody,
  panelPlaceholder,
  sameColumns,
} from './components/VehicleServiceHistoryPanel';

import {
  ClosingDrawer,
  DEFAULT_PAGE_SIZE,
  PageSize,
  PlaceholderKind,
  RowVeil,
  clampPage,
  closingDrawers,
  drawerAfterLookup,
  drawerAfterPage,
  pageForSize,
  pageRows,
  pagingNeedsReveal,
  parseHiddenFields,
  readStoredPageSize,
  storePageSize,
  tableSlots,
} from '~features/lookup-table';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
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
    this.applied = parseHiddenFields(this.hiddenFields);
    this.pageSize = readStoredPageSize(PAGE_SIZE_STORAGE_KEY);
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

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  /** Shuts the rows under the skeleton before a clear or an error lands; false if a newer call took over. */
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

  @State() applied: ReadonlySet<string> = new Set();

  @State() reconfiguring: boolean = false;

  private configGeneration = 0;

  @Watch('hiddenFields')
  async reconfigure() {
    const next = parseHiddenFields(this.hiddenFields);
    const generation = ++this.configGeneration;

    if (sameColumns(next, this.applied)) {
      this.reconfiguring = false;
      return;
    }

    if (this.leaving || panelBody(this.panelState()) !== 'rows') {
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

  // The body and the placeholder keep their last kind for a settle, so they slide shut with their content.
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

  @State() expandedKey: string | null = null;

  @State() closing: ClosingDrawer<VehicleServiceHistoryDTO>[] = [];

  private closingTimer?: ReturnType<typeof setTimeout>;

  /** Single-open: the drawer that gives way is kept in `closing` until it has slid shut. */
  private openDrawer(next: string | null, nextRows: VehicleServiceHistoryDTO[]) {
    this.closing = closingDrawers({ closing: this.closing, previousKey: this.expandedKey, nextKey: next, previousRows: this.shownBefore, nextRows, keyOf: historyKey });
    this.expandedKey = next;

    clearTimeout(this.closingTimer);
    this.closingTimer = undefined;
    if (!this.closing.length) return;
    this.closingTimer = setTimeout(() => {
      this.closingTimer = undefined;
      this.closing = [];
    }, this.settleMs() + 80);
  }

  private onToggle = (key: string) => {
    if (this.isLoading || this.leaving || this.paging) return;
    this.openDrawer(this.expandedKey === key ? null : key, this.shownBefore);
  };

  @State() pageSize: PageSize = DEFAULT_PAGE_SIZE;

  @State() page: number = 0;

  @State() paging: boolean = false;

  private pagingGeneration = 0;
  private pagingTimer?: ReturnType<typeof setTimeout>;
  private stopFollowing?: () => void;
  private revealPending = false;

  @Watch('vehicleLookup')
  onVehicleChange() {
    this.pagingGeneration++;
    clearTimeout(this.pagingTimer);
    this.paging = false;
    this.page = 0;

    const state = this.panelState();
    const first = panelBody(state) === 'rows' ? pageRows(listed(state), 0, this.pageSize) : [];
    this.openDrawer(drawerAfterLookup(this.expandedKey, first, historyKey), first);
  }

  private goTo(page: number, size: PageSize) {
    const rows = listed(this.panelState());
    const target = clampPage(page, rows.length, size);
    const now = pageRows(rows, this.page, this.pageSize);
    const next = pageRows(rows, target, size);

    if (size !== this.pageSize) storePageSize(PAGE_SIZE_STORAGE_KEY, size);
    if (this.expandedKey !== null)
      this.openDrawer(
        drawerAfterPage(
          this.expandedKey,
          next.map(row => historyKey(row)),
        ),
        next,
      );

    const overlap = Math.min(now.length, next.length);
    const changes = now.slice(0, overlap).some((row, index) => row !== next[index]);
    const generation = ++this.pagingGeneration;

    const swap = () => {
      if (generation !== this.pagingGeneration) return;
      this.prepareReveal(next.length);
      this.swapCause = 'page';
      this.page = target;
      this.pageSize = size;
      this.paging = false;
    };

    clearTimeout(this.pagingTimer);
    if (!changes) {
      swap();
      return;
    }

    this.paging = true;
    this.pagingTimer = setTimeout(swap, this.settleMs());
  }

  private onPage = (page: number) => this.goTo(page, this.pageSize);

  private onPageSize = (size: PageSize) => this.goTo(pageForSize(this.page, this.pageSize, size, listed(this.panelState()).length), size);

  /** A shorter page lifts the pager; if that takes it off screen, it is brought back once the rows settle. */
  private prepareReveal(nextCount: number) {
    const root = this.el.shadowRoot;
    const pager = root?.querySelector<HTMLElement>('.lt-pager');
    if (!root || !pager) return;

    const removed = Array.from(root.querySelectorAll<HTMLElement>('.lt-slot[data-open="true"]'))
      .slice(nextCount)
      .reduce((sum, slot) => sum + slot.getBoundingClientRect().height, 0);

    this.revealPending = removed > 0 && pagingNeedsReveal(pager.getBoundingClientRect().top, removed);
  }

  private shownBefore: VehicleServiceHistoryDTO[] = [];
  private bodyWasOpen = false;

  private outgoing: VehicleServiceHistoryDTO[] = [];
  private outgoingVeil: RowVeil = 'cover';
  private outgoingTimer?: ReturnType<typeof setTimeout>;

  private opened = 0;
  private openPending = false;

  private swapCause: 'lookup' | 'page' = 'lookup';
  private lastVeil: RowVeil = 'none';

  /** Rows a page or lookup drops leave where they stood; rows it adds mount shut and open a frame later. */
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

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

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
      this.page,
      this.pageSize,
      this.opened,
      this.outgoing.length,
      this.isError,
      panelPlaceholder(state, this.leaving),
      this.expandedKey,
      this.closing.length,
    ].join('|');
  }

  componentDidRender() {
    this.announceVerdict();

    if (this.openPending) {
      this.openPending = false;
      this.el.shadowRoot?.querySelector('.lt-rows')?.getBoundingClientRect();
      requestAnimationFrame(() => {
        this.opened = Number.MAX_SAFE_INTEGER;
        forceUpdate(this);
      });
    }

    if (this.revealPending) {
      this.revealPending = false;
      const anchor = this.el.shadowRoot?.querySelector('.lt-pager');
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
    clearTimeout(this.placeholderTimer);
    this.retainTimer = undefined;
    this.retainedBody = 'none';
    this.placeholderTimer = undefined;
    this.retainedPlaceholder = null;
    clearTimeout(this.pagingTimer);
    clearTimeout(this.outgoingTimer);
    clearTimeout(this.closingTimer);
    this.closingTimer = undefined;
    this.stopFollowing?.();
    this.stopFollowing = undefined;
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
      history: this.vehicleLookup?.serviceHistory,
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
    this.retainPlaceholder(panelPlaceholder(state, this.leaving));

    const open = bodyOpen(state, this.leaving, this.reconfiguring);
    const current = panelBody(state) === 'rows' ? pageRows(listed(state), this.page, this.pageSize) : [];
    this.updateSlots(current, open);
    const veil: RowVeil = busy ? 'cover' : this.paging ? 'fade' : 'none';
    this.lastVeil = veil;
    const slots = tableSlots({ current: current.length, outgoing: this.outgoing.length, opened: this.opened, bodyOpen: open, currentVeil: veil, outgoingVeil: this.outgoingVeil });

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
            state={state}
            verdict={verdict}
            busy={busy}
            leaving={this.leaving}
            reconfiguring={this.reconfiguring}
            retainedBody={this.retainedBody}
            retainedPlaceholder={this.retainedPlaceholder}
            slotRows={[...current, ...this.outgoing]}
            slots={slots}
            expandedKey={this.expandedKey}
            closing={this.closing}
            onToggle={this.onToggle}
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
