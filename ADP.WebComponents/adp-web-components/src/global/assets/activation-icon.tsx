import { h, FunctionalComponent } from '@stencil/core';

type ActivationIconProps = {
  class?: string;
};

export const ActivationIcon: FunctionalComponent<ActivationIconProps> = props => (
  <svg class={props.class} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <g stroke-width="0"></g>
    <g stroke-linecap="round" stroke-linejoin="round"></g>
    <g>
      <circle cx="12" cy="12" r="8" fill-opacity="0.24"></circle>
      <path d="M8.5 11L11.3939 13.8939C11.4525 13.9525 11.5475 13.9525 11.6061 13.8939L19.5 6" stroke-width="1.2"></path>
    </g>
  </svg>
);
