import { h, FunctionalComponent } from '@stencil/core';

type SearchIconProps = {
  part?: string;
  class?: string;
};

export const SearchIcon: FunctionalComponent<SearchIconProps> = props => (
  <svg fill="none" stroke-width="2" viewBox="0 0 24 24" {...props} stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" xmlns="http://www.w3.org/2000/svg">
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-4.35-4.35" />
  </svg>
);
