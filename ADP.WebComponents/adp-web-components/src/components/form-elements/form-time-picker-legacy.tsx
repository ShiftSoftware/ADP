import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';

@Component({
  shadow: false,
  tag: 'form-time-picker',
  styleUrl: 'form-inputs.css',
})
export class FormTimePicker implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() isDisabled?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop({ mutable: true }) defaultValue?: string;
  @Prop() minTime?: string; // HH:mm
  @Prop() maxTime?: string; // HH:mm
  @Prop() minuteStep?: number = 1;

  @State() isOpen: boolean = false;
  @State() selectedValue: string = '';
  @State() openUpwards: boolean = false;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);

    if (this.staticValue) this.defaultValue = this.staticValue;
    if (this.defaultValue) this.selectedValue = this.defaultValue;
  }

  @Watch('staticValue')
  async onStaticValueChange(newStaticValue?: string) {
    if (newStaticValue) this.selectedValue = newStaticValue;
  }

  async componentDidLoad() {
    document.addEventListener('click', this.handleOutsideClick);
    document.addEventListener('keydown', this.handleKeyDown);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
    document.removeEventListener('click', this.handleOutsideClick);
    document.removeEventListener('keydown', this.handleKeyDown);
  }

  reset(newValue?: string) {
    this.selectedValue = (newValue as string) || this.defaultValue || '';
  }

  handleOutsideClick = (event: MouseEvent) => {
    if (!this.el.contains(event.target as Node)) this.isOpen = false;
  };

  handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Escape' && this.isOpen) this.isOpen = false;
  };

  toggleDropdown = () => {
    if (this.isOpen) {
      this.isOpen = false;
      return;
    }
    this.adjustDropdownPosition();
  };

  adjustDropdownPosition() {
    requestAnimationFrame(() => {
      const input = this.el.querySelector('.form-input-style') as HTMLElement;
      if (!input) return;

      const rect = input.getBoundingClientRect();
      const spaceBelow = window.innerHeight - rect.bottom - 20;

      this.openUpwards = spaceBelow < 220;

      setTimeout(() => {
        this.isOpen = true;
        this.scrollToSelected();
      }, 10);
    });
  }

  private scrollToSelected() {
    requestAnimationFrame(() => {
      const hourCol = this.el.querySelector('.form-time-picker-hours') as HTMLElement;
      const minuteCol = this.el.querySelector('.form-time-picker-minutes') as HTMLElement;

      const scrollTo = (container: HTMLElement) => {
        const selected = container?.querySelector('.selected') as HTMLElement;
        if (selected) selected.scrollIntoView({ block: 'center' });
      };

      scrollTo(hourCol);
      scrollTo(minuteCol);
    });
  }

  private toTimeMinutes(time: string): number {
    const [h, m] = time.split(':').map(Number);
    return h * 60 + m;
  }

  private isHourDisabled(hour: number): boolean {
    const step = this.minuteStep || 1;

    for (let m = 0; m < 60; m += step) {
      if (!this.isMinuteDisabled(hour, m)) return false;
    }

    return true;
  }

  private isMinuteDisabled(hour: number, minute: number): boolean {
    const time = hour * 60 + minute;
    if (this.minTime && time < this.toTimeMinutes(this.minTime)) return true;
    if (this.maxTime && time > this.toTimeMinutes(this.maxTime)) return true;
    return false;
  }

  private getSelectedHour(): number {
    if (!this.selectedValue) return -1;
    return parseInt(this.selectedValue.split(':')[0], 10);
  }

  private getSelectedMinute(): number {
    if (!this.selectedValue) return -1;
    return parseInt(this.selectedValue.split(':')[1], 10);
  }

  private formatTime(hour: number, minute: number): string {
    return `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
  }

  selectHour = (hour: number) => {
    const currentMinute = this.getSelectedMinute();
    const minute = currentMinute >= 0 ? currentMinute : 0;
    this.selectedValue = this.formatTime(hour, minute);
  };

  selectMinute = (minute: number) => {
    const currentHour = this.getSelectedHour();
    const hour = currentHour >= 0 ? currentHour : 0;
    this.selectedValue = this.formatTime(hour, minute);
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    const selectedHour = this.getSelectedHour();
    const selectedMinute = this.getSelectedMinute();
    const step = this.minuteStep || 1;

    const hours = Array.from({ length: 24 }, (_, i) => i);
    const minutes = Array.from({ length: Math.ceil(60 / step) }, (_, i) => i * step);

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <form-shadow-input name={this.name} form={this.form} value={this.selectedValue} />

          <div part={`${this.name}-container form-input-container`} class={cn('form-input-container', { open: this.isOpen })}>
            <input
              type="text"
              readOnly
              disabled={isDisabled}
              value={this.selectedValue}
              onClick={this.toggleDropdown}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              class={cn('form-input-style form-input-select', {
                'form-input-error-style': isError,
              })}
            />

            <span part={`${this.name}-icon form-time-picker-icon`} class="form-input-icon form-input-icon-end">
              <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="size-full">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 16 14" />
              </svg>
            </span>

            <div
              part={`${this.name}-dropdown form-time-picker-dropdown`}
              class={cn('form-picker-dropdown', {
                upwards: this.openUpwards,
                downwards: !this.openUpwards,
              })}
            >
              <div class="form-time-picker-columns">
                <div class="form-time-picker-column form-time-picker-hours">
                  <div class="form-time-picker-column-header">HH</div>
                  {hours.map(hour => {
                    const hourDisabled = this.isHourDisabled(hour);

                    return (
                      <button
                        type="button"
                        disabled={hourDisabled}
                        onClick={() => this.selectHour(hour)}
                        part={cn('form-time-picker-option', { 'form-time-picker-option-selected': hour === selectedHour })}
                        class={cn('form-time-picker-option', {
                          selected: hour === selectedHour,
                        })}
                      >
                        {String(hour).padStart(2, '0')}
                      </button>
                    );
                  })}
                </div>
                <div class="form-time-picker-column form-time-picker-minutes">
                  <div class="form-time-picker-column-header">MM</div>
                  {minutes.map(minute => {
                    const minuteDisabled = selectedHour >= 0 ? this.isMinuteDisabled(selectedHour, minute) : false;

                    return (
                      <button
                        type="button"
                        disabled={minuteDisabled}
                        onClick={() => this.selectMinute(minute)}
                        part={cn('form-time-picker-option', { 'form-time-picker-option-selected': minute === selectedMinute })}
                        class={cn('form-time-picker-option', {
                          selected: minute === selectedMinute,
                        })}
                      >
                        {String(minute).padStart(2, '0')}
                      </button>
                    );
                  })}
                </div>
              </div>
            </div>
          </div>

          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}
