import { Component, Element, Event, EventEmitter, Host, Listen, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { revealBelow } from '~lib/reveal-below';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import {
  CertificateSnapshot,
  DEFAULT_HIDDEN_FIELDS,
  PaintRow,
  PaintTab,
  PanelState,
  PhotoGroup,
  ReadingSummary,
  VehiclePaintThicknessPanel,
  allPhotoGroups,
  bodyOpen,
  certificateKind,
  hasRecords,
  panelBody,
  photosSectionOpen,
  reshapesTable,
  rowHasPhotos,
  sameConfig,
  shownRows,
  summaryOf,
  tabsOf,
} from './components/VehiclePaintThicknessPanel';

import { RowVeil, parseHiddenFields, retainDrawers, settleDrawer, shownDrawers, tableSlots, toggleDrawer } from '~features/lookup-table';
import { VehicleInfoLayout, VehicleInfoLayoutInterface, VerdictState, recordVerdict } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent, VehicleLookupMock } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

const DEFAULT_SETTLE_MS = 480;

/**
 * Content that must outlive its own state by one settle: when the state that drew it ends, it is kept
 * rendered while its region shuts and dropped afterwards, so it slides away instead of blinking out
 * (motion.md rule 5; the owner's `retainedBody` is the same idea). The owner calls `track` every
 * render with what the state holds now, or null when it holds nothing.
 */
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
   * Comma-separated parts of the panel to leave out: `inspectionDate` (the date under each tab's
   * source), `source` (the source name on each tab), `photos` (the photos column, the per-row photo
   * drawers and the all-photos section) and `certificate` (the certificate control). By default
   * every part is shown. A change is live: what it changes fades, is swapped while it is out and
   * fades back; nothing is fetched.
   */
  @Prop() hiddenFields: string = DEFAULT_HIDDEN_FIELDS;

  /**
   * Adds the reading summary to the head: how many readings the selected inspection holds, the lowest
   * and the highest. Facts, with no threshold asserted. Off by default.
   */
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

    // The certificate is under the skeleton for the lookup: an open menu is not the new vehicle's.
    if (newValue) this.certificateMenuOpen = false;
  }

  // #endregion

  // #region Leaving a state

  /** The panel is on its way to empty or to an error: the head leaves and the body is shutting over its rows. */
  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  private settleCache?: number;

  /**
   * How long a state change takes to settle: the --settle token plus a margin for the frame it starts on.
   * Read once and kept: the token is a design constant, and reading a computed style while the render has
   * just dirtied the tree makes the browser restyle the whole panel inside the script (about 50 ms at 4x
   * CPU, once at the start of a lookup and once at its answer). `componentDidLoad` warms the cache off the frame.
   */
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
    // After the first frame's style is clean, so the one read costs nothing and no later render has to make it.
    if (typeof requestAnimationFrame === 'function') this.loadFrame = requestAnimationFrame(() => (this.loadTimer = setTimeout(() => this.settleMs())));
  }

  /**
   * Shuts the body over whatever it holds and resolves once it has settled, so the caller can swap
   * the content while nothing is on screen. True when the caller may go on; false when a newer call
   * took over meanwhile. Resolves at once when there is nothing to leave. An open drawer or the
   * open all-photos section shuts at once: a clear is not a lookup (owner, 2026-10-27).
   */
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

  // #endregion

  // #region Configuration (hiddenFields, showReadingSummary)

  /** The hidden set and the summary flag the render uses. They trail the props by a settle: what changes fades, these swap, it fades back. */
  @State() applied: ReadonlySet<string> = new Set();
  @State() appliedSummary: boolean = false;

  /** A live configuration change is in progress: the tabs' labels, the certificate and the summary are faded out. */
  @State() reconfiguring: boolean = false;

  /** That change reshapes the table (photos): the body is shut and the headings fade while the columns swap. */
  @State() reshaping: boolean = false;

  private configGeneration = 0;

  // Not a lookup: nothing is fetched, so there is no skeleton and no spinner.
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
      // The photos going away takes every drawer and the all-photos section with it.
      if (next.has('photos')) {
        this.retainedDrawers = [];
        this.openDrawer = null;
        this.photosOpen = false;
      }

      this.applied = next;
      this.appliedSummary = this.showReadingSummary;
      this.reconfiguring = false;
      this.reshaping = false;
    };

    // Nothing on screen to change (idle, leaving, nothing on file): swap at once.
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

  // #region Tabs: which inspection is on screen

  /** The tab the reader pressed: `aria-selected`, the underline. */
  @State() activeTabIndex: number = 0;

  /** The tab whose readings are in the table. It follows `activeTabIndex` once the values have faded out. */
  @State() shownTabIndex: number = 0;

  /** An inspection is being switched: nothing is fetched, so the values fade out, the swap happens while they are out, and they fade back. */
  @State() swapping: boolean = false;

  private swapGeneration = 0;
  private swapTimer?: ReturnType<typeof setTimeout>;

  // A new vehicle lands at the end of a lookup, under the skeleton: its first inspection is what the cover lifts on.
  @Watch('vehicleLookup')
  onVehicleLookupChange() {
    this.swapGeneration++;
    clearTimeout(this.swapTimer);
    this.swapping = false;
    this.activeTabIndex = 0;
    this.shownTabIndex = 0;
    this.certificateMenuOpen = false;
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
      // The values are out: swap the readings, and let a drawer that has nothing to hold on the new inspection shut.
      this.swapCause = 'page';
      this.shownTabIndex = index;
      this.settleSelection(false);
      this.swapping = false;
    }, this.settleMs());
  };

  // #endregion

  // #region Photos: one drawer open at a time, and the all-photos section

  /** The slot whose drawer is open, or null. Single open. It survives a lookup: owner, 2026-10-27. */
  @State() openDrawer: number | null = null;

  /** Drawers that have just shut and are still rendered while they slide away. */
  @State() retainedDrawers: number[] = [];

  /** The all-photos section is expanded. It survives a lookup, and shuts only if the new answer has no photo. */
  @State() photosOpen: boolean = false;

  private drawerTimer?: ReturnType<typeof setTimeout>;
  /** The rows as they were last rendered, taken when a drawer shuts because its slot lost its photos: what it slides away on. */
  private drawerSnapshot: PaintRow[] = [];
  private photosGroups = new Retained<PhotoGroup[]>(
    () => this.settleMs(),
    () => forceUpdate(this),
  );
  private photosRendering = new Retained<true>(
    () => this.settleMs(),
    () => forceUpdate(this),
  );
  private revealTarget: 'drawer' | 'photos' | null = null;

  /** Drops the retained drawers a settle after the last change. */
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
    if (next !== null) this.revealTarget = 'drawer';
  };

  private onTogglePhotos = () => {
    this.photosOpen = !this.photosOpen;
    if (this.photosOpen) this.revealTarget = 'photos';
  };

  /**
   * Runs when the shown readings have changed under cover (an answer, an inspection switched) or the
   * panel is cleared: a drawer stays open if its slot still has photos and shuts if not, the
   * all-photos section stays open if the new readings have any photo. Never while a lookup is in
   * flight: the open drawer goes straight into loading, under the skeleton.
   */
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

    if (this.photosOpen && (cleared || allPhotoGroups(rows, this.locale).length === 0)) this.photosOpen = false;
  }

  // #endregion

  // #region Certificate menu

  /** Host-controlled visibility (e.g. a feature gate). The links themselves come from the lookup
   *  response (`paintThicknessCertificateUrls`, one signed URL per print language with its menu
   *  label) — the server only issues them to callers allowed to print, so no endpoint, auth
   *  headers, fetch or language knowledge live on the client. */
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

  // Shadow DOM retargets inner clicks to the host element, so any click whose composed
  // path includes the host happened inside the component; everything else closes the menu.
  @Listen('click', { target: 'document' })
  onDocumentClick(event: MouseEvent) {
    if (this.certificateMenuOpen && !event.composedPath().includes(this.el)) this.certificateMenuOpen = false;
  }

  // Escape closes the menu wherever focus is; focus goes back to the button only when it was in this panel.
  @Listen('keydown', { target: 'document' })
  onDocumentKeyDown(event: KeyboardEvent) {
    if (event.key !== 'Escape' || !this.certificateMenuOpen) return;
    this.onCloseCertificate(event.composedPath().includes(this.el));
  }

  /** Set when the button opens the menu from the keyboard or a press: the first item takes focus once it is rendered focusable. */
  private focusMenuOnRender = false;
  private focusOutTimer?: ReturnType<typeof setTimeout>;

  private menuItems = () => Array.from(this.el.shadowRoot?.querySelectorAll<HTMLElement>('.paint-certificate-item') ?? []);

  private onToggleCertificate = () => {
    this.certificateMenuOpen = !this.certificateMenuOpen;
    this.focusMenuOnRender = this.certificateMenuOpen;
  };

  // Menu keyboard: Down / Up / Home / End move between the language items (wrapping), Down on the button opens the menu.
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
    items[next]?.focus();
  }

  // Focus leaving the button and the menu (Tab away) closes the menu. Focus moving between the two stays inside.
  @Listen('focusout')
  onMenuFocusOut(event: FocusEvent) {
    if (!this.certificateMenuOpen) return;
    const to = event.relatedTarget as Node | null;
    if (!to) return; // a pointer press elsewhere is the document click handler's
    if (to !== this.el) {
      this.certificateMenuOpen = false;
      return;
    }
    clearTimeout(this.focusOutTimer);
    this.focusOutTimer = setTimeout(() => {
      const active = this.el.shadowRoot?.activeElement;
      if (this.certificateMenuOpen && active && !active.closest('.paint-certificate-button, .paint-certificate-menu')) this.certificateMenuOpen = false;
    });
  }

  private onCloseCertificate = (restoreFocus: boolean) => {
    this.certificateMenuOpen = false;
    if (restoreFocus) this.el.shadowRoot?.querySelector<HTMLElement>('.paint-certificate-button')?.focus();
  };

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

  // #region Row slots: rows stay where they are while their values change

  /** The shown inspection's rows as they were last rendered, and whether the body was open on them. */
  private shownBefore: PaintRow[] = [];
  private bodyWasOpen = false;

  /** Rows the body no longer holds, kept in their slots while those shut; dropped a settle later. */
  private outgoing: PaintRow[] = [];
  private outgoingVeil: RowVeil = 'cover';
  private outgoingTimer?: ReturnType<typeof setTimeout>;

  /** How many of the current rows' slots are open; new slots start shut and are opened a frame later. */
  private opened = 0;
  private openPending = false;

  /** What changed the rows: a lookup (rows leave under the skeleton) or an inspection switched (rows leave faded). */
  private swapCause: 'lookup' | 'page' = 'lookup';
  private lastVeil: RowVeil = 'none';

  /**
   * Works out, once per change of the rows on screen, which slots stay, which arrive and which
   * leave (design language § 21a). A slot that stays keeps its element: its values change under the
   * cover (a lookup) or while faded (an inspection switched). Rows past the new count leave in
   * place, still covered or faded, as their slots shut; rows past the old count arrive shut and are
   * opened on the next frame, so they slide. When the body itself opens or shuts, it carries every
   * row and no slot moves on its own.
   */
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

  // #endregion

  // #region Height

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;
  private stopFollowing?: () => void;

  /** Everything that moves the panel's height, joined; a render that changes none of it announces nothing. */
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
      this.photosOpen,
      this.opened,
      this.outgoing.length,
      this.isError,
    ].join('|');
  }

  private openFrame?: number;

  componentDidRender() {
    this.announceVerdict();

    if (this.focusMenuOnRender) {
      this.focusMenuOnRender = false;
      if (this.certificateMenuOpen) this.menuItems()[0]?.focus();
    }

    if (this.openPending) {
      this.openPending = false;
      this.cancelOpenFrame();
      // The new slots are in the document, shut. Opening them a whole frame later (the second callback runs after
      // the frame that styled them shut) makes it a transition, not their first style, without a layout read
      // inside this render, which forced the whole table to lay out in the script (about 40 ms at 4x CPU).
      this.openFrame = requestAnimationFrame(() => {
        this.openFrame = requestAnimationFrame(() => {
          this.openFrame = undefined;
          this.opened = Number.MAX_SAFE_INTEGER;
          forceUpdate(this);
        });
      });
    }

    // What opens below the fold brings its anchor up and the page follows it (motion.md rule 10).
    if (this.revealTarget) {
      const anchor = this.el.shadowRoot?.querySelector(this.revealTarget === 'drawer' ? '.lt-row[data-open="true"]' : '.paint-photos-toggle');
      this.revealTarget = null;
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
    clearTimeout(this.swapTimer);
    clearTimeout(this.drawerTimer);
    clearTimeout(this.outgoingTimer);
    clearTimeout(this.focusOutTimer);
    clearTimeout(this.loadTimer);
    this.cancelOpenFrame();
    if (this.loadFrame !== undefined) cancelAnimationFrame(this.loadFrame);
    this.photosGroups.dispose();
    this.photosRendering.dispose();
    this.certificate.dispose();
    this.summary.dispose();
    this.tabs.dispose();
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
      inspections: this.vehicleLookup?.paintThicknessInspections,
      shownIndex: this.shownTabIndex,
      hidden: this.applied,
      showSummary: this.appliedSummary,
      certificateAvailable: this.vehicleLookup?.paintThicknessCertificateAvailable,
      certificateUrls: this.vehicleLookup?.paintThicknessCertificateUrls,
      showCertificateButton: this.showCertificateButton,
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
    const body = panelBody(state);
    this.retainBody(body);

    const open = bodyOpen(state, this.leaving, this.reshaping);
    const current = body === 'rows' ? shownRows(state) : [];
    this.updateSlots(current, open);

    const veil: RowVeil = busy ? 'cover' : this.swapping ? 'fade' : 'none';
    this.lastVeil = veil;
    const slots = tableSlots({ current: current.length, outgoing: this.outgoing.length, opened: this.opened, bodyOpen: open, currentVeil: veil, outgoingVeil: this.outgoingVeil });

    // What the strips would say to hold on to once their state ends.
    const tabs = tabsOf(state, this.locale);
    this.tabs.track(tabs.length ? tabs : null);
    const kind = certificateKind(state);
    this.certificate.track(kind === 'hidden' ? null : { kind, urls: state.certificateUrls ?? [] });
    this.summary.track(state.showSummary ? summaryOf(current) : null);
    const groups = allPhotoGroups(current, this.locale);
    this.photosGroups.track(groups.length ? groups : null);
    const photosBodyOpen = this.photosOpen && photosSectionOpen(state, groups.length, this.leaving, this.reshaping);
    this.photosRendering.track(photosBodyOpen ? true : null);

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
            selectedTab={this.activeTabIndex}
            retainedTabs={this.tabs.value ?? []}
            retainedCertificate={this.certificate.value ?? null}
            retainedSummary={this.summary.value ?? null}
            slotRows={[...current, ...this.outgoing]}
            slots={slots}
            drawerOpen={this.openDrawer}
            drawerShown={shownDrawers(this.openDrawer, this.retainedDrawers)}
            // A slot that lost its photos to the new answer slides away on the photos it last showed.
            drawerRowFor={(row, index) => (rowHasPhotos(row) || !this.retainedDrawers.includes(index) ? row : (this.drawerSnapshot[index] ?? row))}
            onToggleDrawer={this.onToggleDrawer}
            photosOpen={this.photosOpen}
            photosRendered={photosBodyOpen || this.photosRendering.value === true}
            retainedGroups={this.photosGroups.value ?? []}
            onTogglePhotos={this.onTogglePhotos}
            onSelectTab={this.onSelectTab}
            certificateOpen={this.certificateMenuOpen}
            onToggleCertificate={this.onToggleCertificate}
            onCloseCertificate={this.onCloseCertificate}
            onOpenPhoto={(image, src, label) => this.openImage(image, src, label)}
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
