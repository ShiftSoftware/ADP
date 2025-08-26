import { Component, Element, h, Host, Method, Prop } from '@stencil/core';

import cn from '~lib/cn';

@Component({
  shadow: false,
  tag: 'shift-switch',
  styleUrl: 'empty.css',
})
export class ShiftSwitch {
  @Prop() name: string;
  @Prop() label: string;
  @Prop() checked: boolean;
  @Prop() disabled: boolean;
  @Prop() componentId: string;
  @Prop() componentClass: string;

  @Prop() onChange?: (event: Event) => void;

  @Element() el: HTMLElement;
  inputRef: HTMLInputElement;

  async componentDidLoad() {
    this.inputRef = this.el.getElementsByClassName('shift-checkbox')[0] as HTMLInputElement;
  }

  @Method()
  async getInputRef(): Promise<HTMLInputElement> {
    return this.inputRef;
  }

  @Method()
  async getIsChecked(): Promise<boolean> {
    return this.inputRef.checked;
  }

  render() {
    return (
      <Host>
        <label part="shift-checkbox" class={cn('relative flex items-center cursor-pointer select-none', { 'opacity-75 cursor-default': this.disabled })}>
          <input type="checkbox" value="true" name={this.name} checked={this.checked} disabled={this.disabled} onChange={this.onChange} class="shift-checkbox peer hidden" />
          <div class="relative w-[50px] h-[24px] rounded-full border-2 border-blue-300 bg-blue-100 peer-checked:bg-blue-500 peer-checked:[&_div]:translate-x-[28px] peer-checked:border-blue-500 transition-colors duration-300">
            <div class="absolute top-[-2px] left-[-2px] size-[24px] rounded-full bg-white shadow-md transition-all duration-300 peer-checked:bg-blue-500" />
          </div>
          <span class="ms-2 text-gray-700">{this.label}</span>
        </label>
      </Host>
    );
  }
}
