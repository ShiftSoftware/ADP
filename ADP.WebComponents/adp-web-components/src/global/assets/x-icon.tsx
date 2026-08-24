import { h, FunctionalComponent } from '@stencil/core';

type XIconProps = {
  part?: string;
  class?: string;
};

export const XIcon: FunctionalComponent<XIconProps> = props => (
  <svg fill="none" stroke-width="2" viewBox="0 0 24 24" {...props} stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M19.2 4.8 4.8 19.2" />
    <path d="m4.8 4.8 14.4 14.4" />
  </svg>
);
