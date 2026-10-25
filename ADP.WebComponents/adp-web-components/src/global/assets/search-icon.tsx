import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
};

export const SearchIcon: FunctionalComponent<Props> = props => (
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
    <circle cx="11" cy="11" r="8" />
    <path d="m21 21-4.3-4.3" />
  </svg>
);
