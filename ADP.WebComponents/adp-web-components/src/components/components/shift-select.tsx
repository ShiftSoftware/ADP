import { Component, Element, Host, Prop, State, h } from '@stencil/core';
import { FormSelectItem } from 'components';
import cn from '~lib/cn';
import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';
import { ArrowUpIcon } from '~assets/arrow-up-icon';
import { AddIcon } from '~assets/add-icon';

@Component({
  shadow: false,
  tag: 'shift-select',
  styleUrl: 'shift-select.css',
})
export class ShiftSelect {
  @Prop() name: string;
  @Prop() hasTick = true;
  @Prop() hasArrow = true;
  @Prop() searchValue = '';
  @Prop() isError?: boolean;
  @Prop() searchable: boolean;
  @Prop() isLoading?: boolean;
  @Prop() placeholder?: string;
  @Prop() disableInput?: boolean;
  @Prop() isOpen: boolean = false;
  @Prop() noSelectionText?: string;
  @Prop() selectedValue: string = '';
  @Prop() clearable?: boolean = false;
  @Prop() options: FormSelectItem[] = [];
  @Prop() reverseOptions?: boolean = false;
  @Prop() forceOpenUpwards?: boolean = false;
  @Prop() fetchingErrorText?: string | boolean;
  @Prop() handleSelection: (option: FormSelectItem) => void;
  @Prop() onSelection?: (newSelection: FormSelectItem) => void;
  @Prop() renderOption?: (option: FormSelectItem, isSelected) => any;
  @Prop() fullRenderOption?: (option: FormSelectItem, isSelected, handleSelection) => any;
  @Prop() updateContext: (newValues: Partial<{ isOpen: boolean; searchValue: string; selectedValue: string }>) => void;

  @State() openUpwards: boolean = false;

  @Element() el!: HTMLElement;

  toggleDropdown = () => {
    if (this.isOpen && !this.searchable) this.updateContext({ isOpen: false });
    else this.adjustDropdownPosition();
  };
  async componentDidLoad() {
    document.addEventListener('click', this.closeDropdown);
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    window.addEventListener('resize', this.handleResize);
  }

  adjustDropdownPosition() {
    requestAnimationFrame(() => {
      const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;
      const selectContainer = this.el.getElementsByClassName('form-select-container')[0] as HTMLDivElement;

      const rect = selectButton.getBoundingClientRect();

      const spaceBelow = window.innerHeight - rect.bottom - 20; // 20 is padding

      this.openUpwards = spaceBelow < selectContainer.getBoundingClientRect().height || this.forceOpenUpwards;

      setTimeout(() => {
        this.updateContext({ isOpen: true });
      }, 10);
    });
  }

  handleKeyDown(event: KeyboardEvent) {
    if (!this.isOpen) return;

    if (event.key === 'Escape') {
      this.updateContext({ isOpen: false });
    }
  }

  closeDropdown = (event: MouseEvent) => {
    if (!this.el.contains(event.target as Node)) {
      this.updateContext({ isOpen: false });
    }
  };

  async disconnectedCallback() {
    document.removeEventListener('click', this.closeDropdown);
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    window.removeEventListener('resize', this.handleResize);
  }

  handleResize = () => {
    if (this.isOpen) {
      this.adjustDropdownPosition();
    }
  };

  onSearchInput = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;

    this.updateContext({ searchValue: target.value, selectedValue: '' });
  };

  clearInput = () => {
    this.updateContext({ searchValue: '', selectedValue: '' });

    const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;

    selectButton.focus();
    if (!this.isOpen) this.adjustDropdownPosition();
  };

  render() {
    const selectedItem = this.options.find(item => this.selectedValue === item.value);

    return (
      <Host translate="no">
        <div
          part={`${this.name}-container form-input-container`}
          class={cn('form-input-container', { open: this.isOpen, disabled: this.disableInput, disableInput: this.disableInput })}
        >
          <slot />
          <input
            type="text"
            disabled={this.disableInput}
            part={`${this.name}-input-select form-input-select`}
            value={this.searchable ? this.searchValue : selectedItem?.label || ''}
            readOnly={!this.searchable}
            onInput={this.onSearchInput}
            onClick={this.toggleDropdown}
            placeholder={this.placeholder}
            class={cn('form-input-style form-input-select', {
              'form-input-error-style': this.isError,
            })}
          />

          <div
            part={`${this.name}-select-icon-container form-input-select-icon-container`}
            class={cn('form-input-select-icon-container cursor-pointer', { 'pointer-events-none!': !((selectedItem || this.searchValue) && this.clearable) })}
          >
            {(selectedItem || this.searchValue) && this.clearable ? (
              <AddIcon part={`${this.name}-cross-icon`} onClick={this.clearInput} class="form-input-select-icon cross" />
            ) : (
              this.hasArrow && <ArrowUpIcon part={`${this.name}-arrow-icon select-arrow`} class="cursor-pointer form-input-select-icon pointer-events-none! arrow" />
            )}
          </div>

          <div
            part={cn(`${this.name}-select-container form-select-container`, {
              'form-select-container-upwards': this.openUpwards || this.forceOpenUpwards,
              'form-select-container-downwards': !this.openUpwards && !this.forceOpenUpwards,
            })}
            class={cn('form-select-container', {
              upwards: this.openUpwards || this.forceOpenUpwards,
              downwards: !this.openUpwards && !this.forceOpenUpwards,
            })}
          >
            {!!this.options.length &&
              (this.reverseOptions ? [...this.options].reverse() : this.options).map(option =>
                this.fullRenderOption ? (
                  this.fullRenderOption(option, this.selectedValue === option.value, this.handleSelection)
                ) : (
                  <button
                    type="button"
                    part={cn(`${this.name}-select-option form-select-option`, { 'form-select-option-selected': this.selectedValue === option.value })}
                    onClick={() => this.handleSelection(option)}
                    class={cn('form-select-option', {
                      selected: this.selectedValue === option.value,
                    })}
                  >
                    {this.renderOption ? (
                      this.renderOption(option, this.selectedValue === option.value)
                    ) : (
                      <div part={`${this.name}-select-option-label form-select-option-label`} class="form-select-option-label">
                        {option.label}
                      </div>
                    )}
                    {this.hasTick && <TickIcon part={`${this.name}-tick-icon`} class="form-select-option-tick" />}
                  </button>
                ),
              )}
            {!this.options.length && (
              <div part={`${this.name}-select-empty-container form-select-empty-container`} class={cn('form-select-empty-container', { error: this.fetchingErrorText })}>
                {this.fetchingErrorText}
                {!this.fetchingErrorText &&
                  (this.isLoading ? <img part={`${this.name}-select-spinner form-select-spinner`} class="form-select-spinner" src={Loader} /> : this.noSelectionText)}
              </div>
            )}
          </div>
        </div>
      </Host>
    );
  }
}
