import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
  onClick?: () => void;
};

export const UserIcon: FunctionalComponent<Props> = props => (
  <svg fill="none" {...props} stroke-width="2" viewBox="0 0 24 24" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
    <circle cx="12" cy="7" r="4" />
  </svg>
);
