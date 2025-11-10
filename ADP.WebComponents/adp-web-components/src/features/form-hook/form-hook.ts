import { forceUpdate } from '@stencil/core';
import { AnyObjectSchema, SchemaDescription } from 'yup';

import { LanguageKeys, SharedFormLocales } from '~features/multi-lingual';

import { Field, FormElement, FormHookInterface, FormStateOptions, Subscribers, ValidationType, WatchCallback, Watchers } from './interface';

import { FormStructure } from '../../components/form-elements/form-structure';

export class FormHook<T> {
  haltValidation: boolean = false;
  private isSubmitted = false;
  private watchers: Watchers = {};
  private cachedValues: Partial<T> = {};
  private subscribers: Subscribers = [];
  private schemaObject: AnyObjectSchema;
  private onValuesUpdate: (formValues: T) => void;
  private validationType: ValidationType = 'onSubmit';
  private subscribedFields: { [key: string]: Field<any> } = {};
  formErrors: { [key: string]: string } = {};

  formController;
  formStructure?: FormStructure;
  openDialog: () => void = () => {};
  context: FormHookInterface<T>;

  constructor(context: FormHookInterface<T>, schemaObject: AnyObjectSchema, formStateOptions?: FormStateOptions) {
    this.context = context;
    this.schemaObject = schemaObject;
    this.formController = { onSubmit: this.onSubmit, onInput: this.onInput };
    if (formStateOptions?.validationType) this.validationType = formStateOptions.validationType;
  }

  subscribe = (formName: string, formElement: FormElement) => this.subscribers.push({ name: formName, context: formElement });

  unsubscribe = (formName: string) => (this.subscribers = this.subscribers.filter(({ name }) => name !== formName));

  reset() {
    this.haltValidation = true;

    this.subscribers.forEach(subscriber => {
      subscriber.context.reset();
    });

    this.isSubmitted = false;

    this.signal({ isError: false, disabled: false });

    setTimeout(() => {
      this.haltValidation = false;
      setTimeout(() => {
        this.rerender({ rerenderForm: true, rerenderAll: true });
      }, 50);
    }, 50);
  }

  addWatcher = (key?: string, callback?: WatchCallback) => {
    if (this.watchers[key]) return;

    if (!key?.trim())
      this.watchers['all'] = ({ form }) => {
        form.rerender({ rerenderForm: true });
      };
    else if (callback) this.watchers[key] = callback;
    else
      this.watchers[key] = ({ form }) => {
        form.rerender({ rerenderForm: true });
      };
  };

  removeWatcher = (key: string) => {
    delete this.watchers[key];
  };

  onInput = (event: Event) => {
    const target = event.target as HTMLInputElement;
    let value: string | boolean = target.value;

    if (target.type === 'checkbox') value = target.checked;

    const values = { ...this.getValues(), [target.name]: value } as T;

    if (this.onValuesUpdate) this.onValuesUpdate(values);

    this.validateForm(target.name, value);
  };

  resetFormErrorMessage = () => (this.context.errorMessage = '');

  getFormErrors = () => this.formErrors;

  getFormLocale = (): [{ sharedFormLocales: SharedFormLocales }, LanguageKeys] => [this.context.locale, this.context.language];

  setCachedValues = (newValues: Partial<T>) => {
    this.cachedValues = newValues;
  };

  getValue = <T>(name: keyof T) => {
    return this.getValues<T>()[name];
  };

  getValues = <T>(): T => {
    const formDom = this.context.el.shadowRoot || this.context.el;

    const form = formDom.querySelector('form') as HTMLFormElement;

    if (!form) return {} as T;

    const formData = new FormData(form);

    const formObject = Object.fromEntries(formData.entries());
    for (const el of form.querySelectorAll('[disabled][name]')) {
      // @ts-ignore
      formObject[el.name] = (el as HTMLInputElement).value;
    }

    // const formObject = Object.fromEntries(formData.entries() as Iterable<[string, FormDataEntryValue]>);

    return { ...this.cachedValues, ...formObject } as T;
  };

  private focusFirstInput = (errorFields: Partial<Field<any>>[]) => {
    if (errorFields.length) {
      const formDom = this.context.el.shadowRoot || this.context.el;

      const domElements = errorFields.map(field => formDom.querySelector(`*[name="${field.name}"]`)).filter(dom => dom) as HTMLInputElement[];

      const sortedDomElements = domElements.sort((a, b) => {
        if (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING) return -1; // a comes before b

        if (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_PRECEDING) return 1; // b comes before a

        return 0; // They are the same
      });

      if (sortedDomElements[0]) {
        if (sortedDomElements[0].hidden) {
          sortedDomElements[0].parentElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else sortedDomElements[0].focus();
      }
    }
  };

  hasItemInStructure = (target, structure = this.context.structure) => {
    if (typeof structure === 'string') {
      return structure === target;
    }

    const { tag, name, children, isHidden } = structure;

    if ((tag === target || name === target) && !isHidden) return true;

    if (Array.isArray(children)) {
      return children.some(child => this.hasItemInStructure(target, child));
    }

    return false;
  };

  onSubmit = (formEvent: SubmitEvent) => {
    formEvent.preventDefault();
    (async () => {
      try {
        this.isSubmitted = true;
        this.context.isLoading = true;
        this.signal({ isError: false, disabled: true });
        const formObject = { ...this.cachedValues, ...this.getValues<T>() };

        const description = this.schemaObject.describe();

        Object.entries(description.fields)
          .filter(([_, desc]) => desc.type === 'boolean')
          .forEach(([name]) => {
            // @ts-ignore
            formObject[name] = typeof formObject[name] === 'string' && formObject[name] === 'true';
          });

        const excludedFields = Object.keys(this.schemaObject.fields).filter(key => !this.hasItemInStructure(key));

        const values = await (this.schemaObject.omit(excludedFields) as AnyObjectSchema).validate(formObject, { abortEarly: false });

        await this.context.formSubmit(values);
      } catch (error) {
        if (error.name === 'ValidationError') {
          this.formErrors = {};
          const errorFields: Partial<Field<any>>[] = [];

          error.inner.forEach((element: { path: string; message: string }) => {
            if (element.path) {
              this.formErrors[element.path] = element.message;
              if (!errorFields.find(field => field.name === element.path)) {
                errorFields.push({
                  isError: true,
                  name: element.path,
                  errorMessage: element.message,
                });
              }
            }
          });

          console.log(this.formErrors);
          this.signal(errorFields);
          this.focusFirstInput(errorFields);
          this.rerender({ rerenderAll: true, rerenderForm: true });
        } else console.error('Unexpected Error:', error);
      } finally {
        this.signal({ disabled: false });
        this.context.isLoading = false;
      }
    })();
  };

  getInputState = <MetaType>(name: string): Field<MetaType> => {
    const validationDescription = this.schemaObject.describe().fields[name] as SchemaDescription;

    if (!this.subscribedFields[name])
      this.subscribedFields[name] = {
        name,
        isError: false,
        disabled: false,
        errorMessage: '',
        continuousValidation: false,
        meta: validationDescription?.meta as MetaType,
        isRequired: validationDescription?.tests.some(test => test.name === 'required'),
      };

    return this.subscribedFields[name];
  };

  private signal = (partialSignal: Partial<Field<any>> | Partial<Field<any>>[]) => {
    if (Array.isArray(partialSignal)) {
      partialSignal.forEach(field => {
        if (this.subscribedFields[field.name]) Object.assign(this.subscribedFields[field.name], field);
      });
    } else {
      Object.values(this.subscribedFields).forEach(field => Object.assign(field, partialSignal));
    }
  };

  validateInput = (name: string) => {
    const value = (this.getValues()[name] || '') as string;

    return this.validateForm(name, value, false);
  };

  rerender = ({ inputName, rerenderAll, rerenderForm }: { inputName?: string; rerenderForm?: boolean; rerenderAll?: boolean }) => {
    if (rerenderForm) {
      forceUpdate(this.context);

      if (this?.formStructure) forceUpdate(this?.formStructure);
    }

    if (rerenderAll) {
      this.subscribers.forEach(sub => forceUpdate(sub.context));
    } else if (inputName) {
      const ctx = this.subscribers.find(sub => sub.name === inputName)?.context;
      forceUpdate(ctx);
    }
  };

  validateForm = (name: string, value: string | boolean, strict = true) => {
    setTimeout(() => {
      const values = { ...this.getValues<T>(), [name]: value };
      const watch = this.watchers[name];
      if (watch) watch({ form: this, values });

      if (this.watchers['all']) this.watchers['all']({ form: this, values });
    }, 50);

    if (strict) {
      if (this.haltValidation) return;
      if (!this.isSubmitted && this.validationType !== 'always' && !this.subscribedFields[name]?.continuousValidation) return;
    }

    const wasError = this.subscribedFields[name].isError;

    try {
      // @ts-ignore
      this.schemaObject.fields[name].validateSync(value);
      this.signal([{ name, isError: false }]);
      if (wasError !== false) this.rerender({ inputName: name || '', rerenderForm: true });
      return { isError: false, errorMessage: '' };
    } catch (error) {
      if (error.message) {
        this.signal([{ name, isError: true, errorMessage: error.message }]);
        this.rerender({ inputName: name || '', rerenderForm: true });
        return { isError: true, errorMessage: error.message };
      }
    } finally {
      try {
        this.subscribedFields[name].continuousValidation = true;
      } catch (error) {}
    }
  };
}
