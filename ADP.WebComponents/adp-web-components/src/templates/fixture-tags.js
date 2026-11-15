/**
 * Fixture tags, one set per component: what each vehicle is FOR, which state it reaches. A state
 * with no vehicle tagged for it is a state no page can show, and one that regresses unnoticed.
 *
 * Here rather than in a page because two pages need the same answer — the component's own showcase
 * and the composite, whose rail follows its tab strip. Written separately they drifted.
 *
 * Keyed by component tag; a component with no set falls back to the page's own `labels`. Grouped
 * by environment (the rail shows one at a time) and in reading order, which the rail follows.
 *
 * A tag is its words, or `[accent, words]` with the panel's accent for that vehicle (positive,
 * negative, neutral, attention, idle), which colours the chip like the panel's bar.
 */
export const FIXTURE_TAGS = {
  'vehicle-specification': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['positive', 'every detail field · 12 details, both groups · paint and trim named'],
    ZDWWEB029TA000002: ['positive', 'seven details · a grade too long for the head’s line · long colour names'],
    ZDWWEB03XTA000003: ['positive', 'one detail · the singular count'],
    ZDWWEB040TA000004: ['idle', 'authorized, empty record · no records'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWDEC012SA000006: ['neutral', 'not in distributor records · decoded from the VIN, with the year from character 10'],
    ZDWDEC0270A000007: ['neutral', 'decoded from the VIN · no single katashiki, no year code'],
    ZDWWEB084TA000008: ['positive', 'full identity, no details · a paint code with no name'],
    ZDWWEB095TA000009: ['positive', 'the two model years disagree'],
    ZDWWEB101TA000010: ['positive', 'year from the record only'],
    ZDWWEB112TA000011: ['positive', 'no year, no model code'],
    ZDWWEB123TA000012: ['positive', 'UNKNOWN sentinels, shown verbatim'],
    ZDWWEB134TA000013: ['positive', 'production month · model code = description, so the code cell dashes'],

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
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['positive', 'the whole chain · two intermediaries · sold on by a broker'],
    ZDWWEB029TA000002: ['positive', 'longest names · sold through a non-official broker'],
    ZDWWEB03XTA000003: ['positive', 'one of each leg · customer from the customer records'],
    ZDWWEB040TA000004: ['idle', 'authorized, no sale on record'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWWEB084TA000008: ['positive', 'broker invoiced · no end customer on record'],
    ZDWWEB095TA000009: ['positive', 'legs on record, no dealer'],
    ZDWWEB101TA000010: ['positive', 'end customer without an id number · no country'],
    ZDWWEB112TA000011: ['positive', 'legs with gaps · a number without a date, a date without a number'],
    ZDWWEB123TA000012: ['positive', 'activation date, no invoice date'],
    ZDWWEB134TA000013: ['positive', 'dealer without a branch · distributor invoice without a number'],
    ZDWWEB145TA000014: ['positive', 'with the distributor, not sold on yet'],
    ZDWWEB156TA000015: ['positive', 'in a broker’s stock, not invoiced on'],
    ZDWWEB167TA000016: ['positive', 'blank and zero placeholders, shown as sent'],
    ZDWWEB178TA000017: ['positive', 'the distributor sold it direct'],
    ZDWWEB189TA000018: ['positive', 'customer not in the customer records'],
    ZDWWEB19XTA000019: ['positive', 'distributor → dealer, nothing between'],

    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['positive', 'the whole chain · two intermediaries · sold on by a broker'],
    ZDWWEB029TA000002: ['positive', 'longest names · sold through a non-official broker'],
    ZDWWEB03XTA000003: ['positive', 'one of each leg · customer from the customer records'],
    ZDWWEB040TA000004: ['idle', 'authorized, no sale on record'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWWEB084TA000008: ['positive', 'broker invoiced · no end customer on record'],
    ZDWWEB095TA000009: ['positive', 'legs on record, no dealer'],
    ZDWWEB101TA000010: ['positive', 'end customer without an id number · no country'],
    ZDWWEB112TA000011: ['positive', 'legs with gaps · a number without a date, a date without a number'],
    ZDWWEB123TA000012: ['positive', 'activation date, no invoice date'],
    ZDWWEB134TA000013: ['positive', 'dealer without a branch · distributor invoice without a number'],
    ZDWWEB145TA000014: ['positive', 'with the distributor, not sold on yet'],
    ZDWWEB156TA000015: ['positive', 'in a broker’s stock, not invoiced on'],
    ZDWWEB167TA000016: ['positive', 'blank and zero placeholders, shown as sent'],
    ZDWWEB178TA000017: ['positive', 'the distributor sold it direct'],
    ZDWWEB189TA000018: ['positive', 'customer not in the customer records'],
    ZDWWEB19XTA000019: ['positive', 'distributor → dealer, nothing between'],

    /* broker-market — the only environment with brokers and end customers both looked up. */
    ZT9DW6MP5S6570632: 'the whole chain · every leg invoiced',
    ZS8AJAYC9P6174790: 'the distributor sold it direct',
    ZS8HN4KB3S6512083: 'dealer sale · end customer from the customer records',
    ZS8SV9KXXST918958: 'end customer without an id number · no country',
    ZS8AJAYC9R6971589: 'customer not in the customer records',
    ZS8EU2ZF6ST814892: 'sold on by a broker · customer from its invoice',
    ZW8CT2HN3S6553461: 'broker invoiced · no end customer on record',
    ZS9LK8FV6S6562597: 'sold through a non-official broker',
    ZS8S63PG5RJ137144: 'in a broker’s stock, not invoiced on',
    ZT8AWXSJ0R4973290: 'blank and zero placeholders, shown as sent',
    ZU8ZL7VAXG2426255: 'authorized, no sale on record',
    ZV8GHHHP37P214642: 'not in distributor records',

    /* edge-cases — the dates and the legs, one variation each. */
    ZW8UWF8J4TJ368365: 'one intermediary · every leg invoiced',
    ZU8MC2VH8T6636219: 'legs with gaps · a number without a date, a date without a number',

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

  'vehicle-accessories': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['positive', '10 accessories · every drawing, generic carton, no picture · same part twice · 2 pages with pagination'],
    ZDWWEB029TA000002: ['positive', 'two accessories · a 54-char part number and a 159-char description'],
    ZDWWEB03XTA000003: ['positive', 'one accessory · the singular count'],
    ZDWWEB040TA000004: ['idle', 'authorized, nothing on file · no records'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWDEC012SA000006: ['neutral', 'not in distributor records · accessories still listed'],
    ZDWWEB095TA000009: ['positive', 'five accessories · fits one page, no pager'],
    ZDWWEB101TA000010: ['positive', 'no part number on one row, no description on another · dashes'],

    /* older environments, from the page */
    ZT8LY7CZ0S2959088: 'no accessories on record',
    JTMW43FV10D123456: 'no accessories on record',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'not in distributor records',
  },

  'vehicle-warranty-timeline': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB029TA000002: ['positive', 'long names · dealer and broker both named · four coverages, tones cycle'],
    ZDWWEB03XTA000003: ['positive', 'one of each · standard + one extended · 1 year singular'],
    ZDWWEB040TA000004: ['negative', 'authorized, nothing on file · awaiting activation'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWDEC012SA000006: ['neutral', 'not in distributor records · activation record only'],
    ZDWWEB095TA000009: ['positive', 'awaiting activation · extended already on file · no dealer'],
    ZDWWEB101TA000010: ['positive', 'multiple warranties · standard + two extended, one named · activated by the dealer'],
    ZDWWEB112TA000011: ['positive', 'standard expired · extended running'],
    ZDWWEB123TA000012: ['negative', 'not started yet · date shift to a future start'],
    ZDWWEB134TA000013: ['negative', 'expired'],
    ZDWWEB145TA000014: ['negative', 'distributor possession'],
    ZDWWEB156TA000015: ['negative', 'broker, un-invoiced'],
    ZDWWEB167TA000016: ['negative', 'awaiting activation'],
    ZDWWEB178TA000017: ['positive', 'distributor direct sale · activated by the distributor'],
    ZDWWEB206TA000020: ['positive', 'activation on file, no vehicle entry · dealer dashes'],

    /* older environments, from the page */
    ZU8PC1Z95PP935130: 'multiple warranties',
    ZU95VW7N9PL234530: 'awaiting activation',
    ZT9T1XG12PT901120: 'awaiting activation, unauthorized',
    ZU99J61K7PZ167712: 'distributor possession',
    ZW9V34J06PY398057: 'broker, un-invoiced',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'not in distributor records',
  },

  'vehicle-ssc': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['negative', '10 campaigns · 7 open, 3 repaired'],
    ZDWWEB029TA000002: ['negative', 'long texts · 3 labor codes and 5 parts on one campaign · chips wrap'],
    ZDWWEB03XTA000003: ['negative', 'one open campaign · one labor code, one part, in stock'],
    ZDWWEB040TA000004: ['positive', 'authorized, nothing on file · no campaign affects this vehicle'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records · manufacturer check offered'],
    ZDWWEB084TA000008: ['positive', 'all repaired · one by record date, one by a claim that names the campaign in its comment'],
    ZDWWEB095TA000009: ['positive', 'record date and a qualifying claim both · the record decides'],
    ZDWWEB101TA000010: ['positive', 'repaired by a claim with no completion date · Repaired, no date'],
    ZDWWEB112TA000011: ['negative', 'open campaign with no labor code and no parts · both cells dash'],
    ZDWWEB123TA000012: ['negative', 'same campaign listed twice by the feed · two rows'],
    ZDWWEB167TA000016: ['positive', 'repaired by invoice under an interchangeable code · order closed, invoice status blank'],
    ZDWWEB206TA000020: ['negative', '5 campaigns · 2 open, 3 repaired by record, claim and invoice · interchangeable code · parts in and out of stock · near misses traced'],

    /* older environments, from the page */
    ZT8P9NAL1LG988010: 'open + repaired, traced',
    JTMHF33P5D5012345: 'open campaign',
    JTEBU5JR8L5234567: 'repaired campaign',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'not in distributor records',
  },

  'vehicle-service-history': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: [
      'positive',
      '7 visits, 2 dealers · a parent invoice in the drawer · many labor and part lines · a visit with no part lines · drawer drops empty sub-columns',
    ],
    ZDWWEB03XTA000003: ['positive', '1 visit'],
    ZDWWEB040TA000004: ['idle', 'no service history on record'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWDEC012SA000006: ['neutral', 'not in records, visits listed'],
    ZDWWEB084TA000008: ['positive', '51 visits'],
    ZDWWEB095TA000009: ['positive', 'missing branch'],
    ZDWWEB101TA000010: ['positive', 'missing dealer and branch'],
    ZDWWEB112TA000011: ['positive', 'a parts-only visit'],
    ZDWWEB123TA000012: ['positive', 'an undated visit'],
    ZDWWEB134TA000013: ['positive', 'no job or account no.'],
    ZDWWEB156TA000015: ['positive', 'long service type, missing mileage'],

    /* older environments, from the page */
    ZS8AJAYC7N6060123: '51 visits',
    ZS8SV9KXXST918958: 'long service type, missing mileage',
    ZS8EU2ZF5RT696831: 'missing branch',
    ZU9TC6CF8S6255694: 'a parent invoice in the drawer',
    ZS8AJAYC9P6174790: 'many labor and part lines',
    ZT8AWXSJ0R4973290: 'a visit with no part lines',
    ZS8AJAYCXR6471313: 'no service history on record',
    ZS88F5CL80J407317: 'no service history on record',
    JTMW43FV10D123456: 'no service history on record',
    ZV8GHHHP37P214642: 'not in distributor records',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'unknown VIN, not in distributor records',
  },

  'vehicle-paint-thickness': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB018TA000001: ['positive', 'four inspections · readings and photos on every row · certificate'],
    ZDWWEB029TA000002: ['positive', 'five inspections · PDI and four pre-owned · decimal readings'],
    ZDWWEB03XTA000003: ['positive', 'one inspection · one photo per reading · certificate'],
    ZDWWEB040TA000004: ['idle', 'no readings on record'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWDEC012SA000006: ['neutral', 'not in distributor records · inspections listed'],
    ZDWWEB084TA000008: ['positive', 'one pre-owned inspection · no certificate'],
    ZDWWEB095TA000009: ['positive', 'a tab with no source · a tab with no date'],
    ZDWWEB112TA000011: ['positive', 'one inspection · no photos at all'],
    ZDWWEB123TA000012: ['positive', 'one inspection · some rows without photos'],
    ZDWWEB134TA000013: ['positive', 'one inspection · no readings in it'],
    ZDWWEB167TA000016: ['positive', 'PDI after the distributor sale · no certificate'],
    ZDWWEB206TA000020: ['positive', 'one panel read twice · second row'],

    /* older environments, from the page */
    ZU9LG6DGXS6464981: 'four inspections · readings and photos on every row',
    ZS8AJAYC9P6174790: 'three inspections · PDI and two pre-owned',
    ZS8SV9KXXST918958: 'one inspection · certificate available',
    ZS8MFGFV7R6554235: 'one pre-owned inspection · no certificate',
    JTEBU5JR8L5234567: 'one inspection · no photos at all (broker-dealer)',
    ZT8P9NAL1LG988010: 'one inspection · some rows without photos (standard-dealer)',
    ZS8AJAYCXR6471313: 'no readings on record',
    ZT8LY7CZ0S2959088: 'no readings on record',
    JTMW43FV10D123456: 'no readings on record',
    ZV8GHHHP37P214642: 'not in distributor records',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'not in distributor records',
  },

  'vehicle-claimable-items': {
    /* website — made up for the published site: every state of this panel in one environment. */
    ZDWWEB029TA000002: ['positive', 'long names and codes · reward unlocked, evidence complete'],
    ZDWWEB03XTA000003: ['positive', 'one paid item'],
    ZDWWEB040TA000004: ['idle', 'authorized, nothing on file · no records'],
    ZDWWEB051TA000005: ['neutral', 'not in distributor records'],
    ZDWWEB145TA000014: ['positive', 'activation blocked · at distributor stock, not allocated'],
    ZDWWEB156TA000015: ['positive', 'broker stock, not invoiced · broker warning'],
    ZDWWEB167TA000016: ['positive', 'activation required · allocated to this dealer'],
    ZDWWEB178TA000017: ['positive', 'every status · expired, cancelled, claimed, locked reward, paid, future campaign'],
    ZDWWEB189TA000018: ['positive', 'nothing left to claim · reward missed'],
    ZDWWEB19XTA000019: ['positive', 'reward claimed · prerequisites kept'],

    /* older environments, from the page */
    ZT8P9NAL1LG988010: 'not available · null',
    ZT919TCX9L8082249: 'not available · empty',
    ZU8PC1Z95PP935130: 'realistic',
    ZV8DK3F90PP174225: 'realistic, at distributor stock',
    BLOCKEDNOTALLOC01: 'blocked, not allocated',
    ZS8WJEXK206451985: 'no claimable items on record',
    ZV9LMW6D75T151181: 'not in distributor records',
    UNKNOWN_VIN_12345: 'not in distributor records',
  },
};

/** The set for one component, never undefined so a caller can index it without a guard. */
export const tagsFor = component => FIXTURE_TAGS[component] || {};
