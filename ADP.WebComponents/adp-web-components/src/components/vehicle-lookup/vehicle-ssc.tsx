import { Component, Element, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import cn from '~lib/cn';
import { Grecaptcha } from '~lib/recaptcha';
import { revealBelow } from '~lib/reveal-below';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { SscRepairTraceDTO } from '~types/generated/vehicle-lookup/ssc-repair-trace-dto';

import sscSchema from '~locales/vehicleLookup/ssc/type';

import { BodyKind, ManufacturerCheckStatus, PanelState, SscCampaigns, openDrawerKey, panelBody, sscTraceKey } from './components/SscCampaigns';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import {
  RequestHeadersProvider,
  VehicleLookupComponent,
  VehicleLookupMock,
  resolveRequestHeaders,
  setVehicleLookupData,
  setVehicleLookupErrorState,
} from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

declare const grecaptcha: Grecaptcha;

const RECAPTCHA_SCRIPT_SRC = 'https://www.google.com/recaptcha/api.js?render=explicit';

/** The --settle token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 320;

const isAbort = (error: unknown) => (error as DOMException)?.name === 'AbortError';

/**
 * The Special Service Campaigns (safety recalls) affecting a vehicle, and nothing else: warranty
 * coverage lives in <vehicle-warranty-timeline>. Replaces the campaign half of the retired
 * <vehicle-warranty-details>.
 *
 * A vehicle is on exactly one of two paths, and the panel must never mix them (the rule is spelled
 * out on `panelVerdict` in ./components/SscCampaigns.tsx and in
 * .shift/repos/adp/web-components/vehicle-lookup-invariants.md):
 *
 *  - authorized (in the distributor's records): the campaign list is the verdict;
 *  - unauthorized: the distributor has no records, so the only verdict is the manufacturer's
 *    answer to the reCAPTCHA-gated check — a yes/no, no list. Until it arrives the panel says the
 *    vehicle is not in the records and asserts nothing else.
 *
 * A third state is neither: a wrapper that only counts the SSC tab's own request as a campaign
 * check (`sscQueryString`) tells this panel through `skipLookup` that a vehicle was looked up
 * without it. The panel then says the check was not run and offers to run it, rather than sitting
 * blank next to a VIN as if the vehicle had no campaigns.
 *
 * With `show-trace`, every campaign row can open the evidence behind its repair status: the labor
 * codes accepted for the campaign, every warranty claim on the vehicle with how it was judged, and
 * the service-history lines that matched. The trace is fetched on first use with `?trace=ssc`, so
 * the ordinary lookup response stays small and the host can gate the detail behind a permission.
 *
 * Every state change is a movement (lookup-motion.css): the outgoing content stays rendered while
 * the body shuts over it, and the incoming content is swapped in only while the body is shut. The
 * shared lookup helper waits a second before it asks the server, which is what guarantees the body
 * has settled shut before a response can land.
 */
@Component({
  shadow: true,
  tag: 'vehicle-ssc',
  styleUrl: 'vehicle-ssc.css',
})
export class VehicleSsc implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization

  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof sscSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...sscSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.ssc', sscSchema)]);
    this.locale = { sharedLocales, ...locale };
  }

  // #endregion

  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion

  // #region Vehicle Lookup Component Shared Logic

  @Prop() isDev: boolean;
  @Prop() disableVinValidation: boolean = false;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';
  /**
   * Appended to the campaign lookup request only, never to the trace request. A wrapper passes its
   * `sscQueryString` — typically a lookup-logging flag — here rather than into `queryString`, so
   * opening a campaign's evidence (a re-read of a lookup that was already logged) is never counted
   * as another lookup. The host's endpoint must refuse to log traced requests as well.
   */
  @Prop() lookupQueryString: string = '';

  /** Asked for the current headers before every request this component makes itself; lets a host refresh its token on demand. */
  @Prop() requestHeadersProvider?: RequestHeadersProvider;
  /** Name of a [JSInvokable] method on the Blazor reference that answers with the current headers. */
  @Prop() blazorRequestHeadersProvider: string = '';

  lastRequestHeaders?: object;

  @Prop() errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop() loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop() loadedResponse?: BlazorInvokableFunction<(response: VehicleLookupDTO) => void>;
  @Prop() unauthorizedSscLookupResponse?: BlazorInvokableFunction<(sscLookupStatus: number) => void>;

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
    this.leaveGeneration++;
    this.skippedVin = undefined;
    this.resetTraces();
    // The check's machinery stops now; its widget stays on screen so it leaves with the body.
    this.suspendManufacturerCheck();

    await setVehicleLookupData(this, newData, headers, {
      beforeAssignment: async (response, { scopedTimeoutRef }) => {
        // By now the body has settled shut (the helper's one-second wait outlasts --settle), so
        // the previous vehicle's check can go and the next one's be prepared unseen.
        this.resetManufacturerCheck();
        this.prepareManufacturerCheck(response, scopedTimeoutRef);
        return response;
      },
    });
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  /** Back to "no vehicle": the body shuts over what it holds, then the panel is emptied unseen. */
  @Method()
  async clearData() {
    await this.leave();
    this.emptyPanel();
  }

  /**
   * The other panels were given a vehicle but this one's own lookup was not run for it — a wrapper
   * with `sscQueryString` only counts the SSC tab's own request as a campaign check. The panel says
   * so and offers to run the check itself; the VIN is kept for that.
   */
  @Method()
  async skipLookup(vin: string) {
    if (!this.vehicleLookup && this.skippedVin === vin) return;

    await this.leave();
    this.emptyPanel();
    this.skippedVin = vin;
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

  // #endregion

  // #region Leaving a state

  /** A vehicle was looked up without this panel; set by `skipLookup`, cleared by the next lookup. */
  @State() skippedVin?: string;

  /** The panel is on its way to empty: the sheen is on and the body is shutting over its content. */
  @State() leaving: boolean = false;

  private leaveGeneration = 0;

  /**
   * The body's last non-empty content, kept while the body is shut so the content slides away
   * with it instead of vanishing the frame the state changes. Dropped once the body has settled.
   */
  private retainedBody: BodyKind = 'none';
  private retainTimer?: ReturnType<typeof setTimeout>;

  /** How long a state change takes to settle: the --settle token plus a margin for the frame it starts on. */
  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  /**
   * Puts the chip and lead on the sheen, shuts the body over whatever it holds, and resolves once
   * they have settled — so the caller can swap the content while nothing is on screen. Resolves at
   * once when there is nothing to leave. A lookup started meanwhile takes over.
   */
  private async leave() {
    const generation = ++this.leaveGeneration;

    if (!this.vehicleLookup && !this.skippedVin && !this.isError) return;

    clearTimeout(this.networkTimeoutRef);
    this.abortController?.abort();
    this.suspendManufacturerCheck();
    this.leaving = true;

    await new Promise(resolve => setTimeout(resolve, this.settleMs()));

    if (generation === this.leaveGeneration) this.leaving = false;
  }

  private emptyPanel() {
    this.vehicleLookup = undefined;
    this.isError = false;
    this.errorMessage = undefined;
    this.isLoading = false;
    this.skippedVin = undefined;
    this.resetManufacturerCheck();
    this.resetTraces();
  }

  /**
   * Height changes here are CSS transitions inside this shadow root, which an enclosing
   * <flexible-container> (the wrapper's tab strip) cannot see; it would keep clipping at the old
   * height. So every render that moved something announces the change to those containers, the way
   * a nested <flexible-container> does, and they stop clipping until it has settled.
   */
  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private layoutSignature?: string;

  componentDidRender() {
    this.followOpenDrawer();

    const state = this.panelState();
    const body = panelBody(state);
    const signature = [
      this.isLoading || this.leaving,
      body,
      this.retainedBody,
      this.openTraceKey,
      this.traceLoading,
      this.traceError,
      this.checkingUnauthorizedSSC,
      this.showRecaptcha,
      this.vehicleLookup?.vin,
      this.skippedVin,
      this.recaptchaRes?.status,
    ].join('|');

    const changed = this.layoutSignature !== undefined && this.layoutSignature !== signature;
    this.layoutSignature = signature;

    if (!changed) return;

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  private runSkippedLookup = () => {
    const vin = this.skippedVin;
    if (!vin) return;
    void this.fetchVin(vin);
  };

  /** Remembers what the body shows, and forgets it a settle after the body shut. */
  private retainBody(body: BodyKind) {
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

  // #region Blazor Invokable logic

  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }

  // #endregion

  // #region Component props

  /** Shows a per-campaign "why this status?" control. The host gates it on its own permission check; the trace lists claim and invoice details. */
  @Prop() showTrace: boolean = false;
  @Prop() recaptchaKey: string = '';
  /** Renders a click-to-pass stand-in for the reCAPTCHA widget. Implied by `isDev`; the real widget is only ever used in production mode. */
  @Prop() mockRecaptcha: boolean = false;
  @Prop() unauthorizedSscLookupBaseUrl: string = '';
  @Prop() unauthorizedSscLookupQueryString: string = '';

  @Prop() cityId?: string = null;
  @Prop() cityIntegrationId?: string = null;
  @Prop() companyId?: string = null;
  @Prop() companyIntegrationId?: string = null;
  @Prop() companyBranchId?: string = null;
  @Prop() companyBranchIntegrationId?: string = null;
  @Prop() userId?: string = null;
  @Prop() brandIntegrationId: string = null;

  @Prop() customerName?: string = null;
  @Prop() customerPhone?: string = null;
  @Prop() customerEmail?: string = null;

  // #endregion

  // #region Manufacturer check (unauthorized vehicles)

  /** The check is offered: the vehicle is unauthorized and the host configured a site key. */
  @State() showRecaptcha: boolean = false;
  @State() checkingUnauthorizedSSC: boolean = false;
  @State() devRecaptchaChecked: boolean = false;
  @State() recaptchaRes: { status: ManufacturerCheckStatus | null } | null = null;

  private recaptchaIntervalRef: ReturnType<typeof setInterval>;
  private recaptchaHideTimer?: ReturnType<typeof setTimeout>;
  private recaptchaWidgetId: number | undefined;
  private recaptchaPortalEl?: HTMLDivElement;
  private recaptchaPlaceholderRef: HTMLDivElement;
  private recaptchaReady?: Promise<void>;
  private positionRAF: number;

  /**
   * Development never shows Google's widget. The stand-in is the default whenever `isDev` is on, and
   * the real widget is created lazily, only when a production-mode lookup actually needs it — so a
   * host that flips `isDev` after the element mounted (the dev showcase does) still gets the mock,
   * instead of a real widget rendered at mount time sitting on top of a mock that never polls it.
   */
  private get useMockRecaptchaWidget(): boolean {
    return this.isDev || this.mockRecaptcha;
  }

  private mockRecaptchaTrigger?: () => Promise<void>;

  @Watch('isDev')
  @Watch('mockRecaptcha')
  onRecaptchaModeChange() {
    this.syncRealRecaptchaVisibility();
  }

  private handleDevRecaptchaClick = async () => {
    if (this.devRecaptchaChecked) return;
    if (!this.mockRecaptchaTrigger) return;
    this.devRecaptchaChecked = true;

    try {
      await this.mockRecaptchaTrigger();
    } catch (error) {
      if (!isAbort(error)) console.error('SSC manufacturer check failed', error);
    }
  };

  /** Stops the check from answering — polling, the stand-in's trigger, a request in flight — without touching what is on screen. */
  private suspendManufacturerCheck() {
    clearInterval(this.recaptchaIntervalRef);
    this.mockRecaptchaTrigger = undefined;
  }

  /** Back to "nothing asked yet" — every new lookup, once the previous check has left the screen, and a cleared panel start here. */
  private resetManufacturerCheck() {
    this.suspendManufacturerCheck();
    clearTimeout(this.recaptchaHideTimer);
    this.recaptchaRes = null;
    this.showRecaptcha = false;
    this.checkingUnauthorizedSSC = false;
    this.devRecaptchaChecked = false;
    this.hideRealRecaptcha();
  }

  /**
   * Offers the check for an unauthorized vehicle. Not awaited by the lookup: loading Google's script
   * must not delay the vehicle landing on screen, so the real widget is armed in the background and
   * checked against the lookup generation before it starts polling.
   */
  private prepareManufacturerCheck(newVehicleLookup: VehicleLookupDTO, scopedTimeoutRef: ReturnType<typeof setTimeout>) {
    if (newVehicleLookup?.isAuthorized !== false || this.recaptchaKey === '') {
      this.showRecaptcha = false;
      return;
    }

    this.showRecaptcha = true;

    if (this.useMockRecaptchaWidget) {
      this.mockRecaptchaTrigger = () => this.runUnauthorizedSscLookup(newVehicleLookup, 'mock-recaptcha-token', scopedTimeoutRef);
      return;
    }

    void this.armRealRecaptcha(newVehicleLookup, scopedTimeoutRef);
  }

  private async armRealRecaptcha(newVehicleLookup: VehicleLookupDTO, scopedTimeoutRef: ReturnType<typeof setTimeout>) {
    try {
      await this.ensureRealRecaptcha();
    } catch (error) {
      console.error('reCAPTCHA could not be loaded', error);
      return;
    }

    // A newer lookup, a cleared panel or a flip to mock mode while the script loaded: this one is stale.
    if (this.networkTimeoutRef !== scopedTimeoutRef || !this.showRecaptcha || this.useMockRecaptchaWidget) return;

    if (this.recaptchaWidgetId !== undefined) grecaptcha.reset(this.recaptchaWidgetId);

    this.syncRealRecaptchaVisibility();

    clearInterval(this.recaptchaIntervalRef);
    this.recaptchaIntervalRef = setInterval(async () => {
      const recaptchaResponse = grecaptcha.getResponse(this.recaptchaWidgetId);
      if (!recaptchaResponse) return;

      clearInterval(this.recaptchaIntervalRef);

      try {
        await this.runUnauthorizedSscLookup(newVehicleLookup, recaptchaResponse, scopedTimeoutRef);
      } catch (error) {
        if (!isAbort(error)) console.error('SSC manufacturer check failed', error);
      }
    }, 500);
  }

  /** Loads Google's script and renders the widget into a body-level portal, once per element. */
  private ensureRealRecaptcha(): Promise<void> {
    if (!this.recaptchaReady) {
      this.recaptchaReady = new Promise<void>((resolve, reject) => {
        if (!this.recaptchaPortalEl) {
          this.recaptchaPortalEl = document.createElement('div');
          this.recaptchaPortalEl.style.cssText = 'position: fixed; z-index: 1; display: none;';
          document.body.appendChild(this.recaptchaPortalEl);
        }

        const renderWidget = () =>
          grecaptcha.ready(() => {
            this.recaptchaWidgetId = grecaptcha.render(this.recaptchaPortalEl, { sitekey: this.recaptchaKey });
            resolve();
          });

        if (typeof grecaptcha !== 'undefined' && typeof grecaptcha?.render === 'function') {
          renderWidget();
          return;
        }

        const script = document.createElement('script');
        script.src = RECAPTCHA_SCRIPT_SRC;
        script.async = true;
        script.defer = true;
        script.onload = renderWidget;
        script.onerror = () => {
          this.recaptchaReady = undefined;
          reject(new Error('reCAPTCHA script failed to load'));
        };
        document.head.appendChild(script);
      });
    }

    return this.recaptchaReady;
  }

  private hideRealRecaptcha() {
    cancelAnimationFrame(this.positionRAF);
    clearTimeout(this.recaptchaHideTimer);
    if (this.recaptchaPortalEl) this.recaptchaPortalEl.style.display = 'none';
  }

  /**
   * The real widget is visible only while a production-mode check is offered and unanswered. It
   * lives outside the shadow root, so the body cannot clip it as it shuts; instead it follows the
   * body's opacity (see `getAncestorOpacity`) and is only taken down once the body has settled.
   */
  private syncRealRecaptchaVisibility() {
    if (!this.recaptchaPortalEl) return;

    if (this.showRecaptcha && !this.recaptchaRes && !this.useMockRecaptchaWidget) {
      clearTimeout(this.recaptchaHideTimer);
      this.recaptchaPortalEl.style.display = 'block';
      this.syncRecaptchaPosition();
      return;
    }

    clearTimeout(this.recaptchaHideTimer);
    this.recaptchaHideTimer = setTimeout(() => this.hideRealRecaptcha(), this.settleMs());
  }

  @Watch('showRecaptcha')
  @Watch('recaptchaRes')
  onRecaptchaStateChange() {
    this.syncRealRecaptchaVisibility();
  }

  private async runUnauthorizedSscLookup(newVehicleLookup: VehicleLookupDTO, recaptchaToken: string, scopedTimeoutRef) {
    this.checkingUnauthorizedSSC = true;

    if (this.isDev) {
      await new Promise(r => setTimeout(r, 3000));

      if (this.networkTimeoutRef !== scopedTimeoutRef) return;

      this.checkingUnauthorizedSSC = false;

      const randomValue = Math.random();
      const devSscLookupStatus = randomValue < 0.33 ? 0 : randomValue > 0.33 && randomValue < 0.66 ? 2 : 1;

      this.recaptchaRes = {
        status: devSscLookupStatus === 0 ? 'noRecall' : devSscLookupStatus === 2 ? 'noApplicableVehicleFound' : 'recallExists',
      };

      smartInvokable.bind(this)(this.unauthorizedSscLookupResponse, devSscLookupStatus);
      return;
    }

    // Resolved at call time: the token handed over with the search may have expired by the time
    // the visitor completes the captcha.
    const headers = await resolveRequestHeaders(this);

    const response = await fetch(`${this.unauthorizedSscLookupBaseUrl}${newVehicleLookup?.vin}/${newVehicleLookup?.sscLogId}?${this.unauthorizedSscLookupQueryString}`, {
      signal: this.abortController.signal,
      headers: {
        ...headers,
        'Ssc-Recaptcha-Token': recaptchaToken,
      },
    });

    const vinResponse = await response.json();

    // A newer lookup started while the manufacturer was answering: this answer is for a vehicle no longer shown.
    if (this.networkTimeoutRef !== scopedTimeoutRef) return;

    if (!vinResponse) throw new Error('wrongResponseFormat');

    this.checkingUnauthorizedSSC = false;

    this.recaptchaRes = {
      status:
        vinResponse.sscLookupStatus === 0 ? 'noRecall' : vinResponse.sscLookupStatus === 1 ? 'recallExists' : vinResponse.sscLookupStatus === 2 ? 'noApplicableVehicleFound' : null,
    };

    smartInvokable.bind(this)(this.unauthorizedSscLookupResponse, vinResponse.sscLookupStatus);
  }

  disconnectedCallback() {
    this.heightAnnouncer?.dispose();
    this.stopFollowingDrawer();
    cancelAnimationFrame(this.positionRAF);
    clearInterval(this.recaptchaIntervalRef);
    clearTimeout(this.recaptchaHideTimer);
    clearTimeout(this.retainTimer);
    this.traceAbortController?.abort();
    if (this.recaptchaPortalEl) {
      this.recaptchaPortalEl.remove();
      this.recaptchaPortalEl = undefined;
      this.recaptchaReady = undefined;
      this.recaptchaWidgetId = undefined;
    }
  }

  private syncRecaptchaPosition = () => {
    if (!this.recaptchaPlaceholderRef || !this.recaptchaPortalEl || this.recaptchaPortalEl.style.display === 'none') return;

    const rect = this.recaptchaPlaceholderRef.getBoundingClientRect();
    this.recaptchaPortalEl.style.top = `${rect.top}px`;
    this.recaptchaPortalEl.style.left = `${rect.left}px`;

    const opacity = this.getAncestorOpacity();
    this.recaptchaPortalEl.style.opacity = String(opacity);
    this.recaptchaPortalEl.style.pointerEvents = opacity < 0.1 ? 'none' : 'auto';

    this.positionRAF = requestAnimationFrame(this.syncRecaptchaPosition);
  };

  /**
   * How see-through the widget's place on the page is: the product of every ancestor's opacity,
   * from the placeholder up through this shadow root (the body fading as it shuts) and on up the
   * host page (a tab sliding out). The portal copies it, so the real widget leaves with the panel.
   */
  private getAncestorOpacity(): number {
    let opacity = 1;

    const walk = (from: Element | null, until: Element | null) => {
      for (let el = from; el && el !== until; el = el.parentElement) {
        const own = parseFloat(getComputedStyle(el).opacity);
        if (!Number.isNaN(own)) opacity *= own;
      }
    };

    walk(this.recaptchaPlaceholderRef, null);
    walk(this.el, document.body);

    return opacity;
  }

  // #endregion

  // #region Repair trace

  @State() openTraceKey?: string;
  @State() traces: Record<string, SscRepairTraceDTO> = {};
  @State() traceLoading: boolean = false;
  @State() traceError?: string;

  private traceAbortController?: AbortController;
  /** The VIN the cached traces belong to; a lookup for another vehicle invalidates them. */
  private tracesVin?: string;

  private resetTraces() {
    this.traceAbortController?.abort();
    this.stopFollowingDrawer();
    this.openTraceKey = undefined;
    this.traces = {};
    this.tracesVin = undefined;
    this.traceLoading = false;
    this.traceError = undefined;
  }

  private toggleTrace = async (key: string) => {
    if (this.openTraceKey === key) {
      this.openTraceKey = undefined;
      return;
    }

    this.openTraceKey = key;

    const vin = this.vehicleLookup?.vin;
    if (!vin) return;

    // A response that already carries traces (a host that turned the option on for this caller,
    // or mock data) needs no second request.
    const seeded = Object.fromEntries((this.vehicleLookup?.ssc || []).map((item, index) => [sscTraceKey(item, index), item?.trace]).filter(([, trace]) => !!trace));

    if (this.tracesVin !== vin && Object.keys(seeded).length) {
      this.traces = seeded;
      this.tracesVin = vin;
    }

    if (this.traces[key] || this.tracesVin === vin) return;

    await this.loadTraces(vin);
  };

  private async loadTraces(vin: string) {
    this.traceAbortController?.abort();
    this.traceAbortController = new AbortController();
    const { signal } = this.traceAbortController;

    this.traceLoading = true;
    this.traceError = undefined;

    try {
      let response: VehicleLookupDTO | undefined;

      if (this.isDev) {
        response = this.mockData?.[vin];
      } else {
        if (!this.baseUrl) throw new Error('noBaseUrl');

        // KPI integrity: built from queryString alone. lookupQueryString (a wrapper's logging flag)
        // must never join a trace request — this is a re-read, not a lookup. vehicle-ssc.spec.tsx pins it.
        const query = [this.queryString, 'trace=ssc'].filter(Boolean).join('&');
        const headers = await resolveRequestHeaders(this);
        const httpResponse = await fetch(`${this.baseUrl}${vin}?${query}`, { headers, signal });

        if (!httpResponse.ok) throw new Error(`HTTP ${httpResponse.status}`);

        response = (await httpResponse.json()) as VehicleLookupDTO;
      }

      if (signal.aborted) return;

      const traces: Record<string, SscRepairTraceDTO> = {};
      (response?.ssc || []).forEach((item, index) => {
        if (item?.trace) traces[sscTraceKey(item, index)] = item.trace;
      });

      this.traces = traces;
      this.tracesVin = vin;
      this.traceError = Object.keys(traces).length ? undefined : this.locale.traceUnavailable;
    } catch (error) {
      if (isAbort(error)) return;
      console.error('SSC trace fetch failed', error);
      this.traceError = this.locale.traceFailed;
    } finally {
      if (!signal.aborted) this.traceLoading = false;
    }
  }

  /** The row whose drawer the page is following, and the way to let go of it. */
  private followedDrawer?: string;
  private stopFollowing?: () => void;

  /**
   * When a row's evidence opens, brings the row to the top of the page and keeps it there while the
   * drawer slides open beneath it — as an expanded service-history line does — so the reader is
   * looking at the evidence they asked for and not at the drawer's edge below the fold. Runs after
   * every render and acts only when the open drawer changes; closing it, or leaving, lets go.
   */
  private followOpenDrawer() {
    const key = openDrawerKey({
      campaigns: this.vehicleLookup?.ssc,
      showTrace: this.showTrace,
      openTraceKey: this.openTraceKey,
      traces: this.traces,
      traceLoading: this.traceLoading,
    });

    if (key === this.followedDrawer) return;

    this.stopFollowingDrawer();
    this.followedDrawer = key;
    if (!key) return;

    const row = Array.from(this.el.shadowRoot?.querySelectorAll('.ssc-row') ?? []).find(el => el.getAttribute('data-trace-key') === key);
    if (row) this.stopFollowing = revealBelow(row, this.settleMs());
  }

  private stopFollowingDrawer() {
    this.stopFollowing?.();
    this.stopFollowing = undefined;
    this.followedDrawer = undefined;
  }

  // #endregion

  private panelState(): PanelState {
    return {
      locale: this.locale,
      vehicleLoaded: !!this.vehicleLookup,
      authorized: this.vehicleLookup?.isAuthorized,
      campaigns: this.vehicleLookup?.ssc,
      skipped: !!this.skippedVin,
      checkAvailable: this.recaptchaKey !== '',
      checkStatus: this.recaptchaRes?.status,
    };
  }

  render() {
    const state = this.panelState();

    this.retainBody(panelBody(state));

    // The widget block for an unauthorized vehicle: the stand-in or the real widget's anchor. It
    // stays rendered after the manufacturer has answered — the body shuts over it and it leaves
    // with the body — and is dropped with the check on the next lookup. Rendered by the campaigns
    // panel inside its "check" body only, so it can never sit beside a list.
    const manufacturerCheck = this.showRecaptcha && (
      <div class="ssc-check-widget">
        <div class="recaptcha-container">
          {this.useMockRecaptchaWidget ? (
            <div
              ref={el => (this.recaptchaPlaceholderRef = el)}
              onClick={this.handleDevRecaptchaClick}
              class={cn('dev-recaptcha', { 'dev-recaptcha-disabled': this.devRecaptchaChecked })}
            >
              <div class={cn('dev-recaptcha-checkbox', { checked: this.devRecaptchaChecked })}>{this.devRecaptchaChecked && <span class="dev-recaptcha-checkmark">✓</span>}</div>
              <div class="dev-recaptcha-label">I'm not a robot</div>
              <div class="dev-recaptcha-brand">
                <div class="dev-recaptcha-brand-name">reCAPTCHA</div>
                <div class="dev-recaptcha-brand-mock">(dev mock)</div>
              </div>
            </div>
          ) : (
            <div ref={el => (this.recaptchaPlaceholderRef = el)} style={{ minWidth: '302px', minHeight: '76px' }}></div>
          )}
        </div>
      </div>
    );

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          header={this.vehicleLookup?.vin ?? this.skippedVin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div class="ssc-panel" dir={this.locale.sharedLocales.direction}>
            <SscCampaigns
              {...state}
              loading={this.isLoading || this.leaving}
              checking={this.checkingUnauthorizedSSC}
              retainedBody={this.retainedBody}
              onRunLookup={this.runSkippedLookup}
              showTrace={this.showTrace}
              openTraceKey={this.openTraceKey}
              traces={this.traces}
              traceLoading={this.traceLoading}
              traceError={this.traceError}
              onToggleTrace={this.toggleTrace}
            >
              {manufacturerCheck}
            </SscCampaigns>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
