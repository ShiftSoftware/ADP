import { h, FunctionalComponent } from '@stencil/core';

type CheckIconProps = {
  class?: string;
};

export const CheckIcon: FunctionalComponent<CheckIconProps> = props => (
  <svg class={props.class} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" xmlns="http://www.w3.org/2000/svg" stroke-linejoin="round">
    <path d="M20 6 9 17l-5-5" />
  </svg>
);
