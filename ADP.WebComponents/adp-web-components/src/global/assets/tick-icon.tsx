import { h, FunctionalComponent } from '@stencil/core';

type TickIconProps = {
  part?: string;
  class?: string;
};

export const TickIcon: FunctionalComponent<TickIconProps> = props => (
  <svg fill="none" stroke-width="2" viewBox="0 0 24 24" {...props} stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">
    <path d="M20 6 9 17l-5-5" />
  </svg>
);
