import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { revealBelow } from '~lib/reveal-below';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';

import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';

import {
  DEFAULT_HIDDEN_FIELDS,
  PAGE_SIZE_STORAGE_KEY,
  PanelNotice,
  PanelState,
  VehicleAccessoriesPanel,
  bodyOpen,
  hasRecords,
  leadNotice,
  listed,
  panelBody,
  panelPlaceholder,
  sameColumns,
} from './components/VehicleAccessoriesPanel';

import {
  DEFAULT_PAGE_SIZE,
  PageSize,
  PlaceholderKind,
  RowVeil,
  clampPage,
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
  tag: 'vehicle-accessories',
  styleUrl: 'vehicle-accessories.css',
})
export class VehicleAccessories implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof accessoriesSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...accessoriesSchema.getDefault() };

  async componentWillLoad() {
    this.applied = parseHiddenFields(this.hiddenFields);
    this.pageSize = readStoredPageSize(PAGE_SIZE_STORAGE_KEY);
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

  // #region Image viewer (<shift-image>, portaled onto <body>)

  /** The picture open in the viewer, or empty. */
  @State() expandedImage?: string = '';

  private imageViewer?: HTMLShiftImageElement;

  /** Opens the viewer on `src`, growing out of the thumbnail `origin`. The same picture again while open is ignored. */
  private openImage(origin: HTMLImageElement, src: string, label?: string, caption?: string) {
    if (this.expandedImage === src) return;
    this.expandedImage = src;
    void this.imageViewer?.show(src, { origin, label, caption });
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

  /**
   * Comma-separated column names to leave out: `partNumber`, `description`, `image`. A shown column
   * with no value reads as a dash; a hidden one has no heading, no cell and no caption. By default
   * every column is shown.
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

    // Nothing is open to shut (idle, leaving, a notice, an empty list): swap at once. Rows in flight stay
    // on screen under the skeleton, so they reshape like settled ones.
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

  private retainedPlaceholder: PlaceholderKind | null = null;
  private placeholderTimer?: ReturnType<typeof setTimeout>;

  /**
   * Remembers the placeholder's kind and forgets it a settle after the region shut (a list opened over it),
   * so its words slide away with their region (motion.md rule 5) and no stale sentence is under a region that
   * opens next. While a lookup is in flight the state is the last answer's, so the kind does not change and
   * the placeholder stays where it stands under the skeleton.
   */
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

  // #endregion

  // #region Pages

  /** The reader's page size, kept in this browser (localStorage) so the panel opens on it next time. */
  @State() pageSize: PageSize = DEFAULT_PAGE_SIZE;

  @State() page: number = 0;

  /** A page is changing: the values on screen are fading out; the next page is swapped in once they are out. */
  @State() paging: boolean = false;

  private pagingGeneration = 0;
  private pagingTimer?: ReturnType<typeof setTimeout>;
  private stopFollowing?: () => void;
  private revealPending = false;

  // A new vehicle lands at the end of a lookup, under the skeleton: its first page is what the cover lifts on.
  @Watch('vehicleLookup')
  onVehicleChange() {
    this.pagingGeneration++;
    clearTimeout(this.pagingTimer);
    this.paging = false;
    this.page = 0;
  }

  /**
   * Moves to `page` at `size`. Nothing is fetched, so there is no skeleton: when a row's content
   * changes, the values fade out, the page swaps while they are out, and they fade back in; rows
   * the new page adds slide open and rows it drops slide shut. When every row on screen stays (a
   * bigger page from the first one), nothing fades and the new rows simply open below.
   */
  private goTo(page: number, size: PageSize) {
    const rows = listed(this.panelState());
    const target = clampPage(page, rows.length, size);
    const now = pageRows(rows, this.page, this.pageSize);
    const next = pageRows(rows, target, size);

    if (size !== this.pageSize) storePageSize(PAGE_SIZE_STORAGE_KEY, size);

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

  /** Before a page that is shorter than the one on screen: whether the controls will rise off the top of the screen. */
  private prepareReveal(nextCount: number) {
    const root = this.el.shadowRoot;
    const pager = root?.querySelector<HTMLElement>('.lt-pager');
    if (!root || !pager) return;

    const removed = Array.from(root.querySelectorAll<HTMLElement>('.lt-slot[data-open="true"]'))
      .slice(nextCount)
      .reduce((sum, slot) => sum + slot.getBoundingClientRect().height, 0);

    this.revealPending = removed > 0 && pagingNeedsReveal(pager.getBoundingClientRect().top, removed);
  }

  // #endregion

  // #region Row slots: rows stay where they are while their values change

  /** The page's rows as they were last rendered, and whether the body was open on them. */
  private shownBefore: AccessoryDTO[] = [];
  private bodyWasOpen = false;

  /** Rows the body no longer holds, kept in their slots while those shut; dropped a settle later. */
  private outgoing: AccessoryDTO[] = [];
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
   * slots shut; rows past the old count arrive shut and are opened on the next frame, so they slide.
   * When the body itself opens or shuts, it carries every row and no slot moves on its own.
   */
  private updateSlots(current: AccessoryDTO[], open: boolean) {
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
      [...this.applied].sort().join(','),
      this.reconfiguring,
      this.page,
      this.pageSize,
      this.opened,
      this.outgoing.length,
      this.isError,
      panelPlaceholder(state, this.leaving),
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
    this.stopFollowing?.();
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
    this.retainPlaceholder(panelPlaceholder(state, this.leaving));

    const open = bodyOpen(state, this.leaving, this.reconfiguring);
    const current = panelBody(state) === 'rows' ? pageRows(listed(state), this.page, this.pageSize) : [];
    this.updateSlots(current, open);
    const veil: RowVeil = busy ? 'cover' : this.paging ? 'fade' : 'none';
    this.lastVeil = veil;
    const slots = tableSlots({ current: current.length, outgoing: this.outgoing.length, opened: this.opened, bodyOpen: open, currentVeil: veil, outgoingVeil: this.outgoingVeil });

    // Keeps the last sentence the strip said, so a notice that is fading out still has its words.
    this.retainedNotice = leadNotice(state, this.locale) ?? this.retainedNotice;

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
          <VehicleAccessoriesPanel
            locale={this.locale}
            language={this.language}
            state={state}
            verdict={verdict}
            busy={busy}
            leaving={this.leaving}
            reconfiguring={this.reconfiguring}
            retainedBody={this.retainedBody}
            retainedNotice={this.retainedNotice}
            retainedPlaceholder={this.retainedPlaceholder}
            slotRows={[...current, ...this.outgoing]}
            slots={slots}
            page={this.page}
            pageSize={this.pageSize}
            onPage={this.onPage}
            onPageSize={this.onPageSize}
            onOpenMedia={(image, src) => {
              const row = image.closest('.lt-row');
              const identity = row?.querySelector('[data-identity="true"]')?.textContent?.trim();
              const description = row?.querySelector('[data-grow="true"]')?.textContent?.trim();
              // Under the picture: what it is, "C35753 · Rubber mats", from the row it was opened from.
              const caption = [identity, description].filter(text => text && text !== '—').join(' · ');
              this.openImage(image, src, identity ? `${this.locale.openImage} ${identity}` : this.locale.openImage, caption);
            }}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
