import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

import { PanelState, VehicleServiceHistoryPanel, bodyOpen, listed, panelBody, panelVerdict } from './components/VehicleServiceHistoryPanel';

import { tableSlots } from '~features/lookup-table';
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

  /** The visits as they were last rendered, and whether the body was open on them. */
  private shownBefore: VehicleServiceHistoryDTO[] = [];
  private bodyWasOpen = false;

  /** Visits the body no longer holds, kept in their slots while those shut; dropped a settle later. */
  private outgoing: VehicleServiceHistoryDTO[] = [];
  private outgoingTimer?: ReturnType<typeof setTimeout>;

  /** How many of the current rows' slots are open; new slots start shut and are opened a frame later. */
  private opened = 0;
  private openPending = false;

  /**
   * Works out, once per change of the rows on screen, which slots stay, which arrive and which
   * leave. A slot that stays keeps its element and its values change under the cover. Rows past the
   * new count leave in place, still covered, as their slots shut; rows past the old count arrive
   * shut and are opened on the next frame, so they slide. When the body itself opens or shuts, it
   * carries every row and no slot moves on its own.
   */
  private updateSlots(current: VehicleServiceHistoryDTO[], open: boolean) {
    const before = this.shownBefore;
    const changed = current.length !== before.length || current.some((row, index) => row !== before[index]);

    if (changed) {
      if (this.bodyWasOpen && before.length > current.length) {
        this.outgoing = before.slice(current.length);
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
      this.opened,
      this.outgoing.length,
      this.isError,
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
    clearTimeout(this.outgoingTimer);
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
    const current = panelBody(state) === 'rows' ? listed(state) : [];
    this.updateSlots(current, open);
    const slots = tableSlots({
      current: current.length,
      outgoing: this.outgoing.length,
      opened: this.opened,
      bodyOpen: open,
      currentVeil: busy ? 'cover' : 'none',
      outgoingVeil: 'cover',
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
          />
        </VehicleInfoLayout>
      </Host>
    );
  }
}
