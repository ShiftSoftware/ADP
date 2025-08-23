import { Component, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

@Component({
  shadow: false,
  tag: 'form-error-message',
  styleUrl: 'form-inputs.css',
})
export class FormErrorMessage {
  @Prop() isError: boolean;
  @Prop() errorMessage: string = '';

  render() {
    return (
      <div
        part="form-error-message"
        class={cn('absolute text-[12px] pt-[1px] -z-10 text-red-500 opacity-0 -translate-y-[4px] bottom-0 transition !duration-300', {
          'translate-y-full error-message opacity-100': this.isError,
        })}
      >
        {this.errorMessage}
      </div>
    );
  }
}
