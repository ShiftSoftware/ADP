import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormHook } from '~features/form-hook/form-hook';
import { ErrorKeys, LanguageKeys } from '~features/multi-lingual';
import { FormElement, FormInputMeta, FormSelectFetcher, FormSelectItem } from '~features/form-hook/';

import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';
import { ArrowUpIcon } from '~assets/arrow-up-icon';

const partKeyPrefix = 'form-select-';
@Component({
  shadow: false,
  tag: 'form-select',
  styleUrl: 'form-inputs.css',
})
export class FormSelect implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() language: LanguageKeys = 'en';
  @Prop() fetcher: FormSelectFetcher<any>;

  @State() isLoading: boolean;
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
    if (this.isOpen) this.isOpen = false;
    else this.adjustDropdownPosition();
  };

  adjustDropdownPosition() {
    requestAnimationFrame(() => {
      const selectButton = this.el.getElementsByClassName('select-button')[0] as HTMLDivElement;
      const selectContainer = this.el.getElementsByClassName('select-container')[0] as HTMLDivElement;

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
      this.isLoading = true;

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
      this.isLoading = false;
    }
  }

  async componentDidLoad() {
    this.fetch();
    document.addEventListener('click', this.closeDropdown);
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label);
    const placeholder = getNestedValue(locale, meta?.placeholder) || 'Select an option';

    const selectedItem = this.options.find(item => this.selectedValue === item.value);

    if (!selectedItem) {
      this.selectedValue = '';
    }

    return (
      <Host>
        <label part={part} id={this.wrapperId} class={cn('relative w-full inline-flex flex-col', this.wrapperClass)}>
          {label && (
            <div class="mb-[4px]">
              {label}
              {isRequired && <span class="ms-0.5 text-red-600">*</span>}
            </div>
          )}

          <form-shadow-input name={this.name} form={this.form} value={this.selectedValue} />

          <div class="relative">
            <button
              type="button"
              name={this.name}
              disabled={disabled}
              onClick={this.toggleDropdown}
              class={cn(
                'select-button enabled:focus:border-slate-600 enabled:focus:shadow-[0_0_0_0.2rem_rgba(71,85,105,0.25)] border bg-white h-[38px] flex justify-between overflow-hidden disabled:opacity-75 flex-1 py-[6px] px-[12px] transition-all duration-300 rounded-md outline-none w-full',
                {
                  'text-[#9CA3AF]': !selectedItem,
                  '!border-red-500 focus:shadow-[0_0_0_0.2rem_rgba(239,68,68,0.25)]': isError,
                  'enabled:border-slate-600 enabled:shadow-[0_0_0_0.2rem_rgba(71,85,105,0.25)]': this.isOpen,
                },
              )}
            >
              {selectedItem ? selectedItem.label : <span>{placeholder}</span>}
              <ArrowUpIcon class={cn('select-arrow size-6 transition text-[#9CA3AF] duration-300', { 'rotate-180': !this.isOpen })} />
            </button>

            <div
              class={cn(
                'select-container z-[10] flex flex-col pointer-events-none absolute w-full bg-white border rounded-md shadow opacity-0 transition-opacity duration-300 max-h-[250px] overflow-auto',
                {
                  'opacity-100 pointer-events-auto': this.isOpen,
                  'top-0 -mt-[8px] -translate-y-full': this.openUpwards,
                  'top-0 mt-[8px] translate-y-[38px]': !this.openUpwards,
                },
              )}
            >
              {!!this.options.length &&
                this.options.map(option => (
                  <button
                    type="button"
                    onClick={() => this.handleSelection(option)}
                    class={cn('select-option flex justify-between px-4 py-2 hover:bg-slate-100 transition-colors duration-300', {
                      'bg-slate-200': this.selectedValue === option.value,
                    })}
                  >
                    <div>{option.label}</div>
                    <TickIcon class={cn('size-5 opacity-0 transition duration-300', { 'opacity-100': this.selectedValue === option.value })} />
                  </button>
                ))}
              {!this.options.length && (
                <div class={cn('select-empty-container h-[100px] flex items-center justify-center', { 'text-red-500': this.fetchingErrorMessage })}>
                  {this.fetchingErrorMessage && (getNestedValue(locale, this.fetchingErrorMessage) || locale.errors.wildCard)}
                  {!this.fetchingErrorMessage && (this.isLoading ? <img class="spin-slow size-[22px]" src={Loader} /> : locale.noSelectOptions)}
                </div>
              )}
            </div>
          </div>
          <form-error-message isError={isError} errorMessage={locale[errorMessage] || locale?.inputValueIsIncorrect || errorMessage} />
        </label>
      </Host>
    );
  }
}
