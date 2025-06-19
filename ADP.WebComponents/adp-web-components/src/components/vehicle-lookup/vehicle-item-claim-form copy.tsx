// @ts-nocheck

import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';
import { VehicleSaleInformation } from '~types/generated/vehicle-lookup/vehicle-sale-information';

import cn from '~lib/cn';

import { claimFormSchema, ClaimFormType } from '~locales/vehicleLookup/claimableItems/type';

@Component({
  shadow: true,
  tag: 'vehicle-item-claim-form-r',
  styleUrl: 'vehicle-item-claim-form.css',
})
export class VehicleItemClaimFormR {
  //@Prop() handleQrChanges?: (code: string) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;

  @Prop() locale: ClaimFormType = claimFormSchema.getDefault();

  @State() claimViaBarcodeScanner: boolean = false;

  @State() internalVin?: string = '';
  @State() isLoading: boolean = false;
  @State() isOpened?: boolean = false;
  @State() isDocumentError: boolean = false;
  @State() selectedFile: File | null = null;
  @State() internalItem?: VehicleServiceItemDTO = null;
  @State() confirmServiceCancellation: boolean = false;
  @State() confirmUnInvoicedTBPVehicles: boolean = false;
  @State() internalCanceledItem?: VehicleServiceItemDTO[] = null;
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

  @Watch('item')
  async changeInternalItem(newItem) {
    this.isOpened = !!newItem;
    if (newItem) this.internalItem = newItem;

    this.claimViaBarcodeScanner = this.item?.claimingMethodEnum === 'ClaimByScanningQRCode';

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

  onFileUploaderClick = () => {
    this.documentUploader.click();
  };

  render() {
    const texts = this.locale;
    const disableInput = !this.confirmServiceCancellation || !this.confirmUnInvoicedTBPVehicles || this.isLoading;

    return (
      <Host>
        <div
          dir={this.sharedLocales.direction}
          class={cn('dynamic-claim-processor min-w-[100dvw] h-[100dvh] flex flex-col justify-between p-[50px] box-border overflow-auto', this?.isOpened && 'active')}
        >
          <div style={{ flex: '1', width: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-evenly' }}>
            <div class={cn('dynamic-claim-processor-progress', disableInput && 'disabled')}>
              <div id="scan-invoice-step" class={cn('dynamic-claim-processor-progress-step', this.isLoading && 'processing')}>
                <div class="relative size-[130px] m-auto mt-[15px] flex justify-center"></div>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
