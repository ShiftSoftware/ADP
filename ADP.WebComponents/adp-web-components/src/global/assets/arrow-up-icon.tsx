import { h, FunctionalComponent } from '@stencil/core';

type ArrowUpIconProps = {
  class?: string;
};

export const ArrowUpIcon: FunctionalComponent<ArrowUpIconProps> = props => (
  <svg class={props.class} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor">
    <path
      fill-rule="evenodd"
      clip-rule="evenodd"
      d="M11.78 9.78a.75.75 0 0 1-1.06 0L8 7.06 5.28 9.78a.75.75 0 0 1-1.06-1.06l3.25-3.25a.75.75 0 0 1 1.06 0l3.25 3.25a.75.75 0 0 1 0 1.06Z"
    />
  </svg>
);
