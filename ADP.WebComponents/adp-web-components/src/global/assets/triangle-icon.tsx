import { h, FunctionalComponent } from '@stencil/core';

type TriangleIconProps = {
  class?: string;
};

export const TriangleIcon: FunctionalComponent<TriangleIconProps> = props => (
  <svg fill="none" stroke-width="2" class={props.class} viewBox="0 0 32 18" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M17.8 2a2 2 0 0 0-3.6 0L3 16h26L17.8 2Z" />
  </svg>
);
