import type { VNode } from '@stencil/core';

import type { InputStrings } from '../input-locale';

export type ReadResult = { value: string } | { error: string };

// What a type module may read from and do to the field. The field implements it; modules never import the field.
export interface InputField {
  readonly el: HTMLElement;
  readonly value: string;
  readonly text: string;
  readonly language: string;
  readonly strings: InputStrings;
  readonly fieldDisabled: boolean;
  readonly readonly: boolean;
  readonly fieldLabel: string;
  readonly mobileSheet: boolean;
  readonly control?: HTMLElement;
  readonly input?: HTMLInputElement;
  expanded: boolean;
  choose(value: string): void;
  forwardMonthChange(detail: unknown): void;
}

// One module per type: how a value is shown, read back from typed text, and what the type adds to the control.
export interface InputType<Props = unknown> {
  inputMode: string;
  // Our own input may take the role its popup pattern needs; a popover never changes a foreign anchor's role.
  inputRole?: string;
  placeholder(field: InputField & Props): string;
  normalize(value: unknown, field: InputField & Props): string;
  display(value: string, field: InputField & Props): string;
  read(text: string, field: InputField & Props): ReadResult;
  mounted?(field: InputField & Props): void;
  keydown?(event: KeyboardEvent, field: InputField & Props): void;
  controlClick?(field: InputField & Props): void;
  adornments?(field: InputField & Props): VNode | VNode[] | null;
  outside?(field: InputField & Props): VNode | VNode[] | null;
}
