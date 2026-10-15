import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
};

export const CloseIcon: FunctionalComponent<Props> = props => (
  <svg
    fill="none"
    stroke-width="2"
    class={props.class}
    part={props.part}
    aria-hidden="true"
    viewBox="0 0 24 24"
    stroke="currentColor"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M18 6 6 18" />
    <path d="m6 6 12 12" />
  </svg>
);
