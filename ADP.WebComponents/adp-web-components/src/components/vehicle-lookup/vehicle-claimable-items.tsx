import { InferType } from 'yup';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { scrollIntoContainerView } from '~lib/scroll-into-container-view';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import { LanguageKeys } from '~types/locale';
import { MockJson } from '~types/components';
import { vehicleLookupDTO } from '~types/vehicleLookup/vehicleLookupDTO';
import { vehicleServiceItemDTO } from '~types/vehicleLookup/vehicleServiceItemDTO';

import expiredIcon from './assets/expired.svg';
import pendingIcon from './assets/pending.svg';
import cancelledIcon from './assets/cancelled.svg';
import processedIcon from './assets/processed.svg';
import activationRequiredIcon from './assets/activationRequired.svg';

import { getVehicleInformation, VehicleInformationInterface } from '~api/vehicleInformation';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import { ClaimFormPayload, VehicleItemClaimForm } from './vehicle-item-claim-form';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';

let mockData: MockJson<vehicleLookupDTO> = {};

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
};

@Component({
  shadow: true,
  tag: 'vehicle-claimable-items',
  styleUrl: 'vehicle-claimable-items.css',
})
export class VehicleClaimableItems implements VehicleInformationInterface {
  @Prop() baseUrl: string;
  @Prop() headers: any = {};
  @Prop() isDev: boolean = false;
  @Prop() queryString: string = '';
  @Prop() coreOnly: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() print?: (claimResponse: any) => void;
  @Prop() maximumDocumentFileSizeInMb: number = 30;
  @Prop() claimEndPoint: string = 'api/vehicle/swift-claim';
  @Prop() errorCallback: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: vehicleLookupDTO) => void;
  @Prop() activate?: (vehicleInformation: vehicleLookupDTO) => void;

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() locale: InferType<typeof dynamicClaimSchema> = dynamicClaimSchema.getDefault();

  @State() activeTab: string = '';
  @State() isError: boolean = false;
  @State() showPopup: boolean = false;
  @State() isLoading: boolean = false;
  @State() externalVin?: string = null;
  @State() showPrintBox: boolean = false;
  @State() errorMessage?: ErrorKeys = null;
  @State() tabAnimationLoading: boolean = false;
  @State() activePopupIndex: null | number = null;
  @State() tabs: vehicleServiceItemDTO['group'][] = [];
  @State() lastSuccessfulClaimResponse: any = null;
  @State() vehicleInformation?: vehicleLookupDTO;

  pendingItemHighlighted = false;

  @Element() el: HTMLElement;

  scrollListenerRef: () => void;
  abortController: AbortController;
  timeoutRef: ReturnType<typeof setTimeout>;
  networkTimeoutRef: ReturnType<typeof setTimeout>;
  tabAnimationTimeoutRef: ReturnType<typeof setTimeout>;

  cachedClaimItem: vehicleServiceItemDTO;

  progressBar: HTMLElement;
  popupPositionRef: HTMLElement;
  claimForm: VehicleItemClaimForm;
  claimableContentWrapper: HTMLElement;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const localeResponses = await Promise.all([getLocaleLanguage(newLanguage, 'vehicleLookup.claimableItems', dynamicClaimSchema), getSharedLocal(newLanguage)]);
    this.locale = localeResponses[0];
    this.sharedLocales = localeResponses[1];
  }

  async componentDidLoad() {
    this.claimableContentWrapper = this.el.shadowRoot.querySelector('.claimable-content-wrapper');
    this.claimForm = this.el.shadowRoot.getElementById('vehicle-item-claim-form') as unknown as VehicleItemClaimForm;
    this.progressBar = this.el.shadowRoot.querySelector('.progress-bar');
  }

  @Method()
  async setMockData(newMockData: MockJson<vehicleLookupDTO>) {
    mockData = newMockData;
  }

  @Method()
  async setData(newData: vehicleLookupDTO | string, headers: any = {}) {
    clearTimeout(this.networkTimeoutRef);
    if (this.abortController) this.abortController.abort();
    this.abortController = new AbortController();
    let scopedTimeoutRef: ReturnType<typeof setTimeout>;

    const isVinRequest = typeof newData === 'string';

    const vin = isVinRequest ? newData : newData?.vin;
    this.externalVin = vin;

    try {
      if (!vin || vin.trim().length === 0) {
        this.isError = false;
        this.vehicleInformation = null;
        return;
      }

      this.isLoading = true;
      this.showPrintBox = false;
      await new Promise(r => {
        scopedTimeoutRef = setTimeout(r, 1000);
        this.networkTimeoutRef = scopedTimeoutRef;
      });

      this.claimableContentWrapper.scrollLeft = 0;

      const vehicleResponse = isVinRequest ? await getVehicleInformation(this, { scopedTimeoutRef, vin, mockData }, headers) : newData;

      if (this.networkTimeoutRef === scopedTimeoutRef) {
        if (!vehicleResponse) throw new Error('wrongResponseFormat');
        if (!Array.isArray(vehicleResponse.serviceItems)) throw new Error('noServiceAvailable');
        this.vehicleInformation = vehicleResponse;

        if (vehicleResponse?.serviceItems?.length) {
          let orderedGroups: vehicleServiceItemDTO['group'][] = [];
          const unOrderedGroups: vehicleServiceItemDTO['group'][] = [];

          vehicleResponse.serviceItems.forEach(({ group }) => {
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
      }

      this.errorMessage = null;
      this.isLoading = false;
      this.isError = false;
    } catch (error) {
      if (error && error?.name === 'AbortError') return;
      if (this.errorCallback) this.errorCallback(error.message);
      console.error(error);
      this.setErrorMessage(error.message);
    }
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    this.isError = true;
    this.isLoading = false;
    this.vehicleInformation = null;
    this.errorMessage = message;
  }

  @Method()
  async fetchData(requestedVin: string = this.externalVin, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
    this.setLoadingUi(newValue);
  }

  async setLoadingUi(isLoading: boolean) {
    if (!isLoading) {
      this.progressBar.style.width = '0';
      this.progressBar.style.opacity = '0';
      await new Promise(r => setTimeout(r, 200));
      this.updateProgressBar();
    }
  }

  updateProgressBar() {
    const serviceItems = this.getServiceItems();

    if (!!this.tabs?.length && this.tabs.find(tab => tab.name === this.activeTab) && !this.tabs.find(tab => tab.name === this.activeTab)?.isSequential) {
      this.progressBar.style.width = '0%';
      this.progressBar.style.opacity = '0';
      return;
    }

    if (serviceItems.filter(x => x.status === 'pending').length === 0) {
      if (serviceItems.length === 0 || serviceItems.filter(x => x.status === 'activationRequired').length === serviceItems.length) {
        this.progressBar.style.width = '0%';
        this.progressBar.style.opacity = '0';
      } else {
        this.progressBar.style.width = '100%';
        this.progressBar.style.opacity = '1';
        const claimableItems = this.claimableContentWrapper.getElementsByClassName('claimable-item') as HTMLCollectionOf<HTMLElement>;
        scrollIntoContainerView(claimableItems[claimableItems.length - 1], this.claimableContentWrapper);
      }
    } else {
      const firstPendingItem = serviceItems.find(x => x.status === 'pending');
      const firstPendingItemIndex = serviceItems.indexOf(firstPendingItem) + 1;
      const firstPendingItemRef = this.claimableContentWrapper.getElementsByClassName('claimable-item')[firstPendingItemIndex - 1] as HTMLElement;
      this.progressBar.style.width = (firstPendingItemIndex / serviceItems.length - 1 / (serviceItems.length * 2)) * 100 + '%';
      this.progressBar.style.opacity = this.progressBar.style.width === '0%' ? '0' : '1';

      scrollIntoContainerView(firstPendingItemRef, this.claimableContentWrapper);
    }
  }

  @Watch('vehicleInformation')
  resetProgressBar() {
    setTimeout(() => {
      this.updateProgressBar();
    }, 100);
  }

  @Watch('popupClasses')
  windowScrollListener(newValue) {
    if (newValue) {
      this.scrollListenerRef = () => this.calculatePopupPos(this.el.shadowRoot);
      window.addEventListener('scroll', this.scrollListenerRef);
      this.claimableContentWrapper.addEventListener('scroll', this.scrollListenerRef);
    } else {
      window.removeEventListener('scroll', this.scrollListenerRef);
      this.claimableContentWrapper.removeEventListener('scroll', this.scrollListenerRef);
    }
  }

  onMouseLeave = () => {
    clearTimeout(this.timeoutRef);

    this.showPopup = false;

    this.timeoutRef = setTimeout(() => {
      this.activePopupIndex = null;
    }, 400);
  };

  onMouseEnter = (dynamicClaimItemHeader: HTMLElement, idx: number) => {
    clearTimeout(this.timeoutRef);

    this.activePopupIndex = idx;

    this.timeoutRef = setTimeout(() => {
      const positionRef = dynamicClaimItemHeader.querySelector('.popup-ref') as HTMLElement;

      this.popupPositionRef = positionRef;
      this.calculatePopupPos(this.el.shadowRoot);

      this.showPopup = true;
    }, 50);
  };

  calculatePopupPos(root: ShadowRoot) {
    const popupPositionRef = root.querySelector('.popup-ref') as HTMLElement;

    let { x, y } = popupPositionRef.getBoundingClientRect();
    const popupContainer = popupPositionRef.querySelector('.popup-container') as HTMLElement;

    const { width } = popupContainer.getBoundingClientRect();

    const popupInfo = popupContainer.querySelector('.popup-info') as HTMLElement;

    const windowWidth = window.innerWidth; // Get the viewport's width

    popupContainer.style.top = `${y}px`;
    popupContainer.style.left = `${x - width / 2}px`;

    const offsetFromLeft = x - width / 2; // Distance from left side of the viewport
    const offsetFromRight = windowWidth - (x + width / 2); // Distance from right side of the viewport

    let movingNeeded = 0;
    let horizontalMargin = 16;
    if (offsetFromRight < horizontalMargin) movingNeeded = offsetFromRight - horizontalMargin;
    else if (offsetFromLeft < horizontalMargin) movingNeeded = Math.abs(offsetFromLeft - horizontalMargin);

    popupInfo.style.transform = `translateX(${movingNeeded}px)`;
  }

  removeLoadAnimationClass(event: AnimationEvent) {
    const component = event.target as HTMLDivElement;
    component.classList.remove('load-animation');
  }

  @Method()
  async completeClaim(response: any) {
    const serviceItems = this.getServiceItems();

    const item = this.cachedClaimItem;
    const serviceDataClone = JSON.parse(JSON.stringify(serviceItems));

    const index = serviceItems.indexOf(item);
    const pendingItemsBefore = serviceDataClone.slice(0, index).filter(x => x.status === 'pending');

    serviceDataClone[index].claimable = false;
    serviceDataClone[index].status = 'processed';

    pendingItemsBefore.forEach(function (otherItem) {
      otherItem.status = 'cancelled';
    });

    this.pendingItemHighlighted = false;

    const vehicleDataClone = JSON.parse(JSON.stringify(this.vehicleInformation)) as vehicleLookupDTO;
    vehicleDataClone.serviceItems = serviceDataClone;
    this.vehicleInformation = JSON.parse(JSON.stringify(vehicleDataClone));

    this.showPrintBox = true;
    this.lastSuccessfulClaimResponse = response;
  }

  @Method()
  async claim(item: vehicleServiceItemDTO) {
    const serviceItems = this.getServiceItems();

    const vinDataClone = JSON.parse(JSON.stringify(serviceItems));
    const index = serviceItems.indexOf(item);

    //Find other items before this item that have status 'pending'
    let pendingItemsBefore = vinDataClone.slice(0, index).filter(x => x.status === 'pending') as vehicleServiceItemDTO[];

    this.cachedClaimItem = item;

    if (item.maximumMileage === null) {
      pendingItemsBefore = [];
    }

    this.onMouseLeave();

    this.openRedeem(item, pendingItemsBefore);
  }

  private async handleClaiming() {
    if (this.isDev) {
      this.claimForm.handleClaiming = async ({ document }: ClaimFormPayload) => {
        if (document) {
          this.claimForm.uploadProgress = 0;
          let uploadChunks = 20;
          for (let index = 0; index < uploadChunks; index++) {
            const uploadPercentage = Math.round(((index + 1) / uploadChunks) * 100);

            await new Promise(r => setTimeout(r, 200));

            this.claimForm.setFileUploadProgression(uploadPercentage);
          }
        }

        this.claimForm.quite();
        this.completeClaim({ Success: true, ID: '11223344', PrintURL: 'http://localhost/test/print/1122' });
        this.claimForm.handleClaiming = null;
      };
    } else {
      this.claimForm.handleClaiming = async ({ document, ...payload }: ClaimFormPayload) => {
        try {
          const formData = new FormData();
          formData.append(
            'payload',
            JSON.stringify({
              ...payload,
              vin: this.vehicleInformation.vin,
              saleInformation: this.vehicleInformation.saleInformation,
              serviceItem: this.claimForm.item,
              cancelledServiceItems: this.claimForm.canceledItems,
            }),
          );
          if (document) formData.append('document', document);

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
              this.claimForm.quite();
              this.claimForm.handleClaiming = null;
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
                reject(new Error(`Upload failed with status ${xhr.status}`));
              }
            };

            xhr.onerror = e => {
              console.log(e);

              this.claimForm.quite();
              this.claimForm.handleClaiming = null;
              reject(new Error('Network error'));
            };

            xhr.send(formData);
          });
        } catch (error) {
          console.error(error);
          alert(this.sharedLocales.errors.requestFailedPleaseTryAgainLater);
          this.claimForm.quite();
          this.claimForm.handleClaiming = null;
        }
      };
    }
  }

  private openRedeem(item: vehicleServiceItemDTO, oldItems: vehicleServiceItemDTO[]) {
    const vehicleInformation = this.vehicleInformation as vehicleLookupDTO;

    this.claimForm.vin = vehicleInformation?.vin;
    this.claimForm.item = item;
    this.claimForm.canceledItems = oldItems;

    if (vehicleInformation?.saleInformation?.broker !== null && vehicleInformation?.saleInformation?.broker?.invoiceDate === null)
      this.claimForm.unInvoicedByBrokerName = vehicleInformation?.saleInformation?.broker?.brokerName;
    else this.claimForm.unInvoicedByBrokerName = null;

    this.handleClaiming();
  }

  private getServiceItems = (): vehicleLookupDTO['serviceItems'] => {
    if (!this.vehicleInformation?.serviceItems?.length) return [];

    if (!this.tabs?.length) return this.vehicleInformation?.serviceItems;

    return this.vehicleInformation?.serviceItems.filter(serviceItem => serviceItem?.group?.name === this.activeTab);
  };

  private onActiveTabChange = ({ label }: { label: string; idx: number }) => {
    this.tabAnimationLoading = true;
    clearTimeout(this.tabAnimationTimeoutRef);

    this.tabAnimationTimeoutRef = setTimeout(() => {
      this.activeTab = label;
      this.claimableContentWrapper.scrollLeft = 0;
      setTimeout(() => {
        this.tabAnimationLoading = false;
        this.updateProgressBar();
      }, 100);
    }, 500);
  };

  createPopup(item: vehicleServiceItemDTO) {
    const texts = this.locale;

    return (
      <div dir={this.sharedLocales.direction} class="popup-ref w-0 h-0 bottom-0 flex absolute justify-center">
        <div class="popup-container fixed z-[100]">
          <div
            class={cn('opacity-0 w-full z-[101] flex transition-all duration-[0.4s] relative invisible justify-center translate-y-[-9px]', {
              '!opacity-100 !visible': this.showPopup,
            })}
          >
            <div class="absolute w-0 h-0 border-[10px] border-t-0 !border-b-[#dddddd] border-transparent"></div>
            <div class="mt-[1px] absolute w-0 h-0 border-[10px] border-t-0 !border-b-[#f9f9f9] border-transparent"></div>
          </div>
          <div
            class={cn('popup-info bg-[#f9f9f9] border border-[#ddd] w-auto p-[20px] rounded-[5px] transition-all duration-[0.4s] text-[#282828] opacity-0 invisible', {
              '!opacity-100 !visible': this.showPopup,
            })}
          >
            <table
              class={cn(
                'w-full border-collapse',
                '[&_th]:border-b [&_th]:border-[#ddd] [&_th]:p-[10px] [&_th]:pe-[50px] [&_th]:text-start [&_th]:whitespace-nowrap',
                '[&_td]:border-b [&_td]:border-[#ddd] [&_td]:p-[10px] [&_td]:pe-[50px] [&_td]:text-start [&_td]:whitespace-nowrap',
              )}
            >
              <tbody>
                <tr>
                  <th>{texts.serviceType}</th>
                  <td>{item.type.charAt(0).toUpperCase() + item.type.slice(1)}</td>
                </tr>

                <tr>
                  <th>{texts.activationDate}</th>
                  <td>{item.activatedAt}</td>
                </tr>

                <tr>
                  <th>{texts.expireDate}</th>
                  <td>{item.expiresAt}</td>
                </tr>

                <tr>
                  <th>{texts.claimAt}</th>
                  <td>{item.claimDate}</td>
                </tr>

                <tr>
                  <th>{texts.claimingCompany}</th>
                  <td>{item.companyName}</td>
                </tr>

                <tr>
                  <th>{texts.invoiceNumber}</th>
                  <td>{item.invoiceNumber}</td>
                </tr>

                <tr>
                  <th>{texts.jobNumber}</th>
                  <td>{item.jobNumber}</td>
                </tr>

                <tr>
                  <th>{texts.packageCode}</th>
                  <td>{item.packageCode}</td>
                </tr>
              </tbody>
            </table>

            {item.claimable && (
              <button onClick={() => this.claim(item)} class="claim-button m-auto mt-[15px] w-[80%] justify-center">
                <svg class="size-[30px] duration-200" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <g stroke-width="0"></g>
                  <g stroke-linecap="round" stroke-linejoin="round"></g>
                  <g>
                    <circle cx="12" cy="12" r="8" fill-opacity="0.24"></circle>
                    <path d="M8.5 11L11.3939 13.8939C11.4525 13.9525 11.5475 13.9525 11.6061 13.8939L19.5 6" stroke-width="1.2"></path>
                  </g>
                </svg>
                <span>{texts.claim}</span>
              </button>
            )}
          </div>
        </div>
      </div>
    );
  }

  render() {
    const serviceItems = this.getServiceItems();
    const texts = this.locale;

    const hasInactiveItems = serviceItems.filter(x => x.status === 'activationRequired').length > 0;

    const hideTabs = this.isLoading || this.isError || !this.tabs.length || !serviceItems.length;

    const tabs = this.tabs.map(group => group.name);

    return (
      <Host>
        <vehicle-item-claim-form
          locale={texts.claimForm}
          language={this.language}
          id="vehicle-item-claim-form"
          maximumDocumentFileSizeInMb={this.maximumDocumentFileSizeInMb}
        ></vehicle-item-claim-form>

        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          vin={this.vehicleInformation?.vin}
          direction={this.sharedLocales.direction}
          errorMessage={this.sharedLocales.errors[this.errorMessage] || this.sharedLocales.errors.wildCard}
        >
          <div class="absolute z-10 w-full pt-[16px]">
            <div class={cn('duration-300', { 'translate-y-[-50%] opacity-0': hideTabs })}>
              <shift-tabs activeTabLabel={this.activeTab} changeActiveTab={this.onActiveTabChange} tabs={tabs}></shift-tabs>
            </div>
          </div>
          <div class="relative">
            <div class="flex px-[30px] absolute w-full h-[320px] items-center">
              <div class="h-[10px] translate-y-[-5px] relative bg-[#f2f2f2] border border-[#ddd] rounded-[10px] w-[calc(100%-62px)] items-center justify-around">
                <div class="w-full h-full rounded-[4px] overflow-x-hidden absolute left-0 top-0">
                  <div class="absolute opacity-0 bg-[#1a1a1a] w-[150%] h-full" />
                  <div class="absolute h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)] lane-inc" />
                  <div class="absolute h-full bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)] lane-dec" />
                </div>
              </div>
            </div>

            <div
              dir="ltr"
              class={cn('flex overflow-x-scroll px-[30px] relative h-[320px] items-center transition-all duration-300 claimable-content-wrapper', {
                'hide-scroll': this.tabAnimationLoading,
              })}
            >
              <div
                class={cn('h-[10px] bg-[#f2f2f2] border flex-1 border-[#ddd] rounded-[10px] flex items-center justify-around relative', {
                  'bg-transparent border-transparent': this.isLoading || this.tabAnimationLoading || !serviceItems.length,
                })}
              >
                {serviceItems.map((item: vehicleServiceItemDTO, idx) => {
                  let statusClass = '';

                  if (item.status === 'pending') {
                    if (serviceItems.findIndex(i => i.status === 'pending') === idx) statusClass = item.status;
                  } else statusClass = item.status;

                  return (
                    <div key={item.name} class={cn('claimable-item flex flex-col items-center gap-[5px] font-[14px] min-w-[250px]', statusClass)} onMouseLeave={this.onMouseLeave}>
                      <div
                        onAnimationEnd={this.removeLoadAnimationClass}
                        class={cn(
                          'claimable-item-header load-animation hover:[&>img]:rotate-[360deg] hover:[&>img]:scale-[125%] relative duration-[0.4s] py-[10px] leading-[1em] h-[3em] flex flex-col items-center cursor-pointer',
                          {
                            '!opacity-0 !translate-y-[-5px] !scale-[70%]': this.isLoading || this.tabAnimationLoading,
                          },
                        )}
                        onMouseEnter={event => this.onMouseEnter(event.target as HTMLElement, idx)}
                      >
                        <img class="duration-[0.4s]" src={icons[item.status]} alt="status icon" />
                        <span dir={this.sharedLocales.direction} class="font-bold">
                          {texts[item.status]}
                        </span>
                        {this.activePopupIndex === idx && this.createPopup(item)}
                      </div>
                      <div
                        onAnimationEnd={this.removeLoadAnimationClass}
                        class={cn(
                          'claimable-item-circle load-animation w-[18px] translate-y-[2px] h-[18px] rounded-[50%] bg-[#a1a1a1] border-[5px] border-double border-[#ececec] transition-all duration-[0.4s] z-[1]',
                          {
                            '!opacity-0 !scale-[150%]': this.isLoading || this.tabAnimationLoading,
                          },
                        )}
                      ></div>
                      <p
                        onAnimationEnd={this.removeLoadAnimationClass}
                        class={cn(
                          'claimable-item-footer load-animation transition-all duration-[0.4s] px-[20px] text-center leading-[1.5em] h-[4.5em] overflow-hidden text-ellipsis m-0',
                          {
                            '!opacity-0 !translate-y-[10px] !scale-[70%]': this.isLoading || this.tabAnimationLoading,
                          },
                        )}
                      >
                        {item.name}
                      </p>
                    </div>
                  );
                })}

                <div
                  class={cn(
                    'progress-bar h-[10px] opacity-0 bg-[linear-gradient(to_bottom,_#428bca_0%,_#3071a9_100%)] border border-[#ddd] rounded-[10px] w-0 absolute left-0 transition-all duration-500 z-0',
                    {
                      '!w-0 !opacity-0': this.isLoading || this.tabAnimationLoading,
                    },
                  )}
                ></div>
              </div>

              <div class={cn('absolute w-[90%] left-1/2 ml-[-45%] bottom-[40px] z-[1]', { '!z-[-1]': !(this.vehicleInformation && hasInactiveItems) })}>
                <div
                  class={cn('card warning-card span-entire-1st-row activation-panel', {
                    loading: this.isLoading || this.tabAnimationLoading,
                    visible: hasInactiveItems,
                  })}
                  onAnimationEnd={this.removeLoadAnimationClass}
                >
                  <p class="no-padding flex gap-2">
                    <span class="font-semibold">{texts.warrantyAndServicesNotActivated}</span>
                  </p>

                  <button
                    onClick={() => {
                      if (this.activate) {
                        this.activate(this.vehicleInformation);
                      }
                    }}
                    class="claim-button"
                  >
                    <svg class="size-[30px] duration-200" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <g stroke-width="0"></g>
                      <g stroke-linecap="round" stroke-linejoin="round"></g>
                      <g>
                        <circle cx="12" cy="12" r="8" fill-opacity="0.24"></circle>
                        <path d="M8.5 11L11.3939 13.8939C11.4525 13.9525 11.5475 13.9525 11.6061 13.8939L19.5 6" stroke-width="1.2"></path>
                      </g>
                    </svg>
                    <span>{texts.activateNow}</span>
                  </button>
                </div>
              </div>

              <div class={cn('absolute w-[90%] left-1/2 ml-[-45%] bottom-[40px] z-[1]', { '!z-[-1]': !this.showPrintBox })}>
                <div
                  class={cn('card warning-card span-entire-1st-row activation-panel', {
                    loading: this.isLoading,
                    visible: this.showPrintBox,
                  })}
                  onAnimationEnd={this.removeLoadAnimationClass}
                >
                  <p class="no-padding flex gap-2">
                    <span class="font-semibold">{texts.successFulClaimMessage}</span>
                  </p>

                  <button
                    onClick={() => {
                      if (this.print) {
                        this.print(this.lastSuccessfulClaimResponse);
                      } else {
                        if (this.lastSuccessfulClaimResponse.PrintURL) {
                          window.open(this.lastSuccessfulClaimResponse.PrintURL, '_blank').focus();
                        }
                      }
                    }}
                    class="claim-button dynamic-claim-button"
                  >
                    <svg width="30px" height="30px" viewBox="-5 -5 36 36" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path
                        fill-rule="evenodd"
                        clip-rule="evenodd"
                        d="M17 7H7V6h10v1zm0 12H7v-6h10v6zm2-12V3H5v4H1v8.996C1 17.103 1.897 18 3.004 18H5v3h14v-3h1.996A2.004 2.004 0 0 0 23 15.996V7h-4z"
                        fill="rgb(252, 248, 227)"
                      />
                    </svg>

                    <span>{texts.print}</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
