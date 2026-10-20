import { AnyObjectSchema } from 'yup';

import { AdpFormValidity, dispatchFormEvent } from './events';

// FormHook keeps these private, and forms v1 is frozen, so validity reads them as submit does instead of widening FormHook.
type FormInternals = {
  schemaObject?: AnyObjectSchema;
  requiredContext?: Record<string, boolean>;
  cachedValues?: Record<string, unknown>;
  getValues?: () => Record<string, unknown>;
  hasItemInStructure?: (target: string) => boolean;
  formStructure?: { currentStep?: number };
  context?: { el?: HTMLElement; emitValidity?: boolean };
};

const fieldOf = (path: string) => path.split(/[.[]/)[0];

function currentStep(form: FormInternals | undefined) {
  const step = form?.formStructure?.currentStep;

  return typeof step === 'number' ? step : 1;
}

export async function computeValidity(form: unknown): Promise<AdpFormValidity> {
  const internals = form as FormInternals | undefined;
  const step = currentStep(internals);
  const schema = internals?.schemaObject;

  if (!schema || typeof internals.getValues !== 'function' || typeof internals.hasItemInStructure !== 'function') return { valid: false, step, invalidFields: [] };

  try {
    const formObject: Record<string, unknown> = { ...internals.cachedValues, ...internals.getValues() };

    Object.entries(schema.describe().fields)
      .filter(([, description]) => description.type === 'boolean')
      .forEach(([name]) => {
        formObject[name] = typeof formObject[name] === 'string' && formObject[name] === 'true';
      });

    const order = Object.keys(schema.fields);
    const excludedFields = order.filter(key => !internals.hasItemInStructure(key));

    await (schema.omit(excludedFields) as AnyObjectSchema).validate(formObject, { abortEarly: false, context: internals.requiredContext });

    return { valid: true, step, invalidFields: [] };
  } catch (error) {
    if (error?.name !== 'ValidationError') return { valid: false, step, invalidFields: [] };

    const paths: string[] = (error.inner?.length ? error.inner : [error])
      .map((inner: { path?: string }) => inner?.path)
      .filter((path: unknown) => typeof path === 'string' && path);
    const order = Object.keys(schema.fields);
    const rank = (field: string) => (order.indexOf(field) < 0 ? order.length : order.indexOf(field));
    const invalidFields = [...new Set(paths.map(fieldOf))].sort((a, b) => rank(a) - rank(b));

    return { valid: false, step, invalidFields };
  }
}

type ValidityState = { run: number; last?: string };

const states = new WeakMap<object, ValidityState>();

export function scheduleValidity(form: object | undefined) {
  const el = (form as FormInternals | undefined)?.context?.el;
  if (!form || !el) return;

  let state = states.get(form);
  if (!state) {
    state = { run: 0 };
    states.set(form, state);
  }

  const run = ++state.run;

  computeValidity(form).then(validity => {
    if (run !== state.run) return;

    const key = JSON.stringify([validity.valid, validity.invalidFields]);
    if (key === state.last) return;

    state.last = key;
    dispatchFormEvent(el, 'adp-form-validity-change', validity);
  });
}

export function scheduleValidityIfOptedIn(form: object | undefined) {
  if ((form as FormInternals | undefined)?.context?.emitValidity) scheduleValidity(form);
}
