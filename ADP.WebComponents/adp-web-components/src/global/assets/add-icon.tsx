import { h, FunctionalComponent } from '@stencil/core';

type AddIconProps = {
  class?: string;
  part?: string;
  onClick?: () => void;
};

export const AddIcon: FunctionalComponent<AddIconProps> = props => (
  <svg fill="none" {...props} stroke-width="2" viewBox="0 0 24 24" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="M5 12h14" />
    <path d="M12 5v14" />
  </svg>
);
