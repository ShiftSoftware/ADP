import { Component, h, Prop } from '@stencil/core';
import { FormElement, FormHook, FormInputLocalization } from '~features/form-hook';

const formStepperId = 'form-input-preview';

@Component({
  shadow: false,
  tag: 'form-input-preview',
  styleUrl: 'form-inputs.css',
})
export class FormStepper implements FormElement {
  @Prop() name?: string;
  @Prop() props?: any = {};
  @Prop() form: FormHook<any>;
  @Prop() localization?: FormInputLocalization<{ value?: string }> = {};

  reset: (newValue?: unknown) => void;

  componentDidLoad() {
    this.form.subscribe(formStepperId, this);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(formStepperId);
  }

  private parseTemplate(template?: string) {
    if (!template) return '';

    return template.replace(/\$\{(.*?)\}/g, (_, fieldName: string) => {
      const value = this.form?.getValue(fieldName?.trim());
      return value != null ? String(value) : '';
    });
  }

  render() {
    const [_, language] = this.form.getFormLocale();

    const rawValue = this?.localization?.[language]?.value;
    const parsedValue = this.parseTemplate(rawValue);

    // @ts-ignore
    window?.ss = this.form;
    return <form-input key={this?.name} form={this.form} {...this?.props} localization={this?.localization} inputProps={{ readOnly: true, value: parsedValue }} />;
  }
}
