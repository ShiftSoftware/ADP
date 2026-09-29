import { h } from '@stencil/core';

import type { LanguageKeys } from '~features/multi-lingual';
import type { PickerChangeDetail, PickerStatusDetail } from '~lib/picker';

import type { InputField, InputType } from '../input-type';

export interface PickerField extends InputField {
  pickerEl: HTMLElement | null;
  pickerLabel: string;
  pickerHint: string;
  pickerBusy: boolean;
  appearance?: string;
  colorScheme: string;
  size: string;
}

type Picker = HTMLElement & { value?: string; setFocus?: () => Promise<void> };

const popovers = new WeakMap<PickerField, HTMLShiftPopoverElement>();
const wired = new WeakMap<HTMLElement, PickerField>();

const nextFrame = () => new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

async function open(field: PickerField, focus: boolean) {
  const popover = popovers.get(field);
  if (!popover || field.fieldDisabled || field.readonly) return;

  await popover.show({ focus: false });
  if (!focus) return;

  await nextFrame();
  await nextFrame();
  await (field.pickerEl as Picker | null)?.setFocus?.();
}

function onChange(field: PickerField, event: CustomEvent<PickerChangeDetail>) {
  event.stopPropagation();

  const { value, label, complete } = event.detail;
  field.pickerLabel = label;
  field.choose(value);

  if (!complete) return;

  // Done: the panel closes and the field lets go of focus; the browser's sequential focus point stays on it, so Tab carries on from the field.
  popovers.get(field)?.hide();
  field.input?.blur();
}

function onStatus(field: PickerField, event: CustomEvent<PickerStatusDetail>) {
  event.stopPropagation();
  field.pickerHint = event.detail.text;
  field.pickerBusy = !!event.detail.busy;
}

// Wired as soon as the host finds it: the picker reports its first status before the host has rendered.
export function wirePicker(field: PickerField) {
  const picker = field.pickerEl;
  if (!picker || wired.has(picker)) return;

  wired.set(picker, field);
  picker.addEventListener('pickerChange', event => onChange(wired.get(picker), event as CustomEvent<PickerChangeDetail>));
  picker.addEventListener('pickerStatus', event => onStatus(wired.get(picker), event as CustomEvent<PickerStatusDetail>));
}

// The picker is the host's child; it moves into the popover, which carries it onto <body> with the panel.
export function adoptPicker(field: PickerField) {
  const picker = field.pickerEl as Picker | null;
  const popover = popovers.get(field);

  if (popover) popover.anchor = field.input;
  if (!picker) return;
  if (popover && field.expanded && field.fieldDisabled) popover.hide();

  wirePicker(field);

  for (const [name, value] of Object.entries({ language: field.language, appearance: field.appearance, colorScheme: field.colorScheme, size: field.size })) {
    if (name in picker && picker[name] !== value) picker[name] = value;
  }
  if ('value' in picker && (picker.value || '') !== field.value) picker.value = field.value;

  if (popover && picker.parentElement === field.el) {
    picker.removeAttribute('slot');
    popover.appendChild(picker);
  }
}

export const pickerType: InputType<Partial<PickerField>> = {
  inputMode: 'none',
  inputRole: 'combobox',

  placeholder: () => '',

  normalize: value => (value === null || value === undefined ? '' : String(value)),

  display: (value, field) => (value ? field.pickerLabel || value : ''),

  read: (_text, field) => ({ value: field.value }),

  mounted: field => adoptPicker(field as PickerField),

  keydown(event, field) {
    if (!['ArrowDown', 'Enter', ' '].includes(event.key)) return;

    event.preventDefault();
    open(field as PickerField, true);
  },

  controlClick(field) {
    if (!field.expanded) open(field as PickerField, false);
  },

  adornments(field) {
    const unavailable = field.fieldDisabled || field.readonly;

    return [
      <span class="in-icon in-busy" part="busy" aria-hidden="true" data-shown={(field as PickerField).pickerBusy ? '' : undefined}>
        <span class="in-spinner" />
      </span>,
      <button
        type="button"
        class="in-icon in-calendar-button"
        part="calendar-button"
        aria-label={field.value ? `${field.fieldLabel || field.strings.open}: ${field.text}` : field.fieldLabel || field.strings.open}
        aria-haspopup="dialog"
        aria-expanded={String(field.expanded)}
        disabled={unavailable}
        onClick={() => {
          if (field.expanded) popovers.get(field as PickerField)?.toggle();
          else open(field as PickerField, true);
        }}
      >
        <svg viewBox="0 0 16 16" aria-hidden="true" focusable="false">
          <rect x="2.5" y="3.5" width="11" height="10" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.4" />
          <path d="M2.5 6.5h11M5.5 2v3M10.5 2v3" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
        </svg>
      </button>,
    ];
  },

  outside(field) {
    const picker = field as PickerField;

    return (
      <shift-popover
        ref={element => element && popovers.set(picker, element)}
        anchor={field.input}
        label={field.fieldLabel || field.strings.open}
        language={field.language as LanguageKeys}
        appearance={picker.appearance as HTMLShiftPopoverElement['appearance']}
        colorScheme={picker.colorScheme as HTMLShiftPopoverElement['colorScheme']}
        size={picker.size as HTMLShiftPopoverElement['size']}
        onOpenChange={(event: CustomEvent<{ open: boolean }>) => {
          event.stopPropagation();
          field.expanded = event.detail.open;
        }}
      />
    );
  },
};

export const hidePicker = (field: PickerField) => popovers.get(field)?.hide();
