import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormHook, FormInputLocalization, FormInputMeta, FormElement, getInputLocalization } from '~features/form-hook';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { LoaderIcon } from '~assets/loader-icon';
import { TickIcon } from '~assets/tick-icon';
import { AnyObjectSchema, mixed } from 'yup';
import { meta, format, size, require, max, upload } from '../forms/defaults/validation';
import { Grecaptcha } from '~lib/recaptcha';

type UploadState = 'idle' | 'uploading' | 'success' | 'error';

declare const grecaptcha: Grecaptcha;

const partKeyPrefix = 'form-file-';

@Component({
  shadow: false,
  tag: 'form-file',
  styleUrl: 'form-inputs.css',
})
export class FormFile implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() isLoading?: boolean;
  @Prop() wrapperClass: string;
  @Prop() isDisabled?: boolean;
  @Prop() accept?: string;
  @Prop() maxSize?: number;
  @Prop() required?: boolean;
  @Prop() signUrl?: string;
  @Prop() maxUpload: number = 1;
  @Prop() signMethod: 'POST' | 'PUT' = 'POST';
  @Prop() signPrefix?: string;
  @Prop() accountName?: string;
  @Prop() containerName?: string;
  @Prop() localization?: FormInputLocalization<{
    uploading?: string;
    upload?: string;
    failure?: string;
    success?: string;
  }> = {};

  @State() fileName: string = '';
  @State() uploadState: UploadState = 'idle';
  @State() failedText: string = '';

  @Element() el!: HTMLElement;

  sasFiles: any[] = [];

  private fileInput?: HTMLInputElement;
  private controlsFormLoading = false;
  private wasFormLoading = false;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
  }

  componentDidLoad() {
    this.fileInput = this.el.querySelector(`.${partKeyPrefix}${this.name}-input`) as HTMLInputElement;
  }

  async disconnectedCallback() {
    this.stopGlobalLoading();
    this.form.unsubscribe(this.name);
  }

  reset() {
    this.stopGlobalLoading();
    if (this.fileInput) this.fileInput.value = '';
    delete this.form?.pendingRequests?.[this.name];
    this.fileName = '';
    this.failedText = '';
    this.uploadState = 'idle';
  }

  private startGlobalLoading() {
    if (!this.form?.context?.setIsLoading || this.controlsFormLoading) return;

    this.wasFormLoading = !!this.form?.context?.isLoading;
    this.controlsFormLoading = true;
    this.form.context.setIsLoading(true);
  }

  private stopGlobalLoading() {
    if (!this.controlsFormLoading || !this.form?.context?.setIsLoading) return;

    this.form.context.setIsLoading(this.wasFormLoading);
    this.controlsFormLoading = false;
  }

  private openFilePicker = () => {
    if (!this.fileInput) return;

    // Only clear the input value, so re-picking the same file still fires a change event.
    // Visible state is left alone until a file actually arrives — clearing it here would
    // swap the upload failure for a "required" error while the picker is still open, and
    // would wipe a valid selection if the user cancels.
    this.fileInput.value = '';
    this.fileInput.click();
  };

  private getLoadingText(locale: any, language: string) {
    return this.localization?.[language]?.uploading || locale?.sharedFormLocales?.loading || 'Uploading...';
  }

  private getUploadText(locale: any, language: string, placeholder?: string) {
    return this.localization?.[language]?.upload || placeholder || locale?.sharedFormLocales?.uploadLabel || 'Upload file';
  }

  private getFailureText(locale: any, language: string) {
    return this.localization?.[language]?.failure || locale?.sharedFormLocales?.errors?.wildCard || 'Upload failed. Please try again.';
  }

  // Tagged so setUploadError knows the text came from the server and is safe to show,
  // unlike a network-level "Failed to fetch".
  private async responseError(response: Response) {
    const error = new Error(await this.getResponseError(response));
    (error as any).fromServer = true;

    return error;
  }

  private async getResponseError(response: Response) {
    const [locale, language] = this.form.getFormLocale();
    const fallback = this.getFailureText(locale, language);

    const raw = await response.text().catch(() => '');
    if (!raw.trim()) return fallback;

    const contentType = response.headers.get('content-type') || '';

    if (contentType.includes('json') || raw.trimStart().startsWith('{')) {
      try {
        const parsed = JSON.parse(raw);
        const message = parsed?.message ?? parsed?.Message;
        const text = message?.body ?? message?.Body ?? message?.title ?? message?.Title;

        if (typeof text === 'string' && text.trim()) return text.trim();
      } catch {
        // fall through to the localized fallback
      }

      return fallback;
    }

    // Blob storage answers with XML: <Error><Code>..</Code><Message>..</Message></Error>
    const xmlMessage = raw.match(/<Message>([\s\S]*?)<\/Message>/)?.[1];
    const text = (xmlMessage || raw).trim().split('\n')[0].trim();

    return text && text.length <= 300 ? text : fallback;
  }

  private setUploadError(error: any) {
    const [locale, language] = this.form.getFormLocale();
    const message = typeof error === 'string' ? error : error?.fromServer ? error?.message : '';

    delete this.form?.pendingRequests?.[this.name];
    this.sasFiles = [];
    this.uploadState = 'error';
    this.failedText = message?.trim() || this.getFailureText(locale, language);
    this.syncValue();

    if (this.fileInput) this.fileInput.value = '';
  }

  private matchesAccept(value: File | string) {
    if (!this.accept) return true;

    const rules = this.accept
      .split(',')
      .map(rule => rule.trim())
      .filter(Boolean)
      .map(rule => rule.toLowerCase());

    if (!rules.length) return true;

    const fileName = typeof value === 'string' ? value : value?.name || '';
    const mime = typeof value === 'string' ? '' : value?.type?.toLowerCase() || '';
    const ext = fileName.includes('.') ? fileName.slice(fileName.lastIndexOf('.')).toLowerCase() : '';

    return rules.some(rule => {
      if (rule === '*/*') return true;
      if (rule.endsWith('/*')) return mime ? mime.startsWith(rule.replace('/*', '/')) : false;
      if (rule.startsWith('.')) return ext === rule;
      if (rule.includes('/')) return mime ? mime === rule : false;
      return true;
    });
  }

  private syncValue(value?: File[]) {
    const resolved: string | File = value?.[0] || '';

    this.fileName = typeof resolved === 'string' ? resolved : resolved?.name;
    // @ts-ignore
    this.form?.validateForm(this.name, value ? value : [], false);
    this.form?.rerender({ inputName: this.name, rerenderForm: true });
  }

  private async requestSignedUpload(files: File[]) {
    const [, language] = this.form.getFormLocale();

    // Send an explicit single-value Accept-Language, matching every other backend call.
    // Left unset, the browser sends its own weighted header (`en-US,en;q=0.9`) whenever more
    // than one language is configured, which the backend cannot parse.
    const formHeaders = { 'Content-Type': 'application/json', 'Accept-Language': language || 'en' };
    const recaptchaKey = this.form?.context.structure?.data?.recaptchaKey;

    if (grecaptcha && recaptchaKey) {
      const token = await grecaptcha.execute(recaptchaKey, { action: 'submit' });
      formHeaders['Recaptcha-Token'] = token;
    }

    const payload = files?.map(file => ({
      name: file.name,
      blob: this.signPrefix ? `${this.signPrefix}${file.name}` : file.name,
      accountName: this.accountName,
      containerName: this.containerName,
      contentType: null,
      size: file?.size,
      width: null,
      height: null,
    }));

    const response = await fetch(this.signUrl, {
      headers: formHeaders,
      body: JSON.stringify(payload),
      method: this.signMethod || 'POST',
    });

    if (!response.ok) throw await this.responseError(response);

    const json = await response.json();
    const list = json?.entity || json?.Entity || json;
    const arr = Array.isArray(list) ? list : Array.isArray(json) ? json : [];

    const signedFiles = arr.map((file, idx) => {
      // @ts-ignore
      files[idx].blob = file?.Blob || file?.blob;
      // @ts-ignore
      files[idx].contentType = files[idx]?.type;
      // @ts-ignore
      files[idx].url = file.Url || file.url;

      return {
        uploadUrl: file.url || file.Url || file.uploadUrl || file.UploadUrl,
        headers: file.headers || file.Headers || {},
        key: file.blob || file.Blob || file.path || file.Path || file.key || file.Key || file?.name,
      };
    });

    this.sasFiles = arr;
    return signedFiles;
  }

  private async uploadToSignedUrl(files: File[], signedFiles: { uploadUrl: string; headers: Record<string, string> }[]) {
    await Promise.all(
      (files || []).map(async (file, idx) => {
        const response = await fetch(signedFiles?.[idx]?.uploadUrl, {
          method: 'PUT',
          headers: {
            'Content-Type': file.type || 'application/octet-stream',
            'x-ms-blob-type': 'BlockBlob',
            ...signedFiles?.[idx]?.headers,
          },
          body: file,
        });

        if (!response.ok) throw await this.responseError(response);
      }),
    );
  }

  // The signature is minted when the file is picked, but only spent when the form is submitted,
  // and an applicant can sit on a long form for longer than the SAS lives. A stale signature is
  // by far the likeliest failure here, so mint a fresh one and try once more before giving up.
  // Re-signing also refreshes the blob/url written onto the File objects, and the payload is
  // serialised after this runs, so the record always points at what was actually uploaded.
  private async uploadWithRetry(files: File[], signedFiles: { uploadUrl: string; headers: Record<string, string> }[]) {
    try {
      await this.uploadToSignedUrl(files, signedFiles);
    } catch (error) {
      console.error(error);

      const refreshed = await this.requestSignedUpload(files);
      await this.uploadToSignedUrl(files, refreshed);
    }
  }

  private handleFileChange = async (event: Event) => {
    const target = event.target as HTMLInputElement;
    const fileList = Array.from(target?.files || []);

    if (!fileList.length) return;

    const limited = typeof this.maxUpload === 'number' && this.maxUpload > 0 ? fileList.slice(0, this.maxUpload) : fileList;

    this.fileName = limited.map(f => f.name).join(', ');
    this.failedText = '';
    this.uploadState = 'uploading';

    this.startGlobalLoading();

    const dataTransfer = new DataTransfer();
    try {
      if (this.signUrl) {
        const signedFiles = await this.requestSignedUpload(limited);

        this.form.pendingRequests[this.name] = async () => {
          try {
            await this.uploadWithRetry(limited, signedFiles);
          } catch (error) {
            console.error(error);
            this.setUploadError(error);
            this.form.rerender({ inputName: this.name, rerenderForm: true });
            throw error;
          }
        };
      }

      this.uploadState = 'success';
      this.syncValue([...limited]);
      limited.forEach(file => dataTransfer.items.add(file));
      target.files = dataTransfer.files;
    } catch (error) {
      console.error(error);
      target.files = dataTransfer.files;
      this.setUploadError(error);
    } finally {
      this.stopGlobalLoading();
    }
  };

  validate = (): AnyObjectSchema => {
    let validation = mixed().meta(meta(this.name));

    // The record must never be created without its file, so an in-flight or failed upload blocks
    // the whole form — including when this field is optional, where nothing else would stop it.
    validation = validation.test(upload(this.name), upload(this.name), () => this.uploadState !== 'uploading' && this.uploadState !== 'error');

    if (this.required) {
      validation = validation.test(require(this.name), require(this.name), (value: File[]) => {
        if (!Array.isArray(value)) return false;

        const count = value.length;

        return count > 0;
      });
    } else {
      validation = validation.optional();
    }

    if (this.maxUpload && this.maxUpload > 0) {
      validation = validation.test(max(this.name), max(this.name), (value: File[]) => {
        if (!Array.isArray(value)) return true;
        const count = value.length;
        return count <= this.maxUpload;
      });
    }

    if (this.maxSize) {
      validation = validation.test(size(this.name), size(this.name), (value: File[]) => {
        if (!Array.isArray(value)) return true;
        if (!value.length) return true;

        const maxSizeInBytes = this.maxSize * 1024 * 1024;

        return value.every(f => f.size <= maxSizeInBytes);
      });
    }
    if (this.accept) {
      validation = validation.test(format(this.name), format(this.name), (value: File[]) => {
        if (!Array.isArray(value)) return true;
        if (!value.length) return true;

        return value.every(f => this.matchesAccept(f));
      });
    }

    return validation as unknown as AnyObjectSchema;
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale, language] = this.form.getFormLocale();

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const disableInput = disabled || this.isLoading || this.uploadState === 'uploading' || this.isDisabled;

    const showSpinner = this.uploadState === 'uploading';
    const displayText = this.uploadState === 'success' && this.fileName ? this.fileName : this.getUploadText(locale, language, placeholder || meta?.placeholder);

    const isErrorState = this.uploadState !== 'uploading' && (!!this.failedText || isError);

    // @ts-ignore
    window?.s = this.form;

    return (
      <Host translate="no">
        <label part={`form-file ${this.name}`} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: disableInput })}>
          <FormInputLabel name={this.name} isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class={cn('form-input-container')}>
            <input
              type="file"
              name={this.name}
              accept={this.accept}
              disabled={disableInput}
              multiple={this.maxUpload > 1}
              onChange={this.handleFileChange}
              part={`${this.name}-file-input form-file-input`}
              class={cn('form-file-input', `${partKeyPrefix}${this.name}-input`)}
            />
            <button
              type="button"
              disabled={disableInput}
              onClick={this.openFilePicker}
              part={`${this.name}-file-trigger form-file-trigger form-input`}
              title={showSpinner ? this.getLoadingText(locale, language) : displayText}
              class={cn('form-file-trigger form-input-style', {
                'form-input-error-style': isErrorState,
                'form-file-trigger-loading': showSpinner,
                'form-file-trigger-success': this.uploadState === 'success',
              })}
            >
              <span part={`${this.name}-file-text form-file-text`} class="form-file-text">
                {showSpinner ? this.getLoadingText(locale, language) : displayText}
              </span>

              <span part={`${this.name}-file-icon form-file-icon`} class="form-file-icon">
                {showSpinner && <LoaderIcon class="form-file-spinner" />}
                {!showSpinner && this.uploadState === 'success' && <TickIcon class="form-file-success-icon" />}
              </span>
            </button>
          </div>

          <FormErrorMessage name={this.name} isError={isErrorState} errorMessage={this.failedText || errorTextMessage} />
        </label>
      </Host>
    );
  }
}
