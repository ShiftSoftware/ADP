import { h, FunctionalComponent } from '@stencil/core';

type LaptopMinimalCheckIconProps = {
  class?: string;
};

export const LaptopMinimalCheckIcon: FunctionalComponent<LaptopMinimalCheckIconProps> = props => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class={props.class}>
    <path d="M2 20h20" />
    <path d="m9 10 2 2 4-4" />
    <rect x="3" y="4" width="18" height="12" rx="2" />
  </svg>
);
