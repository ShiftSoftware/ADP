import { Component, Element, Host, Prop, State, h } from '@stencil/core';
import { FormSelectItem } from 'components';
import cn from '~lib/cn';
import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';
import { ArrowUpIcon } from '~assets/arrow-up-icon';
import { AddIcon } from '~assets/add-icon';

const PORTAL_STYLES = `
  @keyframes spin-animation { to { transform: rotate(360deg); } }

  .form-select-container {
    z-index: 10;
    display: flex;
    flex-direction: column;
    pointer-events: none;
    position: fixed;
    background-color: #fff;
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    box-shadow: 0 1px 3px 0 rgba(0,0,0,0.1), 0 1px 2px -1px rgba(0,0,0,0.1);
    opacity: 0;
    transition-property: opacity;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
    transition-duration: 0.3s;
    max-height: 250px;
    overflow: auto;
    width: var(--dropdown-width, auto);
  }
  .form-select-container.open {
    opacity: 1;
    pointer-events: auto;
  }
  .form-select-container.upwards {
    transform: translateY(-100%);
  }

  .form-select-option {
    display: flex;
    justify-content: space-between;
    align-items: center;
    text-align: start;
    padding: 0.5rem 1rem;
    background: none;
    border: none;
    width: 100%;
    font: inherit;
    color: inherit;
    cursor: pointer;
  }
  .form-select-option:hover {
    background-color: rgb(241 245 249);
  }
  .form-select-option.selected {
    background-color: rgb(226 232 240);
  }

  .form-select-option-tick {
    width: 1.25rem;
    height: 1.25rem;
    opacity: 0;
    transition-property: opacity;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
    transition-duration: 0.3s;
  }
  .form-select-option.selected .form-select-option-tick {
    opacity: 1;
  }

  .form-select-empty-container {
    height: 100px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .form-select-empty-container.error {
    color: rgb(239 68 68);
  }

  .form-select-spinner {
    animation: spin-animation 2s linear infinite;
    width: 22px;
    height: 22px;
  }
`;

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

  private portalEl: HTMLDivElement;
  private portalShadow: ShadowRoot;
  private scrollRafId: number | null = null;

  toggleDropdown = () => {
    if (this.isOpen && !this.searchable) this.updateContext({ isOpen: false });
    else this.adjustDropdownPosition();
  };

  async componentDidLoad() {
    this.portalEl = document.createElement('div');
    this.portalEl.classList.add('shift-select-portal');
    this.portalShadow = this.portalEl.attachShadow({ mode: 'open' });

    const style = document.createElement('style');
    style.textContent = PORTAL_STYLES;
    this.portalShadow.appendChild(style);

    document.body.appendChild(this.portalEl);

    // Move the dropdown that Stencil just rendered into the portal
    this.moveToPortal();

    document.addEventListener('click', this.closeDropdown);
    document.addEventListener('keydown', this.handleKeyDown.bind(this));
    window.addEventListener('resize', this.handleResize);
    window.addEventListener('scroll', this.handleScroll, true);
  }

  componentWillRender() {
    // Before Stencil renders, move the dropdown back so vdom diffing works
    if (!this.portalShadow) return;
    const dropdown = this.portalShadow.querySelector('.form-select-container') as HTMLElement;
    const container = this.el?.querySelector('.form-input-container') as HTMLElement;
    if (dropdown && container) {
      container.appendChild(dropdown);
    }
  }

  componentDidRender() {
    // After Stencil patched the DOM, move the dropdown back to the portal
    this.moveToPortal();
  }

  private moveToPortal() {
    if (!this.portalShadow) return;
    const dropdown = this.el.querySelector('.form-select-container') as HTMLElement;
    if (!dropdown) return;

    this.portalShadow.appendChild(dropdown);
    dropdown.classList.toggle('open', this.isOpen);
  }

  private getDropdownEl(): HTMLDivElement | null {
    return this.portalShadow?.querySelector('.form-select-container') as HTMLDivElement;
  }

  adjustDropdownPosition() {
    requestAnimationFrame(() => {
      const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;
      const selectContainer = this.getDropdownEl();
      if (!selectButton || !selectContainer) return;

      const rect = selectButton.getBoundingClientRect();

      selectContainer.style.setProperty('--dropdown-width', `${rect.width}px`);
      selectContainer.style.left = `${rect.left}px`;

      const spaceBelow = window.innerHeight - rect.bottom - 20;
      const openUp = spaceBelow < selectContainer.getBoundingClientRect().height || this.forceOpenUpwards;

      this.openUpwards = openUp;
      selectContainer.style.top = openUp ? `${rect.top - 8}px` : `${rect.bottom + 8}px`;

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
    const path = event.composedPath();
    if (path.includes(this.el) || path.includes(this.portalEl)) return;
    this.updateContext({ isOpen: false });
  };

  async disconnectedCallback() {
    document.removeEventListener('click', this.closeDropdown);
    document.removeEventListener('keydown', this.handleKeyDown.bind(this));
    window.removeEventListener('resize', this.handleResize);
    window.removeEventListener('scroll', this.handleScroll, true);
    this.portalEl?.remove();
  }

  handleResize = () => {
    if (this.isOpen) {
      this.updateContext({ isOpen: false });
    }
  };

  handleScroll = (event: Event) => {
    if (!this.isOpen) return;

    const path = event.composedPath();
    if (path.includes(this.portalEl)) return;

    if (this.scrollRafId) return;
    this.scrollRafId = requestAnimationFrame(() => {
      this.scrollRafId = null;
      if (!this.isOpen) return;

      const selectButton = this.el.getElementsByClassName('form-input-select')[0] as HTMLDivElement;
      if (!selectButton) return;

      const rect = selectButton.getBoundingClientRect();
      const isVisible = rect.bottom > 0 && rect.top < window.innerHeight && rect.right > 0 && rect.left < window.innerWidth;

      if (!isVisible) {
        this.updateContext({ isOpen: false });
        return;
      }

      const container = this.getDropdownEl();
      if (!container) return;

      const spaceBelow = window.innerHeight - rect.bottom - 20;
      this.openUpwards = spaceBelow < container.getBoundingClientRect().height || this.forceOpenUpwards;

      container.style.left = `${rect.left}px`;
      container.style.setProperty('--dropdown-width', `${rect.width}px`);
      container.style.top = this.openUpwards ? `${rect.top - 8}px` : `${rect.bottom + 8}px`;
    });
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
              'form-input-error-style': this?.isError,
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
                    {this.hasTick && <TickIcon part={`${this.name}-tick-icon form-select-option-tick`} class="form-select-option-tick" />}
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
