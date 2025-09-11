import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { scrollIntoContainerView } from '~lib/scroll-into-container-view';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { VehicleInfoLayout, VehicleInfoLayoutInterface } from '~features/vehicle-info-layout';
import { BlazorInvokable, DotNetObjectReference, smartInvokable, BlazorInvokableFunction } from '~features/blazor-ref';
import { setVehicleLookupData, setVehicleLookupErrorState, VehicleLookupComponent } from '~features/vehicle-lookup-component';
import { ComponentLocale, ErrorKeys, getLocaleLanguage, getSharedLocal, LanguageKeys, MultiLingual, sharedLocalesSchema } from '~features/multi-lingual';

import { ClaimableItem } from './components/claimable-item';
import { ClaimableItemPopover } from './components/claimable-item-popover';
import { ClaimFormPayload, VehicleItemClaimForm } from './vehicle-item-claim-form';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

import { PrintIcon } from '~assets/print-icon';
import { ActivationIcon } from '~assets/activation-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';
import { VehicleLookupMock } from '~features/vehicle-lookup-component/types';

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
  async fetchData(newData: VehicleLookupDTO | string, headers: any = {}) {
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

  @State() activeTab: string = '';
  @State() showPrintBox: boolean = false;
  @State() tabAnimationLoading: boolean = false;
  @State() lastSuccessfulClaimResponse: any = null;
  @State() showClaimableItemPopover: boolean = false;
  @State() selectedClaimItem?: VehicleServiceItemDTO;
  @State() tabs: VehicleServiceItemDTO['group'][] = [];
  @State() popoverTargetLocation: { left: number; bottom: number; top: number } = { left: 0, bottom: 0, top: 0 };

  claimForm: VehicleItemClaimForm;

  private claimableItemPopoverRef: HTMLDivElement;

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

  private updateProgressBar = async (payload?: object) => {
    // payload indicates its not a fresh list rather it is an update to current one
    if (!payload) {
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

    const firstPendingItemIndex = serviceItems.findIndex(x => x.status === 'pending');

    if (firstPendingItemIndex !== -1) {
      const pendingItemRef = this.el.shadowRoot.querySelectorAll('.claimable-item')[firstPendingItemIndex] as HTMLElement;

      const progressLaneRef = this.el.shadowRoot.querySelector('.progress-lane') as HTMLElement;

      const { left: pendingItemLeftOffset } = pendingItemRef.getBoundingClientRect();
      const { width: progressLaneWidth, left: progressLeftOffset } = progressLaneRef.getBoundingClientRect();

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

    window.addEventListener('resize', this.updateProgressBar);

    if (this.claimableItemsBox) this.claimableItemsBox.addEventListener('scroll', this.updatePopoverLocation);
    window.addEventListener('scroll', this.updatePopoverLocation);
    window.addEventListener('resize', this.updatePopoverLocation);
  }

  async disconnectedCallback() {
    window.removeEventListener('resize', this.updateProgressBar);
    if (this.claimableItemsBox) this.claimableItemsBox.removeEventListener('scroll', this.updatePopoverLocation);
    window.removeEventListener('scroll', this.updatePopoverLocation);
    window.removeEventListener('resize', this.updatePopoverLocation);
  }

  @Watch('vehicleLookup')
  async onVehicleChange() {
    // wait for jsx update
    setTimeout(() => {
      this.updateProgressBar();
    }, 50);
  }

  private onActiveTabChange = ({ label }: { label: string; idx: number }) => {
    this.tabAnimationLoading = true;
    clearTimeout(this.tabAnimationTimeoutRef);

    this.tabAnimationTimeoutRef = setTimeout(() => {
      this.activeTab = label;
      // wait for jsx update
      setTimeout(() => {
        this.tabAnimationLoading = false;
        this.updateProgressBar();
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
        window.open(this.lastSuccessfulClaimResponse.PrintURL, '_blank').focus();
      }
    }
  };

  updatePopoverLocation = () => {
    if (!this.claimableItemPopoverRef) return;

    const { left, bottom, width, top } = this.claimableItemPopoverRef.getBoundingClientRect();

    this.popoverTargetLocation = { left: left + width / 2, bottom, top };
  };

  setClaimableItemPopover = (showPopover: boolean, claimableItem?: VehicleServiceItemDTO, claimableItemPopoverRef?: HTMLDivElement) => {
    if (showPopover) {
      this.claimableItemPopoverRef = claimableItemPopoverRef;
      this.updatePopoverLocation();
      this.selectedClaimItem = claimableItem;
      this.showClaimableItemPopover = true;
    } else {
      this.showClaimableItemPopover = false;
    }
  };

  @Method()
  async completeClaim(response: any) {
    const serviceItems = this.getServiceItems();

    const item = this.selectedClaimItem;
    const serviceDataClone = JSON.parse(JSON.stringify(serviceItems));

    const index = serviceItems.indexOf(item);
    const pendingItemsBefore = serviceDataClone.slice(0, index).filter(x => x.status === 'pending');

    serviceDataClone[index].claimable = false;
    serviceDataClone[index].status = 'processed';

    pendingItemsBefore.forEach(function (otherItem) {
      otherItem.status = 'cancelled';
    });

    const vehicleDataClone = JSON.parse(JSON.stringify(this.vehicleLookup)) as VehicleLookupDTO;
    vehicleDataClone.serviceItems = serviceDataClone;
    this.vehicleLookup = JSON.parse(JSON.stringify(vehicleDataClone));

    if (response.PrintURL) this.showPrintBox = true;

    this.lastSuccessfulClaimResponse = response;
  }

  handleClaim = async ({ documents, ...payload }: ClaimFormPayload) => {
    try {
      const formData = new FormData();
      formData.append(
        'payload',
        JSON.stringify({
          ...payload,
          vin: this.vehicleLookup.vin,
          serviceItem: this.claimForm.item,
          saleInformation: this.vehicleLookup.saleInformation,
        }),
      );

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
            resolve();
          } else {
            try {
              const responseData = JSON.parse(xhr.responseText);

              alert(responseData.Message);
            } catch {
              reject(new Error(`Upload failed with status ${xhr.status}`));
            }
          }
        };

        xhr.onerror = e => {
          console.log(e);

          reject(new Error('Network error'));
        };

        xhr.send(formData);
      });
    } catch (error) {
      console.error(error);
      alert(this.locale.sharedLocales.errors.requestFailedPleaseTryAgainLater);
    }
  };

  handleDevClaim = async ({ documents }: ClaimFormPayload) => {
    if (documents && documents.length > 0) {
      this.claimForm.setFileUploadProgression(0);
      let uploadChunks = 20;
      for (let index = 0; index < uploadChunks; index++) {
        const uploadPercentage = Math.round(((index + 1) / uploadChunks) * 100);

        await new Promise(r => setTimeout(r, 200));

        this.claimForm.setFileUploadProgression(uploadPercentage);
      }
    }

    await new Promise(r => setTimeout(r, 1000));

    this.completeClaim({ Success: true, ID: '11223344', PrintURL: 'http://localhost/test/print/1122' });
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

    const isNoServicesAvailable = !this.isLoading && this.vehicleLookup && !serviceItems.length;

    const hideTabs = this.isLoading || this.isError || !this.tabs.length || !serviceItems.length;

    const tabs = this.tabs.map(group => group.name);

    const hasInactiveItems = serviceItems.filter(x => x.status === 'activationRequired').length > 0;

    return (
      <Host>
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
          targetLocation={this.popoverTargetLocation}
        />

        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          header={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          isLoading={this.isLoading || this.tabAnimationLoading}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div dir="ltr" class={cn('relative flex items-center h-[320px] transition-all duration-300', { loading: this.isLoading || this.tabAnimationLoading })}>
            {/* Tabs container */}
            <div dir={this.locale.sharedLocales.direction} class="absolute top-0 z-10 w-full pt-[16px]">
              <div class={cn('duration-300', { 'translate-y-[-50%] opacity-0': hideTabs })}>
                <shift-tabs activeTabLabel={this.activeTab} changeActiveTab={this.onActiveTabChange} tabs={tabs}></shift-tabs>
              </div>
            </div>

            {/* Loading Component  */}
            <div class={cn('absolute w-[calc(100%-60px)] left-[30px] progress-container-style opacity-0', { 'opacity-100': this.isLoading || this.tabAnimationLoading })}>
              <div class="w-full h-full rounded-[4px] overflow-x-hidden absolute left-0 top-0">
                <div class="absolute opacity-0 bg-[#1a1a1a] w-[150%] h-full" />
                <div class="absolute h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)] lane-inc" />
                <div class="absolute h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)] lane-dec" />
              </div>
            </div>

            {/* Inactive items activation & Print functionality */}
            <div
              dir={this.locale.sharedLocales.direction}
              class={cn(
                'absolute w-[90%] z-10 pointer-events-none border opacity-0 translate-y-[-5px] scale-[70%] text-[#8a6d3b] bg-[#fcf8e3] border-[#faebcc] p-[25px] text-[16px] rounded-[6px] flex items-center justify-between left-1/2 -translate-x-1/2 h-10 bottom-[40px] transition duration-500',
                {
                  'opacity-100 pointer-events-auto translate-y-0 scale-100':
                    !this.isLoading && this.vehicleLookup && !this.tabAnimationLoading && (hasInactiveItems || this.showPrintBox),
                },
              )}
            >
              <span class="font-semibold">{this.showPrintBox ? this.locale.successFulClaimMessage : this.locale.warrantyAndServicesNotActivated}</span>

              <button class="claim-button" onClick={this.showPrintBox ? this.printLastClaimResponse : this.activateClaimItem}>
                {this.showPrintBox ? <PrintIcon class="size-[30px] duration-200" /> : <ActivationIcon class="size-[30px] duration-200" />}
                <span>{this.showPrintBox ? this.locale.print : this.locale.activateNow}</span>
              </button>
            </div>

            <div class="claimable-items-box px-[30px] min-w-full relative overflow-x-scroll h-full overflow-y-hidden">
              <div class="flex relative w-fit min-w-full items-center h-full [&_*]:shrink-0 gap-[250px] justify-between">
                {/* Lane */}
                <div
                  class={cn('progress-container-style progress-lane absolute overflow-hidden w-[calc(100%-0px)] translate-y-0 opacity-100', {
                    'opacity-0': this.isLoading || this.tabAnimationLoading || isNoServicesAvailable || !this.vehicleLookup,
                  })}
                >
                  {/* Progress lane */}
                  <div part="progress-bar" class="progress-bar transition-all w-1/2 h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)]" />
                </div>

                {/* Claim items */}
                <div class="ml-[-125px]" />

                {serviceItems.map((item, idx) => (
                  <ClaimableItem
                    item={item}
                    locale={this.locale}
                    setClaimableItemPopover={this.setClaimableItemPopover}
                    addStatusClass={item.status !== 'pending' || serviceItems.findIndex(i => i.status === 'pending') === idx}
                  />
                ))}

                <div class="ml-[-125px]" />
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
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
