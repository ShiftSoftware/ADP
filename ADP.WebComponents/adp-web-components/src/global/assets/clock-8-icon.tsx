import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
  onClick?: () => void;
};

export const Clock8Icon: FunctionalComponent<Props> = props => (
  <svg fill="none" {...props} stroke-width="2" viewBox="0 0 24 24" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M12 6v6l-4 2" />
    <circle cx="12" cy="12" r="10" />
  </svg>
);
