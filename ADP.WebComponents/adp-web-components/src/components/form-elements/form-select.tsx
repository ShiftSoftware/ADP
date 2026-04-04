import { Component, Element, Host, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormHook } from '~features/form-hook/form-hook';
import { ErrorKeys, LanguageKeys } from '~features/multi-lingual';
import { FormElement, FormInputMeta, FormSelectFetcher, FormSelectItem, FormInputLocalization, getInputLocalization } from '~features/form-hook/';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { getNestedValue } from '~lib/get-nested-value';

@Component({
  shadow: false,
  tag: 'form-select',
  styleUrl: 'form-inputs.css',
})
export class FormSelect implements FormElement {
  @Prop() name: string;
  @Prop() resetKey: any;
  @Prop() fetcherKey: any;
  @Prop() wrapperId: string;
  @Prop() isHidden: boolean;
  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() searchable: boolean;
  @Prop() isRequired: boolean;
  @Prop() isDisabled: boolean;
  @Prop() wrapperClass: string;
  @Prop() fetcher: FormSelectFetcher;
  @Prop() staticValue?: FormSelectItem;
  @Prop() language: LanguageKeys = 'en';
  @Prop() reverseOptions?: boolean = false;
  @Prop() forceOpenUpwards?: boolean = false;
  @Prop({ mutable: true }) clearable = false;
  @Prop({ mutable: true }) defaultValue: string;
  @Prop() localization?: FormInputLocalization = {};

  @State() searchValue = '';
  @State() isOpen: boolean = false;
  @State() selectedValue: string = '';

  @State() isFetching: boolean;

  @State() options: FormSelectItem[] = [];
  @State() fetchingErrorMessage?: ErrorKeys = null;

  @Element() el!: HTMLElement;

  private abortController: AbortController;

  @Watch('language')
  async changeLanguage() {
    this.fetch();
  }

  @Watch('fetcherKey')
  async fetchListener() {
    this.fetch();
  }

  @Watch('resetKey')
  async resetListener() {
    this.reset();
  }

  async componentWillLoad() {
    this.form.subscribe(this.name, this);

    if (this.staticValue) {
      this.defaultValue = this.staticValue.value;
      this.clearable = false;
    }
  }

  async disconnectedCallback() {
    this.abortController?.abort();
    this.form.unsubscribe(this.name);
  }

  handleSelection = (option: FormSelectItem) => {
    this.selectedValue = option.value;
    this.searchValue = option.label;
    this.isOpen = false;
  };

  reset = (newValue: string = '') => {
    const defaultOption = this.options.find(opt => opt.value === newValue || opt.value === this.defaultValue) || { value: newValue, label: '' };

    this.handleSelection(defaultOption);
  };

  updateShiftSelectContext = (newValues: Record<string, any>) => {
    Object.entries(newValues).forEach(([key, value]) => {
      this[key] = value;
    });
  };

  async fetch() {
    try {
      this.isFetching = true;

      if (this.abortController) this.abortController.abort();

      let options: FormSelectItem[];
      if (!!this.staticValue) {
        const localizedStaticValue = this.staticValue[this.language];
        if (localizedStaticValue && localizedStaticValue?.value && localizedStaticValue?.label) options = [localizedStaticValue];
        else options = [this.staticValue];
      } else {
        this.abortController = new AbortController();

        options = await this.fetcher({ language: this.language, signal: this.abortController.signal, locale: this.form.getFormLocale()[0], context: this });
      }

      if (this.form?.formStructure || this.form?.context) forceUpdate(this.form?.formStructure || this.form?.context);

      this.form.context[this.name + 'List'] = options;

      this.fetchingErrorMessage = null;
      this.options = options;

      if (this.defaultValue) {
        const defaultOption = options.find(option => option.value === this.defaultValue);
        if (defaultOption) this.handleSelection(defaultOption);
      }
    } catch (error) {
      if (error && error?.name === 'AbortError') return;
      console.error(error);
      this.options = [];
      this.fetchingErrorMessage = error?.message || null;
    } finally {
      this.isFetching = false;
    }
  }

  async componentDidLoad() {
    this.fetch();
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const selectedItem = this.options.find(item => this.selectedValue === item.value);

    if (!selectedItem) {
      this.selectedValue = '';
    }

    const filteredOptions = selectedItem ? this.options : this.options.filter(option => option?.label?.toLowerCase().includes(this.searchValue?.toLowerCase()));

    const disableInput = disabled || this.isDisabled || this.isLoading || !!this.staticValue;

    if (this.isHidden)
      return (
        <Host translate="no" style={{ display: this.isHidden ? 'none' : 'block' }}>
          <form-shadow-input name={this.name} form={this.form} value={this.selectedValue} />
        </Host>
      );

    return (
      <Host translate="no">
        <label part={`${this.name}`} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: disableInput })}>
          <FormInputLabel name={this.name} isRequired={isRequired || this.isRequired} label={label} />

          <form-shadow-input name={this.name} form={this.form} value={this.selectedValue} />
          <shift-select
            name={this.name}
            isError={isError}
            isOpen={this.isOpen}
            options={filteredOptions}
            clearable={this.clearable}
            isLoading={this.isFetching}
            disableInput={disableInput}
            searchable={this.searchable}
            searchValue={this.searchValue}
            selectedValue={this.selectedValue}
            reverseOptions={this.reverseOptions}
            handleSelection={this.handleSelection}
            forceOpenUpwards={this.forceOpenUpwards}
            updateContext={this.updateShiftSelectContext}
            placeholder={placeholder || meta?.placeholder}
            noSelectionText={locale?.sharedFormLocales?.noSelectOptions}
            fetchingErrorText={
              !this.fetchingErrorMessage ? '' : getNestedValue(locale, this.fetchingErrorMessage) || this.fetchingErrorMessage || locale?.sharedFormLocales?.errors?.wildCard
            }
          ></shift-select>

          <FormErrorMessage name={this.name} isError={isError} errorMessage={errorTextMessage} />
        </label>
      </Host>
    );
  }
}
