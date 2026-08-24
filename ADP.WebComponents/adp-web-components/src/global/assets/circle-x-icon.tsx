import { h, FunctionalComponent } from '@stencil/core';

type CircleXIconProps = {
  class?: string;
};

export const CircleXIcon: FunctionalComponent<CircleXIconProps> = props => (
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
    <path d="m15 9-6 6" />
    <path d="m9 9 6 6" />
  </svg>
);
