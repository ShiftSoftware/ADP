import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { LanguageKeys } from '~types/locale';
import { vehicleServiceItemDTO } from '~types/vehicleLookup/vehicleServiceItemDTO';
import { vehicleSaleInformation } from '~types/vehicleLookup/vehicleSaleInformation';

import cn from '~lib/cn';
import { getSharedLocal, SharedLocales, sharedLocalesSchema } from '~lib/get-local-language';

import { claimFormSchema, ClaimFormType } from '~locales/vehicleLookup/claimableItems/type';

export type ClaimFormPayload = {
  vin: string;
  qrCode?: string;
  document?: File;
  invoice?: string;
  jobNumber?: string;
  serviceItem: vehicleServiceItemDTO;
  saleInformation: vehicleSaleInformation;
  cancelledServiceItems: vehicleServiceItemDTO[];
};

@Component({
  shadow: true,
  tag: 'vehicle-item-claim-form',
  styleUrl: 'vehicle-item-claim-form.css',
})
export class VehicleItemClaimForm {
  @Prop() vin?: string = '';
  @Prop() language: LanguageKeys = 'en';
  @Prop() item?: vehicleServiceItemDTO = null;
  @Prop() maximumDocumentFileSizeInMb: number;
  @Prop() unInvoicedByBrokerName?: string = null;
  @Prop() canceledItems?: vehicleServiceItemDTO[] = null;
  //@Prop() handleQrChanges?: (code: string) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() handleClaiming?: (payload: ClaimFormPayload) => void;
  @Prop() locale: ClaimFormType = claimFormSchema.getDefault();

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();
  @State() claimViaBarcodeScanner: vehicleServiceItemDTO['claimingMethodEnum'] = 'claimByScanningQRCode';

  @State() internalVin?: string = '';
  @State() uploadProgress: number = 0;
  @State() isLoading: boolean = false;
  @State() isOpened?: boolean = false;
  @State() isDocumentError: boolean = false;
  @State() selectedFile: File | null = null;
  @State() internalItem?: vehicleServiceItemDTO = null;
  @State() confirmServiceCancellation: boolean = false;
  @State() confirmUnInvoicedTBPVehicles: boolean = false;
  @State() internalCanceledItem?: vehicleServiceItemDTO[] = null;
  @State() documentError: 'documentLimitError' | 'documentRequiredError' = 'documentRequiredError';

  @State() readyToClaim: boolean = false;
  @State() qrCode?: string = null;
  @State() invoice?: string = null;
  @State() job?: string = null;

  @Element() el: HTMLElement;

  qrInput?: HTMLInputElement;
  invoiceInput?: HTMLInputElement;
  jobInput?: HTMLInputElement;
  dynamicClaimProcessor: HTMLElement;

  private documentButton: HTMLButtonElement;
  private documentUploader: HTMLInputElement;

  closeModalListenerRef: (event: KeyboardEvent) => void;

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    this.sharedLocales = await getSharedLocal(newLanguage);
  }

  @Watch('isLoading')
  onIsLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  async componentDidLoad() {
    this.dynamicClaimProcessor = this.el.shadowRoot.querySelector('.dynamic-claim-processor');
    if (this.unInvoicedByBrokerName === null) this.confirmUnInvoicedTBPVehicles = true;
    else this.confirmUnInvoicedTBPVehicles = false;
  }

  async componentDidRender() {
    this.qrInput = this.el.shadowRoot.querySelector('.dynamic-claim-processor #qr-input');
    this.invoiceInput = this.el.shadowRoot.querySelector('.dynamic-claim-processor #invoice-input');
    this.jobInput = this.el.shadowRoot.querySelector('.dynamic-claim-processor #job-input');
    this.registerFileUploader();
  }

  @Watch('canceledItems')
  async changeInternalCanceledItem(newInternalCanceledItem) {
    this.internalCanceledItem = newInternalCanceledItem;

    if (this.internalCanceledItem.length > 0) {
      this.confirmServiceCancellation = false;
    } else {
      this.confirmServiceCancellation = true;
    }
  }

  @Watch('unInvoicedByBrokerName')
  async changeConfirmUnInvoicedTBPVehicles(newUnInvoicedByBrokerName) {
    if (newUnInvoicedByBrokerName === null) {
      this.confirmUnInvoicedTBPVehicles = true;
    } else {
      this.confirmUnInvoicedTBPVehicles = false;
    }
  }

  @Watch('vin')
  async changeInternalVin(newInternalVin) {
    this.internalVin = newInternalVin;
  }

  @Watch('item')
  async changeInternalItem(newItem) {
    this.isOpened = !!newItem;
    if (newItem) this.internalItem = newItem;

    this.claimViaBarcodeScanner = this.item?.claimingMethodEnum;

    if (newItem) {
      this.closeModalListenerRef = (event: KeyboardEvent) => event.key === 'Escape' && this.quite();

      window.addEventListener('keydown', this.closeModalListenerRef);

      await new Promise(r => setTimeout(r, 300));

      if (this.qrInput) this.qrInput.focus();

      if (this.invoiceInput) this.invoiceInput.focus();
    } else {
      window.removeEventListener('keydown', this.closeModalListenerRef);
      this.quite();
    }
  }

  closeModal = async () => {
    this.isOpened = false;
    this.item = null;
    await new Promise(r => setTimeout(r, 500));
    this.canceledItems = [];
    this.unInvoicedByBrokerName = null;

    if (this.claimViaBarcodeScanner) {
      this.qrInput.value = '';
      this.qrInput.readOnly = false;
    } else {
      this.invoiceInput.value = '';
      this.invoiceInput.readOnly = false;

      this.jobInput.value = '';
      this.jobInput.readOnly = false;
    }

    this.qrCode = null;
    this.invoice = null;
    this.job = null;
    this.readyToClaim = false;

    this.isLoading = false;
  };

  @Method()
  async quite() {
    this.closeModal();
  }

  @Method()
  async getQrValue() {
    return this.invoiceInput.value;
  }

  inputKeyDown = async (event: KeyboardEvent) => {
    if (event === null || event.key === 'Enter') {
      if (!this.confirmServiceCancellation || !this.confirmUnInvoicedTBPVehicles) return;

      if (event !== null) event?.preventDefault();

      if (!this?.handleClaiming) return;

      if (!this.readyToClaim) return;

      if (this.item?.showDocumentUploader) {
        if (this.selectedFile && this.selectedFile.size > this.maximumDocumentFileSizeInMb * 1024 * 1024) {
          this.documentError = 'documentLimitError';
          this.isDocumentError = true;

          return;
        }

        if (this.item?.documentUploaderIsRequired && !this.selectedFile) {
          this.documentError = 'documentRequiredError';
          this.isDocumentError = true;
          return;
        }
      }

      this.readyToClaim = false;

      if (this.claimViaBarcodeScanner) {
        this.qrInput.readOnly = true;
      } else {
        this.invoiceInput.readOnly = true;
        this.jobInput.readOnly = true;
      }

      this.isLoading = true;

      await this.handleClaiming({ jobNumber: this.job, invoice: this.invoice, qrCode: this.qrCode, document: this.selectedFile } as ClaimFormPayload);

      this.isLoading = false;
      this.readyToClaim = true;
    }
  };

  updateReadyToClaim() {
    this.readyToClaim = false;

    if (this.claimViaBarcodeScanner) {
      if (this.qrCode?.trim().length > 0) this.readyToClaim = true;
    } else {
      if (this.invoice?.trim().length > 0 && this.job?.trim().length > 0) this.readyToClaim = true;
    }
  }

  @Watch('isOpened')
  async onOpenChange(newOpenState: boolean) {
    document.body.style.overflow = newOpenState ? 'hidden' : 'auto';
    if (newOpenState) {
      this.uploadProgress = 0;
      this.selectedFile = null;
      this.isDocumentError = false;
    }
  }

  registerFileUploader = () => {
    this.documentButton?.removeEventListener('click', this.onFileUploaderClick);
    this.documentUploader?.removeEventListener('change', this.onFileUploaderChange);

    this.documentButton = this.el.shadowRoot.querySelector('.document-button');
    this.documentUploader = this.el.shadowRoot.querySelector('.document-uploader');

    if (!this.documentButton || !this.documentUploader) return;

    this.documentButton.addEventListener('click', this.onFileUploaderClick);
    this.documentUploader.addEventListener('change', this.onFileUploaderChange);
  };

  onFileUploaderClick = () => {
    this.documentUploader.click();
  };

  onFileUploaderChange = (event: Event) => {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    this.documentUploader.value = '';

    if (!file) return;

    this.selectedFile = file;

    if (this.selectedFile.size > this.maximumDocumentFileSizeInMb * 1024 * 1024) {
      this.documentError = 'documentLimitError';
      this.isDocumentError = true;
      return;
    } else {
      this.isDocumentError = false;
      if (this.claimViaBarcodeScanner) {
        // @ts-ignore
        this.inputKeyDown({ key: 'Enter', preventDefault: () => {} });
      }
    }
  };

  clearFile = (event: MouseEvent) => {
    event.stopPropagation();

    this.selectedFile = null;
    this.isDocumentError = false;
  };

  @Method()
  async setFileUploadProgression(uploadPercentage: number) {
    this.uploadProgress = uploadPercentage;
  }

  render() {
    const texts = this.locale;
    const disableInput = !this.confirmServiceCancellation || !this.confirmUnInvoicedTBPVehicles || this.isLoading;

    return (
      <Host>
        <div
          dir={this.sharedLocales.direction}
          class={cn('dynamic-claim-processor min-w-[100dvw] h-[100dvh] flex flex-col justify-between p-[50px] box-border overflow-auto', this?.isOpened && 'active')}
        >
          <svg id="dynamic-claim-processor-close-icon" onClick={this.closeModal} viewBox="-0.5 0 25 25" fill="none" xmlns="http://www.w3.org/2000/svg">
            <g id="SVGRepo_bgCarrier" stroke-width="0"></g>
            <g id="SVGRepo_tracerCarrier" stroke-linecap="round" stroke-linejoin="round"></g>
            <g id="SVGRepo_iconCarrier">
              <path d="M3 21.32L21 3.32001" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
              <path d="M3 3.32001L21 21.32" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
            </g>
          </svg>

          <div class="p-0 border border-[#dcdcdc] dynamic-claim-processor-info-box box-border">
            <div class="dynamic-claim-processor-info-box-header">
              <strong class="dynamic-claim-header-vin">{this?.internalVin}</strong>
            </div>

            <div class="dynamic-claim-processor-info-box-body">
              <table class="border-separate">
                <tr>
                  <th>{texts.serviceType}</th>
                  <td>{this?.internalItem?.type}</td>

                  <th>{texts.name}</th>
                  <td>{this?.internalItem?.name}</td>
                </tr>

                <tr>
                  <th>{texts.activationDate}</th>
                  <td>{this?.internalItem?.activatedAt}</td>

                  <th>{texts.expireDate}</th>
                  <td>{this?.internalItem?.expiresAt}</td>
                </tr>

                <tr>
                  <th>{texts.packageCode}</th>
                  <td>{this?.internalItem?.packageCode}</td>
                </tr>
              </table>
            </div>
          </div>

          <div style={{ flex: '1', width: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-evenly' }}>
            {this?.internalCanceledItem && Array.isArray(this.internalCanceledItem) && !!this.internalCanceledItem.length && (
              <div class="admonition warning">
                <p class="admonition-title">{texts.warning}</p>
                <div style={{ padding: '0 30px' }}>
                  <p class="my-4">{texts.skipServicesWarning}</p>
                  <ul class="dynamic-claim-processor-to-be-cancelled-list warning-ul my-4 list-disc">
                    {this.internalCanceledItem.map(({ name }) => (
                      <li id={name}>{name}</li>
                    ))}
                  </ul>

                  <div class={cn('cancel-confirmation-box transition duration-300', { 'pointer-events-none select-none opacity-50': this.isLoading })}>
                    <label>
                      <input
                        type="checkbox"
                        disabled={this.isLoading}
                        class="confirm-cancellation-input m-[3px] ml-1"
                        onChange={() => {
                          this.confirmServiceCancellation = !this.confirmServiceCancellation;
                        }}
                      />
                      {texts.confirmSkipServices}
                    </label>
                  </div>
                </div>
              </div>
            )}

            {true && this.unInvoicedByBrokerName && (
              <div class="admonition warning">
                <p class="admonition-title">{texts.warning}</p>
                <div style={{ padding: '0 30px' }}>
                  <p class="my-4">{texts.notInvoiced}</p>
                  <div>
                    <strong>{this.unInvoicedByBrokerName}</strong>
                  </div>

                  <div class={cn('cancel-confirmation-box transition duration-300', { 'pointer-events-none select-none opacity-50': this.isLoading })}>
                    <label>
                      <input
                        type="checkbox"
                        disabled={this.isLoading}
                        class="confirm-cancellation-input m-[3px] ml-1"
                        onChange={() => {
                          this.confirmUnInvoicedTBPVehicles = !this.confirmUnInvoicedTBPVehicles;
                        }}
                      />
                      {texts.confirmNotInvoiced}
                    </label>
                  </div>
                </div>
              </div>
            )}

            <div class={cn('dynamic-claim-processor-progress', disableInput && 'disabled')}>
              <div id="scan-invoice-step" class={cn('dynamic-claim-processor-progress-step', this.isLoading && 'processing')}>
                <div class="relative size-[130px] m-auto mt-[15px] flex justify-center">
                  <div class="absolute scan-invoice-wrapper flex flex-col gap-[4px] justify-center items-center">
                    <svg class="size-[100px]" fill="#3071a9" version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" stroke="#ffffff">
                      <g id="SVGRepo_bgCarrier" stroke-width="0"></g>
                      <g id="SVGRepo_tracerCarrier" stroke-linecap="round" stroke-linejoin="round"></g>
                      <g id="SVGRepo_iconCarrier">
                        <g>
                          <g>
                            <path d="M354.338,34.205h-61.71c-3.354-9.93-12.752-17.102-23.8-17.102h-17.547C243.359,6.451,230.813,0,217.521,0 s-25.839,6.451-33.76,17.102h-17.547c-11.048,0-20.446,7.172-23.8,17.102H80.701c-18.566,0-33.67,15.105-33.67,33.67v359.148 c0,18.566,15.105,33.67,33.67,33.67h171.023c4.427,0,8.017-3.589,8.017-8.017c0-4.427-3.589-8.017-8.017-8.017H80.701 c-9.725,0-17.637-7.912-17.637-17.637V67.875c0-9.725,7.912-17.637,17.637-17.637h60.394v26.188c0,4.427,3.589,8.017,8.017,8.017 H285.93c4.427,0,8.017-3.589,8.017-8.017V50.238h60.391c9.725,0,17.637,7.912,17.637,17.637v230.881 c0,4.427,3.589,8.017,8.017,8.017c4.427,0,8.017-3.589,8.017-8.017V67.875C388.008,49.309,372.904,34.205,354.338,34.205z M277.914,68.409H157.129V42.252c0-0.011,0.001-0.02,0.001-0.031c0-0.005-0.001-0.011-0.001-0.015 c0.009-5.004,4.08-9.071,9.085-9.071h21.846c2.854,0,5.493-1.517,6.929-3.985c4.781-8.213,13.204-13.117,22.532-13.117 s17.751,4.904,22.532,13.117c1.435,2.467,4.075,3.985,6.929,3.985h21.846c4.999,0,9.067,4.059,9.085,9.055 c0,0.011-0.001,0.02-0.001,0.031c0,0.021,0.003,0.041,0.003,0.062V68.409z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M251.722,410.455H97.269V84.443h17.64c4.427,0,8.017-3.589,8.017-8.017s-3.589-8.017-8.017-8.017H89.253 c-4.427,0-8.017,3.589-8.017,8.017v342.046c0,4.427,3.589,8.017,8.017,8.017h162.47c4.427,0,8.017-3.589,8.017-8.017 C259.739,414.044,256.15,410.455,251.722,410.455z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M345.787,68.409h-25.653c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h17.637v214.311 c0,4.427,3.589,8.017,8.017,8.017s8.017-3.589,8.017-8.017V76.426C353.804,71.999,350.214,68.409,345.787,68.409z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M371.44,324.944c-51.572,0-93.528,41.956-93.528,93.528S319.869,512,371.44,512s93.528-41.956,93.528-93.528 S423.012,324.944,371.44,324.944z M371.44,495.967c-42.731,0-77.495-34.764-77.495-77.495s34.764-77.495,77.495-77.495 s77.495,34.764,77.495,77.495S414.172,495.967,371.44,495.967z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M217.607,34.205h-0.086c-4.427,0-7.974,3.589-7.974,8.017c0,4.427,3.631,8.017,8.059,8.017 c4.427,0,8.017-3.589,8.017-8.017C225.624,37.794,222.035,34.205,217.607,34.205z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M419.865,387.15c-3.13-3.131-8.207-3.131-11.337,0l-54.19,54.19l-28.536-28.536c-3.131-3.131-8.207-3.131-11.337,0 c-3.131,3.131-3.131,8.207,0,11.337l34.205,34.205c1.565,1.565,3.617,2.348,5.668,2.348s4.103-0.782,5.668-2.348l59.858-59.858 C422.995,395.356,422.995,390.28,419.865,387.15z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M182.781,177.309v-32.338c11.295,1.934,18.171,7.688,18.171,11.889c0,4.427,3.589,8.017,8.017,8.017 s8.017-3.589,8.017-8.017c0-14.171-14.492-25.621-34.205-28.092v-1.036c0-4.427-3.589-8.017-8.017-8.017s-8.017,3.589-8.017,8.017 v1.036c-19.713,2.47-34.205,13.92-34.205,28.092c0,20.074,18.829,27.788,34.205,32.461v32.338 c-11.295-1.934-18.171-7.688-18.171-11.889c0-4.427-3.589-8.017-8.017-8.017s-8.017,3.589-8.017,8.017 c0,14.171,14.492,25.621,34.205,28.092v1.036c0,4.427,3.589,8.017,8.017,8.017s8.017-3.589,8.017-8.017v-1.036 c19.713-2.47,34.205-13.92,34.205-28.092C216.985,189.697,198.157,181.982,182.781,177.309z M166.747,172.411 c-13.524-4.713-18.171-8.963-18.171-15.551c0-2.535,2.236-5.335,6.134-7.68c3.294-1.982,7.473-3.415,12.037-4.198V172.411z M194.818,217.45c-3.294,1.982-7.473,3.415-12.037,4.198v-27.429c13.524,4.713,18.171,8.963,18.171,15.551 C200.952,212.306,198.716,215.105,194.818,217.45z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M294.48,136.818h-42.756c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h42.756 c4.427,0,8.017-3.589,8.017-8.017S298.908,136.818,294.48,136.818z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M277.378,171.023h-25.653c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h25.653 c4.427,0,8.017-3.589,8.017-8.017S281.805,171.023,277.378,171.023z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M294.48,205.228h-42.756c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h42.756 c4.427,0,8.017-3.589,8.017-8.017S298.908,205.228,294.48,205.228z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M157.662,282.188H140.56c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h17.102 c4.427,0,8.017-3.589,8.017-8.017S162.089,282.188,157.662,282.188z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M157.662,350.597H140.56c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h17.102 c4.427,0,8.017-3.589,8.017-8.017S162.089,350.597,157.662,350.597z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M157.662,316.393H140.56c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h17.102 c4.427,0,8.017-3.589,8.017-8.017S162.089,316.393,157.662,316.393z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M294.48,282.188H191.866c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017H294.48 c4.427,0,8.017-3.589,8.017-8.017S298.908,282.188,294.48,282.188z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M268.827,350.597h-76.96c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017h76.96 c4.427,0,8.017-3.589,8.017-8.017S273.254,350.597,268.827,350.597z"></path>
                          </g>
                        </g>
                        <g>
                          <g>
                            <path d="M294.48,316.393H191.866c-4.427,0-8.017,3.589-8.017,8.017s3.589,8.017,8.017,8.017H294.48 c4.427,0,8.017-3.589,8.017-8.017S298.908,316.393,294.48,316.393z"></path>
                          </g>
                        </g>
                      </g>
                    </svg>
                    <div>{this.claimViaBarcodeScanner ? texts.scanTheVoucher : texts.enterServiceInfo}</div>
                  </div>
                  <div class="loading-wrapper flex flex-col gap-[4px] items-center justify-center absolute">
                    <div class={cn('lds-ripple relative size-[100px] flex items-center justify-center', this.isLoading && 'active')}>
                      <div></div>
                      <div></div>
                    </div>
                    {this.item?.showDocumentUploader && this.selectedFile ? (
                      <div class="relative border-[#3071a9] w-[300px] border-[2px] font-semibold h-[30px] overflow-hidden rounded-full flex items-center justify-center">
                        {texts.processing} {`${this.uploadProgress}%`}
                        <div
                          style={{
                            width: `${this.uploadProgress}%`,
                            background: `linear-gradient(to right, rgba(219, 234, 254, ${Math.min(0.1 + this.uploadProgress / 100, 1)}), rgba(191, 219, 254, ${Math.min(0.15 + this.uploadProgress / 100, 1)}), rgba(147, 197, 253, ${Math.min(0.2 + this.uploadProgress / 100, 1)}))`,
                          }}
                          class="absolute left-0 top-0 h-full -z-10 transition-[width] duration-700 ease-out"
                        />
                      </div>
                    ) : (
                      <div class={cn('lds-ripple-loading', this.isLoading && 'active')}>{texts.processing}</div>
                    )}
                  </div>
                </div>

                <div class={cn('transition-opacity duration-300 ', { 'opacity-50': this.isLoading })}>
                  {this.claimViaBarcodeScanner && (
                    <div class="flex flex-col mt-[50px] px-[75px] gap-[16px]">
                      {this.item?.showDocumentUploader && (
                        <div class="flex mb-[12px] flex-col">
                          <input class="document-uploader" disabled={this.isLoading} type="file" accept="image/*,application/pdf" hidden />
                          <div
                            class={cn(
                              'document-button overflow-hidden flex items-center w-fit mx-auto cursor-pointer gap-[16px] ps-[8px] h-[32px] transition duration-300 text-white bg-[#275e8f] active:bg-[#223f57] hover:bg-[#3071a9] rounded-[5px]',
                              { 'pointer-events-none bg-[#d5d5d5] cursor-default': disableInput },
                            )}
                          >
                            <div title={this.selectedFile?.name || texts.document} class="max-w-[200px] truncate whitespace-nowrap overflow-hidden text-ellipsis">
                              {this.selectedFile?.name || texts.document}
                              {this?.item?.documentUploaderIsRequired && !this.selectedFile && <span class={cn('ps-[4px] text-red-500', { 'text-white': disableInput })}>*</span>}
                            </div>
                            <button
                              onClick={this.clearFile}
                              class={cn('overflow-hidden transition duration-300 hover:bg-red-600 relative cursor-pointer flex items-center justify-center size-[32px]', {
                                'pointer-events-none cursor-default': !this.selectedFile,
                              })}
                            >
                              <svg
                                fill="none"
                                stroke-width="2"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                xmlns="http://www.w3.org/2000/svg"
                                class={cn('size-[24px] absolute transition duration-500', { '-translate-y-[32px]': !!this.selectedFile })}
                              >
                                <path d="m16 6-8.414 8.586a2 2 0 0 0 2.829 2.829l8.414-8.586a4 4 0 1 0-5.657-5.657l-8.379 8.551a6 6 0 1 0 8.485 8.485l8.379-8.551" />
                              </svg>
                              <svg
                                fill="none"
                                stroke-width="2"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                xmlns="http://www.w3.org/2000/svg"
                                class={cn('size-[24px] absolute transition translate-y-[32px] duration-500', { 'translate-y-0': !!this.selectedFile })}
                              >
                                <path d="M18 6 6 18" />
                                <path d="m6 6 12 12" />
                              </svg>
                            </button>
                          </div>
                          <flexible-container isOpened={this.isDocumentError}>
                            <div class="text-red-700 w-fit mx-auto pt-[8px]">
                              {this.documentError === 'documentLimitError'
                                ? texts.documentLimitError + `${this.maximumDocumentFileSizeInMb}Mb`
                                : texts.documentRequiredError || this.sharedLocales.errors.wildCard}
                            </div>
                          </flexible-container>
                        </div>
                      )}
                      <input
                        id="qr-input"
                        dir="ltr"
                        class="dynamic-redeem-input"
                        spellcheck="false"
                        onInput={e => {
                          this.qrCode = (e.target as HTMLInputElement).value;
                          this.updateReadyToClaim();
                        }}
                        onKeyDown={this.inputKeyDown}
                        onBlur={() => {
                          this.qrInput?.focus();
                        }}
                        autocomplete="off"
                        disabled={disableInput}
                        placeholder={texts.qrCode}
                        autofocus
                        style={{ marginTop: '20px', padding: '10px 0px', fontSize: '16px' }}
                      />
                    </div>
                  )}

                  {!this.claimViaBarcodeScanner && (
                    <div>
                      <div style={{ display: 'flex', flexDirection: 'row', gap: '50px', padding: '0 75px' }}>
                        <input
                          id="invoice-input"
                          dir="ltr"
                          class="dynamic-redeem-input"
                          spellcheck="false"
                          onInput={e => {
                            this.invoice = (e.target as HTMLInputElement).value;
                            this.updateReadyToClaim();
                          }}
                          onKeyDown={this.inputKeyDown}
                          autocomplete="off"
                          disabled={disableInput}
                          placeholder={texts.invoice}
                          autofocus
                          style={{ marginTop: '20px', padding: '10px 0px', fontSize: '16px' }}
                        />

                        <input
                          id="job-input"
                          dir="ltr"
                          class="dynamic-redeem-input"
                          spellcheck="false"
                          onInput={e => {
                            this.job = (e.target as HTMLInputElement).value;
                            this.updateReadyToClaim();
                          }}
                          onKeyDown={this.inputKeyDown}
                          autocomplete="off"
                          disabled={disableInput}
                          placeholder={texts.jobNumber}
                          style={{ marginTop: '20px', padding: '10px 0px', fontSize: '16px' }}
                        />
                      </div>

                      <div class="mt-[55px]">
                        {this.item?.showDocumentUploader && (
                          <div class="flex mb-[12px] flex-col">
                            <input class="document-uploader" disabled={this.isLoading} type="file" accept="image/*,application/pdf" hidden />
                            <div
                              class={cn(
                                'document-button overflow-hidden flex items-center w-fit mx-auto cursor-pointer gap-[16px] ps-[8px] h-[32px] transition duration-300 text-white bg-[#275e8f] active:bg-[#223f57] hover:bg-[#3071a9] rounded-[5px]',
                                { 'pointer-events-none bg-[#d5d5d5] cursor-default': disableInput },
                              )}
                            >
                              <div title={this.selectedFile?.name || texts.document} class="max-w-[200px] truncate whitespace-nowrap overflow-hidden text-ellipsis">
                                {this.selectedFile?.name || texts.document}
                                {this?.item?.documentUploaderIsRequired && !this.selectedFile && <span class={cn('ps-[4px] text-red-500', { 'text-white': disableInput })}>*</span>}
                              </div>
                              <button
                                onClick={this.clearFile}
                                class={cn('overflow-hidden transition duration-300 hover:bg-red-600 relative cursor-pointer flex items-center justify-center size-[32px]', {
                                  'pointer-events-none cursor-default': !this.selectedFile,
                                })}
                              >
                                <svg
                                  fill="none"
                                  stroke-width="2"
                                  viewBox="0 0 24 24"
                                  stroke="currentColor"
                                  stroke-linecap="round"
                                  stroke-linejoin="round"
                                  xmlns="http://www.w3.org/2000/svg"
                                  class={cn('size-[24px] absolute transition duration-500', { '-translate-y-[32px]': !!this.selectedFile })}
                                >
                                  <path d="m16 6-8.414 8.586a2 2 0 0 0 2.829 2.829l8.414-8.586a4 4 0 1 0-5.657-5.657l-8.379 8.551a6 6 0 1 0 8.485 8.485l8.379-8.551" />
                                </svg>
                                <svg
                                  fill="none"
                                  stroke-width="2"
                                  viewBox="0 0 24 24"
                                  stroke="currentColor"
                                  stroke-linecap="round"
                                  stroke-linejoin="round"
                                  xmlns="http://www.w3.org/2000/svg"
                                  class={cn('size-[24px] absolute transition translate-y-[32px] duration-500', { 'translate-y-0': !!this.selectedFile })}
                                >
                                  <path d="M18 6 6 18" />
                                  <path d="m6 6 12 12" />
                                </svg>
                              </button>
                            </div>
                            <flexible-container isOpened={this.isDocumentError}>
                              <div class="text-red-700 w-fit mx-auto pt-[8px]">
                                {this.documentError === 'documentLimitError'
                                  ? texts.documentLimitError + `${this.maximumDocumentFileSizeInMb}Mb`
                                  : texts.documentRequiredError || this.sharedLocales.errors.wildCard}
                              </div>
                            </flexible-container>
                          </div>
                        )}
                        <button
                          onClick={() => {
                            this.inputKeyDown(null);
                          }}
                          class={cn('claim-button', 'dynamic-claim-button', (!this?.readyToClaim || disableInput) && 'disabled')}
                        >
                          <svg width="30px" height="30px" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <g stroke-width="0"></g>
                            <g stroke-linecap="round" stroke-linejoin="round"></g>
                            <g>
                              <circle cx="12" cy="12" r="8" fill-opacity="0.24"></circle>
                              <path d="M8.5 11L11.3939 13.8939C11.4525 13.9525 11.5475 13.9525 11.6061 13.8939L19.5 6" stroke-width="1.2"></path>
                            </g>
                          </svg>
                          <span>{texts.claim}</span>
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
