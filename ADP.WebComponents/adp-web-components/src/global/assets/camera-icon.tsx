import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
};

export const CameraIcon: FunctionalComponent<Props> = props => (
  <svg
    fill="none"
    stroke-width="2"
    class={props.class}
    part={props.part}
    aria-hidden="true"
    viewBox="0 0 24 24"
    stroke="currentColor"
    stroke-linecap="round"
    stroke-linejoin="round"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3l-2.5-3z" />
    <circle cx="12" cy="13" r="3" />
  </svg>
);
