import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormHook } from '~features/form-hook/form-hook';
import { ErrorKeys, LanguageKeys } from '~features/multi-lingual';
import { FormElement, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook/';

import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';
import { ArrowUpIcon } from '~assets/arrow-up-icon';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { AddIcon } from '~assets/add-icon';

const partKeyPrefix = 'form-select-';
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
  @Prop() clearable = false;
  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() searchable: boolean;
  @Prop() isRequired: boolean;
  @Prop() isDisabled: boolean;
  @Prop() wrapperClass: string;
  @Prop() language: LanguageKeys = 'en';
  @Prop() fetcher: FormSelectFetcher<any>;

  @State() searchValue = '';
  @State() isFetching: boolean;
  @State() isOpen: boolean = false;
  @State() selectedValue: string = '';
  @State() openUpwards: boolean = false;
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
  }

  async disconnectedCallback() {
    this.abortController.abort();
    this.form.unsubscribe(this.name);
    document.removeEventListener('click', this.closeDropdown);
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
  }

  toggleDropdown = () => {
    if (this.isOpen && !this.searchable) this.isOpen = false;
    else this.adjustDropdownPosition();
  };

  adjustDropdownPosition() {
    requestAnimationFrame(() => {
      const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;
      const selectContainer = this.el.getElementsByClassName('form-select-container')[0] as HTMLDivElement;

      const rect = selectButton.getBoundingClientRect();

      const spaceBelow = window.innerHeight - rect.bottom - 20; // 20 is padding

      this.openUpwards = spaceBelow < selectContainer.getBoundingClientRect().height;

      setTimeout(() => {
        this.isOpen = true;
      }, 10);
    });
  }

  handleSelection(option: FormSelectItem) {
    this.selectedValue = option.value;
    this.searchValue = option.label;
    this.isOpen = false;
  }

  handleKeyDown(event: KeyboardEvent) {
    if (!this.isOpen) return;

    if (event.key === 'Escape') {
      this.isOpen = false;
    }
  }

  reset = (newValue: string = '') => {
    const defaultOption = this.options.find(opt => opt.value === newValue) || { value: newValue, label: '' };

    this.handleSelection(defaultOption);
  };

  closeDropdown = (event: MouseEvent) => {
    if (!this.el.contains(event.target as Node)) {
      this.isOpen = false;
    }
  };

  async fetch() {
    try {
      this.isFetching = true;

      if (this.abortController) this.abortController.abort();

      this.abortController = new AbortController();

      const options = await this.fetcher({ language: this.language, signal: this.abortController.signal, locale: this.form.getFormLocale()[0] });

      this.fetchingErrorMessage = null;
      this.options = options;
    } catch (error) {
      if (error && error?.name === 'AbortError') return;
      console.error(error);
      this.options = [];
      this.fetchingErrorMessage = error.message;
    } finally {
      this.isFetching = false;
    }
  }

  onSearchInput = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;

    this.searchValue = target.value;
    this.selectedValue = '';
  };

  clearInput = () => {
    this.searchValue = '';
    this.selectedValue = '';

    const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;

    selectButton.focus();
    if (!this.isOpen) this.adjustDropdownPosition();
  };

  async componentDidLoad() {
    this.fetch();
    document.addEventListener('click', this.closeDropdown);
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder) || 'Select an option';

    const selectedItem = this.options.find(item => this.selectedValue === item.value);

    if (!selectedItem) {
      this.selectedValue = '';
    }

    const filteredOptions = selectedItem ? this.options : this.options.filter(option => option.label.toLowerCase().includes(this.searchValue.toLowerCase()));

    const disableInput = disabled || this.isDisabled || this.isLoading;

    return (
      <Host>
        <label part={part} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: disableInput })}>
          <FormInputLabel isRequired={isRequired || this.isRequired} label={label} />

          <form-shadow-input name={this.name} form={this.form} value={this.selectedValue} />

          <div part="form-input-container" class={cn('form-input-container', { open: this.isOpen, disableInput })}>
            <input
              type="text"
              disabled={disableInput}
              part="form-input-select"
              value={this.searchable ? this.searchValue : selectedItem?.label || ''}
              readOnly={!this.searchable}
              onInput={this.onSearchInput}
              onClick={this.toggleDropdown}
              placeholder={placeholder || meta?.placeholder}
              class={cn('form-input-style form-input-select', {
                'form-input-error-style': isError,
              })}
            />

            <div part="form-input-select-icon-container" class="form-input-select-icon-container">
              {(selectedItem || this.searchValue) && this.clearable ? (
                <AddIcon onClick={this.clearInput} class="form-input-select-icon cross" />
              ) : (
                <ArrowUpIcon class="form-input-select-icon arrow" />
              )}
            </div>

            <div
              part="form-select-container"
              class={cn('form-select-container', {
                upwards: this.openUpwards,
                downwards: !this.openUpwards,
              })}
            >
              {!!filteredOptions.length &&
                filteredOptions.map(option => (
                  <button
                    type="button"
                    part="form-select-option"
                    onClick={() => this.handleSelection(option)}
                    class={cn('form-select-option', {
                      selected: this.selectedValue === option.value,
                    })}
                  >
                    <div part="form-select-option-label" class="form-select-option-label">
                      {option.label}
                    </div>
                    <TickIcon class="form-select-option-tick" />
                  </button>
                ))}
              {!filteredOptions.length && (
                <div part="form-select-empty-container" class={cn('form-select-empty-container', { error: this.fetchingErrorMessage })}>
                  {this.fetchingErrorMessage && (getNestedValue(locale, this.fetchingErrorMessage) || locale.errors.wildCard)}
                  {!this.fetchingErrorMessage && (this.isFetching ? <img class="form-select-spinner" src={Loader} /> : locale.noSelectOptions)}
                </div>
              )}
            </div>
          </div>
          <FormErrorMessage isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}
