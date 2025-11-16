import { Component, Element, Host, Prop, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormElement } from '~features/form-hook/interface';
import { FormHook, FormInputMeta } from '~features/form-hook';

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
  @Prop() defaultValue: string;
  @Prop() wrapperClass: string;

  @Element() el!: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
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
    const [locale] = this.form.getFormLocale();

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    return (
      <Host>
        <label part={`${this.name}`} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, disabled)}>
          <FormInputLabel name={this.name} isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <textarea
              name={this.name}
              placeholder={placeholder}
              part={`${this.name}-textarea form-input-textarea`}
              class={cn('form-input-style form-input-textarea-style', {
                'form-input-error-style': isError,
              })}
            >
              {this.defaultValue}
            </textarea>
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}
