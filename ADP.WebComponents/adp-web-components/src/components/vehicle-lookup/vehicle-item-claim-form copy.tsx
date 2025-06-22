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

  render() {
    const texts = this.locale;
    const disableInput = !this.confirmServiceCancellation || !this.confirmUnInvoicedTBPVehicles || this.isLoading;

    return <Host></Host>;
  }
}
