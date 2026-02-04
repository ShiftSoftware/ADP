import { h, FunctionalComponent } from '@stencil/core';

type ArrowIconProps = {
  class?: string;
  part?: string;
};

export const ArrowIcon: FunctionalComponent<ArrowIconProps> = props => (
  <svg viewBox="0 0 20 20" width="16" height="16" {...props} aria-hidden="true">
    <path fill="currentColor" d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 10.94l3.71-3.71a.75.75 0 1 1 1.06 1.06l-4.24 4.24a.75.75 0 0 1-1.06 0L5.21 8.29a.75.75 0 0 1 .02-1.08z" />
  </svg>
);
