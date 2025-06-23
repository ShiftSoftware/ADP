import { h, FunctionalComponent } from '@stencil/core';

type EmptyTableIconProps = {
  class?: string;
};

export const EmptyTableIcon: FunctionalComponent<EmptyTableIconProps> = props => (
  <svg
    fill="none"
    width="24"
    height="24"
    stroke-width="2"
    viewBox="0 0 24 24"
    class={props.class}
    stroke="currentColor"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M11 12H3" />
    <path d="M16 6H3" />
    <path d="M16 18H3" />
    <path d="m19 10-4 4" />
    <path d="m15 10 4 4" />
  </svg>
);
