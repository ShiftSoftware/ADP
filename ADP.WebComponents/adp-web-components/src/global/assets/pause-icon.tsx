import { h, FunctionalComponent } from '@stencil/core';

type PauseIconProps = {
  class?: string;
};

export const PauseIcon: FunctionalComponent<PauseIconProps> = props => (
  <svg class={props.class} viewBox="0 0 24 24" fill="currentColor" stroke="none" xmlns="http://www.w3.org/2000/svg">
    <rect x="7" y="5" width="3.6" height="14" rx="1.4" />
    <rect x="13.4" y="5" width="3.6" height="14" rx="1.4" />
  </svg>
);
