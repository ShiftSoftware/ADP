import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
};

export const LoaderCircleIcon: FunctionalComponent<Props> = props => (
  <svg
    fill="none"
    stroke-width="2"
    aria-hidden="true"
    viewBox="0 0 24 24"
    stroke="currentColor"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
    class={props.class}
    part={props.part}
  >
    <path d="M21 12a9 9 0 1 1-6.219-8.56" />
  </svg>
);
