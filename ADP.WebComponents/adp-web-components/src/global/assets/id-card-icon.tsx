import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
  onClick?: () => void;
};

export const IdCardIcon: FunctionalComponent<Props> = props => (
  <svg fill="none" {...props} stroke-width="2" viewBox="0 0 24 24" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M16 10h2" />
    <path d="M16 14h2" />
    <path d="M6.17 15a3 3 0 0 1 5.66 0" />
    <circle cx="9" cy="11" r="2" />
    <rect x="2" y="5" width="20" height="14" rx="2" />
  </svg>
);
