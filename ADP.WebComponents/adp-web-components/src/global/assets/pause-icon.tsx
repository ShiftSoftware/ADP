import { h, FunctionalComponent } from '@stencil/core';

type PauseIconProps = {
  part?: string;
  class?: string;
};

/** Solid bars rather than the usual outlined pair — at glyph size an outline closes up into a smudge. */
export const PauseIcon: FunctionalComponent<PauseIconProps> = props => (
  <svg viewBox="0 0 24 24" {...props} fill="currentColor" stroke="none" xmlns="http://www.w3.org/2000/svg">
    <rect x="6.6" y="3.6" width="4.4" height="16.8" rx="2.2" />
    <rect x="13" y="3.6" width="4.4" height="16.8" rx="2.2" />
  </svg>
);
