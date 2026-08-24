import { h, FunctionalComponent } from '@stencil/core';

type CirclePowerIconProps = {
  class?: string;
};

export const CirclePowerIcon: FunctionalComponent<CirclePowerIconProps> = props => (
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
    <path d="M12 7.4v4.2" />
    <path d="M9.1 10.2a3.7 3.7 0 1 0 5.8 0" />
  </svg>
);
