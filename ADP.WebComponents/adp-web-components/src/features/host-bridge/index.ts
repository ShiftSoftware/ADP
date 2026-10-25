import { emitFormReady, emitFormStep } from './events';
import { peekHostConfig } from './host-config';
import { observeSize } from './size';
import { registerFormValidity, scheduleValidityIfOptedIn } from './validity';

export * from './adp-fetch';
export * from './events';
export * from './validity';
export * from './size';

export function afterFormCreated(context: { el?: HTMLElement; form?: unknown }) {
  const config = peekHostConfig();
  if (!config) return;

  registerFormValidity(context);
  if (config.formSize === true) observeSize(context?.el);
}

export function afterFormStepUpdate(form: Parameters<typeof emitFormStep>[0]) {
  emitFormStep(form);
  scheduleValidityIfOptedIn(form);
}

export function afterFormReady(context: object | undefined) {
  const { el, form } = (context ?? {}) as { el?: HTMLElement; form?: object };
  emitFormReady(el);
  scheduleValidityIfOptedIn(form);
}
