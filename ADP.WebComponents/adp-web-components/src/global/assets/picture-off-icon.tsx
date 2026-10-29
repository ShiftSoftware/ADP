import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
};

export const PictureOffIcon: FunctionalComponent<Props> = props => (
  <svg class={props.class} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path
      fill="currentColor"
      d="M88 80H424A40 40 0 0 1 464 120V392A40 40 0 0 1 424 432H88A40 40 0 0 1 48 392V120A40 40 0 0 1 88 80ZM88 120V392H424V120ZM172 176a32 32 0 1 1 0 64a32 32 0 1 1 0-64ZM120 360L216 252L280 316L336 252L392 360ZM56 84L84 56L456 428L428 456Z"
    />
  </svg>
);
