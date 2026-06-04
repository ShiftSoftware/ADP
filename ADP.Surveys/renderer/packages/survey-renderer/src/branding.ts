/**
 * Branding → CSS custom properties. The stylesheet declares neutral defaults on
 * `.survey-root` (`--survey-primary: #2563eb`, …); when the schema carries a
 * branding block, the renderer overrides them via an inline `style` on the same
 * element. Hover shade and text contrast are derived here in JS rather than with
 * `color-mix()` so the bundle keeps working on slightly older WebViews —
 * agent-assist iframes live inside whatever browser the call-center machine has.
 */

import type { Branding } from '@shiftsoftware/survey-sdk';
import type { CSSProperties } from 'react';

/** Parse #rgb / #rrggbb / #rrggbbaa into [r,g,b] (alpha ignored). Returns null
 *  on anything unparseable — bad author input must never break the renderer. */
function hexToRgb(hex: string): [number, number, number] | null {
  const value = hex.trim().replace(/^#/, '');
  if (!/^[0-9a-fA-F]{3}$|^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$/.test(value)) return null;
  const full =
    value.length === 3
      ? value
          .split('')
          .map((c) => c + c)
          .join('')
      : value.slice(0, 6);
  return [
    parseInt(full.slice(0, 2), 16),
    parseInt(full.slice(2, 4), 16),
    parseInt(full.slice(4, 6), 16),
  ];
}

function rgbToHex([r, g, b]: [number, number, number]): string {
  const c = (n: number) => Math.max(0, Math.min(255, Math.round(n))).toString(16).padStart(2, '0');
  return `#${c(r)}${c(g)}${c(b)}`;
}

/** Darken by a factor (0..1) for hover states. */
function darken(rgb: [number, number, number], factor: number): [number, number, number] {
  return [rgb[0] * factor, rgb[1] * factor, rgb[2] * factor];
}

/** WCAG-ish relative luminance — enough to pick white-vs-dark button text. */
function luminance([r, g, b]: [number, number, number]): number {
  const lin = (c: number) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
}

/**
 * Build the inline-style override object for `.survey-root`. Only sets the
 * variables the branding actually provides, so stylesheet defaults keep doing
 * their job for everything else. Returns an empty object for no/empty branding —
 * safe to spread unconditionally.
 */
export function brandingToCssVars(branding: Branding | undefined): CSSProperties {
  const vars: Record<string, string> = {};

  const primary = branding?.primaryColor ? hexToRgb(branding.primaryColor) : null;
  if (primary) {
    vars['--survey-primary'] = rgbToHex(primary);
    vars['--survey-primary-hover'] = rgbToHex(darken(primary, 0.82));
    vars['--survey-primary-contrast'] = luminance(primary) > 0.45 ? '#111111' : '#ffffff';
  }

  const secondary = branding?.secondaryColor ? hexToRgb(branding.secondaryColor) : null;
  if (secondary) {
    vars['--survey-accent'] = rgbToHex(secondary);
  }

  return vars as CSSProperties;
}
