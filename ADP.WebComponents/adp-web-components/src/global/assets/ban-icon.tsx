import { h, FunctionalComponent } from '@stencil/core';

type BanIconProps = {
  part?: string;
  class?: string;
};

export const BanIcon: FunctionalComponent<BanIconProps> = props => (
  <svg fill="none" stroke-width="2" viewBox="0 0 24 24" {...props} stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <circle cx="12" cy="12" r="8.4" />
    <path d="m6 6 12 12" />
  </svg>
);
