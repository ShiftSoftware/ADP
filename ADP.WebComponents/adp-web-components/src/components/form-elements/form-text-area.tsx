import { Component, Element, Host, Prop, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormElement } from '~features/form-hook/interface';
import { FormHook, FormInputMeta } from '~features/form-hook';

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

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label);
    const placeholder = getNestedValue(locale, meta?.placeholder);

    return (
      <Host>
        <label part={part} id={this.wrapperId} class={cn('relative w-full inline-flex flex-col', this.wrapperClass)}>
          {label && (
            <div class="mb-[4px]">
              {label}
              {isRequired && <span class="ms-0.5 text-red-600">*</span>}
            </div>
          )}
          <div class={cn('relative h-fit', { 'opacity-75': disabled })}>
            <textarea
              name={this.name}
              placeholder={placeholder}
              class={cn(
                'border h-[200px] form-input resize-none disabled:bg-white flex-1 py-[6px] px-[12px] transition !duration-300 rounded-md outline-none focus:border-slate-600 focus:shadow-[0_0_0_0.2rem_rgba(71,85,105,0.25)] w-full',
                'form-textarea-' + this.name,
                { '!border-red-500 focus:shadow-[0_0_0_0.2rem_rgba(239,68,68,0.25)]': isError },
              )}
            />
          </div>
          <form-error-message isError={isError} errorMessage={locale[errorMessage] || locale?.inputValueIsIncorrect || errorMessage} />
        </label>
      </Host>
    );
  }
}
