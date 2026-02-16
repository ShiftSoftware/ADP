import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';

interface DayCell {
  day: number;
  month: number;
  year: number;
  isCurrentMonth: boolean;
}

@Component({
  shadow: false,
  tag: 'form-date-picker',
  styleUrl: 'form-inputs.css',
})
export class FormDatePicker implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() isDisabled?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop({ mutable: true }) defaultValue?: string;
  @Prop() language?: string = 'en';
  @Prop() minDate?: string; // YYYY-MM-DD
  @Prop() maxDate?: string; // YYYY-MM-DD
  @Prop() disabledDates?: (date: Date) => boolean;
  @Prop() firstDayOfWeek?: number = 0; // 0=Sun, 1=Mon, 6=Sat

  @State() isOpen: boolean = false;
  @State() selectedValue: string = '';
  @State() viewYear: number;
  @State() viewMonth: number;
  @State() openUpwards: boolean = false;

  @Element() el: HTMLElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);

    if (this.staticValue) this.defaultValue = this.staticValue;
    if (this.defaultValue) this.selectedValue = this.defaultValue;

    const initDate = this.selectedValue ? new Date(this.selectedValue + 'T00:00:00') : new Date();
    this.viewYear = initDate.getFullYear();
    this.viewMonth = initDate.getMonth();
  }

  @Watch('staticValue')
  async onStaticValueChange(newStaticValue?: string) {
    if (newStaticValue) {
      this.selectedValue = newStaticValue;
      const date = new Date(newStaticValue + 'T00:00:00');
      this.viewYear = date.getFullYear();
      this.viewMonth = date.getMonth();
    }
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

    if (this.selectedValue) {
      const date = new Date(this.selectedValue + 'T00:00:00');
      this.viewYear = date.getFullYear();
      this.viewMonth = date.getMonth();
    }
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

      this.openUpwards = spaceBelow < 320;

      setTimeout(() => {
        this.isOpen = true;
      }, 10);
    });
  }

  // Date utilities

  private toDateString(year: number, month: number, day: number): string {
    return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }

  private isDateDisabled(year: number, month: number, day: number): boolean {
    const dateStr = this.toDateString(year, month, day);
    if (this.minDate && dateStr < this.minDate) return true;
    if (this.maxDate && dateStr > this.maxDate) return true;
    if (this.disabledDates) return this.disabledDates(new Date(year, month, day));
    return false;
  }

  private getCalendarDays(): DayCell[] {
    const year = this.viewYear;
    const month = this.viewMonth;
    const firstDayOfMonth = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();
    const leadingDays = (firstDayOfMonth - this.firstDayOfWeek + 7) % 7;

    const days: DayCell[] = [];

    for (let i = leadingDays - 1; i >= 0; i--) {
      const prevMonth = month === 0 ? 11 : month - 1;
      const prevYear = month === 0 ? year - 1 : year;
      days.push({ day: daysInPrevMonth - i, month: prevMonth, year: prevYear, isCurrentMonth: false });
    }

    for (let i = 1; i <= daysInMonth; i++) {
      days.push({ day: i, month, year, isCurrentMonth: true });
    }

    const remaining = 42 - days.length;
    for (let i = 1; i <= remaining; i++) {
      const nextMonth = month === 11 ? 0 : month + 1;
      const nextYear = month === 11 ? year + 1 : year;
      days.push({ day: i, month: nextMonth, year: nextYear, isCurrentMonth: false });
    }

    return days;
  }

  private getWeekDayNames(): string[] {
    const names: string[] = [];
    const baseDate = new Date(2024, 0, 7); // known Sunday

    for (let i = 0; i < 7; i++) {
      const d = new Date(baseDate);
      d.setDate(d.getDate() + ((this.firstDayOfWeek + i) % 7));
      names.push(new Intl.DateTimeFormat(this.language, { weekday: 'short' }).format(d));
    }

    return names;
  }

  private getMonthYearTitle(): string {
    return new Intl.DateTimeFormat(this.language, { month: 'long', year: 'numeric' }).format(new Date(this.viewYear, this.viewMonth));
  }

  private formatDisplayValue(): string {
    if (!this.selectedValue) return '';

    try {
      return new Intl.DateTimeFormat(this.language, { year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date(this.selectedValue + 'T00:00:00'));
    } catch {
      return this.selectedValue;
    }
  }

  private canGoPrev(): boolean {
    if (!this.minDate) return true;
    const [minYear, minMonth] = this.minDate.split('-').map(Number);
    return this.viewYear > minYear || (this.viewYear === minYear && this.viewMonth > minMonth - 1);
  }

  private canGoNext(): boolean {
    if (!this.maxDate) return true;
    const [maxYear, maxMonth] = this.maxDate.split('-').map(Number);
    return this.viewYear < maxYear || (this.viewYear === maxYear && this.viewMonth < maxMonth - 1);
  }

  selectDate = (year: number, month: number, day: number) => {
    this.selectedValue = this.toDateString(year, month, day);
    this.isOpen = false;
  };

  prevMonth = () => {
    if (!this.canGoPrev()) return;

    if (this.viewMonth === 0) {
      this.viewMonth = 11;
      this.viewYear--;
    } else {
      this.viewMonth--;
    }
  };

  nextMonth = () => {
    if (!this.canGoNext()) return;

    if (this.viewMonth === 11) {
      this.viewMonth = 0;
      this.viewYear++;
    } else {
      this.viewMonth++;
    }
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    const displayValue = this.formatDisplayValue();
    const weekDays = this.getWeekDayNames();
    const days = this.getCalendarDays();
    const monthTitle = this.getMonthYearTitle();
    const now = new Date();
    const todayStr = this.toDateString(now.getFullYear(), now.getMonth(), now.getDate());

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
              value={displayValue}
              onClick={this.toggleDropdown}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              class={cn('form-input-style form-input-select', {
                'form-input-error-style': isError,
              })}
            />

            <span part={`${this.name}-icon form-date-picker-icon`} class="form-input-icon form-input-icon-end">
              <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="size-full">
                <path d="M8 2v4" />
                <path d="M16 2v4" />
                <rect width="18" height="18" x="3" y="4" rx="2" />
                <path d="M3 10h18" />
              </svg>
            </span>

            <div
              part={`${this.name}-dropdown form-date-picker-dropdown`}
              class={cn('form-picker-dropdown', {
                upwards: this.openUpwards,
                downwards: !this.openUpwards,
              })}
            >
              <div class="form-date-picker-header">
                <button
                  type="button"
                  disabled={!this.canGoPrev()}
                  part={`${this.name}-prev form-date-picker-nav`}
                  onClick={this.prevMonth}
                  class="form-date-picker-nav"
                >
                  <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="size-4 rtl:rotate-180">
                    <path d="m15 18-6-6 6-6" />
                  </svg>
                </button>
                <span part={`${this.name}-title form-date-picker-title`} class="form-date-picker-title">
                  {monthTitle}
                </span>
                <button
                  type="button"
                  disabled={!this.canGoNext()}
                  part={`${this.name}-next form-date-picker-nav`}
                  onClick={this.nextMonth}
                  class="form-date-picker-nav"
                >
                  <svg aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="size-4 rtl:rotate-180">
                    <path d="m9 18 6-6-6-6" />
                  </svg>
                </button>
              </div>

              <div class="form-date-picker-grid">
                {weekDays.map(day => (
                  <div class="form-date-picker-weekday">{day}</div>
                ))}

                {days.map(cell => {
                  const dateStr = this.toDateString(cell.year, cell.month, cell.day);
                  const isSelected = dateStr === this.selectedValue;
                  const isToday = dateStr === todayStr;
                  const dateDisabled = !cell.isCurrentMonth || this.isDateDisabled(cell.year, cell.month, cell.day);

                  return (
                    <button
                      type="button"
                      disabled={dateDisabled}
                      onClick={() => this.selectDate(cell.year, cell.month, cell.day)}
                      part={cn('form-date-picker-day', { 'form-date-picker-day-selected': isSelected, 'form-date-picker-day-today': isToday })}
                      class={cn('form-date-picker-day', {
                        selected: isSelected,
                        today: isToday && !isSelected,
                        outside: !cell.isCurrentMonth,
                      })}
                    >
                      {cell.day}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}
