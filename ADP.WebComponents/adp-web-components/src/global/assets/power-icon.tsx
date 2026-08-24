import { h, FunctionalComponent } from '@stencil/core';

type PowerIconProps = {
  class?: string;
};

export const PowerIcon: FunctionalComponent<PowerIconProps> = props => (
  <svg
    class={props.class}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="2.2"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M12 3v9" />
    <path d="M18.4 6.6a9 9 0 1 1-12.8 0" />
  </svg>
);
