/**
 * Fixture tags, one set per component: what each vehicle is FOR, which state it reaches. A state
 * with no vehicle tagged for it is a state no page can show, and one that regresses unnoticed.
 *
 * Here rather than in a page because two pages need the same answer — the component's own showcase
 * and the composite, whose rail follows its tab strip. Written separately they drifted.
 *
 * Keyed by component tag; a component with no set falls back to the page's own `labels`. Grouped
 * by environment (the rail shows one at a time) and in reading order, which the rail follows.
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

  'vehicle-sale-information': {
    /* broker-market — the only environment with brokers and end customers both looked up. */
    ZT9DW6MP5S6570632: 'the whole chain · every leg invoiced',
    ZU9GR4ZC4S6583745: 'three intermediaries, in invoice order',
    ZS8AJAYC9P6174790: 'the distributor sold it direct',
    ZS8HN4KB3S6512083: 'full end customer · id, name, phone, id number',
    ZS8SV9KXXST918958: 'end customer without an id number · no country',
    ZT8RM7DC8S6524196: 'end customer without a name',
    ZU8PX3LAXS6537204: 'end customer without a phone',
    ZS8AJAYC9R6971589: 'customer not in the customer records',
    ZS8EU2ZF6ST814892: 'sold on by a broker · customer from its invoice',
    ZV8EB5GW5S6541378: 'broker-invoice customer with an id number, no customer id',
    ZW8CT2HN3S6553461: 'broker invoiced · no end customer on record',
    ZS9LK8FV6S6562597: 'sold through a non-official broker',
    ZS8S63PG5RJ137144: 'in a broker’s stock, not invoiced on',
    ZV9NF7XK1S6596810: 'names too long for one line',
    ZW9BH3TLXS6604927: 'arabic names in a latin record',
    ZS8YJ5RE0S6617052: 'an all-arabic record',
    ZT8AWXSJ0R4973290: 'blank and zero placeholders, shown as sent',
    ZU8ZL7VAXG2426255: 'authorized, no sale on record',
    ZV8GHHHP37P214642: 'not in distributor records',

    /* edge-cases — the dates and the legs, one variation each. */
    ZW8UWF8J4TJ368365: 'one intermediary · every leg invoiced',
    ZT8WA9PD8T6621184: 'invoice and activation dates both present',
    ZU8MC2VH8T6636219: 'legs with gaps · a number without a date, a date without a number',
    ZV8KS6EX6T6648350: 'UNKNOWN sentinels, shown verbatim',

    /* allocation-market — end customers are never looked up here. */
    ZT8LY7CZ3S2547425: 'distributor → dealer, nothing between',
    ZT8LY7CZ0S2959088: 'activation date, no invoice date',
    ZS8WJEXK506960999: 'dealer without a branch · distributor invoice without a number',
    ZS88F5CL80J407317: 'legs on record, no dealer',
    ZS8WJEXK206451985: 'authorized, no sale on record',
    ZV9LMW6D75T151181: 'not in distributor records',

    /* broker-dealer */
    JTMHF33P5D5012345: 'broker invoice · end customer not looked up',
    ZW8FL4NBXS6659473: 'a sale record with nothing in it',

    /* standard-dealer */
    UNKNOWN_VIN_12345: 'not in distributor records',
  },
};

/** The set for one component, never undefined so a caller can index it without a guard. */
export const tagsFor = component => FIXTURE_TAGS[component] || {};
