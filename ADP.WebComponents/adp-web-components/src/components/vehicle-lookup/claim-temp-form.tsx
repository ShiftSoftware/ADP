import { Component, Element, Host, Method, Prop, State, h } from '@stencil/core';
import { MaterialCard } from 'components/components/material-card';
import { AddIcon } from '~assets/add-icon';
import { AlertIcon } from '~assets/alert-icon';
import { AttachIcon } from '~assets/attach-icon';
import { CheckIcon } from '~assets/check-icon';
import { FormSubmitSVG } from '~assets/form-submit-svg';
import cn from '~lib/cn';
import { SharedLocales } from '~lib/get-local-language';
import { ClaimFormType } from '~locales/vehicleLookup/claimableItems/type';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

export type ClaimFormPayload = {
  qrCode?: string;
  document?: File;
  invoice?: string;
  jobNumber?: string;
};

@Component({
  shadow: true,
  tag: 'claim-temp-form',
  styleUrl: 'claim-temp-form.css',
})
export class ClaimTempForm {
  // ====== Start Component Logic
  @Prop() vin?: string;
  @State() uploadProgress = 0;
  @Prop() item?: VehicleServiceItemDTO;
  @Prop() unInvoicedByBrokerName?: string;
  @Prop() maximumDocumentFileSizeInMb: number;
  @Prop() canceledItems?: VehicleServiceItemDTO[] = [];
  @Prop() locale: { sharedLocales: SharedLocales } & ClaimFormType;
  @Prop() handleClaiming?: (payload: ClaimFormPayload) => Promise<void>;

  @State() isOpened: boolean = false;
  @State() isLoading: boolean = false;
  @State() inputs: Record<string, any> = {};
  @State() confirmationStates: Record<string, boolean> = {};
  @State() errors: Record<string, { text: string; show: boolean }> = {};

  @Element() el: HTMLElement;

  @Method()
  async open() {
    this.isOpened = true;

    document.body.style.overflow = 'hidden';

    this.focusInputs();
  }

  @Method()
  async close() {
    if (this.isLoading) return;

    this.isOpened = false;
    document.body.style.overflow = 'auto';

    await new Promise(r => setTimeout(r, 500));

    this.resetState();
  }

  @Method()
  async setFileUploadProgression(uploadPercentage: number) {
    this.uploadProgress = uploadPercentage;
  }

  setInputValue = (inputName: string, value: any) => {
    this.inputs = { ...this.inputs, [inputName]: value };
  };

  onFileUploaderChange = (event: Event) => {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    input.value = '';

    if (!file) return;

    this.setInputValue('document', file);

    if (this.inputs.document.size > this.maximumDocumentFileSizeInMb * 1024 * 1024) {
      this.errors.document = {
        show: true,
        text: 'documentLimitError',
      };
      return;
    } else {
      if (this.errors?.document) this.errors = { ...this.errors, document: { text: this.errors.document?.text, show: false } };
      if (this.item?.claimingMethodEnum === 'ClaimByScanningQRCode') {
        if (this.inputs?.qrCode) this.submit();
        else this.focusInputs();
      }
    }
  };

  onDocumentButtonClicked = () => {
    const documentUploader = this.el.shadowRoot.querySelector('.document-uploader') as HTMLInputElement;

    if (documentUploader) documentUploader.click();
  };

  focusInputs = () => {
    setTimeout(() => {
      const inputs = this.el.shadowRoot.querySelectorAll('input[type="text"]') as NodeListOf<HTMLInputElement>;

      console.log(inputs);

      const firstEnabled = Array.from(inputs).find(input => !input.disabled && !input.readOnly);

      if (firstEnabled) firstEnabled.focus();
    }, 50);
  };

  submit = async () => {};

  private onConfirmCheckboxChange = (confirmationName: string) => (event: Event) => {
    const target = event.target as HTMLInputElement;

    this.confirmationStates = { ...this.confirmationStates, [confirmationName]: target.checked };

    if (this.getIsFormConfirmed) this.focusInputs();
  };

  private getIsFormConfirmed = (): boolean => {
    if (!!this.canceledItems?.length && !this.confirmationStates?.canceledItems) return false;

    if (!!this.unInvoicedByBrokerName && !this.confirmationStates?.unInvoicedByBrokerName) return false;
    return true;
  };

  resetState = () => {
    this.item = null;
    this.errors = {};
    this.inputs = {};
    this.isLoading = false;
    this.canceledItems = [];
    this.confirmationStates = {};
    this.unInvoicedByBrokerName = null;
  };

  clearFile = (event: MouseEvent) => {
    event.stopPropagation();

    this.setInputValue('document', undefined);
    if (this.errors?.document) this.errors = { ...this.errors, document: { text: this.errors.document?.text, show: false } };
  };

  onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter') this.submit();
  };

  // ====== End Component Logic

  render() {
    console.log(this.inputs);

    const isQRScannerForm = this.item?.claimingMethodEnum === 'ClaimByScanningQRCode';

    const disableInputs = !this.getIsFormConfirmed() || this.isLoading;

    // @ts-ignore
    window.a = this;

    return (
      <Host>
        <div
          dir={this.locale.sharedLocales.direction}
          class={cn(
            'min-w-[100dvw] pointer-events-none h-[100dvh] bg-white fixed top-0 left-0 z-[999] flex flex-col transition duration-500 scale-[110%] opacity-0 justify-between p-[50px] box-border overflow-auto',
            {
              'opacity-1 scale-100 pointer-events-auto': this.isOpened,
            },
          )}
        >
          <button
            disabled={this.isLoading}
            onClick={this.close.bind(this)}
            class="disabled:opacity-50 transition absolute right-[50px] duration-500 rounded-full enabled:hover:bg-red-200 enabled:hover:text-red-400 text-black enabled:hover:scale-[150%] enabled:hover:rotate-[135deg] size-[35px] rotate-[45deg]"
          >
            <AddIcon class="stroke-[1.5px]" />
          </button>
          <div class="flex pt-[50px] flex-col h-full gap-[32px] justify-between">
            <div class="flex flex-col gap-[20px]">
              <div class="border overflow-hidden rounded-[6px] border-[#dcdcdc] box-border">
                <div class="text-[16px] h-[50px] font-semibold flex items-center justify-center bg-[#f6f6f6]">{this.vin}</div>

                <div class="overflow-hidden flex p-[22px] [&_div]:min-w-[200px] gap-[32px] flex-wrap justify-center">
                  <MaterialCard title={this.locale.serviceType}>{this.item?.type}</MaterialCard>
                  <MaterialCard title={this.locale.name}>{this.item?.name}</MaterialCard>
                  <MaterialCard title={this.locale.activationDate}>{this.item?.activatedAt}</MaterialCard>
                  <MaterialCard title={this.locale.expireDate}>{this.item?.expiresAt}</MaterialCard>
                  <MaterialCard title={this.locale.packageCode}>{this.item?.packageCode}</MaterialCard>
                </div>
              </div>

              <div class={cn('border overflow-hidden rounded-[6px] border-[#ff9100] box-border', { hidden: !this.canceledItems.length })}>
                <div class="text-[16px] px-[22px] h-[50px] font-semibold flex items-center gap-[8px] bg-[#ff91001a]">
                  <AlertIcon class="size-[25px] text-[#ff9100]" /> {this.locale.warning}
                </div>

                <div class="p-[16px]">
                  <div class="text-[16px]">{this.locale.skipServicesWarning}</div>
                  <ul class="list-disc mt-[16px]">
                    {this.canceledItems.map(({ name }) => (
                      <li class="ms-[50px] text-[16px] mt-[4px]">{name}</li>
                    ))}
                  </ul>

                  <label
                    class={cn(
                      'select-none text-[16px] flex font-semibold group items-center px-[16px] gap-[10px] mt-[20px] h-[36px] transition duration-300 rounded-[6px] cursor-pointer text-[#663c00] bg-[#ff91001a] border border-[#ffd28f]',
                      {
                        'hover:bg-[#fff8e1]': !this.isLoading,
                        'opacity-50 pointer-events-none': this.isLoading,
                      },
                    )}
                  >
                    <input type="checkbox" class="peer sr-only" disabled={this.isLoading} onChange={this.onConfirmCheckboxChange('canceledItems')} />
                    <div class="size-[20px] rounded border border-[#ffb74d] transition duration-300 text-transparent peer-checked:text-white peer-checked:bg-[#ff9100] peer-checked:border-[#ff9100] flex items-center justify-center">
                      <CheckIcon class="size-[18px]" />
                    </div>
                    {this.locale.confirmSkipServices}
                  </label>
                </div>
              </div>

              <div class={cn('border overflow-hidden rounded-[6px] border-[#ff9100] box-border', { hidden: !this.unInvoicedByBrokerName })}>
                <div class="text-[16px] px-[22px] h-[50px] font-semibold flex items-center gap-[8px] bg-[#ff91001a]">
                  <AlertIcon class="size-[25px] text-[#ff9100]" /> {this.locale.warning}
                </div>

                <div class="p-[16px]">
                  <div class="flex gap-[8px] text-[16px]">
                    {this.locale.notInvoiced} <b>{this.unInvoicedByBrokerName}</b>
                  </div>

                  <label
                    class={cn(
                      'select-none text-[16px] flex font-semibold group items-center px-[16px] gap-[10px] mt-[20px] h-[36px] transition duration-300 rounded-[6px] cursor-pointer text-[#663c00] bg-[#ff91001a] border border-[#ffd28f]',
                      {
                        'hover:bg-[#fff8e1]': !this.isLoading,
                        'opacity-50 pointer-events-none': this.isLoading,
                      },
                    )}
                  >
                    <input type="checkbox" class="peer sr-only" disabled={this.isLoading} onChange={this.onConfirmCheckboxChange('unInvoicedByBrokerName')} />
                    <div class="size-[20px] rounded border border-[#ffb74d] transition duration-300 text-transparent peer-checked:text-white peer-checked:bg-[#ff9100] peer-checked:border-[#ff9100] flex items-center justify-center">
                      <CheckIcon class="size-[18px]" />
                    </div>
                    {this.locale.confirmNotInvoiced}
                  </label>
                </div>
              </div>
            </div>
            <div
              class={cn('flex flex-col gap-[20px] p-[50px] border-[2px] border-[#3071a9] transition duration-500 items-center rounded-[8px] shadow-lg bg-[#f4f4f4]', {
                'border-[#f4f4f4]': this.isLoading,
              })}
            >
              <div class="relative w-[200px] h-[140px]">
                <div class={cn('absolute size-full transition-all duration-500 flex flex-col items-center gap-[12px] left-0 top-0', { 'left-full opacity-0': this.isLoading })}>
                  <FormSubmitSVG class="size-[100px] fill-[#3071a9] text-[#3071a9]" />
                  <div class="text-[16px] text-[#3071a9] font-semibold">{isQRScannerForm ? this.locale.scanTheVoucher : this.locale.enterServiceInfo}</div>
                </div>

                <div
                  class={cn('absolute size-full opacity-0 transition-all duration-500 -left-full flex flex-col gap-[12px] items-center', { 'left-0 opacity-100': this.isLoading })}
                >
                  <div class={cn('relative size-[100px] flex items-center justify-center')}>
                    <div class="lds-ripple" />
                    <div class="lds-ripple" />
                  </div>
                  {this.item?.showDocumentUploader && this.inputs['documents'] ? (
                    <div class="relative border-[#3071a9] text-[#3071a9] text-[16px] w-[300px] border-[2px] font-semibold h-[30px] overflow-hidden rounded-full flex items-center justify-center">
                      {this.locale.processing} {`${this.uploadProgress}%`}
                      <div
                        style={{
                          width: `${this.uploadProgress}%`,
                          background: `linear-gradient(to right, rgba(219, 234, 254, ${Math.min(0.1 + this.uploadProgress / 100, 1)}), rgba(191, 219, 254, ${Math.min(0.15 + this.uploadProgress / 100, 1)}), rgba(147, 197, 253, ${Math.min(0.2 + this.uploadProgress / 100, 1)}))`,
                        }}
                        class="absolute left-0 top-0 h-full -z-10 transition-[width] duration-700 ease-out"
                      />
                    </div>
                  ) : (
                    <div class="text-[16px] text-[#3071a9] font-semibold">{this.locale.processing}</div>
                  )}
                </div>
              </div>

              {this.item?.showDocumentUploader && (
                <div class="flex mb-[16px] flex-col">
                  <input class="document-uploader" disabled={this.isLoading} onChange={this.onFileUploaderChange} type="file" accept="image/*,application/pdf" hidden />
                  <div
                    onClick={this.onDocumentButtonClicked}
                    class={cn(
                      'overflow-hidden flex items-center w-fit mx-auto cursor-pointer gap-[16px] ps-[8px] h-[32px] transition duration-300 text-white bg-[#275e8f] active:bg-[#223f57] hover:bg-[#3071a9] rounded-[5px]',
                      { 'pointer-events-none bg-[#d5d5d5] cursor-default': disableInputs },
                    )}
                  >
                    <div title={this.inputs?.document?.name || this.locale.document} class="max-w-[200px] truncate whitespace-nowrap overflow-hidden text-ellipsis">
                      {this.inputs?.document?.name || this.locale.document}
                      {this?.item?.documentUploaderIsRequired && !this.inputs?.document && <span class={cn('ps-[4px] text-red-500', { 'text-white': disableInputs })}>*</span>}
                    </div>
                    <button
                      onClick={this.clearFile}
                      class={cn('overflow-hidden transition duration-300 hover:bg-red-600 relative cursor-pointer flex items-center justify-center size-[32px]', {
                        'pointer-events-none cursor-default': !this.inputs?.document,
                      })}
                    >
                      <AttachIcon class={cn('size-[24px] absolute transition duration-500', { '-translate-y-[32px]': !!this.inputs?.document })} />
                      <AddIcon class={cn('size-[24px] rotate-[45deg] absolute transition translate-y-[32px] duration-500', { 'translate-y-0': !!this.inputs?.document })} />
                    </button>
                  </div>
                  <flexible-container isOpened={!!this.errors?.document?.show}>
                    <div class="text-red-700 w-fit mx-auto pt-[8px]">
                      {this.errors?.document?.text === 'documentLimitError'
                        ? this.locale.documentLimitError + `${this.maximumDocumentFileSizeInMb}Mb`
                        : this.locale.documentRequiredError || this.locale.sharedLocales.errors.wildCard}
                    </div>
                  </flexible-container>
                </div>
              )}

              {isQRScannerForm && (
                <div class="overflow-hidden w-full">
                  <input
                    dir="ltr"
                    autofocus
                    type="text"
                    spellcheck="false"
                    autocomplete="off"
                    disabled={disableInputs}
                    onBlur={this.focusInputs}
                    onKeyDown={this.onKeyDown}
                    value={this.inputs?.qrCode}
                    placeholder={this.locale.qrCode}
                    onInput={event => this.setInputValue('qrCode', (event.target as HTMLInputElement).value)}
                    class="bg-transparent flex-1 outline-none border-b border-[#ddd] text-[#424242] w-full text-center p-[7px] text-[16px] transition-all duration-300 focus:border-[#3071a9] focus:scale-110 focus:-translate-y-[8px]"
                  />
                </div>
              )}

              {!isQRScannerForm && (
                <div class="flex w-full overflow-hidden gap-[75px]">
                  <input
                    dir="ltr"
                    autofocus
                    type="text"
                    spellcheck="false"
                    autocomplete="off"
                    disabled={disableInputs}
                    onKeyDown={this.onKeyDown}
                    value={this.inputs?.invoice}
                    placeholder={this.locale.invoice}
                    onInput={event => this.setInputValue('invoice', (event.target as HTMLInputElement).value)}
                    class="bg-transparent flex-1 outline-none border-b border-[#ddd] text-[#424242] w-full text-center p-[7px] text-[16px] transition-all duration-300 focus:border-[#3071a9] focus:scale-110 focus:-translate-y-[8px]"
                  />

                  <input
                    dir="ltr"
                    type="text"
                    spellcheck="false"
                    autocomplete="off"
                    disabled={disableInputs}
                    onKeyDown={this.onKeyDown}
                    value={this.inputs?.jobNumber}
                    placeholder={this.locale.jobNumber}
                    onInput={event => this.setInputValue('jobNumber', (event.target as HTMLInputElement).value)}
                    class="bg-transparent flex-1 outline-none border-b border-[#ddd] text-[#424242] w-full text-center p-[7px] text-[16px] transition-all duration-300 focus:border-[#3071a9] focus:scale-110 focus:-translate-y-[8px]"
                  />
                </div>
              )}
            </div>
          </div>
        </div>
      </Host>
    );
  }
}
