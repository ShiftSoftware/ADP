import { h, FunctionalComponent } from '@stencil/core';

type PrintIconProps = {
  class?: string;
  /** Path fill. Defaults to the cream used on the colored claim buttons; pass "currentColor" to follow the text color. */
  fill?: string;
  /**
   * Defaults to the historical padded viewBox (the glyph fills ~2/3 of the box). Pass "0 0 24 24"
   * for a tight box whose visual size matches the other 24x24 icons at the same width/height.
   */
  viewBox?: string;
};

export const PrintIcon: FunctionalComponent<PrintIconProps> = props => (
  <svg class={props.class} viewBox={props.viewBox || '-5 -5 36 36'} fill="none" xmlns="http://www.w3.org/2000/svg">
    <path
      fill-rule="evenodd"
      clip-rule="evenodd"
      d="M17 7H7V6h10v1zm0 12H7v-6h10v6zm2-12V3H5v4H1v8.996C1 17.103 1.897 18 3.004 18H5v3h14v-3h1.996A2.004 2.004 0 0 0 23 15.996V7h-4z"
      fill={props.fill || 'rgb(252, 248, 227)'}
    />
  </svg>
);
