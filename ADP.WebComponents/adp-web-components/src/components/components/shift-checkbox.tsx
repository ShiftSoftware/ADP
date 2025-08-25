import { Component, Element, h, Host, Method, Prop } from '@stencil/core';
import { CheckIcon } from '~assets/check-icon';

@Component({
  shadow: false,
  tag: 'shift-checkbox',
  styleUrl: 'empty.css',
})
export class ShiftCheckbox {
  @Prop() name: string;
  @Prop() label: string;
  @Prop() disabled: boolean;
  @Prop() componentId: string;
  @Prop() componentClass: string;
  @Prop({ mutable: true, reflect: true }) checked: boolean;

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
        <label part="shift-checkbox" class="relative flex items-center cursor-pointer select-none">
          <input type="checkbox" value="true" name={this.name} checked={this.checked} disabled={this.disabled} onChange={this.onChange} class="shift-checkbox peer hidden" />
          <span class="relative size-[22px] rounded-[6px] flex items-center justify-center text-transparent peer-checked:text-white [&_.check-icon]:scale-0 peer-checked:[&_.check-icon]:scale-100 transition bg-transparent !duration-300 border-2 peer-checked:bg-blue-500 border-gray-400 peer-checked:border-blue-500 overflow-hidden">
            <CheckIcon class="check-icon transition !duration-300 stroke-[3px]" />
          </span>
          <span class="ms-2 text-gray-700">{this.label}</span>
        </label>
      </Host>
    );
  }
}
