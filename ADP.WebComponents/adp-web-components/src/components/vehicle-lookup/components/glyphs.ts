/**
 * Inline SVG paths shared by the panels in the newer design language. Kept as path data rather
 * than <img> assets so a glyph takes the colour of the badge it sits in and stays crisp at any
 * size. All are drawn on a 512 x 512 box.
 */
export const BADGE_GLYPHS = {
  /** A filled circle with a check mark: a positive verdict. */
  positive:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zM369 209L241 337c-9.4 9.4-24.6 9.4-33.9 0l-64-64c-9.4-9.4-9.4-24.6 0-33.9s24.6-9.4 33.9 0l47 47L335 175c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9z',
  /** A filled circle with a cross: a negative verdict. */
  negative:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zM175 175c9.4-9.4 24.6-9.4 33.9 0l47 47 47-47c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9l-47 47 47 47c9.4 9.4 9.4 24.6 0 33.9s-24.6 9.4-33.9 0l-47-47-47 47c-9.4 9.4-24.6 9.4-33.9 0s-9.4-24.6 0-33.9l47-47-47-47c-9.4-9.4-9.4-24.6 0-33.9z',
  /** A filled circle with an exclamation mark: a required action not taken yet — the check must be run. */
  alert:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zm0-384c13.3 0 24 10.7 24 24V264c0 13.3-10.7 24-24 24s-24-10.7-24-24V152c0-13.3 10.7-24 24-24zM224 352a32 32 0 1 1 64 0 32 32 0 1 1-64 0z',
  /** A filled circle with a question mark: "why?", the trace trigger. */
  question:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zM169.8 165.3c7.9-22.3 29.1-37.3 52.8-37.3h58.3c34.9 0 63.1 28.3 63.1 63.1 0 22.6-12.1 43.5-31.7 54.8L280 264.4c-.2 13-10.9 23.6-24 23.6-13.3 0-24-10.7-24-24v-13.5c0-8.6 4.6-16.5 12.1-20.8l44.3-25.4c4.7-2.7 7.6-7.7 7.6-13.1 0-8.4-6.8-15.1-15.1-15.1h-58.3c-3.4 0-6.4 2.1-7.5 5.3l-.4 1.2c-4.4 12.5-18.2 19-30.6 14.6s-19-18.2-14.6-30.6l.4-1.2zM224 352a32 32 0 1 1 64 0 32 32 0 1 1-64 0z',
} as const;

/**
 * The lookup table's media cell: what stands in for a picture that is not there. Drawn here on the
 * same 512 x 512 box as the verdict glyphs, in a single winding (outer frame clockwise, the opening
 * counter-clockwise, the sun and the hill clockwise again) so it needs no fill-rule.
 */
export const MEDIA_GLYPHS = {
  /** A framed picture, a sun and a hill: nothing is on record for this row. */
  picture:
    'M88 80H424A40 40 0 0 1 464 120V392A40 40 0 0 1 424 432H88A40 40 0 0 1 48 392V120A40 40 0 0 1 88 80ZM88 120V392H424V120ZM172 176a32 32 0 1 1 0 64a32 32 0 1 1 0-64ZM120 360L216 252L280 316L336 252L392 360Z',
  /** The same picture struck through: there is a URL on record and it did not load. */
  pictureOff:
    'M88 80H424A40 40 0 0 1 464 120V392A40 40 0 0 1 424 432H88A40 40 0 0 1 48 392V120A40 40 0 0 1 88 80ZM88 120V392H424V120ZM172 176a32 32 0 1 1 0 64a32 32 0 1 1 0-64ZM120 360L216 252L280 316L336 252L392 360ZM56 84L84 56L456 428L428 456Z',
} as const;

/**
 * The lookup table's body placeholders (what stands in where a list would be): large, solid and
 * quiet, drawn on the same 512 x 512 box and in the same single winding as the glyphs above (outer
 * shapes clockwise, every opening counter-clockwise), so they need no fill-rule and take the colour
 * of their role. A panel picks one per outcome.
 */
export const PLACEHOLDER_GLYPHS = {
  /** A magnifier: before a lookup, "look a vehicle up". Asserts nothing about the vehicle. */
  search: 'M216 40A176 176 0 1 1 216 392A176 176 0 1 1 216 40ZM216 88A128 128 0 1 0 216 344A128 128 0 1 0 216 88ZM305 339L339 305L466 432A24 24 0 0 1 432 466Z',
  /** A cube in three faces: a list that would hold things and holds none -- no records on file. */
  box: 'M245.5 61.9Q256 56.1 266.5 61.9L409 142.1Q419.4 148 409 153.9L266.5 234.1Q256 239.9 245.5 234.1L103 153.9Q92.6 148 103 142.1ZM78 183.9Q78 171.9 88.5 177.8L231.5 258.3Q242 264.2 242 276.2L242 436.1Q242 448.1 231.5 442.2L88.5 361.7Q78 355.8 78 343.8ZM270 276.2Q270 264.2 280.5 258.3L423.5 177.8Q434 171.9 434 183.9L434 343.8Q434 355.8 423.5 361.7L280.5 442.2Q270 448.1 270 436.1Z',
  /** A triangle with an exclamation mark: the lookup failed. */
  warning: 'M234.5 86.4Q256 48 277.5 86.4L458.5 409.6Q480 448 436 448L76 448Q32 448 53.5 409.6ZM232 176L232 320L280 320L280 176ZM256 348A28 28 0 1 0 256 404A28 28 0 1 0 256 348Z',
  /** A filled circle with a question mark (the verdict pill's neutral glyph): the vehicle is not in the distributor's records. */
  unknown: BADGE_GLYPHS.question,
} as const;
