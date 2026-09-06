/**
 * Theming reference for the ticket-form components.
 *
 * Plain data. No DOM, no framework — the docs page renders it.
 *
 * Everything here was read off the source, not off the demos:
 *
 *   src/components/forms/defaults/style.css          the host reset
 *   src/components/form-elements/form-inputs.css     every form-element style
 *   src/components/components/shift-portal.tsx       the light-DOM escape hatch
 *   src/global/lib/get-custom-classes-for-portal.ts  what travels with it
 *   src/global/lib/middleware.ts                     the Google Fonts injection
 *   src/features/form-hook/render-structure.tsx      how a structure node names itself
 *   every `part=` under src/components/forms and src/components/form-elements
 *
 * The one sentence to read first: `::part()` is the entire theming surface.
 * There are no CSS custom properties to set, no theme tokens, no Sass entry
 * point. You get a list of named elements and ordinary CSS on each of them.
 */

export const layers = [
  {
    id: 'global',
    title: 'The whole form, through the shift-form part',
    blurb:
      'Every form component renders one outer div carrying part="shift-form". It is the only part guaranteed to exist whichever form tag you used, so it is where the frame lives: the font, the max width, the rhythm between fields. It is also the first token copied onto the panels that leave the shadow root, which is why the portaled overlays can be reached by the same class you put on the form element.',
    examples: [
      {
        label: 'The frame',
        css: `general-form::part(shift-form) {
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 640px;
  font-family: 'Nunito', system-ui, sans-serif !important;
}`,
        note: 'The !important on font-family is not decoration — see the Fonts layer and the "The form does not inherit your page font" limit. Everything else here is an ordinary declaration.',
      },
      {
        label: 'Every field at once',
        css: `/* These aliases are carried by every field of their kind, so one rule
   reaches all of them regardless of what the structure named the field. */
general-form::part(form-input),
general-form::part(form-input-select),
general-form::part(form-input-textarea),
general-form::part(form-file-trigger) {
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 10px 12px;
  background: #fff;
}

general-form::part(form-input):focus,
general-form::part(form-input-select):focus,
general-form::part(form-input-textarea):focus {
  border-color: #475569;
  outline: 2px solid #cbd5e1;
  outline-offset: 1px;
}`,
        note: 'form-file-trigger also carries form-input, so the first selector already covers the upload button; list it separately only when the button should differ from the text fields.',
      },
      {
        label: 'Two forms on one page, styled differently',
        css: `<!-- Put the class on the element, not on a wrapper. -->
<general-form class="enquiry-theme"></general-form>
<service-booking-form class="booking-theme"></service-booking-form>

/* A class on the host is the only hook that reaches BOTH the shadow tree and
   the panels the form portals to <body>. A wrapper around the element
   reaches neither. */
.enquiry-theme::part(shift-form) {
  max-width: 560px;
}

.booking-theme::part(shift-form) {
  max-width: 820px;
}`,
        note: 'Prefer the host class over the bare tag name in anything you ship. general-form::part(...) is fine while there is one form on the page and becomes a bug the day there are two.',
      },
      {
        label: 'The parts around shift-form',
        css: `/* form-container is the positioned box holding the loader and the form.
   form-loader-container is the spinner covering it until the structure has
   rendered; it fades out on its own. */
general-form::part(form-container) {
  min-height: 220px;
}

general-form::part(form-loader-container) {
  background: #f8fafc;
}

general-form::part(form-loader-icon) {
  width: 28px;
  height: 28px;
}`,
        note: 'form-structure-form-container sits one level in and wraps the rendered structure; form-stepper-container becomes the direct wrapper instead when the structure declares steps.',
      },
    ],
  },

  {
    id: 'theme-identifier',
    title: 'The theme identifier: data.theme and the theme prop',
    blurb:
      'The shift-form div is rendered as part={cn("shift-form", structure.data.theme, this.theme)}. Two independent inputs — a theme key in the structure JSON and a theme attribute on the element — become extra part tokens on that same div. Both then travel to the portaled panels as classes, so one identifier can style the form and its overlays together. This is the mechanism a deployment uses when the structure, not the page, decides which look applies.',
    examples: [
      {
        label: 'From the structure JSON',
        css: `// structure.json
{
  "data": { "theme": "night" },
  "children": [ /* ... */ ]
}

/* The token lands on the same div as shift-form. */
general-form::part(night) {
  background: #0f172a;
  padding: 28px;
  border-radius: 14px;
}`,
        note: 'The structure is fetched, so this lets one page render a different look per form without a deploy. It is also the identifier a page author cannot see in the markup — worth knowing when a theme rule appears to fire from nowhere.',
      },
      {
        label: 'From the element',
        css: `<general-form theme="night"></general-form>

general-form::part(night) {
  background: #0f172a;
}`,
        note: 'The theme prop and structure.data.theme are additive, not alternatives: set both and the div carries both tokens.',
      },
      {
        label: 'The same identifier reaches the portaled panels',
        css: `/* The token is copied onto the portaled elements as a CLASS, not a part —
   so on the form it is ::part(night), and on the overlays it is .night. */
.night::part(dialog-wrapper) {
  background: #0f172a;
  color: #e2e8f0;
}

.night::part(shift-select-container) {
  background: #1e293b;
  border-color: #334155;
}`,
        note: 'This is the one asymmetry worth memorising. Inside the form the identifier is a part; outside it is a class, because the portal copies part tokens and classes together into the copy’s className.',
      },
      {
        label: 'The host class is an identifier too',
        css: `<general-form class="docs-form"></general-form>

/* A class on the element is copied to the portals the same way, and is the
   hook the four shipped presets use for exactly that reason. */
.docs-form::part(shift-form)     { /* the form */ }
.docs-form::part(dialog-wrapper) { /* the dialog, at the end of <body> */ }`,
        note: 'Practical advice: use the host class as the theme hook and keep data.theme for cases where the structure has to choose. From CSS they behave identically.',
      },
      {
        label: 'vehicle-quotation-form is the exception',
        css: `/* This one form composes its token differently — it ignores the theme prop
   and prefixes the structure key: */
vehicle-quotation-form::part(vehicle-quotation-night) {
  background: #0f172a;
}`,
        note: 'Source: vehicle-quotation.tsx renders part="shift-form vehicle-quotation-<data.theme>". Every other form uses cn("shift-form", data.theme, theme).',
      },
    ],
  },

  {
    id: 'structure-parts',
    title: 'Part names the structure author writes',
    blurb:
      'A structure node with a "tag" is rendered as that HTML tag with part={cn(id, class, "element-" + tag, tag)}. Four tokens: the id you wrote, the class you wrote, a generic element-<tag>, and the bare tag. This is how layout gets named — wrappers, section headings, column groups. None of these names come from the components, which is why they are the flexible half of theming and the half that breaks when the structure changes.',
    examples: [
      {
        label: 'One node, four part names',
        css: `// structure.json
{ "tag": "div", "id": "inputs_wrapper", "class": "stack", "children": [ /* ... */ ] }

/* All four of these match that same div: */
general-form::part(inputs_wrapper) { }  /* the id */
general-form::part(stack)          { }  /* the class */
general-form::part(element-div)    { }  /* generic, every div node */
general-form::part(div)            { }  /* generic, every div node */`,
        note: 'Use the id. element-div and div match every div node in the structure and are almost never what you want.',
      },
      {
        label: 'Layout, which is what these names are for',
        css: `/* Single column first — a form squeezed into two columns on a phone is
   worse than one that never tried. */
.docs-form::part(inputs_wrapper) {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

@media (min-width: 640px) {
  .docs-form::part(inputs_wrapper) {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 20px 24px;
  }

  /* Field part names come from the field name, so they hold whichever
     wrapper the structure put them in. */
  .docs-form::part(message),
  .docs-form::part(submit-button) {
    grid-column: 1 / -1;
  }
}`,
        note: 'This is what themes/two-column.css does. Note the mix: the wrapper is an authored name, the exceptions are derived names.',
      },
      {
        label: 'A heading node',
        css: `// structure.json
{ "tag": "h1", "class": "section-title", "children": { "en": "Your details", "ar": "بياناتك" } }

.docs-form::part(section-title) {
  font-size: 20px;
  font-weight: 700;
  color: #0f172a;
}`,
        note: 'A tag node whose children is an object keyed by language renders the entry for the current language. vehicle-quotation-form also emits part="section-title" from its own element mapper for three built-in headings.',
      },
      {
        label: 'What happens when the structure changes',
        css: `/* Rename the node and the rule stops matching. Nothing warns you.
     { "tag": "div", "id": "inputs_wrapper" }
   →  { "tag": "div", "id": "fields" }                                */

.docs-form::part(inputs_wrapper) { /* now matches nothing */ }
.docs-form::part(fields)         { /* the replacement */ }`,
        note: 'Treat the ids in a structure as part of its public surface once a theme depends on them, the same way you would treat a field name.',
      },
    ],
  },

  {
    id: 'field-parts',
    title: 'Per-field parts derived from name',
    blurb:
      'Every field element builds its parts from its own name — the key the structure used, which is also the key the mapper matched and the key the value is submitted under. A field called email gives you email on the wrapper, email-container on the box, email-input on the control, email-label on the label, email-error-message on the message. These names are stable: they do not care where the layout put the field, and they change only if the field is renamed.',
    examples: [
      {
        label: 'The five parts of a text field',
        css: `/* For a field named "email" in the structure. */
.docs-form::part(email)               { /* <label> wrapper */ }
.docs-form::part(email-label)         { /* the label text */ }
.docs-form::part(email-container)     { /* the positioned box */ }
.docs-form::part(email-input)         { /* the <input> itself */ }
.docs-form::part(email-error-message) { /* the validation message */ }

/* Every one of those also carries a generic alias, so the same rule written
   against the alias reaches every field of that kind: */
.docs-form::part(form-input) { /* every text input in the form */ }`,
        note: 'The generic alias and the derived name are two tokens on one element. Style with the alias, override with the name.',
      },
      {
        label: 'One field, differently',
        css: `/* A grid where one field spans both columns and one is narrow. */
.docs-form::part(message) {
  grid-column: 1 / -1;
}

.docs-form::part(postcode-input) {
  max-width: 10ch;
}`,
        note: 'part(message) is the field wrapper; part(message-textarea) is the <textarea>. Sizing usually belongs on the wrapper, appearance on the control.',
      },
      {
        label: 'The required star and the error state',
        css: `.docs-form::part(form-input-label-required-star) {
  color: #b91c1c;
}

/* Only the field that is failing: */
.docs-form::part(email-label-required-star) {
  color: #dc2626;
}

/* The error message element carries a third token, display-error-message,
   while the message is actually shown. */
.docs-form::part(form-error-message) {
  font-size: 12px;
  color: #b91c1c;
}

.docs-form::part(display-error-message) {
  font-weight: 600;
}`,
        note: 'The message element is always in the DOM; display-error-message is the token that reveals it. Do not display:none the form-error-message-container — it is what reserves the space the message animates into.',
      },
      {
        label: 'Fields whose label carries no name',
        css: `/* form-input, form-picker-input, form-vin-input, form-date-picker and
   form-time-picker call the label component WITHOUT a name, so their labels
   emit form-input-label but NOT <name>-label. */

.docs-form::part(form-input-label) { /* works everywhere */ }
.docs-form::part(email-label)      { /* matches nothing for a form-input field */ }

/* If one of those labels must differ, give the field a distinct wrapper in
   the structure and style the wrapper — ::part() cannot be used as an
   ancestor, so there is no descendant route to it. */`,
        note: 'The full list is in the limits, under "Some labels have no per-field part". The affected controls are exactly the ones whose label component is called without the name prop.',
      },
      {
        label: 'A select and its trigger',
        css: `/* A select field named "city": wrapper and label come from form-select,
   the box and the input from the shift-select inside it. */
.docs-form::part(city)              { /* <label> wrapper */ }
.docs-form::part(city-label)        { /* label text (form-select DOES pass name) */ }
.docs-form::part(city-container)    { /* the box */ }
.docs-form::part(city-input-select) { /* the readonly / search input */ }
.docs-form::part(city-arrow-icon)   { /* the chevron */ }
.docs-form::part(city-cross-icon)   { /* the clear button, when clearable */ }

/* The options list is NOT here — it is portaled. See the next layer. */`,
        note: 'branch-slot-picker and branch-date-picker expose the same trigger shape: <name>, <name>-container, <name>-input-select, <name>-arrow-icon.',
      },
    ],
  },

  {
    id: 'portal',
    title: 'The overlays that leave the shadow root',
    blurb:
      'Four components are not rendered where they are declared. shift-portal creates the element with document.createElement and appends it to document.body, so it escapes any overflow:hidden or stacking context the host page puts around the form. They are form-dialog, shift-select-dropdown, branch-slot-dropdown and branch-date-dropdown — and unlike everything else in the form, all four are shadow:true themselves. general-form::part(x) cannot reach them: they are no longer in that shadow tree. What reaches them is the class shift-portal copies onto them.',
    examples: [
      {
        label: 'What travels, exactly',
        css: `<!-- You write: -->
<general-form class="docs-form" theme="night"></general-form>

<!-- At the end of <body>, at runtime: -->
<form-dialog class="shift-form night docs-form"></form-dialog>
<shift-select-dropdown class="city-select shift-form night docs-form"></shift-select-dropdown>

/* So all of these work: */
.docs-form::part(dialog-wrapper)     { }
.night::part(shift-select-container) { }
.shift-form::part(dialog-wrapper)    { /* every form on the page */ }`,
        note: 'getCustomClassesForPortal walks up from the field, collects part tokens and classes from the first ancestor carrying shift-form or shift-component, crosses the shadow boundary to the host, and adds the host’s classes too. That is the whole contract.',
      },
      {
        label: 'A dark form needs this or the dropdown stays white',
        css: `.docs-form::part(shift-form) {
  background: #0f172a;
}

/* Without the rules below, a dark form opens a white dropdown and a white
   dialog, because neither is inside the form any more. */
.docs-form::part(shift-select-container) {
  background: #1e293b;
  border: 1px solid #334155;
  border-radius: 10px;
}

.docs-form::part(shift-select-option) {
  color: #e2e8f0;
  padding: 10px 14px;
}

.docs-form::part(shift-select-option-selected) {
  background: #334155;
}

.docs-form::part(dialog-drop-container) {
  background: rgb(0 0 0 / 0.65);
}

.docs-form::part(dialog-wrapper) {
  background: #0f172a;
  color: #e2e8f0;
}`,
        note: 'This is the entire point of themes/dark-panel.css. Any theme that changes the background is incomplete until the portaled panels are covered.',
      },
      {
        label: 'Per-field hooks on the panels',
        css: `/* The portal class starts with a field-specific token, so one field's panel
   can differ from the rest:
     shift-select-dropdown  →  <name>-select
     branch-slot-dropdown   →  <name>-slot-picker
     branch-date-dropdown   →  <name>-date-picker   */

.city-select::part(shift-select-container) {
  max-height: 420px;
}

.pickupSlot-slot-picker::part(branch-slot-container) {
  min-width: 340px;
}

.pickupDate-date-picker::part(branch-date-container) {
  border-radius: 16px;
}`,
        note: 'These tokens are classes on the portaled element, so they combine with the theme class: .docs-form.city-select::part(shift-select-container).',
      },
      {
        label: 'Do not target the tag',
        css: `/* Wrong: hits every form on the page, including ones with another theme. */
form-dialog::part(dialog-wrapper) {
  background: #0f172a;
}

/* Right: scoped to the form that owns it. */
.docs-form::part(dialog-wrapper) {
  background: #0f172a;
}`,
        note: 'Every form instance portals its own dialog, and every select portals its own dropdown; they all pile up at the end of <body>. The theme class is the only thing distinguishing them.',
      },
      {
        label: 'The branch pickers, both panels',
        css: `.docs-form::part(branch-slot-container),
.docs-form::part(branch-date-container) {
  background: #1e293b;
  border: 1px solid #334155;
  color: #e2e8f0;
}

.docs-form::part(branch-slot-day-selected),
.docs-form::part(branch-slot-time-selected),
.docs-form::part(branch-date-cell-selected),
.docs-form::part(branch-date-time-selected) {
  background: #e2e8f0;
  color: #0f172a;
}

.docs-form::part(branch-date-cell-blocked) {
  opacity: 0.35;
}

/* Below 600px both panels become a bottom sheet and the scrim appears. */
.docs-form::part(branch-slot-backdrop),
.docs-form::part(branch-date-backdrop) {
  background: rgb(0 0 0 / 0.55);
}`,
        note: 'The panels position themselves through --branch-slot-* / --branch-date-* custom properties written by JS on every reposition. Do not set those from CSS; see the limits.',
      },
    ],
  },

  {
    id: 'fonts',
    title: 'Fonts',
    blurb:
      'The package injects one Google Fonts stylesheet into document.head on load — Noto Kufi Arabic and Nunito — from src/global/lib/middleware.ts, which Stencil runs as the global script. That is a fixed behaviour of the bundle, not a setting. On top of that the form host carries all: initial !important, so the page typography does not cross the boundary by inheritance. The consequence is simple: declare the font on ::part(shift-form), and mark it !important.',
    examples: [
      {
        label: 'Set the form font',
        css: `.docs-form::part(shift-form) {
  font-family: 'Nunito', system-ui, sans-serif !important;
}`,
        note: 'All four shipped presets do exactly this and nothing else about fonts. Everything inside the form inherits from that div — including inputs, buttons and textareas, which the bundled preflight already gives font-family: inherit.',
      },
      {
        label: 'Why the host itself is not the place',
        css: `/* Neither of these does anything. Both component stylesheets that land in
   the form's shadow root put all: initial !important on :host, and an
   important declaration in an inner shadow tree beats an important one from
   the outer page. */
general-form { font-family: 'Nunito', sans-serif; }
general-form { font-family: 'Nunito', sans-serif !important; }

/* This works. */
general-form::part(shift-form) { font-family: 'Nunito', sans-serif !important; }`,
        note: 'Same reason a wrapper div with a font on it has no effect: the reset cuts inheritance before the wrapper font reaches the form.',
      },
      {
        label: 'Self-hosting instead of the injected sheet',
        css: `/* The injected <link> stays — it is unconditional — but nothing forces you
   to use those families. Declare your own on the page and point the form at
   it; the font file is fetched by the page, not by the component. */
@font-face {
  font-family: 'Brand Sans';
  src: url('/assets/fonts/brand-sans.woff2') format('woff2');
  font-weight: 100 900;
  font-display: swap;
}

.docs-form::part(shift-form) {
  font-family: 'Brand Sans', 'Noto Kufi Arabic', system-ui, sans-serif !important;
}`,
        note: 'Keep an Arabic-capable family in the stack. The components ship in en, ar, ku and ru, and the Arabic and Kurdish forms render right-to-left in whatever face you leave them.',
      },
      {
        label: 'The portaled panels keep their own font',
        css: `/* No form of this works. The panels declare their font with !important from
   inside their own shadow tree, twice — on :host and again on *, :host *. */
.docs-form::part(shift-select-container) { font-family: 'Brand Sans' !important; }
shift-select-dropdown                    { font-family: 'Brand Sans' !important; }

/* Everything else about them is themeable. Only the typeface is fixed. */
.docs-form::part(shift-select-container) {
  background: #1e293b;
  border-radius: 10px;
  font-size: 15px;      /* size, weight, colour, spacing: all fine */
}`,
        note: 'Documented in full under the limits. If the mismatch matters, the fix is a source change to those four stylesheets, not a theme.',
      },
    ],
  },
];

/**
 * The inventory.
 *
 * `scope: 'form'` means the element is inside the form's single shadow root and
 * is reached with `<form-tag>::part(x)` or `.your-class::part(x)`.
 *
 * `scope: 'portal'` means the element lives in a shadow root of its own on an
 * element appended to document.body. `<form-tag>::part(x)` will never match it;
 * use the class that shift-portal copied across — see the portal layer.
 *
 * `<name>` is the field's name from the structure. Where a row lists two names
 * separated by a space, both tokens are on the same element: the derived one
 * and the generic alias.
 */
export const parts = [
  // ── The form shell ───────────────────────────────────────────────────────
  {
    part: 'shift-form',
    scope: 'form',
    appliesTo: 'The outer div of every form component',
    note: 'Always present. The frame goes here: font, width, gap, background. Also the first class copied onto the portaled panels.',
  },
  {
    part: '<data.theme>',
    scope: 'form',
    appliesTo: 'The same div as shift-form',
    note: 'The theme key from the structure JSON, verbatim. Absent if the structure declares none.',
  },
  {
    part: '<theme prop>',
    scope: 'form',
    appliesTo: 'The same div as shift-form',
    note: 'The theme attribute on the element, verbatim. Additive with data.theme. Not read by vehicle-quotation-form.',
  },
  {
    part: 'vehicle-quotation-<data.theme>',
    scope: 'form',
    appliesTo: 'vehicle-quotation-form only',
    note: 'That form prefixes the structure key instead of emitting it bare, and ignores the theme prop.',
  },
  {
    part: 'form-container',
    scope: 'form',
    appliesTo: 'The positioned box inside shift-form',
    note: 'Holds the loader overlay and the form. Carries a min-height so the loader has somewhere to sit.',
  },
  {
    part: 'form-loader-container',
    scope: 'form',
    appliesTo: 'The full-cover spinner overlay',
    note: 'Fades to opacity 0 once the structure has rendered. Style the background here if a white flash is wrong on your page.',
  },
  {
    part: 'form-loader-icon',
    scope: 'form',
    appliesTo: 'The spinner image inside the loader overlay',
    note: 'Sized 32px by default; it spins from a keyframe you cannot reach, but width/height/opacity are yours.',
  },
  { part: 'form-structure-form-container', scope: 'form', appliesTo: 'The wrapper around the rendered structure', note: 'Present on every form. One level inside form-container.' },
  {
    part: 'form-stepper-container',
    scope: 'form',
    appliesTo: 'Replaces the plain wrapper when the structure declares steps',
    note: 'Carries overflow:hidden so the step panes can slide. Changing that breaks the step transition.',
  },
  {
    part: 'form-structure-error-container',
    scope: 'form',
    appliesTo: 'Shown instead of the form when no structure could be loaded',
    note: 'Worth theming: it is what an integrator sees when the structure URL is wrong.',
  },
  { part: 'form-structure-error-content', scope: 'form', appliesTo: 'The message box inside the structure-error container', note: 'Default is a red-tinted card at 20px.' },

  // ── Shared field chrome ──────────────────────────────────────────────────
  {
    part: 'form-input-label',
    scope: 'form',
    appliesTo: 'The label text of every field that renders one',
    note: 'Always present when the field has a label; the per-field twin is not — see the limits.',
  },
  {
    part: '<name>-label',
    scope: 'form',
    appliesTo: 'The same label, when the component passes its name',
    note: 'Emitted by form-select, form-text-area, form-file, form-phone-number, branch-slot-picker and branch-date-picker only.',
  },
  { part: 'form-input-label-required-star', scope: 'form', appliesTo: 'The asterisk after a required field label', note: 'A span inside the label. Red by default.' },
  { part: '<name>-label-required-star', scope: 'form', appliesTo: 'The same asterisk, when the component passes its name', note: 'Same six components as <name>-label.' },
  {
    part: 'form-error-message-container',
    scope: 'form',
    appliesTo: 'The clipping box below every field',
    note: 'Reserves the space the message slides into. Do not hide it — hide the message instead.',
  },
  { part: '<name>-error-message-container', scope: 'form', appliesTo: 'The same box, per field', note: 'Every field component passes its name here, unlike the label.' },
  { part: 'form-error-message', scope: 'form', appliesTo: 'The validation message text', note: 'Always rendered, at opacity 0 until there is an error.' },
  { part: '<name>-error-message', scope: 'form', appliesTo: 'The same message, per field', note: 'Use this to give one field a different error treatment.' },
  {
    part: 'display-error-message',
    scope: 'form',
    appliesTo: 'Added to the message element while an error is showing',
    note: 'A state token, not an element. Pair it with form-error-message to style only the visible state.',
  },
  {
    part: 'form-input-prefix',
    scope: 'form',
    appliesTo: 'The static prefix inside an input (form-input, form-picker-input, form-vin-input, form-phone-number)',
    note: 'Absolutely positioned and pointer-events:none. The input padding is set from JS off its measured width.',
  },
  { part: '<name>-prefix', scope: 'form', appliesTo: 'The same prefix, per field', note: 'All four prefix-capable components pass their name.' },

  // ── form-input ───────────────────────────────────────────────────────────
  { part: '<name>', scope: 'form', appliesTo: 'The <label> wrapper of every field', note: 'The outermost per-field element. Grid placement and width belong here.' },
  {
    part: '<name>-container form-input-container',
    scope: 'form',
    appliesTo: 'The positioned box holding the control, prefix and icons',
    note: 'Gains an "open" or "disabled" class internally, but those are classes, not parts.',
  },
  {
    part: '<name>-input form-input',
    scope: 'form',
    appliesTo: 'The <input> of form-input, form-vin-input, form-phone-number and the legacy pickers',
    note: 'The main text-field selector. form-input is the alias that reaches all of them at once.',
  },
  {
    part: '<name>-icon form-input-icon',
    scope: 'form',
    appliesTo: 'The trailing or leading icon of form-input and form-picker-input',
    note: 'Rendered as a button when the field defines an icon action, otherwise a span. Both carry the same parts.',
  },

  // ── form-text-area ───────────────────────────────────────────────────────
  {
    part: 'form-textarea <name>',
    scope: 'form',
    appliesTo: 'The <label> wrapper of a textarea field',
    note: 'Note the shape: form-textarea is on the WRAPPER, not on the textarea element.',
  },
  {
    part: '<name>-textarea form-input-textarea',
    scope: 'form',
    appliesTo: 'The <textarea> itself',
    note: 'This is the one to give borders, padding and font-size. Default height is 200px with resize disabled.',
  },

  // ── form-select / shift-select ───────────────────────────────────────────
  {
    part: '<name>-input-select form-input-select',
    scope: 'form',
    appliesTo: 'The trigger input of a select and of both branch pickers',
    note: 'Readonly unless the select is searchable. Style alongside form-input for a consistent field look.',
  },
  {
    part: '<name>-select-icon-container form-input-select-icon-container',
    scope: 'form',
    appliesTo: 'The 36px box at the trailing edge holding the chevron or the clear button',
    note: 'Pointer events are toggled from a class depending on whether a value can be cleared.',
  },
  {
    part: '<name>-arrow-icon select-arrow',
    scope: 'form',
    appliesTo: 'The chevron on a select or branch picker',
    note: 'Rotates when the panel opens, via an internal class. Colour and size are yours.',
  },
  {
    part: '<name>-cross-icon',
    scope: 'form',
    appliesTo: 'The clear button, on a clearable select with a value',
    note: 'It is the add icon rotated 45 degrees. There is no generic alias for it.',
  },

  // ── form-picker-input ────────────────────────────────────────────────────
  {
    part: 'form-input',
    scope: 'form',
    appliesTo: 'The visible readonly display input of form-picker-input',
    note: 'Deliberate: this element carries ONLY the alias, no <name> token. Target one picker through <name>-container instead.',
  },
  {
    part: '<name>-input form-picker-<name>',
    scope: 'form',
    appliesTo: 'The real (transparent, overlaid) date/time input of form-picker-input',
    note: 'This is the element the native picker opens from. It sits at opacity 0 over the display input.',
  },
  {
    part: '<name>-container form-input-container relative',
    scope: 'form',
    appliesTo: 'form-picker-input adds a third token to its container',
    note: 'Harmless duplication of the positioning class; match on <name>-container or form-input-container.',
  },

  // ── form-file ────────────────────────────────────────────────────────────
  { part: 'form-file <name>', scope: 'form', appliesTo: 'The <label> wrapper of an upload field', note: 'Same shape as the textarea wrapper: the type alias sits on the wrapper.' },
  { part: '<name>-file-input form-file-input', scope: 'form', appliesTo: 'The real <input type="file">', note: 'display:none. Styling it is pointless; style the trigger.' },
  {
    part: '<name>-file-trigger form-file-trigger form-input',
    scope: 'form',
    appliesTo: 'The button the user actually clicks',
    note: 'Carries form-input too, so a generic field rule already covers it.',
  },
  {
    part: '<name>-file-text form-file-text',
    scope: 'form',
    appliesTo: 'The filename / prompt text inside the trigger',
    note: 'Truncates. Give it a max-width if your filenames are long.',
  },
  {
    part: '<name>-file-icon form-file-icon',
    scope: 'form',
    appliesTo: 'The spinner or tick at the trailing edge of the trigger',
    note: 'Holds the upload spinner during transfer and a green tick after success.',
  },

  // ── form-phone-number ────────────────────────────────────────────────────
  {
    part: '<name>-container form-input-container form-phone-container',
    scope: 'form',
    appliesTo: 'The phone field box',
    note: 'Gains form-input-container-country as a fourth token when more than one country code is configured.',
  },
  {
    part: '<name>-input-country-selection form-input form-input-country-selection',
    scope: 'form',
    appliesTo: 'The country-code select sitting flush against the number input',
    note: 'Fixed 75px wide inside a phone field. Its inner border radii are flattened by component CSS.',
  },
  { part: 'form-input-container-wrapper', scope: 'form', appliesTo: 'The flex child holding the number input', note: 'No per-field token on this one.' },
  {
    part: '<name>-input-with-country form-input-with-country',
    scope: 'form',
    appliesTo: 'Added to the number input only when a country selector is present',
    note: 'Use it to adjust the leading radius without affecting single-country phone fields.',
  },

  // ── form-checkbox / form-switch ──────────────────────────────────────────
  {
    part: '<name>',
    scope: 'form',
    appliesTo: 'The wrapper div of a checkbox or switch field',
    note: 'A div, not a label, on these two — the label is inside the inner component.',
  },
  {
    part: 'shift-checkbox',
    scope: 'form',
    appliesTo: 'The clickable label of BOTH shift-checkbox and shift-switch',
    note: 'Not a typo: the switch reuses the checkbox part name. The box, tick, track and knob expose no parts at all.',
  },

  // ── Legacy date / time pickers ───────────────────────────────────────────
  {
    part: '<name>-icon form-date-picker-icon',
    scope: 'form',
    appliesTo: 'The calendar glyph on the legacy date field',
    note: 'form-date-picker is the legacy control; new structures usually use the picker-input or the branch pickers.',
  },
  {
    part: '<name>-dropdown form-date-picker-dropdown',
    scope: 'form',
    appliesTo: 'The legacy calendar panel',
    note: 'This panel is NOT portaled — it renders inside the form and is clipped by any overflow around it.',
  },
  {
    part: '<name>-prev form-date-picker-nav / <name>-next form-date-picker-nav',
    scope: 'form',
    appliesTo: 'The month arrows of the legacy calendar',
    note: 'Both arrows share the nav alias; the prev/next tokens separate them.',
  },
  { part: '<name>-title form-date-picker-title', scope: 'form', appliesTo: 'The month label of the legacy calendar', note: '' },
  {
    part: 'form-date-picker-day',
    scope: 'form',
    appliesTo: 'Every day cell of the legacy calendar',
    note: 'Gains form-date-picker-day-selected and form-date-picker-day-today as extra tokens. No per-field token.',
  },
  { part: '<name>-icon form-time-picker-icon', scope: 'form', appliesTo: 'The clock glyph on the legacy time field', note: '' },
  { part: '<name>-dropdown form-time-picker-dropdown', scope: 'form', appliesTo: 'The legacy hour/minute panel', note: 'Also not portaled.' },
  {
    part: 'form-time-picker-option',
    scope: 'form',
    appliesTo: 'Every hour and minute cell of the legacy time panel',
    note: 'Gains form-time-picker-option-selected. No per-field token.',
  },

  // ── Submit and stepper ───────────────────────────────────────────────────
  {
    part: 'submit-button',
    scope: 'form',
    appliesTo: 'The submit button of form-submit, and of form-stepper-submit',
    note: 'One selector covers both. form-stepper-submit adds stepper-submit-button so the two can still be told apart.',
  },
  { part: 'stepper-submit-button', scope: 'form', appliesTo: 'The submit button on a stepped form only', note: '' },
  {
    part: 'form-submit-text',
    scope: 'form',
    appliesTo: 'The label inside a submit button — rendered twice',
    note: 'Two elements share this part: one invisible copy that sets the width, one that slides out on submit.',
  },
  {
    part: 'form-submit-loading-container',
    scope: 'form',
    appliesTo: 'The spinner layer that slides in while submitting',
    note: 'Translated out of view until the button enters its loading state.',
  },
  {
    part: 'form-submit-loading-icon',
    scope: 'form',
    appliesTo: 'The spinner image itself',
    note: 'A white loader by default; replace the colour by restyling the container background instead.',
  },
  {
    part: 'stepper-control stepper-control-button',
    scope: 'form',
    appliesTo: 'The back button on a stepped form',
    note: 'Also carries stepper-control-<ltr|rtl> and stepper-control-step-<n>, so it can be styled per direction and per step.',
  },
  {
    part: 'stepper-control-icon',
    scope: 'form',
    appliesTo: 'The chevron inside the back button',
    note: 'Also carries stepper-control-icon-<ltr|rtl>. The glyph itself flips with direction.',
  },
  { part: 'form-stepper-line', scope: 'form', appliesTo: 'The stepper rail wrapper', note: 'Also carries the stepper node name from the structure, when one was given.' },
  {
    part: 'form-stepper-linecontainer',
    scope: 'form',
    appliesTo: 'The flex row holding the step cells',
    note: 'The missing hyphen is real — the component concatenates the id with "container".',
  },
  {
    part: 'form-stepper-line-line',
    scope: 'form',
    appliesTo: 'The dashed rule running behind the step indicators',
    note: 'Its width is measured and set from JS; its background is an inline style, so override needs !important.',
  },
  {
    part: 'form-stepper-line-step',
    scope: 'form',
    appliesTo: 'One step cell',
    note: 'Also carries -step-<i>, and -step-active / -step-active<i> / -step-done / -step-done<i> as state.',
  },
  {
    part: 'form-stepper-line-step-indicator',
    scope: 'form',
    appliesTo: 'The numbered circle of a step',
    note: 'Same suffix family: -<i>, -active, -active<i>, -done, -done<i>. This is the main stepper colour hook.',
  },
  { part: 'form-stepper-line-step-indicator-text', scope: 'form', appliesTo: 'The number inside the circle', note: 'Same suffix family.' },
  { part: 'form-stepper-line-step-title', scope: 'form', appliesTo: 'The caption under a step circle', note: 'Same suffix family.' },

  // ── Vehicle image viewer ─────────────────────────────────────────────────
  {
    part: 'vehicle-image-wrapper',
    scope: 'form',
    appliesTo: 'The image frame of the vehicle preview element',
    note: 'Rendered by the vehicleImage mapper entry; present only if the structure uses it.',
  },
  {
    part: 'vehicle-image-loading-wrapper',
    scope: 'form',
    appliesTo: 'The loading layer over the vehicle image',
    note: 'Gains vehicle-image-active-loading-wrapper while fetching.',
  },
  { part: 'vehicle-image-loader-icon', scope: 'form', appliesTo: 'The spinner in the vehicle image loading layer', note: '' },
  { part: 'vehicle-image', scope: 'form', appliesTo: 'The <img> itself', note: 'Radius and opacity are set inline by the component, so overriding them needs !important.' },
  {
    part: 'section-title',
    scope: 'form',
    appliesTo: 'The three built-in headings of vehicle-quotation-form',
    note: 'Also the conventional class to put on a heading tag node in any structure — see the structure layer.',
  },

  // ── Portal: form-dialog ──────────────────────────────────────────────────
  {
    part: 'form-dialog-modal dialog-drop-container',
    scope: 'portal',
    appliesTo: 'The fixed full-screen scrim behind the dialog',
    note: 'Gains dialog-drop-container-opened and dialog-drop-container-error. Default is black/50 with a blur.',
  },
  { part: 'dialog-wrapper', scope: 'portal', appliesTo: 'The dialog card', note: 'Gains dialog-wrapper-opened while shown. Background, radius and padding all belong here.' },
  { part: 'dialog-content', scope: 'portal', appliesTo: 'The message area of the dialog', note: 'Holds the error text, or the success block below.' },
  { part: 'form-success-container', scope: 'portal', appliesTo: 'The success block, shown when there is no error', note: 'A column with an icon above the success message.' },
  {
    part: 'form-success-container-icon',
    scope: 'portal',
    appliesTo: 'The success tick illustration',
    note: 'An inline SVG using currentColor, so colour it through the container or here.',
  },
  { part: 'dialog-close-icon-button', scope: 'portal', appliesTo: 'The small dismiss button in the dialog corner', note: '' },
  { part: 'dialog-close-icon-button-icon', scope: 'portal', appliesTo: 'The glyph inside that dismiss button', note: 'The add icon rotated to a cross.' },
  {
    part: 'dialog-close-button',
    scope: 'portal',
    appliesTo: 'The full-width close button at the bottom of the dialog',
    note: 'Gains dialog-close-button-error when the dialog is showing an error, so success and failure can differ.',
  },

  // ── Portal: shift-select-dropdown ────────────────────────────────────────
  {
    part: '<name>-select-container shift-select-container',
    scope: 'portal',
    appliesTo: 'The options panel of a select',
    note: 'Fixed-positioned from JS-written variables. Height caps and colours are yours; top/left/width are not.',
  },
  { part: 'shift-select-container-open', scope: 'portal', appliesTo: 'Added to the panel while it is open', note: 'A state token. The open/closed transition is opacity only.' },
  { part: '<name>-select-option shift-select-option', scope: 'portal', appliesTo: 'One option row', note: 'The main padding and hover hook for a dropdown.' },
  { part: 'shift-select-option-selected', scope: 'portal', appliesTo: 'Added to the currently selected option', note: '' },
  {
    part: '<name>-select-option-label shift-select-option-label',
    scope: 'portal',
    appliesTo: 'The text of a default-rendered option',
    note: 'Absent when the field supplies a custom option renderer.',
  },
  {
    part: '<name>-tick-icon shift-select-option-tick',
    scope: 'portal',
    appliesTo: 'The tick at the end of an option row',
    note: 'Gains shift-select-option-tick-selected. Hidden by opacity, so it always occupies its space.',
  },
  {
    part: '<name>-select-empty-container shift-select-empty-container',
    scope: 'portal',
    appliesTo: 'The panel body when there are no options',
    note: 'Gains shift-select-empty-container-error when the option fetch failed.',
  },
  { part: '<name>-select-spinner shift-select-spinner', scope: 'portal', appliesTo: 'The spinner shown while options are loading', note: '' },
  {
    part: 'custom-shift-select-option custom-shift-select-country-option',
    scope: 'portal',
    appliesTo: 'A country row in a phone field dropdown',
    note: 'Gains custom-shift-select-option-selected and shift-select-option-selected together.',
  },
  { part: 'shift-select-country-code-label', scope: 'portal', appliesTo: 'The country code text in that row', note: '' },
  { part: 'shift-select-country-number', scope: 'portal', appliesTo: 'The dialling number in that row', note: '' },

  // ── Portal: branch-slot-dropdown ─────────────────────────────────────────
  {
    part: '<name>-slot-container branch-slot-container',
    scope: 'portal',
    appliesTo: 'The day-strip booking panel',
    note: 'Gains branch-slot-container-open. Has a 288px floor and a JS-measured max-height.',
  },
  {
    part: '<name>-slot-backdrop branch-slot-backdrop',
    scope: 'portal',
    appliesTo: 'The sheet scrim, visible only below 600px',
    note: 'Gains branch-slot-backdrop-open. Hidden entirely on wider viewports.',
  },
  {
    part: '<name>-slot-day branch-slot-day',
    scope: 'portal',
    appliesTo: 'One day chip in the strip',
    note: 'Gains branch-slot-day-selected and branch-slot-day-disabled. Closed days are greyed, not removed.',
  },
  { part: '<name>-slot-time branch-slot-time', scope: 'portal', appliesTo: 'One time chip', note: 'Gains branch-slot-time-selected.' },
  {
    part: '<name>-slot-empty-container branch-slot-empty-container',
    scope: 'portal',
    appliesTo: 'The idle, empty and error states of the panel',
    note: 'Gains branch-slot-empty-container-error on a failed fetch.',
  },
  { part: '<name>-slot-retry branch-slot-retry', scope: 'portal', appliesTo: 'The retry button in the error state', note: '' },

  // ── Portal: branch-date-dropdown ─────────────────────────────────────────
  {
    part: '<name>-date-container branch-date-container',
    scope: 'portal',
    appliesTo: 'The month-calendar booking panel',
    note: 'Gains branch-date-container-open. 300px floor, JS-measured max-height, flips upward via a bottom variable.',
  },
  { part: '<name>-date-backdrop branch-date-backdrop', scope: 'portal', appliesTo: 'The sheet scrim, visible only below 600px', note: 'Gains branch-date-backdrop-open.' },
  {
    part: '<name>-date-cell branch-date-cell',
    scope: 'portal',
    appliesTo: 'One day cell in the calendar grid',
    note: 'Gains -open, -blocked, -off, -today and -selected. Blocked means returned but not bookable; off means never offered.',
  },
  {
    part: '<name>-date-time branch-date-time',
    scope: 'portal',
    appliesTo: 'One time chip on the second pane',
    note: 'Gains branch-date-time-selected. Entry is staggered by an inline animation-delay.',
  },
  { part: '<name>-date-prev branch-date-nav / <name>-date-next branch-date-nav', scope: 'portal', appliesTo: 'The month arrows', note: '' },
  { part: '<name>-date-back branch-date-back', scope: 'portal', appliesTo: 'The button returning from the time pane to the calendar', note: '' },
  {
    part: '<name>-date-empty-container branch-date-empty-container',
    scope: 'portal',
    appliesTo: 'The idle, empty and error states of the panel',
    note: 'Gains branch-date-empty-container-error.',
  },
  { part: '<name>-date-retry branch-date-retry', scope: 'portal', appliesTo: 'The retry button in the error state', note: '' },
];

export const limits = [
  {
    id: 'no-custom-properties',
    title: 'There are no themeable CSS custom properties',
    detail:
      'Not one. Every var(--...) in the ticket-form components is a positioning channel written by JavaScript on the portaled panels — --shift-select-top/left/width, --branch-slot-top/left/width/max-height, --branch-date-top/bottom/left/width/max-height — and nothing else declares or reads a custom property. There is no --primary, no --radius, no font token. If a snippet you have been given sets one, it is doing nothing.',
    example: `/* Does nothing. No component reads any of these. */
general-form {
  --primary: #b91c1c;
  --form-radius: 12px;
  --form-font: 'Nunito';
}

/* The equivalent that works. */
.docs-form::part(submit-button) { background: #b91c1c; }
.docs-form::part(form-input)    { border-radius: 12px; }
.docs-form::part(shift-form)    { font-family: 'Nunito', sans-serif !important; }`,
    workaround:
      'Declare your own custom properties on the page and use them in your ::part() rules — the variables live in your stylesheet, the components never see them. That gives you a token layer without the components needing one.',
  },
  {
    id: 'part-is-a-leaf',
    title: 'You cannot select through a part',
    detail:
      '::part() is a terminal pseudo-element: nothing may follow it except a pseudo-class such as :hover, :focus or :disabled. An element with no part= of its own cannot be reached at all, from anywhere. Several elements in the tree are in exactly that position — the skeleton placeholders in both branch panels, the weekday row of the calendar, the day-of-week and month spans inside a day chip, the animated step panes, the <form> element itself.',
    example: `/* Invalid CSS. The whole rule is dropped, silently. */
.docs-form::part(shift-form) .form-input-label { color: #334155; }
.docs-form::part(form-input-container) input   { border: none; }

/* Valid: a pseudo-class may follow a part. */
.docs-form::part(form-input):focus    { border-color: #475569; }
.docs-form::part(form-input):disabled { opacity: 0.5; }
.docs-form::part(submit-button):hover { background: #1e293b; }`,
    workaround:
      'Find the nearest element that does have a part and style that instead. If none exists, the only route is a source change adding a part= — this is the reason to file an issue rather than to reach for a workaround.',
  },
  {
    id: 'portal-font',
    title: 'The portaled panels’ font-family cannot be overridden',
    detail:
      'form-dialog, shift-select-dropdown, branch-slot-dropdown and branch-date-dropdown each declare their typeface twice inside their own shadow tree: once on :host with !important, and again as "*, :host * { font-family: inherit !important }". In the cascade, an important declaration from an inner shadow tree beats an important declaration from the outer page — so neither a ::part() rule nor a rule on the tag itself can change it, with or without !important. Every other property on those panels is themeable; only the typeface is fixed.',
    example: `/* All three have no effect. */
.docs-form::part(shift-select-container) { font-family: 'Brand Sans' !important; }
.docs-form::part(dialog-wrapper)         { font-family: 'Brand Sans' !important; }
shift-select-dropdown                    { font-family: 'Brand Sans' !important; }

/* These do work — size, weight, colour, spacing, background, radius. */
.docs-form::part(shift-select-container) {
  font-size: 15px;
  background: #1e293b;
  border-radius: 10px;
}

.docs-form::part(shift-select-option) {
  font-weight: 600;
  letter-spacing: 0.01em;
  color: #e2e8f0;
}`,
    workaround:
      'None from CSS. If your brand face matters more than the built-in Arabic fallback, the change is to those four stylesheets in the package. In practice the mismatch is only visible when a dropdown is open, and the shipped face already covers Latin, Arabic and Kurdish.',
  },
  {
    id: 'host-reset',
    title: 'The form does not inherit your page font',
    detail:
      'Both stylesheets that land in the form’s shadow root — forms/defaults/style.css and form-elements/form-inputs.css — open with :host { all: initial !important }. That reset applies to the form element itself, and an important declaration from the inner tree beats an important one from the outer page, so the host’s own typography cannot be set from outside at all. Page-level font rules, and font rules on a wrapper around the form, therefore do not reach it.',
    example: `/* None of these reach the form. */
body                { font-family: 'Brand Sans', sans-serif; }
.form-wrapper       { font-family: 'Brand Sans', sans-serif; }
general-form        { font-family: 'Brand Sans', sans-serif !important; }

/* This does. Everything inside the form inherits from this div. */
general-form::part(shift-form) {
  font-family: 'Brand Sans', 'Noto Kufi Arabic', sans-serif !important;
}`,
    workaround:
      'Always declare the font on ::part(shift-form), and keep the !important — it is what the four shipped presets do, and it keeps the rule immune to anything the components later declare inside the tree. Remember the portaled panels are a separate case and stay on their own face.',
  },
  {
    id: 'nested-shadow',
    title: 'The VIN scanner is a nested shadow root and is closed to you',
    detail:
      'Everything in the in-page form is shadow:false and therefore lives in the form’s single shadow root — with one exception. vin-extractor, the camera overlay opened from a VIN field, is shadow:true and is rendered inside the form rather than portaled out. Its two internal parts are not re-exported: Stencil emits no exportparts attribute anywhere in this package, and parts do not cross a second boundary on their own.',
    example: `/* Reaches the field and its scan button — both are in the form's tree. */
.docs-form::part(vin-input)     { text-transform: uppercase; }
.docs-form::part(vin-validator) { color: #0f172a; }

/* Reaches nothing: these parts are inside vin-extractor's own shadow root. */
.docs-form::part(vin-extractor-capture-button) { background: #b91c1c; }
vin-extractor::part(vin-extractor-capture-button) { background: #b91c1c; }`,
    workaround:
      'The scanner overlay is a full-screen camera surface with its own dark treatment; in practice it does not need to match the form. If it must, that is an exportparts change in the package.',
  },
  {
    id: 'unnamed-labels',
    title: 'Some labels have no per-field part',
    detail:
      'The label component emits <name>-label only when it is given the field name. Five components call it without: form-input, form-picker-input, form-vin-input, form-date-picker and form-time-picker. For a field rendered by any of those, ::part(<name>-label) and ::part(<name>-label-required-star) match nothing — only the generic form-input-label and form-input-label-required-star exist. The six that do pass the name are form-select, form-text-area, form-file, form-phone-number, branch-slot-picker and branch-date-picker.',
    example: `/* "email" is a form-input field: */
.docs-form::part(form-input-label) { font-weight: 600; }  /* applies */
.docs-form::part(email-label)      { font-weight: 800; }  /* matches nothing */

/* "city" is a form-select field: */
.docs-form::part(city-label)       { font-weight: 800; }  /* applies */

/* Note the error message is unaffected — every field passes its name there. */
.docs-form::part(email-error-message) { color: #dc2626; } /* applies */`,
    workaround:
      'Wrap the field in a named tag node in the structure and style that wrapper, or accept the generic label rule for those field types. There is no CSS-side fix, because a part cannot be used as an ancestor.',
  },
  {
    id: 'checkbox-switch-internals',
    title: 'Checkboxes and switches expose one part between them',
    detail:
      'shift-checkbox and shift-switch both put part="shift-checkbox" on their clickable label and nothing else. The checkbox square, the tick, the switch track and the switch knob carry classes only. A single selector therefore hits both control types, and neither can be recoloured through ::part() — the default blue is baked into component CSS.',
    example: `/* All you can reach. Both controls answer to it. */
.docs-form::part(shift-checkbox) {
  gap: 10px;
  font-size: 15px;
  color: #334155;
}

/* No equivalent exists for the box, the tick, the track or the knob. */

/* The field wrapper and its error message are still per-field: */
.docs-form::part(consent)               { margin-top: 8px; }
.docs-form::part(consent-error-message) { color: #b91c1c; }`,
    workaround:
      'Keep these two controls on their default palette, or replace them in the structure with a select of yes/no options where the visual matters. Adding parts to their internals is a small source change if it becomes worth it.',
  },
  {
    id: 'picker-display-input',
    title: 'One picker’s display box cannot be named',
    detail:
      'form-picker-input renders two inputs: a visible readonly one showing the formatted value and a transparent real one over it that opens the native picker. The visible one carries only part="form-input" — no <name> token — so a rule for it necessarily hits every text field in the form.',
    example: `/* Hits every field, not just the date picker. */
.docs-form::part(form-input) { background: #f8fafc; }

/* Scope to the one picker through its container instead. */
.docs-form::part(collectionDate-container) {
  background: #f8fafc;
  border-radius: 8px;
}

/* The transparent overlay is separately named, if you need it. */
.docs-form::part(collectionDate-input) { cursor: pointer; }`,
    workaround: 'Style the field’s container, which does carry the name, and let the display input inherit background and colour from it.',
  },
  {
    id: 'positioning-variables',
    title: 'The positioning variables belong to JavaScript',
    detail:
      'The three portaled panels are positioned by setting --shift-select-*, --branch-slot-* and --branch-date-* as inline styles on the panel element, recomputed on open, on resize and on every scroll. An inline style already beats a stylesheet rule, and even a matching !important would be recomputed away on the next reposition.',
    example: `/* Both pointless. Overwritten within a frame. */
.docs-form::part(shift-select-container)  { --shift-select-top: 40px; }
.docs-form::part(branch-date-container)   { --branch-date-width: 420px !important; }

/* What you can control: the floors and caps the panel respects. */
.docs-form::part(shift-select-container)  { max-height: 320px; }
.docs-form::part(branch-slot-container)   { min-width: 340px; }`,
    workaround:
      'Set min-width and max-height, which the panels honour, and leave placement to the component. It measures the trigger and the viewport, and flips the panel upward when there is no room below.',
  },
  {
    id: 'unnamed-structural-elements',
    title: 'The form element and the step panes have no parts',
    detail:
      'The <form> tag itself, and the sliding panes that hold each step of a multi-step form, are rendered with classes only. So the submit-time form behaviour cannot be styled directly, and the step transition — a translate plus opacity over 700ms — cannot be retimed or disabled from a theme.',
    example: `/* No such part. */
.docs-form::part(form) { padding: 24px; }

/* Put the padding on the shell instead. */
.docs-form::part(shift-form) { padding: 24px; }

/* And style the step content through the wrapper the structure named. */
.docs-form::part(step-one),
.docs-form::part(step-two) {
  display: flex;
  flex-direction: column;
  gap: 20px;
}`,
    workaround:
      'Everything a theme normally wants from the form element — padding, background, max width — belongs on shift-form. For the step content, give each step a wrapper tag node with an id in the structure.',
  },
  {
    id: 'authored-names-are-not-a-contract',
    title: 'Structure-authored part names are not a contract',
    detail:
      'Names coming from a structure node’s id or class exist only because that structure says so; rename the node and every rule using it stops matching, with no error. They also pass through the same class-merging helper the components use for real classes, which de-duplicates tokens that look like conflicting utility classes — so an id or class shaped like a utility can be dropped from the part list before it reaches the DOM.',
    example: `/* Fragile: depends on a structure you may not own. */
.docs-form::part(inputs_wrapper) { display: grid; }

/* Stable: field names change only when the submitted payload changes. */
.docs-form::part(message)       { grid-column: 1 / -1; }
.docs-form::part(submit-button) { grid-column: 1 / -1; }

/* Avoid ids and classes shaped like utility classes on tag nodes. */
{ "tag": "div", "id": "p-4" }        /* may not survive as a part name */
{ "tag": "div", "id": "inputs_row" } /* safe */`,
    workaround:
      'Anchor layout to field names where you can, keep authored ids descriptive rather than utility-shaped, and version a theme alongside the structure it was written against.',
  },
];

export const partNameRules = {
  summary:
    'Part names come from three places, and knowing which one you are looking at tells you how stable it is. (1) Fixed aliases the components always emit — shift-form, form-input, form-input-label, submit-button, dialog-wrapper. These never change. (2) Derived names, built by prefixing the field’s name from the structure: <name>, <name>-container, <name>-input, <name>-error-message. These change only if the field is renamed, which also changes the submitted payload. (3) Authored names, taken from the id and class you write on a tag node in the structure, alongside a generic element-<tag> and the bare tag. These are yours, and they are only as stable as the structure file. Most elements carry two tokens at once — one derived and one fixed — so you can style broadly with the alias and override narrowly with the name.',
  example: `// A slice of a structure, and everything it names.
{
  "tag": "div",
  "id": "contact_block",              // → ::part(contact_block)
  "class": "stack",                   // → ::part(stack)
                                      // → ::part(element-div), ::part(div)
  "children": [
    { "name": "email" },              // a field, rendered by the mapper
    { "name": "message" }
  ]
}

/* The layout node — an authored name. */
.docs-form::part(contact_block) {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px;
}

/* The field — a derived name, plus the fixed aliases on the same elements. */
.docs-form::part(email)               { grid-column: 1; }
.docs-form::part(email-container)     { position: relative; }
.docs-form::part(email-input)         { border-radius: 8px; }
.docs-form::part(email-error-message) { color: #b91c1c; }

.docs-form::part(message)             { grid-column: 1 / -1; }
.docs-form::part(message-textarea)    { min-height: 140px; }

/* The fixed aliases, reaching every field of a kind at once. */
.docs-form::part(form-input)          { border: 1px solid #cbd5e1; }
.docs-form::part(form-input-textarea) { border: 1px solid #cbd5e1; }
.docs-form::part(form-input-label)    { font-weight: 600; }`,
  note: 'Two gaps to keep in mind while reading a name. First, a token is a token: part="a b c" means all three of ::part(a), ::part(b) and ::part(c) match that one element, and a state suffix such as -selected, -active, -done or -open is just another token on the same element, not a separate element. Second, ::part() is a leaf — nothing may follow it but a pseudo-class — so if the element you want carries no part of its own, no name gets you there.',
};
