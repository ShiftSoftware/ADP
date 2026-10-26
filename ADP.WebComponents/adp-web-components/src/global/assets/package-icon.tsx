import { h, FunctionalComponent } from '@stencil/core';

type Props = {
  class?: string;
  part?: string;
};

export const PackageIcon: FunctionalComponent<Props> = props => (
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
    <path d="M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z" />
    <path d="M12 22V12" />
    <path d="m3.3 7 7.703 4.734a2 2 0 0 0 1.994 0L20.7 7" />
    <path d="m7.5 4.27 9 5.15" />
  </svg>
);
