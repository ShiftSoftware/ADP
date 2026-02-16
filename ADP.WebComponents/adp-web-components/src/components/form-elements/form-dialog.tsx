import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { AddIcon } from '~assets/add-icon';
import { FormHook } from '~features/form-hook';

@Component({
  shadow: false,
  tag: 'form-dialog',
  styleUrl: 'form-inputs.css',
})
export class FormInput {
  @Prop() message: string;
  @Prop() isError: boolean;
  @Prop() closeText: string;
  @Prop() form: FormHook<any>;
  @State() internalMessage: string;
  @State() isOpened: boolean = false;

  @Prop() dialogClosed: () => void;

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
      this.dialogClosed();
    }, 310);
  };

  render() {
    const [locale] = this.form.getFormLocale();

    return (
      <Host>
        <div
          part="form-dialog-modal"
          dir={locale.sharedFormLocales.direction}
          class={cn('dialog-drop-container', {
            'opened-dialog-drop-container dialog-blur': this.isOpened,
            'error': this.isError,
          })}
        >
          <div
            part="dialog-wrapper"
            class={cn('dialog-wrapper', {
              'opened-dialog-wrapper': this.isOpened,
            })}
          >
            <button aria-label="Close the dialog" part="dialog-close-icon-button" onClick={this.closeDialog} type="button" class="dialog-close-icon-button">
              <AddIcon />
            </button>
            <div part="dialog-content" class="dialog-content">
              {this.isError && this.internalMessage}
              <div style={{ display: this.isError ? 'none' : 'block' }}>
                <slot />
              </div>
            </div>
            <button part="dialog-close-button" type="button" onClick={this.closeDialog} class="dialog-close-button">
              {this.closeText}
            </button>
          </div>
        </div>
      </Host>
    );
  }
}
