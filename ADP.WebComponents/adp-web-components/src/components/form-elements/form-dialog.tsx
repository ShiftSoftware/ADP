import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { AddIcon } from '~assets/add-icon';
import { FormHook } from '~features/form-hook';

@Component({
  shadow: true,
  tag: 'form-dialog',
  styleUrl: 'form-dialog.css',
})
export class FormInput {
  @Prop() message: string = '';
  @Prop() form!: FormHook<any>;
  @Prop() closeText: string = '';
  @Prop() isError: boolean = false;
  @Prop() successMessage: string = '';

  @State() isOpened: boolean = false;
  @State() internalMessage: string = '';

  @Prop() dialogClosed?: () => void;

  async componentWillLoad() {
    this.form.openDialog = () => (this.isOpened = true);
    if (this.internalMessage) {
      this.internalMessage = this.message;
      setTimeout(() => (this.isOpened = true), 100);
    }
  }

  @Watch('message')
  async changeErrorMiddleware(newError: string) {
    if (newError && newError !== this.internalMessage) {
      this.internalMessage = newError;
      setTimeout(() => (this.isOpened = true), 100);
    } else this.isOpened = false;
  }

  closeDialog = () => {
    this.isOpened = false;
    setTimeout(() => {
      this.internalMessage = '';
      if (this.dialogClosed) this.dialogClosed();
    }, 310);
  };

  render() {
    const [locale] = this.form.getFormLocale();

    const dialogContainerIdentifier = cn('form-dialog-modal dialog-drop-container', {
      'dialog-drop-container-opened': this.isOpened,
      'dialog-drop-container-error': this.isError,
    });

    const dialogWrapperIdentifier = cn('dialog-wrapper', {
      'dialog-wrapper-opened': this.isOpened,
    });

    const buttonIdentifiers = cn('dialog-close-button', { 'dialog-close-button-error': this.isError });

    const successContainerIdentifier = 'form-success-container';

    const successContainerIdentifierIcon = 'form-success-container-icon';

    const dialogContentIdentifier = cn('dialog-content');

    const closeButtonIdentifier = 'dialog-close-icon-button';

    const closeButtonIdentifierIcon = 'dialog-close-icon-button-icon';
    return (
      <Host translate="no">
        <div part={dialogContainerIdentifier} dir={locale.sharedFormLocales.direction} class={dialogContainerIdentifier}>
          <div part={dialogWrapperIdentifier} class={dialogWrapperIdentifier}>
            <button aria-label="Close the dialog" part={closeButtonIdentifier} onClick={this.closeDialog} type="button" class={closeButtonIdentifier}>
              <AddIcon class={closeButtonIdentifierIcon} part={closeButtonIdentifierIcon} />
            </button>
            <div part={dialogContentIdentifier} class={dialogContentIdentifier}>
              {this.isError && this.internalMessage}
              <div style={{ display: this.isError ? 'none' : 'block' }}>
                <div class={successContainerIdentifier} part={successContainerIdentifier}>
                  <svg
                    fill="none"
                    stroke-width="2"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    xmlns="http://www.w3.org/2000/svg"
                    part={successContainerIdentifierIcon}
                    class={successContainerIdentifierIcon}
                  >
                    <path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z" />
                    <path d="m9 12 2 2 4-4" />
                  </svg>
                  {this.successMessage}
                </div>
              </div>
            </div>
            <button part={buttonIdentifiers} type="button" onClick={this.closeDialog} class={buttonIdentifiers}>
              {this.closeText}
            </button>
          </div>
        </div>
      </Host>
    );
  }
}
