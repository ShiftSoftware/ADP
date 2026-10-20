import { emitFormReady, emitFormStep } from './events';
import { scheduleValidityIfOptedIn } from './validity';

export * from './adp-fetch';
export * from './events';
export * from './validity';
export * from './size';

export function afterFormStepUpdate(form: Parameters<typeof emitFormStep>[0]) {
  emitFormStep(form);
  scheduleValidityIfOptedIn(form);
}

export function afterFormReady(context: { el?: HTMLElement; form?: object }) {
  emitFormReady(context?.el);
  scheduleValidityIfOptedIn(context?.form);
}
