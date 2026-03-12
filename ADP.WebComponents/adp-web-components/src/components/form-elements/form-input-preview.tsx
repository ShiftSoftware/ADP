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

  render() {
    const [_, language] = this.form.getFormLocale();

    return (
      <form-input
        key={this?.name}
        form={this.form}
        {...this?.props}
        localization={this?.localization}
        inputProps={{ readOnly: true, value: this?.localization?.[language]?.value?.replaceAll('${' + this?.name + '}', (this.form?.getValue(this?.name) || '') as string) }}
      />
    );
  }
}
