import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { scrollIntoContainerView } from '~lib/scroll-into-container-view';
import { bindEscapeFallback, closeModalOverlay, demoteOverlay, openModalOverlay, promoteOverlayIfCaged } from '~lib/overlay';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

import { ClaimableItem } from './components/claimable-item';
import { ClaimableItemPopover, PopoverTarget } from './components/claimable-item-popover';
import { ClaimableTraceModal } from './components/claimable-trace-modal';
import { VehicleItemClaimForm } from './vehicle-item-claim-form';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

import { CarIcon } from '~assets/car-icon';
import { PrintIcon } from '~assets/print-icon';
import { PowerIcon } from '~assets/power-icon';
import { ShieldIcon } from '~assets/shield-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';
import { TriangleAlertIcon } from '~assets/triangle-alert';
import { CircleCheckIcon } from '~assets/circle-check-icon';
import { CirclePowerIcon } from '~assets/circle-power-icon';
import { VehicleLookupMock } from '~features/vehicle-lookup-component/types';
import { ItemClaimDTO } from '../../global/types/generated/vehicle-lookup/item-claim-dto';

/**
 * Whether an item is genuinely waiting to be claimed.
 *
 * A locked or missed reward keeps its ordinary status — usually pending, because nothing about its
 * lifecycle changed — so a plain status check would treat it as the customer's next step. It would
 * take the progress marker, and claiming a later item would "skip" past it and cancel it. Neither is
 * true of an item the customer was never able to claim.
 */
const isAwaitingClaim = (item: VehicleServiceItemDTO) => item.status === 'pending' && !item.lock;

@Component({
  shadow: true,
  tag: 'vehicle-claimable-items',
  styleUrl: 'vehicle-claimable-items.css',
})
export class VehicleClaimableItems implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent, BlazorInvokable {
  // #region Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof dynamicClaimSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...dynamicClaimSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.claimableItems', dynamicClaimSchema)]);
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
  @Prop() uploadMultipleDocumentsAtTheForm: boolean = true;

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
    const beforeAssignment = async (vehicleLookup: VehicleLookupDTO) => {
      this.showPrintBox = false;
      await this.parseGroupData(vehicleLookup);
      return vehicleLookup;
    };
    await setVehicleLookupData(this, newData, headers, { beforeAssignment });
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

  // #region Blazor Invokable logic
  @State() blazorRef?: DotNetObjectReference;

  @Method()
  async setBlazorRef(newBlazorRef: DotNetObjectReference) {
    this.blazorRef = newBlazorRef;
  }
  // #endregion

  // #region Component Logic
  @Prop() print?: (claimResponse: any) => void;
  @Prop() maximumDocumentFileSizeInMb: number = 30;
  @Prop() claimEndPoint: string = 'api/vehicle/swift-claim';
  @Prop() activate?: (vehicleInformation: VehicleLookupDTO) => void;
  @Prop() showTrace: boolean = false;

  @State() activeTab: string = '';
  @State() showPrintBox: boolean = false;
  @State() tabAnimationLoading: boolean = false;
  @State() lastSuccessfulClaimResponse: any = null;
  @State() showClaimableItemPopover: boolean = false;
  @State() selectedClaimItem?: VehicleServiceItemDTO;
  @State() tabs: VehicleServiceItemDTO['group'][] = [];
  @State() popoverTarget: PopoverTarget = { centerX: 0, topY: 0, bottomY: 0 };
  @State() popoverHeight: number = 0;
  @State() popoverBodyContentHeight: number = 0;
  @State() popoverSwapping: boolean = false;
  @State() outgoingClaimItem?: VehicleServiceItemDTO;
  @State() popoverLayerKey: number = 0;
  @State() popoverFadingOut: boolean = false;

  @State() showTraceModal: boolean = false;
  @State() traceModalFadingOut: boolean = false;
  @State() isLoadingTrace: boolean = false;
  @State() traceError?: string;
  @State() traceHtml?: string;

  claimForm: VehicleItemClaimForm;
  private traceAbortController?: AbortController;
  private traceDialogEl?: HTMLDialogElement;
  private releaseTraceFallbacks?: () => void;
  private traceFadeOutTimeoutRef: ReturnType<typeof setTimeout>;

  private popoverAnchorEl: HTMLElement;
  private popoverEl?: HTMLElement;
  private popoverCloseTimeoutRef: ReturnType<typeof setTimeout>;
  private popoverHideTimeoutRef: ReturnType<typeof setTimeout>;
  private popoverSwapEndTimeoutRef: ReturnType<typeof setTimeout>;

  private progressBar: HTMLElement;
  private claimableItemsBox: HTMLElement;
  private tabAnimationTimeoutRef: ReturnType<typeof setTimeout>;

  private getServiceItems = (): VehicleLookupDTO['serviceItems'] => {
    if (!this.vehicleLookup?.serviceItems?.length) return [];

    if (!this.tabs?.length) return this.vehicleLookup?.serviceItems;

    return this.vehicleLookup?.serviceItems.filter(serviceItem => serviceItem?.group?.name === this.activeTab);
  };

  private parseGroupData = (vehicleLookup: VehicleLookupDTO) => {
    if (vehicleLookup?.serviceItems?.length) {
      let orderedGroups: VehicleServiceItemDTO['group'][] = [];
      const unOrderedGroups: VehicleServiceItemDTO['group'][] = [];

      vehicleLookup.serviceItems.forEach(({ group }) => {
        if (!group?.name) return;

        if ([...orderedGroups, ...unOrderedGroups].find(g => g?.name === group?.name)) return;

        if (group?.isDefault) this.activeTab = group?.name;

        if (typeof group?.tabOrder === 'number') orderedGroups.push(group);
        else unOrderedGroups.push(group);
      });

      if (!!unOrderedGroups.length || !!orderedGroups.length) {
        orderedGroups = orderedGroups.sort((a, b) => a.tabOrder - b.tabOrder);
        this.tabs = [...orderedGroups, ...unOrderedGroups];
        if (!this.activeTab) this.activeTab = this.tabs[0].name;
      } else {
        this.tabs = [];
        this.activeTab = '';
      }
    } else {
      this.tabs = [];
      this.activeTab = '';
    }

    return vehicleLookup;
  };

  /**
   * The lane is drawn by measuring where the next claimable card sits, so it cannot be measured
   * until Stencil has written those cards. Stencil writes through requestAnimationFrame, and a
   * browser does not run requestAnimationFrame while its tab is in the background — it does keep
   * firing timers there, only throttled. Measuring on a timer therefore measures a DOM that has
   * not been rendered yet whenever an answer arrives while nobody is watching: there are no cards
   * to find, the measurement fails, and the lane keeps the zero width the reset gave it. So the
   * measurement is requested through this flag and taken in componentDidRender, which ties it to
   * the render it depends on rather than to the clock.
   */
  private progressBarUpdatePending = false;

  private takePendingProgressBarUpdate = () => {
    if (!this.progressBarUpdatePending) return;
    this.progressBarUpdatePending = false;
    this.updateProgressBar();
  };

  private onWindowResize = () => this.updateProgressBar({ preserveWidth: true });

  private onVisibilityChange = () => {
    // Anything but a tab that is still hidden is worth retrying against, including a document that
    // does not report a visibility at all.
    if (document.visibilityState === 'hidden') return;
    this.takePendingProgressBarUpdate();
  };

  private updateProgressBar = async (options?: { preserveWidth?: boolean }) => {
    // Nothing to write into yet — componentDidLoad takes the measurement once it has the bar.
    if (!this.progressBar) {
      this.progressBarUpdatePending = true;
      return;
    }

    // A resize or a retry is not a fresh list, so the bar keeps the width it has rather than
    // flashing back to zero and re-animating.
    if (!options?.preserveWidth) {
      // hard reset of the bar
      this.progressBar.style.transitionDuration = '0s';
      this.progressBar.style.opacity = '0';
      this.progressBar.style.width = '0%';

      // apply changes
      await new Promise(r => setTimeout(r, 10));
    }

    this.progressBar.style.transitionDuration = '1s';
    this.progressBar.style.opacity = '1';

    if (!this.vehicleLookup) return;

    if (!!this.tabs?.length && this.tabs.find(tab => tab.name === this.activeTab) && !this.tabs.find(tab => tab.name === this.activeTab)?.isSequential) return;

    const serviceItems = this.getServiceItems();

    const firstPendingItemIndex = serviceItems.findIndex(x => isAwaitingClaim(x));

    if (firstPendingItemIndex !== -1) {
      const pendingItemRef = this.el.shadowRoot.querySelectorAll('.claimable-item')[firstPendingItemIndex] as HTMLElement;

      const progressLaneRef = this.el.shadowRoot.querySelector('.progress-lane') as HTMLElement;

      const { width: progressLaneWidth, left: progressLeftOffset } = progressLaneRef?.getBoundingClientRect() ?? { width: 0, left: 0 };

      // Either this list has not been rendered yet, or nothing has been laid out — which is what
      // an element measures as while it is not being displayed. Leave the request standing so the
      // next render, resize or wake-up takes it, rather than writing a NaN width that the browser
      // discards and leaving the lane empty for good.
      if (!pendingItemRef || !progressLaneWidth) {
        this.progressBarUpdatePending = true;
        return;
      }

      const { left: pendingItemLeftOffset } = pendingItemRef.getBoundingClientRect();

      const offsetToLeftRatio = ((pendingItemLeftOffset - progressLeftOffset) / progressLaneWidth) * 100;

      this.progressBar.style.width = `${offsetToLeftRatio.toFixed(2)}%`;

      if (firstPendingItemIndex === serviceItems.length - 1)
        this.claimableItemsBox.scrollTo({
          left: this.claimableItemsBox.scrollWidth,
          behavior: 'smooth',
        });
      else scrollIntoContainerView(pendingItemRef, this.claimableItemsBox);
    } else if (!(serviceItems.length === 0 || serviceItems.filter(x => x.status === 'activationRequired').length === serviceItems.length)) {
      this.progressBar.style.width = '100%';

      this.claimableItemsBox.scrollTo({
        left: this.claimableItemsBox.scrollWidth,
        behavior: 'smooth',
      });
    }
  };

  async componentDidLoad() {
    this.progressBar = this.el.shadowRoot.querySelector('.progress-bar');

    this.claimForm = this.el.shadowRoot.querySelector('.vehicle-item-claim-form') as unknown as VehicleItemClaimForm;

    this.claimableItemsBox = this.el.shadowRoot.querySelector('.claimable-items-box');

    window.addEventListener('resize', this.onWindowResize);
    document.addEventListener('visibilitychange', this.onVisibilityChange);

    if (this.claimableItemsBox) this.claimableItemsBox.addEventListener('scroll', this.onViewportChange);
    window.addEventListener('scroll', this.onViewportChange);
    window.addEventListener('resize', this.onViewportChange);

    requestAnimationFrame(() => this.measurePopoverHeight());

    // A lookup can land before the component has finished loading, in which case the measurement
    // it asked for is still owed.
    this.takePendingProgressBarUpdate();
  }

  componentDidRender() {
    this.takePendingProgressBarUpdate();
  }

  async disconnectedCallback() {
    window.removeEventListener('resize', this.onWindowResize);
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
    if (this.claimableItemsBox) this.claimableItemsBox.removeEventListener('scroll', this.onViewportChange);
    window.removeEventListener('scroll', this.onViewportChange);
    window.removeEventListener('resize', this.onViewportChange);
    clearTimeout(this.popoverCloseTimeoutRef);
    clearTimeout(this.popoverHideTimeoutRef);
    clearTimeout(this.popoverSwapEndTimeoutRef);
    clearTimeout(this.traceFadeOutTimeoutRef);
    this.traceAbortController?.abort();
    this.releaseTraceFallbacks?.();
    demoteOverlay(this.popoverEl);
  }

  @Watch('vehicleLookup')
  async onVehicleChange() {
    this.progressBarUpdatePending = true;
  }

  private onActiveTabChange = ({ label }: { label: string; idx: number }) => {
    this.tabAnimationLoading = true;
    clearTimeout(this.tabAnimationTimeoutRef);

    this.tabAnimationTimeoutRef = setTimeout(() => {
      this.activeTab = label;
      // let the new tab's cards settle before dropping the loading lane and measuring against them
      setTimeout(() => {
        this.tabAnimationLoading = false;
        this.progressBarUpdatePending = true;
      }, 50);
    }, 750);
  };

  private activateClaimItem = () => {
    if (this.activate) this.activate(this.vehicleLookup);
  };

  private printLastClaimResponse = () => {
    if (this.print) {
      this.print(this.lastSuccessfulClaimResponse);
    } else {
      if (this.lastSuccessfulClaimResponse.PrintURL) {
        window.open(this.lastSuccessfulClaimResponse.PrintURL, '_blank')?.focus();
      }
    }
  };

  /**
   * The box the popover is placed against: the item's whole column — status icon, chip, node, dot,
   * name — rather than any one piece of it. Anchored to the status block, as it was, the card opened
   * straight over the node and the label of the very item the customer was pointing at.
   *
   * The column is three absolutely positioned boxes hung around a 48px node, so no single element
   * has a rect that covers it. The union of the container's children is that rect.
   */
  private anchorBounds = (anchor: HTMLElement) => {
    const { left, right, top, bottom } = anchor.getBoundingClientRect();
    const bounds = { left, right, top, bottom };

    for (const child of Array.from(anchor.children)) {
      const rect = child.getBoundingClientRect();

      // An element mid-entrance measures small but never zero; one that has not been laid out at all
      // measures zero, and folding that into the union would drag the anchor to the viewport origin.
      if (!rect.width && !rect.height) continue;

      bounds.left = Math.min(bounds.left, rect.left);
      bounds.right = Math.max(bounds.right, rect.right);
      bounds.top = Math.min(bounds.top, rect.top);
      bounds.bottom = Math.max(bounds.bottom, rect.bottom);
    }

    return bounds;
  };

  updatePopoverLocation = () => {
    if (!this.popoverAnchorEl) return;

    const { left, right, top, bottom } = this.anchorBounds(this.popoverAnchorEl);
    const centerX = (left + right) / 2;

    // Writing @State re-renders the whole component, and scroll fires far more often than the anchor
    // actually moves — most notably not at all while the popover is parked over a still page.
    if (this.popoverTarget.centerX === centerX && this.popoverTarget.topY === top && this.popoverTarget.bottomY === bottom) return;

    this.popoverTarget = { centerX, topY: top, bottomY: bottom };
  };

  // The anchor outlives the popover, so scroll and resize would otherwise keep measuring it long
  // after the card closed.
  private onViewportChange = () => {
    if (!this.showClaimableItemPopover && !this.popoverFadingOut) return;
    this.updatePopoverLocation();
  };

  private measurePopoverHeight = () => {
    const body = this.el.shadowRoot?.querySelector('.popover-body') as HTMLElement | null;
    const content = this.el.shadowRoot?.querySelector('.popover-body-content') as HTMLElement | null;
    const inner = this.currentPopoverInner();

    if (!body || !content || !inner) return;

    // Mid-swap the content box is pinned to an animating height and the outgoing layer is still
    // stacked behind the incoming one, so the body's own size is neither where it started nor where
    // it is going. Take the chrome (padding + border) from the live box and the content height from
    // the layer that will remain, which off a swap is exactly the body's scrollHeight anyway.
    this.popoverHeight = Math.ceil(inner.getBoundingClientRect().height) + (body.offsetHeight - content.offsetHeight);
  };

  // Scoped to the current layer: during a cross-fade an outgoing copy of the card is mounted too,
  // and an unscoped lookup would measure whichever of the two the vdom happened to put first.
  private currentPopoverInner = () => this.el.shadowRoot?.querySelector('.popover-layer-current .popover-body-inner') as HTMLElement | null;

  private measurePopoverContentHeight = (): number => {
    const inner = this.currentPopoverInner();
    if (!inner) return 0;
    // Use getBoundingClientRect for sub-pixel precision and ceil to avoid undershooting at
    // fractional browser zoom (e.g. 80%, where offsetHeight may round down by 1px and clip the bottom).
    return Math.ceil(inner.getBoundingClientRect().height);
  };

  private static readonly POPOVER_CLOSE_DELAY_MS = 200;
  private static readonly POPOVER_FADE_OUT_MS = 500;
  /** Cross-fade + resize duration. Handed to the popover as --popover-swap, so this one number
   *  drives both the CSS transitions and the timer that drops the outgoing layer. */
  private static readonly POPOVER_SWAP_MS = 500;
  private static readonly TRACE_FADE_OUT_MS = 420;

  setClaimableItemPopover = (showPopover: boolean, claimableItem?: VehicleServiceItemDTO, anchorEl?: HTMLElement) => {
    clearTimeout(this.popoverCloseTimeoutRef);
    clearTimeout(this.popoverHideTimeoutRef);

    if (showPopover) {
      // Called without args (e.g. popover hover) — just keep it open by cancelling close timers.
      if (!anchorEl || !claimableItem) {
        if (this.popoverFadingOut) {
          this.popoverFadingOut = false;
          this.showClaimableItemPopover = true;
        }
        return;
      }

      // The popover is "still active" if it's open OR mid-fade-out. Don't tear down state in either case.
      const wasActive = this.showClaimableItemPopover || this.popoverFadingOut;
      const sameItem = wasActive && this.selectedClaimItem === claimableItem;

      this.popoverAnchorEl = anchorEl;

      if (sameItem) {
        // Re-hovering the same item that's still showing/fading — just bring it back, no state churn.
        this.popoverFadingOut = false;
        this.showClaimableItemPopover = true;
        return;
      }

      clearTimeout(this.popoverSwapEndTimeoutRef);

      if (wasActive) {
        // Switching to a different item while still active. Both cards are mounted at once, stacked
        // in one grid cell: pin the box to the height it has now, mount the incoming layer hidden,
        // then release everything together next frame so the fade-out, the fade-in and the resize
        // run on the same clock. The previous version faded the values out, swapped the DOM, and
        // only then started resizing — which is what made the move read as two separate jerks, left
        // labels and whole rows hard-swapping, and mounted/unmounted a claim button mid-move.
        this.popoverBodyContentHeight = this.measurePopoverContentHeight();
        this.outgoingClaimItem = this.selectedClaimItem;
        this.selectedClaimItem = claimableItem;
        this.popoverLayerKey += 1;
        this.popoverSwapping = true;
        this.popoverFadingOut = false;
        this.showClaimableItemPopover = true;
        this.updatePopoverLocation();

        // Two frames: the first lets Stencil render the incoming layer, the second lets the browser
        // adopt its opacity:0 starting style. Flipping in the first frame would skip the transition.
        requestAnimationFrame(() =>
          requestAnimationFrame(() => {
            this.popoverBodyContentHeight = this.measurePopoverContentHeight();
            this.popoverSwapping = false;
            this.measurePopoverHeight();
          }),
        );

        this.popoverSwapEndTimeoutRef = setTimeout(() => {
          this.outgoingClaimItem = undefined;
          // Hand the height back to the content, so anything that changes it later (a claim landing,
          // a resize) reflows instead of staying pinned to a stale measurement.
          this.popoverBodyContentHeight = 0;
        }, VehicleClaimableItems.POPOVER_SWAP_MS);
      } else {
        // Truly fresh open. Two-frame dance: set position while still hidden, then flip aria-expanded
        // next frame so the opacity fade-in doesn't drag a position transition along with it.
        this.popoverSwapping = false;
        this.outgoingClaimItem = undefined;
        this.popoverBodyContentHeight = 0;
        this.selectedClaimItem = claimableItem;
        this.updatePopoverLocation();
        requestAnimationFrame(() => {
          this.showClaimableItemPopover = true;
          // Non-modal, and only when an ancestor would otherwise clip it: a hover card that took the
          // top layer unconditionally would outrank the host's own toasts and nav for no reason, and
          // a modal one would inert the very cards it is meant to be hovered between.
          promoteOverlayIfCaged(this.popoverEl);
          requestAnimationFrame(() => this.measurePopoverHeight());
        });
      }
    } else {
      this.popoverCloseTimeoutRef = setTimeout(() => {
        clearTimeout(this.popoverSwapEndTimeoutRef);
        this.showClaimableItemPopover = false;
        this.popoverFadingOut = true;
        this.popoverSwapping = false;
        this.outgoingClaimItem = undefined;

        this.popoverHideTimeoutRef = setTimeout(() => {
          this.popoverFadingOut = false;
          this.popoverBodyContentHeight = 0;
          demoteOverlay(this.popoverEl);
        }, VehicleClaimableItems.POPOVER_FADE_OUT_MS);
      }, VehicleClaimableItems.POPOVER_CLOSE_DELAY_MS);
    }
  };

  private onPopoverMouseEnter = () => {
    // Cancel any pending close + recover from fade-out if needed.
    this.setClaimableItemPopover(true);
  };

  private onPopoverMouseLeave = () => {
    this.setClaimableItemPopover(false);
  };

  @Method()
  async openTrace() {
    await this.openTraceModal();
  }

  private openTraceModal = async () => {
    if (!this.vehicleLookup?.vin) return;

    clearTimeout(this.traceFadeOutTimeoutRef);
    this.traceModalFadingOut = false;
    this.showTraceModal = true;
    this.isLoadingTrace = true;
    this.traceError = undefined;
    this.traceHtml = undefined;

    // Match the claim-form pattern: the top layer clears the host page's stacking contexts and
    // inerts everything behind it, so the only scrollbar is the iframe's.
    const inTopLayer = openModalOverlay(this.traceDialogEl);

    // What the top layer supplies for free, supplied by hand where it is unavailable.
    if (!inTopLayer) {
      document.body.style.overflow = 'hidden';
      const release = bindEscapeFallback(this.onTraceCancel);
      this.releaseTraceFallbacks = () => {
        release();
        document.body.style.overflow = '';
      };
    }

    this.traceAbortController?.abort();
    this.traceAbortController = new AbortController();

    try {
      const traceQuery = [this.queryString, 'trace=html'].filter(Boolean).join('&');
      const url = `${this.baseUrl}${this.vehicleLookup.vin}${traceQuery ? `?${traceQuery}` : ''}`;
      const response = await fetch(url, {
        method: 'GET',
        headers: { Accept: 'text/html', ...((this.headers as Record<string, string>) || {}) },
        signal: this.traceAbortController.signal,
      });

      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      const html = await response.text();

      if (!this.showTraceModal) return;

      this.traceHtml = html;
      this.isLoadingTrace = false;
    } catch (error) {
      if ((error as DOMException)?.name === 'AbortError') return;
      console.error('Trace fetch failed', error);
      this.isLoadingTrace = false;
      this.traceError = this.locale.traceFailed;
    }
  };

  private closeTraceModal = () => {
    if (!this.showTraceModal && !this.traceModalFadingOut) return;
    this.traceAbortController?.abort();
    this.showTraceModal = false;
    this.traceModalFadingOut = true;
    clearTimeout(this.traceFadeOutTimeoutRef);
    this.traceFadeOutTimeoutRef = setTimeout(() => {
      this.traceModalFadingOut = false;
      this.isLoadingTrace = false;
      this.traceError = undefined;
      this.traceHtml = undefined;
      // Last, so the panel is fully faded before it leaves the top layer.
      if (this.showTraceModal) return;
      closeModalOverlay(this.traceDialogEl);
      this.releaseTraceFallbacks?.();
      this.releaseTraceFallbacks = undefined;
    }, VehicleClaimableItems.TRACE_FADE_OUT_MS);
  };

  /** Escape would drop the panel out of the top layer in one frame; close it through the fade. */
  private onTraceCancel = (event: Event) => {
    event.preventDefault();
    this.closeTraceModal();
  };

  @Method()
  async completeClaim(response: any) {
    const serviceItems = this.getServiceItems();

    const item = this.selectedClaimItem;
    const serviceDataClone = JSON.parse(JSON.stringify(serviceItems));

    const index = serviceItems.indexOf(item);
    const pendingItemsBefore = serviceDataClone.slice(0, index).filter(x => isAwaitingClaim(x));

    serviceDataClone[index].claimable = false;
    serviceDataClone[index].status = 'processed';

    pendingItemsBefore.forEach(otherItem => (otherItem.status = 'cancelled'));

    const vehicleDataClone = JSON.parse(JSON.stringify(this.vehicleLookup)) as VehicleLookupDTO;
    vehicleDataClone.serviceItems = serviceDataClone;
    this.vehicleLookup = vehicleDataClone;

    if (response.PrintURL) this.showPrintBox = true;

    this.lastSuccessfulClaimResponse = response;
  }

  handleClaim = async (documents: File[], payload: ItemClaimDTO) => {
    try {
      const formData = new FormData();

      payload.vin = this.vehicleLookup.vin;
      payload.saleInformation = this.vehicleLookup.saleInformation;
      payload.serviceItem = this.claimForm.item;
      payload.identifiers = this.vehicleLookup.identifiers;
      payload.vehicleVariantInfo = this.vehicleLookup.vehicleVariantInfo;
      payload.vehicleSpecification = this.vehicleLookup.vehicleSpecification;

      formData.append('payload', JSON.stringify(payload));

      if (documents && documents.length > 0) {
        documents.forEach(doc => {
          formData.append('document', doc);
        });
      }

      await new Promise<void>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', this.claimEndPoint);

        Object.entries(this.headers || {}).forEach(([key, value]) => {
          xhr.setRequestHeader(key, value as string);
        });

        xhr.upload.onprogress = e => {
          if (e.lengthComputable) this.claimForm.setFileUploadProgression(Math.round((e.loaded / e.total) * 100));
        };

        xhr.onload = () => {
          if (xhr.status === 200) {
            try {
              const responseData = JSON.parse(xhr.responseText);

              this.completeClaim(responseData);
              resolve();
            } catch (parseError) {
              console.error('Response is not valid JSON', {
                rawResponse: xhr.responseText,
                error: parseError,
              });

              reject(new Error('Upload succeeded but response is not valid JSON'));
            }
          } else {
            try {
              const responseData = JSON.parse(xhr.responseText);
              const error = new Error(responseData.Message);
              (error as any).serverMessage = responseData.Message;
              reject(error);
            } catch {
              reject(new Error(`Upload failed with status ${xhr.status}`));
            }
          }
        };

        xhr.onerror = () => reject(new Error('Network error'));

        xhr.send(formData);
      });
    } catch (error) {
      // Show the claim-failure dialog, then re-throw: the rejection resets the
      // claim form's loading state and keeps it open for retry.
      this.claimForm.showError(error?.serverMessage || this.locale.sharedLocales.errors.requestFailedPleaseTryAgainLater);
      throw error;
    }
  };

  handleDevClaim = async (documents: File[]) => {
    try {
      if (documents && documents.length > 0) {
        this.claimForm.setFileUploadProgression(0);
        const uploadChunks = 20;
        for (let index = 0; index < uploadChunks; index++) {
          const uploadPercentage = Math.round(((index + 1) / uploadChunks) * 100);

          await new Promise(r => setTimeout(r, 200));

          this.claimForm.setFileUploadProgression(uploadPercentage);
        }
      }

      await new Promise(r => setTimeout(r, 1000));

      this.completeClaim({ Success: true, ID: '11223344', PrintURL: 'http://localhost/test/print/1122' });
    } catch (error) {
      this.claimForm.showError(this.locale.sharedLocales.errors.requestFailedPleaseTryAgainLater);
      throw error;
    }
  };

  @Method()
  async claim(item: VehicleServiceItemDTO) {
    this.selectedClaimItem = item;

    this.claimForm.item = item;
    this.claimForm.vin = this.vehicleLookup?.vin;

    this.claimForm.handleClaiming = this.isDev ? this.handleDevClaim.bind(this) : this.handleClaim.bind(this);

    this.claimForm.open();
  }
  // #endregion

  render() {
    const serviceItems = this.getServiceItems();

    // Only the first item still awaiting a claim wears its status colour; the rest are drawn plain.
    // Hoisted out of the map below, where it used to be a findIndex per card.
    const firstAwaitingIndex = serviceItems.findIndex(isAwaitingClaim);

    const isNoServicesAvailable = !this.isLoading && this.vehicleLookup && !serviceItems.length;

    const hideTabs = this.isLoading || this.isError || !this.tabs.length || !serviceItems.length;

    const tabs = this.tabs.map(group => group.name);

    const activationStatus = this.vehicleLookup?.warranty?.activationStatus ?? 'NotRequired';
    const showActivationRequired = activationStatus === 'Required';
    const showActivationBlocked = activationStatus === 'BlockedNotAllocated';
    const showActivationBox = showActivationRequired || showActivationBlocked || this.showPrintBox;
    const showActionButton = this.showPrintBox || showActivationRequired;

    // The notice under the timeline is one box with three things it can be saying, not three boxes:
    // a claim just landed and can be printed, this vehicle needs activating, or it needs activating
    // and this dealership is not the one that can do it. A claim landing wins over an activation
    // notice, because it is the newer news and the customer just caused it.
    const noticeTone = this.showPrintBox ? 'success' : showActivationBlocked ? 'blocked' : 'warning';

    const showNotice = !this.isLoading && !this.tabAnimationLoading && !!this.vehicleLookup && showActivationBox;

    const isBusy = this.isLoading || this.tabAnimationLoading;

    // Nothing has been looked up yet — not loading, not failed, just nothing asked. The band would
    // otherwise be an empty 352px of card with no explanation.
    const isIdle = !isBusy && !this.isError && !this.vehicleLookup;

    const noticeMessage = this.showPrintBox
      ? this.locale.successFulClaimMessage
      : showActivationBlocked
        ? this.locale.activationBlockedNotAllocated
        : this.locale.warrantyAndServicesNotActivated;

    return (
      <Host translate="no">
        <vehicle-item-claim-form
          class="vehicle-item-claim-form"
          maximumDocumentFileSizeInMb={this.maximumDocumentFileSizeInMb}
          uploadMultipleDocuments={this.uploadMultipleDocumentsAtTheForm}
          locale={{ sharedLocales: this.locale.sharedLocales, ...this.locale.claimForm }}
        />

        <ClaimableItemPopover
          locale={this.locale}
          claim={this.claim.bind(this)}
          item={this.selectedClaimItem}
          showPopover={this.showClaimableItemPopover}
          target={this.popoverTarget}
          popoverHeight={this.popoverHeight}
          fadingOut={this.popoverFadingOut}
          swapping={this.popoverSwapping}
          outgoingItem={this.outgoingClaimItem}
          layerKey={this.popoverLayerKey}
          swapMs={VehicleClaimableItems.POPOVER_SWAP_MS}
          bodyContentHeight={this.popoverBodyContentHeight}
          onMouseEnter={this.onPopoverMouseEnter}
          onMouseLeave={this.onPopoverMouseLeave}
          rootRef={el => (this.popoverEl = el)}
        />

        <ClaimableTraceModal
          isOpen={this.showTraceModal}
          fadingOut={this.traceModalFadingOut}
          isLoading={this.isLoadingTrace}
          errorMessage={this.traceError}
          vin={this.vehicleLookup?.vin}
          traceHtml={this.traceHtml}
          locale={this.locale}
          onClose={this.closeTraceModal}
          onCancel={this.onTraceCancel}
          dialogRef={el => (this.traceDialogEl = el)}
        />

        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          isLoading={this.isLoading || this.tabAnimationLoading}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
          headerRight={
            this.showTrace && this.vehicleLookup && !this.isLoading && !this.tabAnimationLoading && !this.isError ? (
              <button type="button" class="trace-trigger-button" title={this.locale.viewTrace} aria-label={this.locale.viewTrace} onClick={this.openTraceModal}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="6" cy="19" r="3" />
                  <path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15" />
                  <circle cx="18" cy="5" r="3" />
                </svg>
              </button>
            ) : null
          }
        >
          <div dir={this.locale.sharedLocales.direction} class="timeline-heading">
            <div class="timeline-heading-badge">
              <CarIcon />
            </div>
            <div class="timeline-heading-text">
              <strong class="timeline-heading-title">{this.locale.timelineTitle}</strong>
              <span class="timeline-heading-subtitle">{this.locale.timelineSubtitle}</span>
            </div>
          </div>

          <div dir="ltr" class={cn('timeline-band', { loading: this.isLoading || this.tabAnimationLoading })}>
            {/* Tabs. Outside the scroll box on purpose — they name what is being scrolled, so they
                must not travel with it. */}
            <div dir={this.locale.sharedLocales.direction} class="absolute top-0 z-10 w-full pt-[16px]">
              <div class={cn('duration-300', { 'translate-y-[-50%] opacity-0': hideTabs })}>
                <shift-tabs activeTabLabel={this.activeTab} changeActiveTab={this.onActiveTabChange} tabs={tabs}></shift-tabs>
              </div>
            </div>

            {/* Says why the band is empty when nothing has been looked up yet. Outside the scroll box,
                so it cannot be scrolled away from. */}
            <div dir={this.locale.sharedLocales.direction} class={cn('timeline-idle', { 'is-shown': isIdle })}>
              <div class="timeline-idle-badge">
                <CarIcon />
              </div>
              <p class="timeline-idle-text">{this.locale.timelineIdle}</p>
            </div>

            <div class="claimable-items-box">
              <div class="timeline-track">
                <div class="flex relative w-fit min-w-full items-center h-full [&_*]:shrink-0 gap-[280px] justify-between">
                  {/* Lane */}
                  <div
                    class={cn('progress-container-style progress-lane absolute overflow-hidden w-full opacity-100', {
                      'opacity-0': this.isLoading || this.tabAnimationLoading || isNoServicesAvailable || !this.vehicleLookup,
                    })}
                  >
                    {/* Progress lane */}
                    <div part="progress-bar" class="progress-bar transition-all w-1/2 h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)]" />
                  </div>

                  {/* The lane that stands in for it while the answer is on its way. A sibling of
                      the real one so the two sit on exactly the same line. */}
                  <div class={cn('progress-container-style progress-lane-loading absolute w-full opacity-0', { 'opacity-100': isBusy })}>
                    <div class="absolute left-0 top-0 h-full w-full overflow-x-hidden rounded-full">
                      <div class="lane-sweep lane-inc" />
                      <div class="lane-sweep lane-dec" />
                    </div>
                  </div>

                  {/* Claim items */}
                  <div class="ml-[-140px]" />

                  {serviceItems.map((item, idx) => (
                    <ClaimableItem
                      item={item}
                      locale={this.locale}
                      setClaimableItemPopover={this.setClaimableItemPopover}
                      addStatusClass={!isAwaitingClaim(item) || firstAwaitingIndex === idx}
                    />
                  ))}

                  <div class="ml-[-140px]" />
                </div>

                {/* Empty state */}
                <div
                  dir={this.locale.sharedLocales.direction}
                  class={cn(
                    'absolute top-0 left-0 pointer-events-none size-full box-content flex flex-col justify-center opacity-0 transition duration-500 items-center text-slate-700',
                    {
                      'opacity-100 scale-100': isNoServicesAvailable,
                    },
                  )}
                >
                  <EmptyTableIcon class="size-[90px]" />
                  <div class="text-[22px]">{this.locale.sharedLocales.errors.noServiceAvailable}</div>
                </div>
              </div>

              {/* Both of these belong to the timeline, so they ride inside the scroll box with it —
                  which is what puts the horizontal scrollbar underneath them rather than drawing it
                  as a rule between the timeline and the two boxes that describe it. Pinned, not
                  scrolled: see .timeline-underlay. */}
              <div class="timeline-underlay">
                <div class={cn('timeline-notice-slot', { 'is-open': showNotice })}>
                  <div class="timeline-notice-clip">
                    <div dir={this.locale.sharedLocales.direction} class={cn('timeline-notice', `timeline-notice-${noticeTone}`)}>
                      <div class="timeline-notice-icon">{this.showPrintBox ? <CircleCheckIcon /> : showActivationBlocked ? <TriangleAlertIcon /> : <CirclePowerIcon />}</div>

                      <span class="timeline-notice-text">{noticeMessage}</span>

                      {showActionButton && (
                        <button type="button" class="timeline-notice-action" onClick={this.showPrintBox ? this.printLastClaimResponse : this.activateClaimItem}>
                          {this.showPrintBox ? <PrintIcon viewBox="0 0 24 24" fill="currentColor" /> : <PowerIcon />}
                          <span>{this.showPrintBox ? this.locale.print : this.locale.activateNow}</span>
                        </button>
                      )}
                    </div>
                  </div>
                </div>

                {!this.isError && (
                  <div dir={this.locale.sharedLocales.direction} class="timeline-footnote">
                    <div class="timeline-footnote-icon">
                      <ShieldIcon />
                    </div>
                    <span class="timeline-footnote-text">{this.locale.timelineNote}</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
