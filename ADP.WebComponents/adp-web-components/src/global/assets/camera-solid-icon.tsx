import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
};

export const CameraSolidIcon: FunctionalComponent<Props> = props => (
  <svg class={props.class} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path
      fill="currentColor"
      d="M88 144H152L176 96H336L360 144H424A40 40 0 0 1 464 184V392A40 40 0 0 1 424 432H88A40 40 0 0 1 48 392V184A40 40 0 0 1 88 144ZM256 192a96 96 0 1 0 0 192a96 96 0 1 0 0-192ZM256 232a56 56 0 1 1 0 112a56 56 0 1 1 0-112Z"
    />
  </svg>
);
