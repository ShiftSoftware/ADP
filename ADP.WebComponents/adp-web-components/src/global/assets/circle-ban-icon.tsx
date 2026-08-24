import { h, FunctionalComponent } from '@stencil/core';

type CircleBanIconProps = {
  class?: string;
};

export const CircleBanIcon: FunctionalComponent<CircleBanIconProps> = props => (
  <svg
    class={props.class}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.7"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
  >
    <circle cx="12" cy="12" r="9" />
    <path d="m5.6 5.6 12.8 12.8" />
  </svg>
);
