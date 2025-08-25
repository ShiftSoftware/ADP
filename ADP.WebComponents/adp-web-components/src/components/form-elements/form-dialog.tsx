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
  @Prop() closeText: string;
  @Prop() form: FormHook<any>;
  @Prop() errorMessage: string;
  @State() internalMessage: string;
  @State() isOpened: boolean = false;

  @Prop() dialogClosed: () => void;

  async componentWillLoad() {
    if (this.internalMessage) {
      this.internalMessage = this.errorMessage;
      setTimeout(() => (this.isOpened = true), 100);
    }
  }

  @Watch('errorMessage')
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
          dir={locale.direction}
          class={cn('dialog-drop-container', {
            'opened-dialog-drop-container dialog-blur': this.isOpened,
          })}
        >
          <div
            part="dialog-wrapper"
            class={cn('dialog-wrapper', {
              'opened-dialog-wrapper': this.isOpened,
            })}
          >
            <button part="dialog-close-icon-button" onClick={this.closeDialog} type="button" class="dialog-close-icon-button">
              <AddIcon />
            </button>
            <div part="dialog-content" class="dialog-content">
              {this.internalMessage}
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
