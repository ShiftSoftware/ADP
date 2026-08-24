import { h, FunctionalComponent } from '@stencil/core';

type CirclePauseIconProps = {
  class?: string;
};

export const CirclePauseIcon: FunctionalComponent<CirclePauseIconProps> = props => (
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
    <path d="M10 9.4v5.2" />
    <path d="M14 9.4v5.2" />
  </svg>
);
