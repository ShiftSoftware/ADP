export interface VehicleInfoLayoutInterface {
  coreOnly?: boolean;
}

/**
 * What a panel asserts about the vehicle, in the design language's colour roles: `positive` and
 * `negative` are verdicts (green, red); `neutral` is a statement without a verdict — not in the
 * records, vehicle not found (amber); `attention` is a required action not taken (violet); `idle`
 * asserts nothing (grey) — before a lookup, or when there is nothing to say. The wrapper paints its
 * accent bar from it; a panel derives it in an exported pure function so the accent is testable.
 */
export type VerdictState = 'positive' | 'negative' | 'neutral' | 'attention' | 'idle';
