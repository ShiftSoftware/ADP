import { Component, Prop, h, Watch } from '@stencil/core';

import { FormHook } from '~features/form-hook/form-hook';

@Component({
  shadow: false,
  tag: 'form-shadow-input',
  styleUrl: 'form-shadow-input.css',
})
export class FormInput {
  @Prop() name: string;
  @Prop() value: string;
  @Prop() form: FormHook<any>;

  @Watch('name')
  async handleNameChange(newName: string) {
    this.form.validateForm(newName, this.value);
  }

  @Watch('value')
  async handleValueChange(newValue: string) {
    this.form.validateForm(this.name, newValue);
  }

  render() {
    return <input name={this.name} type="string" hidden value={this.value} />;
  }
}
