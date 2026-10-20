import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';
import { FormSelectItem } from '../../components';
import cn from '~lib/cn';

import { ArrowUpIcon } from '~assets/arrow-up-icon';
import { AddIcon } from '~assets/add-icon';
import getCustomClassesForPortal from '~lib/get-custom-classes-for-portal';

import { SHEET_QUERY } from './shift-popover/sheet-query';
import { SelectDrawerMode, selectDrawerMode } from './select-drawer';

// Referenced by variables, so Stencil adds neither the popover nor the sheet to every form: hosts without a drawer keep their bundles, stylesheets and registry.
const Popover = 'shift-popover';
const Sheet = 'shift-select-sheet';

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
  @Prop() direction?: string = 'ltr';
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
  @Prop() selectDrawer?: SelectDrawerMode | 'off';
  @Prop() drawerLabel?: string;
  @Prop() drawerSearchLabel?: string;

  @State() dropdownAncestorClasses = '';
  @State() narrow = false;

  @Element() el!: HTMLElement;

  private dropdownEl: HTMLElement | null = null;
  private boundKeyDown = (e: KeyboardEvent) => this.handleKeyDown(e);
  private sheetQuery?: MediaQueryList;

  setDropdownRef = (el: HTMLElement | null) => {
    this.dropdownEl = el;
  };

  @Watch('selectDrawer')
  onSelectDrawerChange() {
    this.syncSheetQuery();
  }

  connectedCallback() {
    if (this.selectDrawer !== undefined) this.syncSheetQuery();
  }

  // Without both shift-popover and shift-select-sheet defined (a per-component host that did not load them) the dropdown stays.
  private drawerMode(): SelectDrawerMode | undefined {
    const mode = selectDrawerMode(this.selectDrawer);
    return mode && typeof customElements !== 'undefined' && customElements.get(Popover) && customElements.get(Sheet) ? mode : undefined;
  }

  private drawerNow(): boolean {
    const mode = this.drawerMode();
    return mode === 'always' || (mode === 'mobile' && this.narrow);
  }

  private syncSheetQuery() {
    const wanted = selectDrawerMode(this.selectDrawer) === 'mobile' && typeof window !== 'undefined' && !!window.matchMedia;

    if (wanted && !this.sheetQuery) {
      this.sheetQuery = window.matchMedia(SHEET_QUERY);
      this.sheetQuery.addEventListener?.('change', this.onSheetQueryChange);
    } else if (!wanted && this.sheetQuery) {
      this.sheetQuery.removeEventListener?.('change', this.onSheetQueryChange);
      this.sheetQuery = undefined;
    }

    this.narrow = !!this.sheetQuery?.matches;
  }

  // Crossing the breakpoint swaps the dropdown for the drawer, so whichever layer is open closes first.
  private onSheetQueryChange = () => {
    const before = this.drawerNow();
    this.narrow = !!this.sheetQuery?.matches;
    if (this.isOpen && before !== this.drawerNow()) this.updateContext({ isOpen: false });
  };

  private onTriggerKeyDown = (event: KeyboardEvent) => {
    if (!this.drawerNow() || this.isOpen || this.disableInput || !['Enter', ' ', 'ArrowDown'].includes(event.key)) return;

    event.preventDefault();
    this.openDropdown();
  };

  private onDrawerOpenChange = (event: CustomEvent<{ open: boolean }>) => {
    event.stopPropagation();
    if (!event.detail.open && this.isOpen) this.updateContext({ isOpen: false });
  };

  toggleDropdown = () => {
    if (this.isOpen && !this.searchable) this.updateContext({ isOpen: false });
    else this.openDropdown();
  };

  openDropdown = () => {
    const drawer = this.drawerNow();
    if (!drawer) this.adjustDropdownPosition();

    // The drawer has its own search, so a leftover dropdown filter would only hide options behind a read-only trigger.
    if (drawer && this.searchValue && !this.options.some(item => item.value === this.selectedValue)) this.updateContext({ isOpen: true, searchValue: '' });
    else this.updateContext({ isOpen: true });
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
    if (!this.isOpen || this.drawerNow()) return;
    if (event.key === 'Escape') this.updateContext({ isOpen: false });
  }

  closeDropdown = (event: MouseEvent) => {
    if (this.drawerNow()) return;
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
    this.sheetQuery?.removeEventListener?.('change', this.onSheetQueryChange);
    this.sheetQuery = undefined;
  }

  handleResize = () => {
    if (this.isOpen && !this.drawerNow()) this.adjustDropdownPosition();
  };

  handleScroll = (event: Event) => {
    if (!this.isOpen || this.drawerNow()) return;
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
    if (!this.isOpen && !this.drawerNow()) this.openDropdown();
  };

  private renderDrawer(mode: SelectDrawerMode, drawerNow: boolean) {
    return (
      <Popover
        open={this.isOpen && drawerNow}
        anchor={drawerNow ? this.getInputEl() || undefined : undefined}
        label={this.drawerLabel || undefined}
        mobileSheet={mode === 'mobile'}
        alwaysSheet={mode === 'always'}
        onOpenChange={this.onDrawerOpenChange}
      >
        <Sheet
          class={this.dropdownAncestorClasses}
          name={this.name}
          open={this.isOpen && drawerNow}
          label={this.drawerLabel}
          searchLabel={this.drawerSearchLabel}
          hasTick={this.hasTick}
          searchable={this.searchable}
          isLoading={this.isLoading}
          direction={this.direction}
          options={this.options}
          selectedValue={this.selectedValue}
          reverseOptions={this.reverseOptions}
          noSelectionText={this.noSelectionText}
          fetchingErrorText={this.fetchingErrorText}
          handleSelection={this.handleSelection}
        />
      </Popover>
    );
  }

  render() {
    const selectedItem = this.options.find(item => this.selectedValue === item.value);
    const drawerMode = this.drawerMode();
    const drawerNow = this.drawerNow();
    const triggerAria = drawerMode ? { 'role': 'combobox', 'aria-expanded': String(this.isOpen) } : {};

    const dropdownProps = {
      name: this.name,
      isOpen: drawerNow ? false : this.isOpen,
      options: this.options,
      hasTick: this.hasTick,
      hasArrow: this.hasArrow,
      direction: this.direction,
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
            value={this.searchable && !drawerNow ? this.searchValue : selectedItem?.label || ''}
            readOnly={!this.searchable || drawerNow}
            onInput={this.onSearchInput}
            onClick={this.toggleDropdown}
            onKeyDown={drawerMode ? this.onTriggerKeyDown : undefined}
            {...triggerAria}
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
          {drawerMode !== 'always' && <shift-portal tag="shift-select-dropdown" inheritedClasses={this.dropdownAncestorClasses} componentProps={dropdownProps} />}
          {drawerMode && this.renderDrawer(drawerMode, drawerNow)}
        </div>
      </Host>
    );
  }
}
