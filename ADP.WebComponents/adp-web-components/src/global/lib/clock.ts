const ISO_CALENDAR_DATE = /^\d{4}-\d{2}-\d{2}$/;

/**
 * Resolves a component's optional calendar-date clock to an instant.
 *
 * A supplied value is always midnight UTC. Invalid values fall back to the wall
 * clock so a malformed host attribute cannot leave date UI holding an Invalid
 * Date. Callers that need a calendar label should use `today`, which keeps the
 * same UTC interpretation.
 */
export function now(value?: string): Date {
  if (value && ISO_CALENDAR_DATE.test(value)) {
    const resolved = new Date(`${value}T00:00:00.000Z`);

    if (!Number.isNaN(resolved.getTime()) && resolved.toISOString().slice(0, 10) === value) return resolved;
  }

  return new Date();
}

/** Resolves a component's optional clock to an ISO calendar date in UTC. */
export function today(value?: string): string {
  return now(value).toISOString().slice(0, 10);
}
