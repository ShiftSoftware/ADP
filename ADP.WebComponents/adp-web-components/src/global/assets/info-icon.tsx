import { h, FunctionalComponent } from '@stencil/core';

type InfoIconProps = {
  class?: string;
};

export const InfoIcon: FunctionalComponent<InfoIconProps> = props => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class={props.class}>
    <circle cx="12" cy="12" r="10" />
    <path d="M12 16v-4" />
    <path d="M12 8h.01" />
  </svg>
);
