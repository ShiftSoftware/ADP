import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';
import { AnyObjectSchema } from 'yup';

import type { LanguageKeys } from '~features/multi-lingual';
import type { FormHook } from '~features/form-hook/form-hook';
import type { FormElement } from '~features/form-hook/interface';
import type { FormInputLocalization, FormInputMeta } from '~features/form-hook/inputs/form-input';
import { getNestedValue } from '~lib/get-nested-value';

import '~lib/middleware';

import type { CalendarAppearance, CalendarColorScheme, CalendarDayMetaMap, CalendarMonthChangeDetail, CalendarSize } from '../shift-calendar/shift-calendar';
import { CalendarNumerals, calendarLocale } from '../shift-calendar/calendar-locale';
import { DEFAULT_FORMAT } from './types/date/date-format';
import { InputStrings, inputStrings } from './input-locale';
import { INPUT_TYPES, ShiftInputType } from './types';
import type { InputField, InputType } from './types/input-type';

export interface InputValueChangeDetail {
  value: string;
}

/**
 * @part calendar-button - rendered by the date type module, so declared here for the docs.
 */
@Component({
  shadow: { delegatesFocus: true },
  tag: 'shift-input',
  styleUrl: 'shift-input.css',
})
export class ShiftInput implements FormElement, InputField {
  @Element() el!: HTMLElement;

  @Prop({ reflect: true }) type: ShiftInputType = 'text';
  @Prop({ reflect: true }) name: string = '';
  @Prop({ mutable: true, reflect: true }) value: string = '';
  @Prop({ mutable: true }) defaultValue?: string;
  @Prop() label?: string;
  @Prop() placeholder?: string;
  @Prop() hint?: string;
  @Prop() isRequired: boolean = false;
  @Prop() isDisabled: boolean = false;
  @Prop() readonly: boolean = false;
  @Prop() clearable: boolean = false;
  @Prop() autocomplete?: string;
  @Prop() inputmode?: string;
  @Prop() errorMessage?: string;
  @Prop() form?: FormHook<Record<string, unknown>>;
  @Prop() language: LanguageKeys = 'en';
  @Prop() localization?: FormInputLocalization<{ hint?: string; unavailableMessage?: string }> = {};
  @Prop({ reflect: true }) appearance?: CalendarAppearance;
  @Prop({ reflect: true }) colorScheme: CalendarColorScheme = 'light';
  @Prop({ reflect: true }) size: CalendarSize = 'md';

  @Prop() format: string = DEFAULT_FORMAT;
  @Prop() openOnControlClick: boolean = true;
  @Prop() month?: string;
  @Prop() min?: string;
  @Prop() max?: string;
  @Prop() enabledDates?: string | string[] | null;
  @Prop() disabledDates?: string | string[];
  @Prop() disabledWeekdays?: string | number[];
  @Prop() isDateDisabled?: (date: string) => boolean;
  @Prop() dayMeta?: CalendarDayMetaMap | string;
  @Prop() today?: string;
  @Prop() highlightToday: boolean = false;
  @Prop() weekStartsOn?: number;
  @Prop() numerals?: CalendarNumerals;
  @Prop() disableViews: boolean = false;
  @Prop() showToday: boolean = true;
  @Prop() busy: boolean = false;

  @Event({ bubbles: true, composed: true }) valueChange!: EventEmitter<InputValueChangeDetail>;
  @Event({ bubbles: true, composed: true }) inputFocus!: EventEmitter<void>;
  @Event({ bubbles: true, composed: true }) inputBlur!: EventEmitter<void>;
  @Event({ bubbles: true, composed: true }) monthChange!: EventEmitter<CalendarMonthChangeDetail>;

  @State() text: string = '';
  @State() typedError: string = '';
  @State() expanded: boolean = false;
  @State() focused: boolean = false;
  @State() shownError: string = '';

  input?: HTMLInputElement;
  control?: HTMLElement;

  private settingValue = false;
  private shownLabel = '';
  private uid = Math.random().toString(36).slice(2, 10);

  componentWillLoad() {
    this.form?.subscribe(this.name, this);
    const initial = this.module.normalize(this.value, this);
    this.value = initial || this.module.normalize(this.defaultValue, this);
    this.text = this.module.display(this.value, this);
  }

  componentDidLoad() {
    // The input exists only after the first render, and a type's popup anchors to it.
    this.module.mounted?.(this);
  }

  componentWillRender() {
    const error = this.currentError();
    if (error && error !== this.shownError) this.shownError = error;
  }

  disconnectedCallback() {
    this.form?.unsubscribe(this.name);
  }

  @Watch('value')
  onValueChange(next: string) {
    if (this.settingValue) return;

    const value = this.module.normalize(next, this);
    if (value !== next) {
      this.value = value;
      return;
    }

    this.typedError = '';
    this.text = this.module.display(value, this);
  }

  @Watch('type')
  onTypeChange() {
    this.value = this.module.normalize(this.value, this);
    this.typedError = '';
    this.text = this.module.display(this.value, this);
    this.module.mounted?.(this);
  }

  @Watch('format')
  onFormatChange() {
    if (!this.typedError) this.text = this.module.display(this.value, this);
  }

  @Method()
  async setFocus() {
    this.input?.focus();
  }

  reset(newValue?: unknown) {
    const next = this.module.normalize(newValue ?? this.defaultValue, this);

    this.settingValue = true;
    this.value = next;
    this.settingValue = false;
    this.typedError = '';
    this.text = this.module.display(next, this);
  }

  getValue() {
    return this.value;
  }

  partialValidation(validation: AnyObjectSchema): AnyObjectSchema {
    return validation.test(`${this.name}-typed`, `${this.name}-format`, () => !this.typedError) as AnyObjectSchema;
  }

  get module(): InputType {
    return INPUT_TYPES[this.type] ?? INPUT_TYPES.text;
  }

  get strings(): InputStrings {
    return inputStrings(this.language);
  }

  get fieldDisabled(): boolean {
    return this.isDisabled || !!this.formState?.disabled;
  }

  get fieldLabel(): string {
    return this.formLocalization()?.label || this.ownLocalization?.label || this.label || '';
  }

  choose(value: string) {
    this.typedError = '';
    this.text = this.module.display(value, this);
    this.commitValue(value);
  }

  forwardMonthChange(detail: unknown) {
    this.monthChange.emit(detail as CalendarMonthChangeDetail);
  }

  private get formState() {
    return this.form?.getInputState<FormInputMeta>(this.name);
  }

  private get required(): boolean {
    return this.isRequired || !!this.formState?.isRequired;
  }

  private get ownLocalization() {
    return this.localization?.[this.language];
  }

  // The same lookups as getInputLocalization, which cannot be imported here without the form validations module.
  private formLocalization() {
    const state = this.formState;
    if (!state) return null;

    const [locale, language] = this.form.getFormLocale();
    const own = this.localization?.[language ?? this.language];
    const lookup = (key?: string) => (key ? getNestedValue(locale, key) || key : '');
    const message = state.errorMessage || '';
    const errorTextMessage = (message.endsWith('-require') && own?.require) || (message.endsWith('-format') && own?.format) || locale?.[message] || message;

    return { label: own?.label || lookup(state.meta?.label), placeholder: own?.placeholder || lookup(state.meta?.placeholder), errorTextMessage };
  }

  private currentError(): string {
    if (this.typedError) return this.typedError;

    const state = this.formState;
    if (state?.isError) return this.formLocalization()?.errorTextMessage;

    return this.errorMessage || '';
  }

  private commitValue(next: string) {
    if (next === this.value) return;

    this.settingValue = true;
    this.value = next;
    this.settingValue = false;
    this.valueChange.emit({ value: next });
    // The form listens for input events on its <form>; this one carries the committed value out of the shadow root.
    this.el.dispatchEvent(new CustomEvent('input', { bubbles: true, composed: true }));
  }

  private commitText(): boolean {
    const result = this.module.read(this.text, this);

    if ('error' in result) {
      this.typedError = result.error;
      this.commitValue('');
      return false;
    }

    this.typedError = '';
    this.text = this.module.display(result.value, this);
    this.commitValue(result.value);
    return true;
  }

  private onInput = (event: InputEvent) => {
    this.text = (event.target as HTMLInputElement).value;
  };

  private onFocus = () => {
    this.focused = true;
    this.inputFocus.emit();
  };

  private onBlur = () => {
    this.focused = false;
    this.commitText();
    this.inputBlur.emit();
  };

  private onKeyDown = (event: KeyboardEvent) => {
    this.module.keydown?.(event, this);
    if (event.defaultPrevented || event.key !== 'Enter') return;

    if (!this.commitText()) event.preventDefault();
  };

  private onControlClick = (event: MouseEvent) => {
    if (event.target !== this.input || this.fieldDisabled || this.readonly) return;
    this.module.controlClick?.(this);
  };

  private onClear = () => {
    this.typedError = '';
    this.text = '';
    this.commitValue('');
    this.input?.focus();
  };

  render() {
    const strings = this.strings;
    const localised = this.formLocalization();
    const label = this.fieldLabel;
    // Kept while the label row collapses, so the text leaves with it instead of vanishing first.
    if (label) this.shownLabel = label;
    const placeholder = localised?.placeholder || this.ownLocalization?.placeholder || this.placeholder || this.module.placeholder(this);
    const hint = this.ownLocalization?.hint || this.hint || '';
    const locale = calendarLocale(this.language);
    const disabled = this.fieldDisabled;
    const required = this.required;
    const error = this.currentError();
    const clearShown = this.clearable && !!this.value && !disabled && !this.readonly;
    const inputId = `shift-input-${this.uid}`;
    const hintId = `${inputId}-hint`;
    const errorId = `${inputId}-error`;

    return (
      <Host translate="no">
        <div
          class="in-root"
          part="root"
          dir={locale.direction}
          lang={locale.language === 'ku' ? 'ckb' : locale.language}
          data-type={this.type}
          data-disabled={disabled ? '' : undefined}
          data-invalid={error ? '' : undefined}
        >
          <div class="in-label-row" data-open={label ? '' : undefined}>
            <label class="in-label" part="label" htmlFor={inputId} aria-hidden={label ? undefined : 'true'}>
              {label || this.shownLabel}
              <span class="in-required" part="required" aria-hidden="true" data-hidden={required ? undefined : ''}>
                *
              </span>
            </label>
          </div>

          <div class="in-control" part="control" ref={element => (this.control = element)} data-focused={this.focused ? '' : undefined} onClick={this.onControlClick}>
            <span class="in-prefix" part="prefix">
              <slot name="prefix" />
            </span>
            <input
              ref={element => (this.input = element)}
              id={inputId}
              class="in-input"
              part="input"
              type="text"
              role={this.module.inputRole}
              inputMode={this.inputmode || this.module.inputMode}
              autoComplete={this.autocomplete || 'off'}
              spellcheck={this.type === 'text' ? undefined : false}
              value={this.text}
              placeholder={placeholder}
              disabled={disabled}
              readOnly={this.readonly}
              aria-invalid={error ? 'true' : undefined}
              aria-required={required ? 'true' : undefined}
              aria-describedby={`${hintId} ${errorId}`}
              onInput={this.onInput}
              onFocus={this.onFocus}
              onBlur={this.onBlur}
              onKeyDown={this.onKeyDown}
            />
            <span class="in-suffix" part="suffix">
              <button
                type="button"
                class="in-icon in-clear"
                part="clear"
                aria-label={strings.clear}
                aria-hidden={clearShown ? undefined : 'true'}
                tabindex={clearShown ? undefined : '-1'}
                disabled={!clearShown}
                data-hidden={clearShown ? undefined : ''}
                data-collapsed={this.clearable ? undefined : ''}
                onClick={this.onClear}
              >
                <svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">
                  <path d="M4.5 4.5l7 7M11.5 4.5l-7 7" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
                </svg>
              </button>
              {this.module.adornments?.(this)}
              <slot name="suffix" />
            </span>
          </div>

          <div class="in-support">
            <div class="in-hint" part="hint" id={hintId} data-shown={hint && !error ? '' : undefined}>
              {hint}
            </div>
            <div class="in-error" part="error" id={errorId} aria-live="polite" data-shown={error ? '' : undefined}>
              {error || this.shownError}
            </div>
          </div>

          {this.module.outside?.(this)}
        </div>
      </Host>
    );
  }
}
