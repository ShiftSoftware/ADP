import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
};

export const ChevronDownIcon: FunctionalComponent<Props> = props => (
  <svg class={props.class} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path fill="currentColor" d="M256 368 96 208l32-32 128 128 128-128 32 32z" />
  </svg>
);
