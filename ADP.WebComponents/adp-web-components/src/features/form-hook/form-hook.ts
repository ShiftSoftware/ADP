import { AnyObjectSchema, SchemaDescription } from 'yup';
import { Field, FormElement, FormHookInterface, FormStateOptions, Subscribers, ValidationType } from './interface';

import { LanguageKeys, MultiLingual, SharedFormLocales } from '~features/multi-lingual';
import { forceUpdate } from '@stencil/core';
export class FormHook<T> {
  successAnimation = () => {};

  haltValidation: boolean = false;
  private isSubmitted = false;
  private cachedValues: Partial<T> = {};
  private subscribers: Subscribers = [];
  private schemaObject: AnyObjectSchema;
  private onValuesUpdate: (formValues: T) => void;
  private validationType: ValidationType = 'onSubmit';
  private context: FormHookInterface<T> & MultiLingual;
  private subscribedFields: { [key: string]: Field<any> } = {};
  formErrors: { [key: string]: string } = {};

  formController;

  constructor(context: FormHookInterface<T> & MultiLingual, schemaObject: AnyObjectSchema, formStateOptions?: FormStateOptions) {
    this.context = context;
    this.schemaObject = schemaObject;
    this.formController = { onSubmit: this.onSubmit, onInput: this.onInput };
    if (formStateOptions?.validationType) this.validationType = formStateOptions.validationType;
  }

  setSuccessAnimation = (newSuccessAnimation: typeof this.successAnimation) => (this.successAnimation = newSuccessAnimation);

  subscribe = (formName: string, formElement: FormElement) => this.subscribers.push({ name: formName, context: formElement });

  unsubscribe = (formName: string) => (this.subscribers = this.subscribers.filter(({ name }) => name !== formName));

  reset() {
    this.haltValidation = true;

    this.subscribers.forEach(subscriber => {
      subscriber.context.reset();
    });

    this.isSubmitted = false;

    this.signal({ isError: false, disabled: false });

    this.rerender({ rerenderAll: true, rerenderForm: true });

    setTimeout(() => {
      this.haltValidation = false;
    }, 50);
  }

  onInput = (event: Event) => {
    const target = event.target as HTMLInputElement;

    if (this.onValuesUpdate) this.onValuesUpdate({ ...this.getValues(), [target.name]: target.value } as T);

    this.validateForm(target.name, target.value);
  };

  resetFormErrorMessage = () => (this.context.errorMessage = '');

  getFormErrors = () => this.formErrors;

  getFormLocale = (): [SharedFormLocales, LanguageKeys] => [this.context.locale, this.context.language];

  setCachedValues = (newValues: Partial<T>) => {
    this.cachedValues = newValues;
  };

  getValues = () => {
    const formDom = this.context.el.shadowRoot || this.context.el;

    const form = formDom.querySelector('form') as HTMLFormElement;
    const formData = new FormData(form);
    const formObject = Object.fromEntries(formData.entries() as Iterable<[string, FormDataEntryValue]>);

    return { ...this.cachedValues, ...formObject };
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

      if (sortedDomElements[0]) sortedDomElements[0].focus();
    }
  };

  onSubmit = (formEvent: SubmitEvent) => {
    formEvent.preventDefault();
    (async () => {
      try {
        this.isSubmitted = true;
        this.context.isLoading = true;
        this.signal({ isError: false, disabled: true });
        const formObject = { ...this.cachedValues, ...this.getValues() };
        const values = await this.schemaObject.validate(formObject, { abortEarly: false });

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
    if (rerenderForm) forceUpdate(this.context);

    if (rerenderAll) {
      this.subscribers.forEach(sub => forceUpdate(sub.context));
    } else if (inputName) {
      const ctx = this.subscribers.find(sub => sub.name === inputName)?.context;
      forceUpdate(ctx);
    }
  };

  validateForm = (name: string, value: string, strict = true) => {
    if (strict) {
      if (this.haltValidation) return;
      if (!this.isSubmitted && this.validationType !== 'always' && !this.subscribedFields[name].continuousValidation) return;
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
      this.subscribedFields[name].continuousValidation = true;
    }
  };
}
