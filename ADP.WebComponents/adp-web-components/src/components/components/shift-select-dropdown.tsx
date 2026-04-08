import { Component, Host, Prop, h } from '@stencil/core';
import { FormSelectItem } from '~features/form-hook';
import cn from '~lib/cn';
import Loader from '~assets/loader.svg';
import { TickIcon } from '~assets/tick-icon';

@Component({
  shadow: true,
  tag: 'shift-select-dropdown',
  styleUrl: 'shift-select-dropdown.css',
})
export class ShiftSelectDropdown {
  @Prop() hasTick = true;
  @Prop() hasArrow = true;
  @Prop() name?: string = '';
  @Prop() isLoading?: boolean;
  @Prop() isOpen: boolean = false;
  @Prop() noSelectionText?: string;
  @Prop() direction: string = 'ltr';
  @Prop() selectedValue: string = '';
  @Prop() options: FormSelectItem[] = [];
  @Prop() reverseOptions: boolean = false;
  @Prop() fetchingErrorText?: string | boolean;
  @Prop() setElementRef?: (el: HTMLElement | null) => void;
  @Prop() handleSelection!: (option: FormSelectItem) => void;
  @Prop() renderOption?: (option: FormSelectItem, isSelected: boolean) => any;
  @Prop() fullRenderOption?: (option: FormSelectItem, isSelected: boolean, handleSelection: (option: FormSelectItem) => void) => any;

  private containerEl: HTMLElement | null = null;

  componentDidLoad() {
    this.setElementRef?.(this.containerEl);
  }

  disconnectedCallback() {
    this.setElementRef?.(null);
  }

  render() {
    const containerIdentifiers = cn(`${this.name}-select-container shift-select-container`, {
      'shift-select-container-open': this.isOpen,
    });

    const selectButtonIdentifiers = (option: FormSelectItem) =>
      cn(`${this.name}-select-option shift-select-option`, { 'shift-select-option-selected': this.selectedValue === option.value });

    const tickIdentifiers = (option: FormSelectItem) =>
      cn(`${this.name}-tick-icon shift-select-option-tick`, {
        'shift-select-option-tick-selected': this.selectedValue === option.value,
      });

    const emptyContainerIdentifier = cn(`${this.name}-select-empty-container shift-select-empty-container`, { 'shift-select-empty-container-error': this.fetchingErrorText });

    const spinnerIdentifiers = cn(`${this.name}-select-spinner shift-select-spinner`);

    const optionLabelIdentifier = cn(`${this.name}-select-option-label shift-select-option-label`);
    return (
      <Host>
        <div
          dir={this.direction}
          part={containerIdentifiers}
          class={containerIdentifiers}
          onWheel={e => e.stopPropagation()}
          onTouchMove={e => e.stopPropagation()}
          ref={el => (this.containerEl = el as HTMLElement | null)}
        >
          {!!this.options.length &&
            (this.reverseOptions ? [...this.options].reverse() : this.options).map(option =>
              this.fullRenderOption ? (
                this.fullRenderOption(option, this.selectedValue === option.value, this.handleSelection)
              ) : (
                <button type="button" part={selectButtonIdentifiers(option)} class={selectButtonIdentifiers(option)} onClick={() => this.handleSelection(option)}>
                  {this.renderOption ? (
                    this.renderOption(option, this.selectedValue === option.value)
                  ) : (
                    <div part={optionLabelIdentifier} class={optionLabelIdentifier}>
                      {option.label}
                    </div>
                  )}
                  {this.hasTick && <TickIcon part={tickIdentifiers(option)} class={tickIdentifiers(option)} />}
                </button>
              ),
            )}
          {!this.options.length && (
            <div part={emptyContainerIdentifier} class={emptyContainerIdentifier}>
              {this.fetchingErrorText}
              {!this.fetchingErrorText && (this.isLoading ? <img part={spinnerIdentifiers} class={spinnerIdentifiers} src={Loader} /> : this.noSelectionText)}
            </div>
          )}
        </div>
      </Host>
    );
  }
}
