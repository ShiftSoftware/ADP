import cn from '~lib/cn';
import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { ClaimFormType } from '~locales/vehicleLookup/claimableItems/type';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { SharedLocales } from '~features/multi-lingual';

import { closeImageViewer, ImageViewer, ImageViewerInterface, openImageViewer } from '~features/image-viewer';

import { AddIcon } from '~assets/add-icon';
import { CheckIcon } from '~assets/check-icon';
import { AttachIcon } from '~assets/attach-icon';
import { PrinterIcon } from '~assets/printer-icon';
import { QrCodeScanIcon } from '~assets/qr-code-scan';
import { TriangleAlertIcon } from '~assets/triangle-alert';
import Eye from '~assets/eye.svg';
import { ItemClaimDTO } from '../../global/types/generated/vehicle-lookup/item-claim-dto';

@Component({
  shadow: true,
  tag: 'vehicle-item-claim-form',
  styleUrl: 'vehicle-item-claim-form.css',
})
export class VehicleItemClaimForm implements ImageViewerInterface {
  // #region Image Viewer Logic

  @State() expandedImage?: string = '';

  originalImage: HTMLImageElement;

  @Watch('expandedImage')
  onImageViewerToggles(newValue: string | null) {
    window.removeEventListener('keydown', this.closeModalListenerRef);

    if (newValue === null) {
      setTimeout(() => {
        this.closeModalListenerRef = this.onWindowKeyDown;

        window.addEventListener('keydown', this.closeModalListenerRef);
      }, 200);
    }
  }

  // #endregion

  // #region Component Logic
  @Prop() vin?: string;
  @Prop() item?: VehicleServiceItemDTO;
  @Prop() maximumDocumentFileSizeInMb: number;
  @Prop() uploadMultipleDocuments?: boolean = true;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() locale: { sharedLocales: SharedLocales } & ClaimFormType;
  @Prop() handleClaiming?: (documents: File[], payload: ItemClaimDTO) => Promise<void>;

  @State() uploadProgress = 0;
  @State() isOpened: boolean = false;
  @State() isLoading: boolean = false;
  @State() compact: boolean = false;
  @State() claimError: string = '';
  @State() confirmationStates: Record<string, boolean> = {};
  @State() errors: Record<string, { text: string; show: boolean }> = {};
  @State() inputs: Record<string, any> = {
    documents: [],
  };

  @Element() el: HTMLElement;

  private closeModalListenerRef: (event: KeyboardEvent) => void;

  /** Full-mode stage padding (18px × 2) — the voucher must fit inside it to stay full-size. */
  private static readonly STAGE_PADDING = 36;

  private voucherResizeObserver?: ResizeObserver;
  private observedVoucherEl?: HTMLElement;
  private suppressMeasure = false;

  private onWindowKeyDown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape') return;

    if (this.claimError) this.dismissError();
    else this.close();
  };

  private onWindowResize = () => this.measureCompact();

  @Method()
  async open() {
    this.isOpened = true;

    document.body.style.overflow = 'hidden';

    this.focusInputs();

    window.removeEventListener('keydown', this.closeModalListenerRef);

    this.closeModalListenerRef = this.onWindowKeyDown;

    window.addEventListener('keydown', this.closeModalListenerRef);

    window.addEventListener('resize', this.onWindowResize);

    // Measure once the opened content has rendered, and again after late layout (fonts).
    requestAnimationFrame(() => this.measureCompact());
    setTimeout(() => this.measureCompact(), 150);
  }

  @Watch('isLoading')
  onIsLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  @Method()
  async close(forceClose: boolean = false) {
    if (this.isLoading && !forceClose) return;

    this.isOpened = false;
    document.body.style.overflow = 'auto';

    window.removeEventListener('resize', this.onWindowResize);

    await new Promise(r => setTimeout(r, 500));

    this.resetState();
    window.removeEventListener('keydown', this.closeModalListenerRef);
  }

  @Method()
  async setFileUploadProgression(uploadPercentage: number) {
    this.uploadProgress = uploadPercentage;
  }

  /**
   * Shows the claim-failure dialog (replaces the old browser alert()).
   * The caller keeps re-throwing after this — the rejection resets the form's
   * loading state and keeps it open for retry.
   */
  @Method()
  async showError(message: string) {
    this.claimError = message || this.locale.sharedLocales.errors.requestFailedPleaseTryAgainLater;

    setTimeout(() => {
      const okButton = this.el.shadowRoot.querySelector('.btn-err-ok') as HTMLButtonElement | null;
      if (okButton) okButton.focus();
    }, 50);
  }

  private dismissError = () => {
    this.claimError = '';

    // Scanner-gun retry flow: put the cursor back in the scan field with the
    // stale code selected so the next scan replaces it.
    if (this.item?.claimingMethodEnum === 'ClaimByScanningQRCode') {
      setTimeout(() => {
        const input = this.el.shadowRoot.querySelector('input[type="text"]') as HTMLInputElement | null;

        if (input && !input.disabled) {
          input.focus();
          input.select();
        }
      }, 50);
    }
  };

  componentDidRender() {
    const voucher = this.el.shadowRoot?.querySelector('.voucher') as HTMLElement | null;

    if (voucher === this.observedVoucherEl) return;

    this.voucherResizeObserver?.disconnect();
    this.observedVoucherEl = voucher || undefined;

    if (!voucher) return;

    if (!this.voucherResizeObserver) {
      this.voucherResizeObserver = new ResizeObserver(() => {
        if (!this.suppressMeasure) this.measureCompact();
      });
    }

    this.voucherResizeObserver.observe(voucher);
  }

  disconnectedCallback() {
    this.voucherResizeObserver?.disconnect();
    this.observedVoucherEl = undefined;
    window.removeEventListener('resize', this.onWindowResize);
    window.removeEventListener('keydown', this.closeModalListenerRef);
  }

  /**
   * Vertical compact is MEASURED, never a fixed media query: render full-size,
   * compare the voucher against the viewport, and squeeze only on overflow.
   * Re-runs on resize/orientation and whenever the voucher resizes (warning
   * images/fonts loading). If even compact overflows, the scrim scrolls.
   */
  private measureCompact = () => {
    if (!this.isOpened) return;

    const scrim = this.el.shadowRoot?.querySelector('.claim-scrim') as HTMLElement | null;
    const voucher = this.el.shadowRoot?.querySelector('.voucher') as HTMLElement | null;

    if (!scrim || !voucher) return;

    this.suppressMeasure = true;

    scrim.classList.remove('compact'); // measure at full size
    const fits = voucher.offsetHeight + VehicleItemClaimForm.STAGE_PADDING <= scrim.clientHeight;
    scrim.classList.toggle('compact', !fits);

    this.compact = !fits; // keep the rendered class in sync with the DOM toggle

    requestAnimationFrame(() => (this.suppressMeasure = false));
  };

  setErrorValue = (errorName: string, value: { text?: string; show?: boolean }) => {
    const previousError = (this.errors[errorName] || {}) as { text: string; show: boolean };

    this.errors = { ...this.errors, [errorName]: { ...previousError, ...value } };
  };

  setInputValue = (inputName: string, value: any) => {
    this.inputs = { ...this.inputs, [inputName]: value };
  };

  onFileUploaderChange = (event: Event) => {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;

    const maxSize = parseInt(input.dataset.maxSize || '0', 10);

    const newFiles = this.uploadMultipleDocuments ? Array.from(input.files) : [input.files[0]];

    input.value = '';

    let anyFileExceedsMaxSize = false;

    const filteredNewFiles = newFiles.filter(newFile => {
      const isFileSizeValid = newFile.size <= maxSize;

      if (!isFileSizeValid) {
        anyFileExceedsMaxSize = true;
        return false;
      }
      const isFileAlreadyExists = this.inputs?.documents?.some(
        existingFile => existingFile.name === newFile.name && existingFile.size === newFile.size && existingFile.lastModified === newFile.lastModified,
      );

      return !isFileAlreadyExists;
    });

    this.setInputValue('documents', this.uploadMultipleDocuments ? [...(this.inputs?.documents || []), ...filteredNewFiles] : filteredNewFiles);

    if (anyFileExceedsMaxSize) {
      this.setErrorValue('documents', {
        show: true,
        text: 'documentLimitError',
      });
      return;
    } else if (this.errors?.documents) this.setErrorValue('documents', { text: this.errors.documents?.text, show: false });

    if (this.item?.claimingMethodEnum === 'ClaimByScanningQRCode') {
      if (this.inputs?.qrCode) this.submit();
    } else this.focusInputs();
  };

  onDocumentButtonClicked = () => {
    const documentUploader = this.el.shadowRoot.querySelector('.document-uploader') as HTMLInputElement;

    if (documentUploader) documentUploader.click();
  };

  focusInputs = () => {
    setTimeout(() => {
      const inputs = this.el.shadowRoot.querySelectorAll('input[type="text"]') as NodeListOf<HTMLInputElement>;

      const firstEnabled = Array.from(inputs).find(input => !input.disabled && !input.readOnly);

      if (firstEnabled) firstEnabled.focus();
    }, 50);
  };

  private focusEmptyInputs = () => {
    const inputs = this.el.shadowRoot.querySelectorAll('input[type="text"]') as NodeListOf<HTMLInputElement>;

    const firstEmptyInput = Array.from(inputs).find(input => !input.value?.trim());

    if (firstEmptyInput) firstEmptyInput.focus();

    if (this.item?.documentUploaderIsRequired && !this.inputs?.documents.length) {
      this.setErrorValue('documents', {
        show: true,
        text: 'documentRequiredError',
      });
    }
  };

  private submit = async () => {
    if (this.claimError) return;

    if (!this.getIsFormValid()) return this.focusEmptyInputs();

    if (!this.getIsFormConfirmed()) return;

    if (this.item?.showDocumentUploader) {
      if (this.item?.documentUploaderIsRequired && !this.inputs?.documents?.length) {
        this.setErrorValue('documents', {
          show: true,
          text: 'documentRequiredError',
        });
        return;
      }
    }

    this.isLoading = true;

    try {
      if (this.handleClaiming) {
        await this.handleClaiming(this.inputs?.documents, {
          qrCode: this.inputs?.qrCode,
          invoice: this.inputs?.invoice,
          jobNumber: this.inputs?.jobNumber,
        } as ItemClaimDTO);
      }

      this.close(true);
    } catch (error) {
      console.error(error);
    } finally {
      this.isLoading = false;
    }
  };

  private onConfirmCheckboxChange = (confirmationName: string) => (event: Event) => {
    const target = event.target as HTMLInputElement;

    this.confirmationStates = { ...this.confirmationStates, [confirmationName]: target.checked };

    if (this.getIsFormConfirmed) this.focusInputs();
  };

  private getIsFormConfirmed = (): boolean => {
    if (Array.isArray(this.item?.warnings)) {
      for (let idx = 0; idx < this.item?.warnings.length; idx++) {
        const warning = this.item.warnings[idx];

        if (!!warning?.confirmationText && !this.confirmationStates[warning.key]) return false;
      }
    }

    return true;
  };

  private getIsFormValid = (): boolean => {
    if (this.item?.claimingMethodEnum === 'ClaimByScanningQRCode') {
      if (!this.inputs?.qrCode?.trim().length) return false;
    } else {
      if (!this.inputs?.invoice?.trim().length) return false;
      if (!this.inputs?.jobNumber?.trim().length) return false;
    }

    if (this.item?.documentUploaderIsRequired && !this.inputs?.documents.length) return false;

    return true;
  };

  resetState = () => {
    this.item = null;
    this.errors = {};
    this.inputs = {
      documents: [],
    };
    this.isLoading = false;
    this.compact = false;
    this.claimError = '';
    this.confirmationStates = {};
  };

  clearFile = (event: MouseEvent | number) => {
    if (typeof event === 'number') {
      const newDocuments = this.inputs.documents.filter((_, idx) => idx !== event);
      this.setInputValue('documents', newDocuments);
      if (this?.item?.documentUploaderIsRequired && !newDocuments.length) {
        this.setErrorValue('documents', {
          show: true,
          text: 'documentRequiredError',
        });
      }
    } else {
      event.stopPropagation();

      this.setInputValue('documents', []);
      if (this.errors?.documents) this.setErrorValue('documents', { text: this.errors.documents?.text, show: false });
    }
  };

  onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter' && !this.claimError) this.submit();
  };

  private openWarningImage = (event: MouseEvent, imageUrl: string) => {
    const thumbnail = (event.currentTarget as HTMLElement).querySelector('.doc-thumb') as HTMLImageElement;

    if (thumbnail) openImageViewer.bind(this)(thumbnail, imageUrl);
  };

  // #endregion

  render() {
    const isQRScannerForm = this.item?.claimingMethodEnum === 'ClaimByScanningQRCode';

    // The ONLY layout driver: no document uploader → single-column bottom-stub voucher.
    const isBottomStub = !this.item?.showDocumentUploader;

    const disableInputs = !this.getIsFormConfirmed() || this.isLoading;

    const isFormValid = this.getIsFormValid();

    const hasDocuments = !!this.inputs?.documents?.length;

    const showUploadProgress = this.isLoading && this.item?.showDocumentUploader && hasDocuments;

    return (
      <Host translate="no">
        <ImageViewer
          style={{ 'z-index': '99999999' }}
          expandedImage={this.expandedImage}
          imageStyle={{ 'z-index': '999999999' }}
          closeImageViewer={() => closeImageViewer.bind(this)()}
        />
        <div
          dir={this.locale.sharedLocales.direction}
          class={cn(
            'claim-scrim fixed top-0 left-0 z-[999] w-[100dvw] h-[100dvh] overflow-y-auto overscroll-contain pointer-events-none transition duration-500 scale-[110%] opacity-0',
            {
              'opacity-1 scale-100 pointer-events-auto': this.isOpened,
              'compact': this.compact,
            },
          )}
        >
          <div class="stage">
            <div class={cn('voucher', { side: !isBottomStub, bottom: isBottomStub, qr: isQRScannerForm })}>
              <button disabled={this.isLoading} onClick={this.close.bind(this)} class="btn-close" aria-label="Close">
                <AddIcon class="stroke-[1.5px]" />
              </button>

              {/* Ticket body */}
              <section class="v-body">
                <div class="item-row">
                  <div>
                    {!!this.item?.type && <span class="item-type">{this.item.type}</span>}
                    <div class="item-name">{this.item?.name}</div>
                  </div>
                  <div class="vin-stamp">
                    <div class="lab">{this.locale.vin}</div>
                    <div class="vin" dir="ltr">
                      {this.vin}
                    </div>
                    {!!this.item?.printUrl && (
                      <button class="btn-print" onClick={() => window.open(this.item.printUrl, '_blank').focus()}>
                        <PrinterIcon />
                        {this.locale.print}
                      </button>
                    )}
                  </div>
                </div>

                <div class="specs">
                  <div class="spec">
                    <div class="lab">{this.locale.activationDate}</div>
                    <div class="val" dir="ltr">
                      {this.item?.activatedAt || '—'}
                    </div>
                  </div>
                  <div class="spec">
                    <div class="lab">{this.locale.expireDate}</div>
                    <div class="val" dir="ltr">
                      {this.item?.expiresAt || '—'}
                    </div>
                  </div>
                  <div class="spec pkg">
                    <div class="lab">{this.locale.packageCode}</div>
                    <div class="val" dir="ltr">
                      {this.item?.packageCode || '—'}
                    </div>
                  </div>
                </div>

                {Array.isArray(this.item?.warnings) && !!this.item.warnings.length && (
                  <div class="notices">
                    {this.item.warnings.map(warning => (
                      <div key={warning.key} class={cn('notice', { info: !warning?.confirmationText })}>
                        <div class="doc-row">
                          <div class="ntc-body" innerHTML={warning.bodyContent} />
                          {warning?.imageUrl && (
                            <button class="doc-thumb-btn" title={this.locale.expand} onClick={event => this.openWarningImage(event, warning?.imageUrl)}>
                              <span class="thumb-overlay">
                                <img src={Eye} />
                                {this.locale.expand}
                              </span>
                              <img class="doc-thumb" src={warning?.imageUrl} />
                            </button>
                          )}
                        </div>

                        {warning?.confirmationText && (
                          <div class="confirm">
                            <label class={cn({ disabled: this.isLoading })}>
                              <input
                                type="checkbox"
                                class="sr-only"
                                disabled={this.isLoading}
                                checked={!!this.confirmationStates[warning.key]}
                                onChange={this.onConfirmCheckboxChange(warning.key)}
                              />
                              <span class="cbx-face">
                                <CheckIcon />
                              </span>
                              {warning?.confirmationText}
                            </label>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </section>

              {/* Tear-off claim stub */}
              <aside class="stub">
                <div class="perf" />
                <span class="punch p1" />
                <span class="punch p2" />

                {isQRScannerForm && <div class="field-label">{this.locale.qrCode}</div>}

                <div class="claim-area">
                  {isQRScannerForm ? (
                    <div class={cn('scanzone', { disabled: disableInputs })}>
                      <div class="sz-row">
                        <span class="qr-ic">
                          <QrCodeScanIcon />
                        </span>
                        <input
                          dir="ltr"
                          autofocus
                          type="text"
                          spellcheck="false"
                          autocomplete="off"
                          disabled={disableInputs}
                          onBlur={() => {
                            if (!this.claimError) this.focusInputs();
                          }}
                          onKeyDown={this.onKeyDown}
                          value={this.inputs?.qrCode}
                          placeholder={this.locale.scanTheVoucher}
                          onInput={event => this.setInputValue('qrCode', (event.target as HTMLInputElement).value)}
                        />
                      </div>
                      <div class="scan-hint">
                        <span class="dot" />
                        {this.locale.scanHint}
                      </div>
                    </div>
                  ) : (
                    [
                      <div class="field" key="invoice">
                        <label>{this.locale.invoice}</label>
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
                        />
                      </div>,
                      <div class="field" key="jobNumber">
                        <label>{this.locale.jobNumber}</label>
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
                        />
                      </div>,
                    ]
                  )}

                  {this.item?.showDocumentUploader && (
                    <div class="upload-wrap">
                      <input
                        hidden
                        type="file"
                        class="document-uploader"
                        disabled={this.isLoading}
                        accept="image/*,application/pdf"
                        onChange={this.onFileUploaderChange}
                        multiple={this.uploadMultipleDocuments}
                        data-max-size={this.maximumDocumentFileSizeInMb * 1024 * 1024}
                      />

                      <div class={cn('upload', { disabled: disableInputs })} onClick={this.onDocumentButtonClicked}>
                        <span class="ic">
                          <AttachIcon />
                        </span>
                        <span class="upload-text">
                          <span class="ut" title={this.uploadMultipleDocuments ? this.locale.uploadMultipleDocument : this.locale.uploadSingleDocument}>
                            {this.uploadMultipleDocuments ? this.locale.uploadMultipleDocument : this.locale.uploadSingleDocument}
                            {this?.item?.documentUploaderIsRequired && !hasDocuments && <span class="req">*</span>}
                          </span>
                          <span class="us">{this.locale.uploadHint}</span>
                        </span>
                        {hasDocuments && (
                          <button class="upload-clear" onClick={this.clearFile} aria-label="Clear">
                            <AddIcon />
                          </button>
                        )}
                      </div>

                      <flexible-container isOpened={!!this.errors?.documents?.show}>
                        <div class="upload-error">
                          {this.errors?.documents?.text === 'documentLimitError'
                            ? this.locale.documentLimitError + `${this.maximumDocumentFileSizeInMb}Mb`
                            : this.locale.documentRequiredError || this.locale.sharedLocales.errors.wildCard}
                        </div>
                      </flexible-container>

                      <flexible-container isOpened={hasDocuments} containerClasses="">
                        <div class="chips">
                          {this.inputs?.documents?.map((doc, idx) => (
                            <div class={cn('chip', { disabled: disableInputs })}>
                              <span title={doc.name}>{doc.name}</span>
                              <button disabled={disableInputs} onClick={() => this.clearFile(idx)} aria-label="Remove">
                                <AddIcon />
                              </button>
                            </div>
                          ))}
                        </div>
                      </flexible-container>
                    </div>
                  )}

                  <div class="stub-spacer" />

                  <button class={cn('btn-claim', { loading: this.isLoading })} onClick={this.submit} disabled={!isFormValid || disableInputs}>
                    {this.isLoading && <span class="claim-progress" style={{ width: `${showUploadProgress ? this.uploadProgress : 100}%` }} />}
                    <span class="claim-label">
                      {!this.isLoading && <CheckIcon />}
                      <span>{this.isLoading ? `${this.locale.processing}${showUploadProgress ? ` ${this.uploadProgress}%` : ''}` : this.locale.claim}</span>
                    </span>
                  </button>
                </div>
              </aside>
            </div>
          </div>

          {/* Claim-failure dialog (replaces alert()) */}
          {!!this.claimError && (
            <div
              class="err-scrim"
              onClick={event => {
                if (event.target === event.currentTarget) this.dismissError();
              }}
            >
              <div class="err-card" role="alertdialog" aria-modal="true">
                <button class="btn-close" onClick={this.dismissError} aria-label="Close">
                  <AddIcon class="stroke-[1.5px]" />
                </button>
                <div class="err-head">
                  <span class="err-ic">
                    <TriangleAlertIcon />
                  </span>
                  <span class="err-title">{this.locale.claimFailed}</span>
                </div>
                <div class="err-msg">{this.claimError}</div>
                <div class="err-foot">
                  <button class="btn-err-ok" onClick={this.dismissError}>
                    {this.locale.ok}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </Host>
    );
  }
}
