import { Component, Element, Host, Prop, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormElement } from '~features/form-hook/interface';
import { FormHook, FormInputMeta, FormInputLocalization, getInputLocalization } from '~features/form-hook';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';

const partKeyPrefix = 'form-textarea-';
@Component({
  shadow: false,
  tag: 'form-text-area',
  styleUrl: 'form-inputs.css',
})
export class FormTextArea implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() isLoading?: boolean;
  @Prop() defaultValue: string;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop() isDisabled?: boolean;
  @Prop() localization?: FormInputLocalization = {};

  @Element() el!: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
  }

  @Watch('staticValue')
  async onStaticValueChange(newStaticValue?: string, notInitialLoad = true) {
    if (!!newStaticValue) {
      this.defaultValue = newStaticValue;
      this.inputRef.value = newStaticValue;
    } else if (notInitialLoad) {
      this.defaultValue = '';
      this.inputRef.value = '';
    }
  }

  async componentDidLoad() {
    this.inputRef = this.el.getElementsByClassName(partKeyPrefix + this.name)[0] as HTMLInputElement;
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
  }

  reset(newValue?: string) {
    const value = newValue || this.defaultValue || '';

    this.inputRef.value = value;
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    return (
      <Host>
        <label part={`${this.name}`} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel name={this.name} isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <textarea
              name={this.name}
              disabled={isDisabled}
              placeholder={placeholder}
              part={`${this.name}-textarea form-input-textarea`}
              class={cn('form-input-style form-input-textarea-style', partKeyPrefix + this.name, {
                'form-input-error-style': isError,
              })}
            >
              {this.defaultValue}
            </textarea>
          </div>
          <div class="-mt-1">
            <FormErrorMessage name={this.name} isError={isError} errorMessage={errorTextMessage} />
          </div>
        </label>
      </Host>
    );
  }
}
