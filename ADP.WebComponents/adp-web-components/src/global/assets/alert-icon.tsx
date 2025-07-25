import { h, FunctionalComponent } from '@stencil/core';

type AlertIconProps = {
  class?: string;
};

export const AlertIcon: FunctionalComponent<AlertIconProps> = props => (
  <svg class={props.class} viewBox="0 0 24 24" fill="none" stroke-width="2" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3" />
    <path d="M12 9v4" />
    <path d="M12 17h.01" />
  </svg>
);
