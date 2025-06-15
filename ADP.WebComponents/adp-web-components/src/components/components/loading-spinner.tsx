import { Component, h, Prop } from '@stencil/core';

import Loader from '~assets/loader.svg';

import cn from '~lib/cn';

@Component({
  shadow: false,
  tag: 'loading-spinner',
  styleUrl: 'loading-spinner.css',
})
export class LoadingSpinner {
  @Prop() isLoading: boolean;

  render() {
    return (
      <div class={cn('size-full z-50 flex items-center justify-center pointer-events-none absolute ', { 'opacity-0': !this.isLoading })}>
        <img class="spin-slow size-[40px]" src={Loader} />
      </div>
    );
  }
}
