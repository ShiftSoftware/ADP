/**
 * Fixture tags, one set per component.
 *
 * A tag says what a vehicle is FOR: which state of that component it reaches. The set is the
 * component's own state list written down — a state with no vehicle tagged for it is a state no
 * page can show, and one that will regress unnoticed.
 *
 * It lives here rather than in a page because two pages need the same answer: the component's own
 * showcase page, and the composite, whose rail follows its tab strip. They drifted the first time
 * they were written separately (the composite listed four of the specification's six for one
 * environment), so there is one list and both read it.
 *
 * A component earns a set as it is reworked; a component with none falls back to whatever the page
 * itself declares. Keys are the component's tag name, so the composite can look a set up by the
 * tab it just switched to.
 *
 * Vehicles are grouped by the environment they belong to, because the rail shows one environment at
 * a time — and listed in reading order, because the rail is ordered by this list rather than by the
 * position a vehicle happens to hold in a 31-vehicle environment.
 */
export const FIXTURE_TAGS = {
  'vehicle-specification': {
    /*
     * edge-cases — built for this panel, one vehicle per branch, and the only environment that
     * reaches the model-year disagreement and the full twelve details.
     */
    ZT8VC4MH7TB552731: 'every detail field · 12 details, both groups',
    ZW8UWF8J4TJ368365: 'six details · a real paint code, no name resolved',
    ZU9HB6TM3T4719068: 'one detail · the singular count',
    ZW8PD9FK1T6034557: 'full identity, no details · so no summary row and no trigger',
    ZS8QK3WR5TD881204: 'the two model years disagree',
    ZS8Z4RNS9TC073619: 'year from the record only',

    /* broker-market — realistic volume, and the only place the name precedence shows. */
    ZT8APGED9RB475247: 'the distributor’s own paint name',
    ZS8SV9KXXST918958: 'seven details · a grade too long for the head’s line',
    ZU9XJD4GXPB336861: 'identity only · no details, so no trigger',
    ZU8ZL7VAXG2426255: 'authorized, empty record · no records',
    ZV8GHHHP37P214642: 'not in distributor records',

    /* allocation-market — the sparse end: most vehicles here are identity only. */
    ZT8F9VVT502643013: 'production date · rich details · code = description',
    ZW9KBUWL2T1319799: 'a paint code the record never named',
    ZS8WJEXK106589744: 'UNKNOWN sentinels, shown verbatim',
    ZT8LY7CZ0S2959088: 'every field the panel can draw · 12 details',
    ZS8WJEXK206451985: 'authorized, empty record · no records',
    ZV9LMW6D75T151181: 'not in distributor records',

    /* standard-dealer */
    ZT8P9NAL1LG988010: 'no year, no model code',
    JTMW43FV10D123456: 'a real paint code, no name resolved',
    UNKNOWN_VIN_12345: 'not in distributor records',

    /* broker-dealer — four real paint codes, none of them named by the record. */
    JTMHF33P5D5012345: 'a real paint code',
    JTEBU5JR8L5234567: 'a second real paint code',
    JTEHF21A0Y0123456: 'a third real paint code',
    JTLBR3JR9N5345678: 'a fourth real paint code',
  },
};

/** The set for one component, never undefined so a caller can index it without a guard. */
export const tagsFor = component => FIXTURE_TAGS[component] || {};
