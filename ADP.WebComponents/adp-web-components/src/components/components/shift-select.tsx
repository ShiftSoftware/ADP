import { Component, Element, Host, Prop, State, h } from '@stencil/core';
import { FormSelectItem } from 'components';
import cn from '~lib/cn';

import { ArrowUpIcon } from '~assets/arrow-up-icon';
import { AddIcon } from '~assets/add-icon';
import getCustomClassesForPortal from '~lib/get-custom-classes-for-portal';

@Component({
  shadow: false,
  tag: 'shift-select',
  styleUrl: 'shift-select.css',
})
export class ShiftSelect {
  @Prop() hasTick = true;
  @Prop() hasArrow = true;
  @Prop() gap: number = 8;
  @Prop() searchValue = '';
  @Prop() name: string = '';
  @Prop() isError?: boolean;
  @Prop() isLoading?: boolean;
  @Prop() placeholder?: string;
  @Prop() disableInput?: boolean;
  @Prop() isOpen: boolean = false;
  @Prop() noSelectionText?: string;
  @Prop() selectedValue: string = '';
  @Prop() clearable?: boolean = false;
  @Prop() searchable?: boolean = false;
  @Prop() options: FormSelectItem[] = [];
  @Prop() reverseOptions?: boolean = false;
  @Prop() forceOpenUpwards?: boolean = false;
  @Prop() fetchingErrorText?: string | boolean;
  @Prop() handleSelection!: (option: FormSelectItem) => void;
  @Prop() onSelection?: (newSelection: FormSelectItem) => void;
  @Prop() renderOption?: (option: FormSelectItem, isSelected: boolean) => any;
  @Prop() updateContext!: (newValues: Partial<{ isOpen: boolean; searchValue: string; selectedValue: string }>) => void;
  @Prop() fullRenderOption?: (option: FormSelectItem, isSelected: boolean, handleSelection: (option: FormSelectItem) => void) => any;

  @State() dropdownAncestorClasses = '';

  @Element() el!: HTMLElement;

  private dropdownEl: HTMLElement | null = null;
  private boundKeyDown = (e: KeyboardEvent) => this.handleKeyDown(e);

  setDropdownRef = (el: HTMLElement | null) => {
    this.dropdownEl = el;
  };

  toggleDropdown = () => {
    if (this.isOpen && !this.searchable) this.updateContext({ isOpen: false });
    else this.openDropdown();
  };

  openDropdown = () => {
    this.adjustDropdownPosition();

    this.updateContext({ isOpen: true });
  };

  async componentDidLoad() {
    document.addEventListener('click', this.closeDropdown);
    document.addEventListener('keydown', this.boundKeyDown);
    window.addEventListener('resize', this.handleResize);
    window.addEventListener('scroll', this.handleScroll, true);

    this.dropdownAncestorClasses = `${this.name}-select ${getCustomClassesForPortal(this.el)}`;
  }

  private getInputEl(): HTMLElement | null {
    return this.el.getElementsByClassName('form-input-select')[0] as HTMLElement | null;
  }

  private isInputInView(rect: DOMRect): boolean {
    return rect.bottom > 0 && rect.top < window.innerHeight && rect.right > 0 && rect.left < window.innerWidth;
  }

  adjustDropdownPosition = () => {
    const dropdown = this.dropdownEl;
    const input = this.getInputEl();

    if (!dropdown || !input) return;

    const inputRect = input.getBoundingClientRect();

    if (!this.isInputInView(inputRect)) {
      this.updateContext({ isOpen: false });
      return;
    }

    dropdown.style.setProperty('--shift-select-width', `${inputRect.width}px`);

    const dropdownHeight = dropdown.offsetHeight;
    const spaceBelow = window.innerHeight - inputRect.bottom - this.gap;
    const spaceAbove = inputRect.top - this.gap;
    const openUpwards = this.forceOpenUpwards || (spaceBelow < dropdownHeight && spaceAbove > spaceBelow);

    const top = openUpwards ? inputRect.top - dropdownHeight - this.gap : inputRect.bottom + this.gap;

    dropdown.style.setProperty('--shift-select-left', `${inputRect.left}px`);
    dropdown.style.setProperty('--shift-select-top', `${top}px`);
  };

  handleKeyDown(event: KeyboardEvent) {
    if (!this.isOpen) return;
    if (event.key === 'Escape') this.updateContext({ isOpen: false });
  }

  closeDropdown = (event: MouseEvent) => {
    const path = event.composedPath();
    if (path.includes(this.el)) return;
    if (this.dropdownEl && path.includes(this.dropdownEl)) return;
    this.updateContext({ isOpen: false });
  };

  async disconnectedCallback() {
    document.removeEventListener('click', this.closeDropdown);
    document.removeEventListener('keydown', this.boundKeyDown);
    window.removeEventListener('resize', this.handleResize);
    window.removeEventListener('scroll', this.handleScroll, true);
  }

  handleResize = () => {
    if (this.isOpen) this.adjustDropdownPosition();
  };

  handleScroll = (event: Event) => {
    if (!this.isOpen) return;
    const target = event.target as Node;
    if (this.dropdownEl && (this.dropdownEl === target || this.dropdownEl.contains(target))) return;
    this.adjustDropdownPosition();
  };

  onSearchInput = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;

    this.updateContext({ searchValue: target.value, selectedValue: '' });
  };

  clearInput = () => {
    this.updateContext({ searchValue: '', selectedValue: '' });

    const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;

    selectButton.focus();
    if (!this.isOpen) this.openDropdown();
  };

  render() {
    const selectedItem = this.options.find(item => this.selectedValue === item.value);

    const dropdownProps = {
      name: this.name,
      isOpen: this.isOpen,
      options: this.options,
      hasTick: this.hasTick,
      hasArrow: this.hasArrow,
      isLoading: this.isLoading,
      renderOption: this.renderOption,
      selectedValue: this.selectedValue,
      setElementRef: this.setDropdownRef,
      reverseOptions: this.reverseOptions,
      noSelectionText: this.noSelectionText,
      handleSelection: this.handleSelection,
      fullRenderOption: this.fullRenderOption,
      fetchingErrorText: this.fetchingErrorText,
    };

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

          {
            // @ts-ignore
            false && <shift-select-dropdown />
          }
          <shift-portal tag="shift-select-dropdown" inheritedClasses={this.dropdownAncestorClasses} componentProps={dropdownProps} />
        </div>
      </Host>
    );
  }
}
