import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormInputLocalization, FormInputMeta, getInputLocalization } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormInputPrefix } from './components/form-input-prefix';
import { FormErrorMessage } from './components/form-error-message';

const partKeyPrefix = 'form-input-';
@Component({
  shadow: false,
  tag: 'form-input',
  styleUrl: 'form-inputs.css',
})
export class FormInput implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() isDisabled?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop() inputProps?: any = {};
  @Prop() type?: HTMLInputElement['type'];
  @Prop({ mutable: true }) defaultValue: string;
  @Prop() formatter?: (value: string) => string;
  @Prop() localization?: FormInputLocalization = {};

  @Prop() icon?: any;
  @Prop() iconAction?: () => void;
  @Prop() iconPosition?: 'start' | 'end' = 'end';

  @State() prefixWidth: number = 0;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
  }

  @Watch('staticValue')
  async onStaticValueChange(newStaticValue?: string, notInitialLoad = true) {
    if (!!newStaticValue) {
      const formatted = this.formatter ? this.formatter(newStaticValue) : newStaticValue;
      this.defaultValue = formatted;
      this.inputRef.value = formatted;
    } else if (notInitialLoad) {
      this.defaultValue = '';
      this.inputRef.value = '';
    }
  }

  async componentDidLoad() {
    const prefix = this.el.getElementsByClassName('prefix')[0] as HTMLElement;
    this.prefixWidth = prefix ? prefix.getBoundingClientRect().width : 0;

    this.inputRef = this.el.getElementsByClassName(partKeyPrefix + this.name)[0] as HTMLInputElement;
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
  }

  reset(newValue?: string) {
    const value = newValue || this.defaultValue || '';

    this.inputRef.value = this.formatter ? this.formatter(value) : value;
  }

  onInputChange = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;
    target.value = this.formatter ? this.formatter(target.value) : target.value;
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

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

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <FormInputPrefix name={this.name} direction={locale?.sharedFormLocales?.direction} prefix={this.inputPrefix} />
            <input
              {...this.inputProps}
              name={this.name}
              type={this?.type}
              disabled={isDisabled}
              onInput={this.onInputChange}
              defaultValue={this.formatter ? this.formatter(this.defaultValue || '') : this.defaultValue}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              style={{
                ...(this.prefixWidth ? { [isRtl ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}),
                ...(this.icon ? { [iconPaddingSide]: '40px' } : {}),
              }}
              class={cn('form-input-style', part, {
                'form-input-error-style': isError,
              })}
            />
            {this.icon && renderIcon()}
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={errorTextMessage} />
        </label>
      </Host>
    );
  }
}
