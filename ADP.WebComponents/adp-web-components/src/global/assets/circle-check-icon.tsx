import { h, FunctionalComponent } from '@stencil/core';

type CircleCheckIconProps = {
  class?: string;
};

export const CircleCheckIcon: FunctionalComponent<CircleCheckIconProps> = props => (
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
    <path d="m8.4 12.3 2.5 2.5 4.7-5.6" />
  </svg>
);
