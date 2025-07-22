import { h, FunctionalComponent } from '@stencil/core';

type AttachIconProps = {
  class?: string;
};

export const AttachIcon: FunctionalComponent<AttachIconProps> = props => (
  <svg fill="none" stroke-width="2" class={props.class} viewBox="0 0 24 24" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="m16 6-8.414 8.586a2 2 0 0 0 2.829 2.829l8.414-8.586a4 4 0 1 0-5.657-5.657l-8.379 8.551a6 6 0 1 0 8.485 8.485l8.379-8.551" />
  </svg>
);
