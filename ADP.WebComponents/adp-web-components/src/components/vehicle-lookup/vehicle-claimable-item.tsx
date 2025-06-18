// @ts-nocheck

import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { scrollIntoContainerView } from '~lib/scroll-into-container-view';
import { ErrorKeys } from '~lib/get-local-language';

import { MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import expiredIcon from './assets/expired.svg';
import pendingIcon from './assets/pending.svg';
import cancelledIcon from './assets/cancelled.svg';
import processedIcon from './assets/processed.svg';
import activationRequiredIcon from './assets/activationRequired.svg';

import { getVehicleLookup, VehicleInformationInterface } from '~api/vehicleInformation';

import { ClaimFormPayload, VehicleItemClaimForm } from './vehicle-item-claim-form';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';

let mockData: MockJson<VehicleLookupDTO> = {};

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
};

@Component({
  shadow: true,
  tag: 'vehicle-claimable-item',
  styleUrl: 'vehicle-claimable-item.css',
})
export class VehicleClaimableItem {
  @Prop() print?: (claimResponse: any) => void;
  @Prop() maximumDocumentFileSizeInMb: number = 30;
  @Prop() claimEndPoint: string = 'api/vehicle/swift-claim';

  @Prop() activate?: (vehicleInformation: VehicleLookupDTO) => void;

  @State() activeTab: string = '';

  @State() showPopup: boolean = false;
  @State() externalVin?: string = null;
  @State() showPrintBox: boolean = false;

  @State() tabAnimationLoading: boolean = false;
  @State() activePopupIndex: null | number = null;
  @State() tabs: VehicleServiceItemDTO['group'][] = [];
  @State() lastSuccessfulClaimResponse: any = null;

  pendingItemHighlighted = false;

  scrollListenerRef: () => void;
  timeoutRef: ReturnType<typeof setTimeout>;
  tabAnimationTimeoutRef: ReturnType<typeof setTimeout>;

  cachedClaimItem: VehicleServiceItemDTO;

  progressBar: HTMLElement;
  popupPositionRef: HTMLElement;
  claimForm: VehicleItemClaimForm;
  claimableContentWrapper: HTMLElement;

  async componentDidLoad() {
    this.claimableContentWrapper = this.el.shadowRoot.querySelector('.claimable-content-wrapper');
    this.claimForm = this.el.shadowRoot.getElementById('vehicle-item-claim-form') as unknown as VehicleItemClaimForm;
    this.progressBar = this.el.shadowRoot.querySelector('.progress-bar');
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

    const vehicleDataClone = JSON.parse(JSON.stringify(this.vehicleInformation)) as VehicleLookupDTO;
    vehicleDataClone.serviceItems = serviceDataClone;
    this.vehicleInformation = JSON.parse(JSON.stringify(vehicleDataClone));

    this.showPrintBox = true;
    this.lastSuccessfulClaimResponse = response;
  }

  @Method()
  async claim(item: VehicleServiceItemDTO) {
    const serviceItems = this.getServiceItems();

    const vinDataClone = JSON.parse(JSON.stringify(serviceItems));
    const index = serviceItems.indexOf(item);

    //Find other items before this item that have status 'pending'
    let pendingItemsBefore = vinDataClone.slice(0, index).filter(x => x.status === 'pending') as VehicleServiceItemDTO[];

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

  private openRedeem(item: VehicleServiceItemDTO, oldItems: VehicleServiceItemDTO[]) {
    const vehicleInformation = this.vehicleInformation as VehicleLookupDTO;

    this.claimForm.vin = vehicleInformation?.vin;
    this.claimForm.item = item;
    this.claimForm.canceledItems = oldItems;

    if (vehicleInformation?.saleInformation?.broker !== null && vehicleInformation?.saleInformation?.broker?.invoiceDate === null)
      this.claimForm.unInvoicedByBrokerName = vehicleInformation?.saleInformation?.broker?.brokerName;
    else this.claimForm.unInvoicedByBrokerName = null;

    this.handleClaiming();
  }

  private getServiceItems = (): VehicleLookupDTO['serviceItems'] => {
    if (!this.vehicleInformation?.serviceItems?.length) return [];

    if (!this.tabs?.length) return this.vehicleInformation?.serviceItems;

    return this.vehicleInformation?.serviceItems.filter(serviceItem => serviceItem?.group?.name === this.activeTab);
  };

  createPopup(item: VehicleServiceItemDTO) {
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

    return (
      <Host>
        <vehicle-item-claim-form
          locale={texts.claimForm}
          language={this.language}
          id="vehicle-item-claim-form"
          maximumDocumentFileSizeInMb={this.maximumDocumentFileSizeInMb}
        ></vehicle-item-claim-form>
      </Host>
    );
  }
}
