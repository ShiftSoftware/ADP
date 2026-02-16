import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormInputPrefix } from './components/form-input-prefix';
import { FormErrorMessage } from './components/form-error-message';
import { DateTypes, decodeTimeOffset } from '~lib/decode-time-offset';
import { format } from 'date-fns';

const partKeyPrefix = 'form-picker-';

@Component({
  shadow: false,
  tag: 'form-picker-input',
  styleUrl: 'form-inputs.css',
})
export class FormPickerInput implements FormElement {
  @Prop() name: string;
  @Prop() format?: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() isDisabled?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop() inputProps?: any = {};
  @Prop() max?: string | number[];
  @Prop() min?: string | number[];
  @Prop() type?: DateTypes = 'datetime-local';
  @Prop({ mutable: true }) defaultValue: string;
  @Prop() formatter?: (value: string) => string;

  @Prop() icon?: any;
  @Prop() iconAction?: () => void;
  @Prop() iconPosition?: 'start' | 'end' = 'end';

  @State() prefixWidth: number = 0;
  @State() displayedValue?: string;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
    this.updateDisplayValue();
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

    this.updateDisplayValue();
  }

  async componentDidLoad() {
    const prefix = this.el.getElementsByClassName('prefix')[0] as HTMLElement;
    this.prefixWidth = prefix ? prefix.getBoundingClientRect().width : 0;

    this.inputRef = this.el.getElementsByClassName(partKeyPrefix + this.name)[0] as HTMLInputElement;
    this.updateDisplayValue();
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
  }

  updateDisplayValue = () => {
    this.displayedValue = this.inputRef?.value || '';

    if (this.displayedValue && (this.formatter || this.format)) {
      if (this.formatter) this.displayedValue = this.formatter(this.displayedValue);
      if (this.format) this.displayedValue = format(new Date(this.displayedValue), this.format);

      this.form.context[`${this.name}-format`] = this.displayedValue;
    } else {
      delete this.form.context[`${this.name}-format`];
    }
  };

  reset(newValue?: string) {
    const value = newValue || this.defaultValue || '';

    this.inputRef.value = value;
    this.updateDisplayValue();
  }

  onInputChange = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;

    if (this.inputRef) this.inputRef.value = target.value;
    this.updateDisplayValue();
  };

  onInputClick = () => {
    this.inputRef?.showPicker();
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    const isRtl = locale?.sharedFormLocales?.direction === 'rtl';
    const isIconStart = this.iconPosition === 'start';

    const iconPaddingSide = isIconStart ? (isRtl ? 'paddingRight' : 'paddingLeft') : isRtl ? 'paddingLeft' : 'paddingRight';

    const iconClass = cn('form-input-icon', {
      'form-input-icon-start': isIconStart,
      'form-input-icon-end': !isIconStart,
      'form-input-icon-interactive': !!this.iconAction,
    });

    const renderIcon = () =>
      this.iconAction ? (
        <button type="button" disabled={isDisabled} onClick={this.iconAction} part={`${this.name}-icon form-input-icon`} class={iconClass}>
          {this.icon}
        </button>
      ) : (
        <span part={`${this.name}-icon form-input-icon`} class={iconClass}>
          {this.icon}
        </span>
      );

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container relative`} class="form-input-container">
            <FormInputPrefix name={this.name} direction={locale?.sharedFormLocales?.direction} prefix={this.inputPrefix} />
            <input
              type="text"
              onInput={() => {}}
              part={`form-input`}
              disabled={isDisabled}
              value={this.displayedValue}
              onClick={this.onInputClick}
              placeholder={placeholder || meta?.placeholder}
              style={{
                ...(this.prefixWidth ? { [isRtl ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}),
                ...(this.icon ? { [iconPaddingSide]: '40px' } : {}),
              }}
              class={cn('form-input-style enabled:cursor-pointer', {
                'form-input-error-style': isError,
              })}
            />
            <input
              type={this.type}
              name={this.name}
              {...this.inputProps}
              onInput={this.onInputChange}
              part={`${this.name}-input ${part}`}
              class={cn('shadow-input size-full absolute top-0 left-0 opacity-0 -z-[99]', part)}
              min={Array.isArray(this.min) ? decodeTimeOffset({ offsets: this.min, type: this.type }) : this.min}
              max={Array.isArray(this.max) ? decodeTimeOffset({ offsets: this.max, type: this.type }) : this.max}
            />
            {this.icon && renderIcon()}
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}
