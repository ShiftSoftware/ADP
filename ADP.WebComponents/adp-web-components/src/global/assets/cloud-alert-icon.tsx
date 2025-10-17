import { h, FunctionalComponent } from '@stencil/core';

type CloudAlertIconProps = {
  class?: string;
};

export const CloudAlertIcon: FunctionalComponent<CloudAlertIconProps> = props => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class={props.class}>
    <path d="M12 12v4" />
    <path d="M12 20h.01" />
    <path d="M17 18h.5a1 1 0 0 0 0-9h-1.79A7 7 0 1 0 7 17.708" />
  </svg>
);
