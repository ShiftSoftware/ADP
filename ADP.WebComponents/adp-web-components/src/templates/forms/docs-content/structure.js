/**
 * Structure-JSON reference for the ticket-form components.
 *
 * Plain data. No DOM, no framework, no imports — a documentation page renders
 * this; nothing here runs inside a form.
 *
 * Every claim below was read out of the source, not remembered:
 *   src/features/form-hook/interface.ts        the structure type
 *   src/features/form-hook/render-structure.tsx  the whole authoring contract
 *   src/features/form-hook/functions.ts        load, reCAPTCHA, submit, UTM
 *   src/features/form-hook/form-hook.ts        validation, steps, required
 *   src/components/forms/defaults/mappers.tsx  the field-name table
 *   src/components/forms/defaults/validation.ts  the base yup schema
 *
 * The five components that share this format are `general-form`,
 * `general-inquiry-form`, `service-booking-form`, `ssc-lookup-form` and
 * `test-drive-form`. `vehicle-quotation-form` reads a structure that looks the
 * same but is wired to its own mapper table and its own submit function, so
 * nothing on this page is guaranteed to hold there.
 *
 * Examples are template literals holding strictly valid JSON, so a page can
 * print them verbatim or `JSON.parse` them.
 */

// ---------------------------------------------------------------------------
// Top level
// ---------------------------------------------------------------------------

export const topLevel = [
  {
    key: 'data',
    type: 'object',
    required: false,
    summary:
      'Everything about the form that is not a rendered node: where to POST, which headers to add, the reCAPTCHA key, the payload rewrites, the localisation bundle. Nothing under `data` renders. It is read straight off `structure.data` by the component and by `onFormSubmit` — `renderStructure` destructures `data` away and never looks at it, so a `data` key on a *child* node is silently discarded.',
    example: `{
  "data": {
    "requestUrl": "https://example.invalid/api/tickets",
    "requestMethod": "POST",
    "theme": "docs-preset",
    "localization": {
      "en": { "submit": "Send", "Form submitted successfully.": "Thanks - we will be in touch." }
    }
  },
  "tag": "div",
  "children": [{ "name": "name" }, { "name": "submit" }]
}`,
    absent:
      'The form still renders. It fails at submit: `requestEndpoint` is empty, so `onFormSubmit` throws `Request endpoint is not configured`, which surfaces in the error dialog. Until v-current this was worse — `Object.hasOwn(undefined, …)` threw during load and the form sat on its spinner forever; `resolveIsMobileForm` now defaults `data` to `{}`.',
  },

  {
    key: 'requiredContext',
    type: 'Record<string, boolean>',
    required: false,
    summary:
      'The only structure-level required switch. `FormHook.getRequiredContext()` turns every entry into a yup context variable by appending `Required` — `{"email": true}` becomes `$emailRequired` — and the base schema branches on exactly those names via `.when("$<field>Required", …)`. It is read once, in the `FormHook` constructor.',
    example: `{
  "requiredContext": { "name": true, "email": true, "message": true, "companyBranchId": true }
}`,
    absent:
      'Every base-schema field is optional. `.when` falls to its `otherwise` branch, which is `schema.optional()` for all of them. A field name that is not in the base schema (phone, file, bookingSlot, bookingDate, or any name you invented with `type`) ignores this key completely — see `requiredMechanics.exceptions`.',
  },

  {
    key: 'steps',
    type: 'Array<Record<"en" | "ar" | "ku" | "ru", Step>>',
    required: false,
    summary:
      'Presence of this array is what turns the form into a wizard. `form-structure` then renders the tree once as a chrome pass (`currentStep === -1`) plus once per entry (`currentStep === 1 … n`), and `FormHook.submitForm` advances a step instead of POSTing until `steps.length === currentStep`. Array order is step order; the array is indexed `steps[currentStep - 1]`.',
    example: `{
  "steps": [
    {
      "en": { "title": "Details", "stepCell": "1", "stepTitle": "Your details", "submitButton": "Continue" },
      "ar": { "title": "البيانات", "stepCell": "١", "stepTitle": "بياناتك", "submitButton": "متابعة" }
    },
    {
      "en": { "title": "Vehicle", "stepCell": "2", "stepTitle": "Your vehicle", "submitButton": "Book", "back": "Back" },
      "ar": { "title": "المركبة", "stepCell": "٢", "stepTitle": "مركبتك", "submitButton": "احجز", "back": "رجوع" }
    }
  ]
}`,
    absent:
      'Single-pass form. `form-structure` calls `renderStructure(…, -2)` once, and `submitForm` POSTs immediately. In that single pass a node carrying a truthy `step` is dropped, because `-2` never equals it — so leaving `step` on a node after deleting `steps` makes the node vanish.',
  },

  {
    key: 'tag',
    type: 'string',
    required: true,
    summary:
      'The root is a node like any other, so it needs `tag` (an HTML tag name) or `name`. In practice it is always `tag`, because the root is the wrapper everything else hangs inside. A `name`-only root gives you a single field and no wrapper.',
    example: `{
  "tag": "div",
  "id": "container",
  "children": []
}`,
    absent:
      'With neither `tag` nor `name` (and no `type`), `renderStructure` returns `false` and the entire form renders as nothing — no error, no console warning. See `nodeProps` → `tag`.',
  },

  {
    key: 'id',
    type: 'string',
    required: false,
    summary:
      'On a tag node this becomes the literal DOM `id` and is also folded into the element `part`, which is how a host stylesheet reaches inside the shadow root: `::part(container)`.',
    example: `{
  "tag": "div",
  "id": "container",
  "children": [{ "tag": "div", "id": "inputs_wrapper", "children": [{ "name": "name" }] }]
}`,
    absent: 'No id attribute; `part` is built from the remaining pieces (`class`, `element-<tag>`, `<tag>`).',
  },

  {
    key: 'class',
    type: 'string',
    required: false,
    summary: 'On a tag node this becomes the DOM `class` and is also folded into `part`. On a field node it arrives as `wrapperClass` instead — see `nodeProps` → `class`.',
    example: `{
  "tag": "div",
  "id": "container",
  "class": "form-grid",
  "children": [{ "tag": "h2", "class": "section-title", "children": { "en": "Contact us", "ar": "اتصل بنا" } }]
}`,
    absent: 'No class attribute.',
  },

  {
    key: 'children',
    type: 'Array<node | string> | Record<LanguageKeys, string>',
    required: false,
    summary:
      'On a tag node, either an array of child nodes (each recursed through `renderStructure`) or a plain object keyed by language, which renders as localised text — that is how you get a heading. A string entry in the array is shorthand for `{ "name": "<string>" }`. On a field node `children` is destructured away and ignored.',
    example: `{
  "tag": "div",
  "children": [
    { "tag": "h2", "children": { "en": "Contact us", "ar": "اتصل بنا", "ku": "پەیوەندیمان پێوە بکە", "ru": "Свяжитесь с нами" } },
    { "name": "name" },
    "submit"
  ]
}`,
    absent: 'An empty element. A root with no `children` renders an empty form with a hidden submit button and nothing else.',
  },
];

// ---------------------------------------------------------------------------
// structure.data.*
// ---------------------------------------------------------------------------

export const dataKeys = [
  {
    key: 'requestUrl',
    type: 'string',
    summary: 'Where a browser form POSTs. Used whenever the form is not in mobile mode.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets" } }`,
    absent: 'Submit throws `Request endpoint is not configured` and the error dialog opens. Everything before that — validation, step advance, file uploads — still runs.',
    seeAlso: ['requestMethod', 'isMobileForm', 'requestAppUrl', 'requestAppCheckUrl'],
  },

  {
    key: 'requestMethod',
    type: 'string',
    summary:
      'HTTP verb. Two separate checks read it and they do not agree: UTM values move into the headers when the verb is `get` OR `head`, but the body is omitted only when the verb is `get`. So `"requestMethod": "HEAD"` still builds a body, and `fetch` then rejects the call outright — a HEAD request cannot carry one. `GET` is the only bodyless verb this code actually supports.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "requestMethod": "POST" } }`,
    absent: 'Defaults to `POST`, in both the header branch and the `fetch` call.',
    seeAlso: ['requestUrl', 'utm'],
  },

  {
    key: 'requestAppUrl',
    type: 'string',
    summary: 'Mobile-mode endpoint used when `getMobileToken()` returns a value starting (case-insensitively) with `bearer`. The token is sent as `Authorization`.',
    example: `{ "data": { "isMobileForm": true, "requestAppUrl": "https://example.invalid/api/app/tickets" } }`,
    absent: 'In mobile mode with a bearer token, the endpoint is empty and submit throws `Request endpoint is not configured`.',
    seeAlso: ['isMobileForm', 'requestAppCheckUrl'],
  },

  {
    key: 'requestAppCheckUrl',
    type: 'string',
    summary: 'The other mobile-mode endpoint — used when the token does not start with `bearer`. The token is then sent as a `verification-token` header instead.',
    example: `{ "data": { "isMobileForm": true, "requestAppCheckUrl": "https://example.invalid/api/appcheck/tickets" } }`,
    absent: 'Same failure as above, for the non-bearer branch.',
    seeAlso: ['isMobileForm', 'requestAppUrl'],
  },

  {
    key: 'isMobileForm',
    type: 'boolean',
    summary:
      'Switches the whole submit path: no reCAPTCHA script is injected at load, no reCAPTCHA token at submit, and the endpoint comes from `requestAppUrl` / `requestAppCheckUrl`. The check is `Object.hasOwn(data, "isMobileForm")`, not truthiness, so `"isMobileForm": false` in the structure deliberately overrides a `is-mobile-form` attribute set to true on the element.',
    example: `{ "data": { "isMobileForm": false, "requestUrl": "https://example.invalid/api/tickets" } }`,
    absent: 'Falls back to the component prop `isMobileForm`, which defaults to `false`.',
    seeAlso: ['recaptchaKey', 'requestAppUrl', 'requestAppCheckUrl'],
  },

  {
    key: 'recaptchaKey',
    type: 'string',
    summary:
      'Site key for reCAPTCHA v3. When present and not in mobile mode, `formDidLoadHandler` appends `https://www.google.com/recaptcha/api.js?render=<key>&hl=<language>` to `document.head`, and submit calls `grecaptcha.execute(key, { action: "submit" })` and sends the result as a `Recaptcha-Token` header. `form-file` does the same before asking for its signed-upload URLs.',
    example: `{
  "data": {
    "requestUrl": "https://example.invalid/api/tickets",
    "requestMethod": "POST"
  }
}`,
    absent:
      'THE DOCUMENTED DEFAULT, and what every example on this page does. No script is injected, no token header is sent, and the form submits. Do not paste a placeholder key to "show the shape": the page looks healthy right up to Submit, where `grecaptcha.execute` rejects with `Invalid site key` and the failure reads like a network error.',
    seeAlso: ['isMobileForm', 'wireFormat'],
  },

  {
    key: 'brandId',
    type: 'string',
    summary: 'Sent verbatim as the `Brand` request header on every submit.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "brandId": "demo-brand" } }`,
    absent:
      'The header is still set — to the string `"undefined"`, because the header object is built unconditionally and `Headers` stringifies the value. If your backend rejects an unknown brand, an omitted `brandId` fails as a bad brand rather than a missing one.',
    seeAlso: ['wireFormat'],
  },

  {
    key: 'extraHeader',
    type: 'object',
    summary: 'Merged into the request headers last, after the component `extraHeader` prop. Structure wins over prop.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "extraHeader": { "X-Source": "docs-site" } } }`,
    absent: 'Only the built-in headers plus whatever the `extraHeader` prop carries.',
    seeAlso: ['extraPayload', 'wireFormat'],
  },

  {
    key: 'extraPayload',
    type: 'object',
    summary:
      'Merged into the JSON body — and here the precedence is the other way round: `data.extraPayload` is applied first, then the component `extraPayload` prop, so the PROP wins. Headers and payload disagree about which side is authoritative; that asymmetry is in the source, not a typo here.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "extraPayload": { "source": "web", "campaignId": 42 } } }`,
    absent: 'The body is the validated form values plus UTM values plus `additionalData` if `truncatedFields` is set.',
    seeAlso: ['extraHeader', 'truncatedFields', 'wireFormat'],
  },

  {
    key: 'truncatedFields',
    type: 'Record<string, string | string[]>',
    summary:
      'A tiny rewrite language applied to the payload just before it is sent, in object-key order. Four behaviours, chosen by the shape of each entry:\n' +
      '1. value is an ARRAY — join. Each item is looked up in the payload; if there is no such key the item is used as a literal. The result is assigned to the entry key. Source fields are NOT removed.\n' +
      '2. key starts with `parse date: ` — `payload[rest] = parse(payload[rest], <value>, new Date())` using the date-fns format in the value. Produces a Date object.\n' +
      '3. key starts with `format date: ` — `payload[rest] = formatISO(payload[rest])`. The value is ignored; any non-empty string works, so write something self-documenting.\n' +
      '4. anything else — RENAME AND MOVE: `additionalData[<value>] = payload[<key>]`, then `delete payload[<key>]`.\n' +
      'Whenever this key is present and non-empty, `payload.additionalData` is set, even if it ends up `{}`.',
    example: `{
  "data": {
    "requestUrl": "https://example.invalid/api/tickets",
    "truncatedFields": {
      "preferredDateTime": ["date", " ", "time"],
      "parse date: preferredDateTime": "yyyy-MM-dd HH:mm",
      "format date: preferredDateTime": "iso",
      "message": "customerNotes"
    }
  }
}`,
    absent: 'The payload is sent as validated. No `additionalData` key is added at all.',
    seeAlso: ['extraPayload', 'wireFormat'],
  },

  {
    key: 'disableUTMLog',
    type: 'boolean',
    summary: 'Truthy value makes `getMarketingValues` return `{}`, so no marketing parameter is collected or sent.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "disableUTMLog": true } }`,
    absent: 'Marketing parameters present in the page URL are collected and sent.',
    seeAlso: ['utm'],
  },

  {
    key: 'localization',
    type: 'Record<LanguageKeys, object>',
    summary:
      'The form-wide locale bundle. On every language change the block for the new language is picked (falling back to `.en`, then `{}`) and becomes the component `locale`, with its optional `sharedFormLocales` sub-object layered over the shipped shared locales. Two well-known keys: `submit` (the default submit-button label, via `submitTextKey`) and `"Form submitted successfully."` (the success dialog text). Per-field text does NOT live here — it lives in each node\'s own `localization`.',
    example: `{
  "data": {
    "requestUrl": "https://example.invalid/api/tickets",
    "localization": {
      "en": {
        "submit": "Send",
        "Form submitted successfully.": "Thanks - we will be in touch.",
        "sharedFormLocales": { "close": "Dismiss" }
      },
      "ar": { "submit": "إرسال", "Form submitted successfully.": "تم الإرسال بنجاح." }
    }
  }
}`,
    absent: 'The shipped shared locales are used throughout; the submit button falls back to `sharedFormLocales.submit`, then to the literal `Submit`.',
    seeAlso: ['nodeProps → localization'],
  },

  {
    key: 'theme',
    type: 'string',
    summary:
      'Appended to the outer wrapper `part`, alongside the component `theme` prop: `part="shift-form <data.theme> <theme prop>"`. It is a styling hook, nothing more — no stylesheet is loaded for you.',
    example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "theme": "docs-preset" } }`,
    absent: 'The wrapper part is just `shift-form` plus the `theme` prop if one was set.',
    seeAlso: [],
  },

  {
    key: 'currentVehiclesApi',
    type: 'string',
    summary:
      'Endpoint for the `currentVehicleBrand` field only. It is the one field mapper that reaches into `structure.data` instead of taking a node prop — and it reads `structure?.data.currentVehiclesApi`, with no `?.` after `data`, so a form that uses this field and omits `data` entirely throws inside the fetcher.',
    example: `{
  "data": {
    "requestUrl": "https://example.invalid/api/tickets",
    "currentVehiclesApi": "https://example.invalid/api/vehicle-brands"
  },
  "tag": "div",
  "children": [{ "name": "currentVehicleBrand" }, { "name": "currentVehicleModel" }]
}`,
    absent: '`fetch(undefined)` — the select shows a fetch error and stays empty.',
    seeAlso: ['mappers → currentVehicleBrand', 'mappers → conditionalCurrentVehicleBrand'],
  },
];

// ---------------------------------------------------------------------------
// Node properties
// ---------------------------------------------------------------------------

export const nodeProps = [
  {
    prop: 'tag',
    appliesTo: 'container nodes',
    summary:
      'Renders a raw element of that name and recurses into `children`. The element gets `part={cn(id, class, "element-<tag>", "<tag>")}`. A `tag` node is never a form field: `name` is destructured but unused on this branch, and no mapper is consulted.',
    example: `{ "tag": "section", "id": "contact", "class": "panel", "children": [{ "name": "name" }] }`,
    absent: 'The node is treated as a field node and looked up in the mapper table by `type || name`.',
    warning:
      'TRAP 1. A node with neither `tag` nor `name` — and no `type` — renders NOTHING and says nothing. `renderStructure` falls off the end and returns `false`. The same silence covers a `name`/`type` that is not a key in the mapper table: a typo like `"emial"` produces an empty gap, no console warning, no error panel. `part` is assigned after the prop spread, so a `part` you write on the node is overwritten.',
  },

  {
    prop: 'name',
    appliesTo: 'field nodes',
    summary:
      'Two jobs at once. It is the mapper key (unless `type` overrides it) and it is the field name — the `name` attribute on the input, the key in the submitted payload, the key in the yup schema, and the key `requiredContext` and `truncatedFields` refer to.',
    example: `{ "name": "email", "localization": { "en": { "label": "Email", "placeholder": "you@example.invalid" } } }`,
    absent: 'Only legal on a `tag` node. On a field node without `type`, nothing renders.',
    warning:
      'A string in a `children` array is shorthand for this: `"submit"` and `{ "name": "submit" }` reach the same mapper. The shorthand cannot carry `id`, `class`, `step` or `localization`.',
  },

  {
    prop: 'type',
    appliesTo: 'field nodes',
    summary:
      'Overrides the mapper lookup while leaving `name` as the field name: `elementMapper[type || name]`. This is the ONLY way to give a form a field name the base schema has never heard of — reuse a shipped widget under a new name.',
    example: `{
  "name": "secondaryEmail",
  "type": "email",
  "localization": { "en": { "label": "Second email", "placeholder": "you@example.invalid" } }
}`,
    absent: 'The mapper key is `name`.',
    warning:
      'TRAP 7 (the useful half). A new field NAME is fine — the value is collected by `getValues` and reaches the wire. A new field TYPE is impossible: `getFormMappers()` is called once at module scope in each form component, stored in a `const`, and passed down as a prop the host cannot set. There is no registry, no `extraMappers` prop, no custom-element escape hatch. A name invented this way is also unvalidated and permanently starred — see `requiredMechanics.exceptions`. Note also that `type` is destructured out by `renderStructure` and never forwarded, so you cannot use it to set an `<input type>`; use `inputProps` for that.',
  },

  {
    prop: 'id',
    appliesTo: 'both',
    summary:
      'On a tag node: the DOM id, plus a `part` token. On a field node: forwarded as `wrapperId`, which each input puts on its `<label>` wrapper — and, because the raw `id` is *also* still in the forwarded props, on the component host element as well.',
    example: `{ "name": "message", "id": "message_field", "class": "wide" }`,
    absent: 'No id anywhere; `part` is built from what remains.',
    warning: 'Ids are not de-duplicated. A node without a `step` in a stepped form is rendered once per pass, so its id is emitted n+1 times.',
  },

  {
    prop: 'class',
    appliesTo: 'both',
    summary:
      'On a tag node: the DOM class, plus a `part` token. On a field node: forwarded as `wrapperClass` onto the `<label>` wrapper — and, like `id`, also left in the raw props, so it lands on the host element too.',
    example: `{ "tag": "div", "id": "inputs_wrapper", "class": "grid two-up", "children": [{ "name": "name" }, { "name": "lastName" }] }`,
    absent: 'No class.',
    warning: null,
  },

  {
    prop: 'step',
    appliesTo: 'both',
    summary:
      'Which render pass the node belongs to, 1-based. `-1` means the chrome pass — rendered once, outside the sliding step panels, which is where the stepper rail goes. The filter runs before anything else in `renderStructure`, so a filtered-out node never reaches the mapper and its children are never walked.',
    example: `{
  "steps": [{ "en": { "title": "One", "stepCell": "1", "submitButton": "Continue" } }, { "en": { "title": "Two", "stepCell": "2", "submitButton": "Book", "back": "Back" } }],
  "tag": "div",
  "children": [
    { "name": "formStepper", "step": -1 },
    { "tag": "div", "step": 1, "children": [{ "name": "name" }, { "name": "stepperSubmit", "step": 1 }] },
    { "tag": "div", "step": 2, "children": [{ "name": "email" }, { "name": "back", "step": 2 }, { "name": "stepperSubmit", "step": 2 }] }
  ]
}`,
    absent:
      'TRAP 4. In a SINGLE-STEP form, absent is correct and required. In a STEPPED form, a node with no `step` renders in the chrome pass AND in every step pass — n+1 copies, with n+1 identical `name` attributes, n+1 identical ids, and n+1 `form.subscribe()` calls under the same key. `getValues` then keeps whichever copy the DOM walk sees last, and a single `unsubscribe` drops them all. Give every node under a stepped root a `step`.',
    warning:
      'Two more edge cases fall out of the same guard. `step: 0` is falsy, so it behaves like "no step" in the step passes but IS excluded from the chrome pass. And `step` is not destructured away — it stays in the forwarded props, so a tag node ships a literal `step="1"` attribute into the DOM.',
  },

  {
    prop: 'staticValue',
    appliesTo: 'field nodes',
    summary:
      'Pins a value and disables the control (`isDisabled` is computed as `… || !!this.staticValue`). A disabled input is deliberately still read by `getValues`, which special-cases `el.disabled` because `FormData` would skip it — so the pinned value does reach the payload.',
    example: `{ "name": "email", "staticValue": "prefilled@example.invalid" }`,
    absent: 'A normal editable field.',
    warning:
      'On a select-backed field this is an OPTION object, not a string — `{ "value": …, "label": … }`, optionally keyed by language — and it replaces the fetcher entirely, so no request is made:\n' +
      '{ "name": "companyBranchId", "staticValue": { "value": "12", "label": "Central branch" } }',
  },

  {
    prop: 'isHidden',
    appliesTo: 'field nodes',
    summary:
      'Intended to mean "present but invisible". It half works. `FormHook.hasItemInStructure` treats an `isHidden` node as absent, and `submitForm` omits every schema field that `hasItemInStructure` cannot find — so the field is dropped from submit validation. Its VALUE is still collected and still sent, because yup keeps unknown keys.',
    example: `{ "name": "companyBranchId", "isHidden": true, "staticValue": { "value": "12", "label": "Central branch" }, "branchApi": "https://example.invalid/api/branches" }`,
    absent: 'The field renders and validates normally.',
    warning:
      'TRAP 3, and the single most surprising thing in this format. `isHidden` is a declared prop on exactly ONE component: `form-select`. So on a select-backed field (vehicle, companyBranchId, vacancyId, cityId, time, year, ownVehicle, currentVehicleBrand, currentVehicleModel, conditionalCurrentVehicleBrand, conditionalCurrentVehicleModel, generalTicketType) it renders `display: none` around a `form-shadow-input` that carries the value — genuinely hidden, still submitted. On EVERY OTHER field — text inputs, textarea, phone, file, date/time pickers, VIN, the branch pickers — nothing reads the prop. The field stays fully visible and fully interactive; the only effect is that it stops being validated. It reaches the DOM as an inert `ishidden` attribute.',
  },

  {
    prop: 'isDisabled',
    appliesTo: 'field nodes',
    summary:
      'Greys the control out. Read by form-input, form-text-area, form-phone-number, form-file, form-picker-input, form-vin-input and form-select. The value is still submitted (see `staticValue`).',
    example: `{ "name": "message", "isDisabled": true }`,
    absent: 'Enabled, unless the form is loading or a `staticValue` is pinned.',
    warning:
      'Ignored on `bookingSlot` and `bookingDate`: their mappers spread your props first and then set `isDisabled={!hasBranch}` last, so the picker is disabled exactly while no branch is selected and your value is discarded. Also ignored by the buttons (`submit`, `stepperSubmit`, `back`), which disable on `isLoading` only.',
  },

  {
    prop: 'required',
    appliesTo: 'field nodes (file, phone)',
    summary:
      'A component-level prop, not a structure concept. `form-file` reads it and adds a "at least one file" test to its own schema; `form-phone-number` reads it in its own require test. Nothing else looks at it.',
    example: `{
  "name": "file",
  "required": true,
  "accept": "application/pdf",
  "maxUpload": 1,
  "maxSize": 5,
  "signUrl": "https://example.invalid/api/sign",
  "localization": { "en": { "label": "CV", "upload": "Upload your CV", "size": "File must be under 5 MB", "max": "One file only" } }
}`,
    absent: 'The file field is optional (`.optional()`); the phone field defers entirely to whatever `getInputState` decided — which, in practice, is "required". See trap 5.',
    warning: 'Writing `"required": true` on a base-schema field such as `email` does NOTHING. That knob is `requiredContext`.',
  },

  {
    prop: 'defaultValue',
    appliesTo: 'field nodes',
    summary: 'Initial value, and the value `reset()` returns to after a successful submit. On a select it is matched against the fetched options and selects the matching one.',
    example: `{ "name": "year", "min": 2015, "max": 2026, "defaultValue": "2024" }`,
    absent: 'Empty field. On `vehicle`, an absent `defaultValue` lets the URL query parameter named by `vehicleIdQueryParam` preselect an option instead.',
    warning: null,
  },

  {
    prop: 'children',
    appliesTo: 'container nodes',
    summary: 'Array of child nodes and/or name strings, OR an object keyed by language for localised text. Both forms are handled on the `tag` branch only.',
    example: `{ "tag": "p", "class": "hint", "children": { "en": "We reply within one working day.", "ar": "نرد خلال يوم عمل واحد." } }`,
    absent: 'An empty element.',
    warning: 'On a field node `children` is destructured away and dropped. Fields have no children.',
  },

  {
    prop: 'localization',
    appliesTo: 'field nodes',
    summary:
      'Per-field text, keyed by language. `label` and `placeholder` are read on every input. The error keys are matched by SUFFIX against the yup message, which is always `<field>-<kind>`: `require`, `format`, `size`, and `failure` (for the `-upload` message). `max` is read by the file field. The date/time picker adds `minMessage`, `maxMessage` and `betweenMessage`, which support the `$minDate$` and `$maxDate$` slots.',
    example: `{
  "name": "date",
  "min": [0, 0, 1],
  "max": [0, 1, 0],
  "localization": {
    "en": {
      "label": "Date",
      "placeholder": "Pick a date",
      "require": "Date is required.",
      "format": "That date is not valid.",
      "minMessage": "Earliest available is $minDate$",
      "maxMessage": "Latest available is $maxDate$"
    },
    "ar": { "label": "التاريخ", "placeholder": "اختر تاريخاً", "require": "التاريخ مطلوب." }
  }
}`,
    absent:
      'Text falls back to the shipped locale entry named by the schema `meta` (`<field>-label`, `<field>-placeholder`), and finally to that raw key string, which is what a missing translation looks like on screen.',
    warning:
      'There is no fallback BETWEEN languages here. Unlike `data.localization`, which falls back to `en`, a node `localization` block is indexed by the active language only — so a node with only an `en` block shows the raw locale keys in Arabic.',
  },
];

// ---------------------------------------------------------------------------
// The mapper table
// ---------------------------------------------------------------------------

export const mappers = [
  {
    name: 'submit',
    renders: 'form-submit',
    minimalNode: `{ "name": "submit", "localization": { "en": { "label": "Send" } } }`,
    needs: [],
    notes:
      'A `type="submit"` button. Label resolution, first hit wins: node `localization[lang].label` → `locale[submitTextKey]` (default key `submit`, i.e. `data.localization[lang].submit`) → `sharedFormLocales.submit` → the literal `Submit`. Works inside a stepped form too — it just cannot read a per-step label.',
  },

  {
    name: 'stepperSubmit',
    renders: 'form-stepper-submit',
    minimalNode: `{ "name": "stepperSubmit", "step": 2 }`,
    needs: ['structure.steps', 'step on the node'],
    notes:
      'Identical button, one extra label source: `steps[step - 1][lang].submitButton`, inserted between the node localisation and the form-wide `submit` key. Its `step` does double duty — it decides which pass renders the button AND which step label it reads. Omit `step` and it falls back to the current step, which is usually right but not always during a transition.',
  },

  {
    name: 'back',
    renders: 'form-stepper-control',
    minimalNode: `{ "name": "back", "step": 2 }`,
    needs: ['structure.steps'],
    notes:
      'Previous-step button; calls `form.updateStep(-1)`. Its label is `steps[currentStep - 1][lang].back` and there is no node-level override — leave `back` out of a step and the button renders with a chevron and no text.',
  },

  {
    name: 'formStepper',
    renders: 'form-stepper',
    minimalNode: `{ "name": "formStepper", "step": -1 }`,
    needs: ['structure.steps'],
    notes:
      'The numbered rail. Reads `steps[i][lang].stepCell` for the circle and `stepTitle` for the caption under it, straight from `form.context.structure.steps` — so it always shows every step, regardless of where the node sits. Give it `step: -1` so it renders once in the chrome pass instead of once per panel.',
  },

  {
    name: 'inputPreview',
    renders: 'form-input-preview (which renders a read-only form-input)',
    minimalNode: `{
  "name": "inputPreview",
  "props": { "name": "summaryLine" },
  "localization": { "en": { "label": "Summary", "value": "\${name} - \${email}" } }
}`,
    needs: ['props.name', 'localization[lang].value'],
    notes:
      'A read-only echo of other fields. `localization[lang].value` is a template: every `${…}` is replaced with `form.getValue(name)` at render time. The nested `props` object is spread onto the inner `form-input`, and it is where the inner field NAME has to go — the outer `name` is consumed as the mapper key. It subscribes under the constant key `form-input-preview`, so two previews in one form collide.',
  },

  {
    name: 'name',
    renders: 'form-input',
    minimalNode: `{ "name": "name", "localization": { "en": { "label": "Full name", "placeholder": "Full name", "require": "Full name is required." } } }`,
    needs: [],
    notes: 'Base schema: optional by default; with `requiredContext.name` it is required and must be at least 3 characters (message key `name-format`).',
  },

  {
    name: 'lastName',
    renders: 'form-input',
    minimalNode: `{ "name": "lastName", "localization": { "en": { "label": "Surname", "require": "Surname is required." } } }`,
    needs: [],
    notes: 'Same rules as `name`: min 3 characters when required.',
  },

  {
    name: 'email',
    renders: 'form-input with type="email"',
    minimalNode: `{ "name": "email", "localization": { "en": { "label": "Email", "placeholder": "you@example.invalid", "format": "Email format is invalid." } } }`,
    needs: [],
    notes: 'The `.email()` check is OUTSIDE the `.when()`, so format is enforced even when the field is optional — an optional email must be empty or valid.',
  },

  {
    name: 'message',
    renders: 'form-text-area',
    minimalNode: `{ "name": "message", "localization": { "en": { "label": "Message", "format": "Message must be 10 characters or more." } } }`,
    needs: [],
    notes: 'Min 10 characters when required.',
  },

  {
    name: 'phone',
    renders: 'form-phone-number',
    minimalNode: `{ "name": "phone", "countryCode": "IQ", "localization": { "en": { "label": "Phone", "placeholder": "Phone number", "require": "Phone number is required.", "format": "That number is not valid." } } }`,
    needs: ['countryCode'],
    notes:
      'Not in the base schema — it brings its own yup schema through `FormElement.validate()`, using libphonenumber. `countryCode` is a string, an array of ISO codes, or an array of `{ "code": "IQ" }`; with more than one entry a country selector appears and the initial pick comes from the browser timezone, then the browser locale, then the first entry. The submitted value is the prefixed, formatted number (`+964 …`). See trap 5 in `requiredMechanics.exceptions`.',
  },

  {
    name: 'vin',
    renders: 'form-vin-input',
    minimalNode: `{ "name": "vin", "useOcr": true, "scannerIcon": "camera", "ocrEndpoint": "https://example.invalid/api/vin-ocr", "localization": { "en": { "label": "VIN", "format": "That VIN is not valid.", "scan": "Scan your VIN" } } }`,
    needs: [],
    notes:
      'The VIN check digit is only validated when `requiredContext.vin` is true — the `validateVin` test sits inside the required branch, unlike `email`, whose format check runs either way. `useOcr`, `readSticker`, `scannerIcon` (`""`, `"qr-code"`, `"camera"`) and `ocrEndpoint` enable the camera scanner.',
  },

  {
    name: 'file',
    renders: 'form-file',
    minimalNode: `{
  "name": "file",
  "required": true,
  "accept": "application/pdf,.doc,.docx",
  "maxUpload": 1,
  "maxSize": 5,
  "signUrl": "https://example.invalid/api/sign",
  "localization": { "en": { "label": "CV", "upload": "Upload your CV", "size": "File must be under 5 MB", "max": "One file only" } }
}`,
    needs: ['signUrl (for deferred upload)'],
    notes:
      'Not in the base schema; brings its own via `validate()`. `maxSize` IS IN MEGABYTES — the test computes `maxSize * 1024 * 1024`, so `5` means 5 MB and `5242880` means five terabytes. `accept` matches MIME types, `type/*` wildcards and `.ext` suffixes. `signUrl` turns on deferred upload: files are signed and uploaded during submit, before the record is created, and a failed or in-flight upload blocks the whole form even when the field is optional. `signMethod`, `signPrefix`, `accountName` and `containerName` shape that request.',
  },

  {
    name: 'fileUploader',
    renders: 'form-file',
    minimalNode: `{ "name": "attachment", "type": "fileUploader", "required": true, "maxSize": 10, "signUrl": "https://example.invalid/api/sign" }`,
    needs: ['signUrl (for deferred upload)'],
    notes:
      'A second key pointing at the identical component. Its practical use is exactly the example above: a second file field under a different field name, reached with `type`.',
  },

  {
    name: 'vehicle',
    renders: 'form-select (searchable)',
    minimalNode: `{
  "name": "vehicle",
  "vehicleApi": "https://example.invalid/api/models",
  "dynamic": true,
  "items": "data",
  "item-value": "id",
  "item-label": "title",
  "localization": { "en": { "label": "Model", "placeholder": "Choose a model" } }
}`,
    needs: ['vehicleApi'],
    notes:
      'Three response shapes. With `dynamic: true` the response is mapped by `populateItems` using `items` (path to the array), `item-value` and `item-label` (paths within each item; each accepts an array of paths and takes the first non-null). With `vehiclesApiStrapiFormat: true` it expects `{ data: [{ attributes: { GradeName, Cover } }] }`. With neither it expects a flat array of `{ ID, Title, Image }`. `useNamedValue` submits the label instead of the id. `vehicleIdQueryParam` names a URL query parameter that preselects a matching option when no `defaultValue` is set. The chosen option keeps the whole record in `meta`, which is what `vehicleImage` draws from.',
  },

  {
    name: 'vehicleImage',
    renders: 'VehicleImageViewer (a functional component, not a field)',
    minimalNode: `{ "name": "vehicleImage" }`,
    needs: ['a vehicle field in the same form'],
    notes:
      'Renders the selected vehicle image and nothing else — no name, no value, no validation. It watches `vehicle` and reads `form.context.vehicleList`, the option array `form-select` writes back after fetching, taking `meta.image`. Without a `vehicle` field it renders a collapsed empty container. Images are fetched, base64-cached in a module-level map and re-rendered, so they survive a step change.',
  },

  {
    name: 'companyBranchId',
    renders: 'form-select (clearable, searchable)',
    minimalNode: `{ "name": "companyBranchId", "branchApi": "https://example.invalid/api/branches", "localization": { "en": { "label": "Branch", "placeholder": "Select a branch" } } }`,
    needs: ['branchApi'],
    notes:
      'Expects an array of `{ ID, Name, … }`; label is `Name`, value is `String(ID)`, and the ENTIRE record is kept in `meta`. That is not cosmetic: `bookingSlot` and `bookingDate` resolve their four calendar ids out of this record (`CompanyIntegrationId`, `IntegrationId`, `Departments[].IntegrationId`, `Brands[].IntegrationId`), so those fields do not work without this one.',
  },

  {
    name: 'vacancyId',
    renders: 'form-select (clearable, searchable)',
    minimalNode: `{ "name": "vacancyId", "vacancyApi": "https://example.invalid/api/vacancies", "localization": { "en": { "label": "Position", "placeholder": "Choose a position" } } }`,
    needs: ['vacancyApi'],
    notes: 'Array of `{ ID, Title }`; label is `Title`, value is `String(ID)`, full record in `meta`.',
  },

  {
    name: 'cityId',
    renders: 'form-select (clearable, searchable)',
    minimalNode: `{ "name": "cityId", "cityApi": "https://example.invalid/api/cities", "localization": { "en": { "label": "City", "placeholder": "Choose a city" } } }`,
    needs: ['cityApi'],
    notes: 'Array of `{ ID, Name }`; label is `Name`, value is `String(ID)`.',
  },

  {
    name: 'date',
    renders: 'form-picker-input with type="date"',
    minimalNode: `{
  "name": "date",
  "min": [0, 0, 1],
  "max": [0, 1, 0],
  "localization": { "en": { "label": "Date", "placeholder": "Pick a date", "minMessage": "Earliest available is $minDate$", "maxMessage": "Latest available is $maxDate$" } }
}`,
    needs: [],
    notes:
      '`min` and `max` are either literal date strings or OFFSET ARRAYS relative to the start of today, in the order `[years, months, days, hours, minutes, seconds]` — so `[0, 0, 1]` is tomorrow and `[0, 1, 0]` is one month out. They are layered onto the base schema through `partialValidation`, which adds a `min-date`, `max-date` or `between-date` test whose message key (`minMessage` / `maxMessage` / `betweenMessage`) you localise on the node. An optional `format` string (date-fns) reformats the DISPLAYED value and publishes it as the submitted value through the `<field>-format` back-channel.',
  },

  {
    name: 'time',
    renders: 'form-select (clearable)',
    minimalNode: `{
  "name": "time",
  "min": [0, 0, 0, 9, 0],
  "max": [0, 0, 0, 17, 0],
  "span": [0, 0, 0, 0, 30],
  "format": "HH:mm",
  "localization": { "en": { "label": "Time", "placeholder": "Pick a time" } }
}`,
    needs: ['min', 'max', 'span', 'format'],
    notes:
      'A fixed ladder built in the browser — no request. All four props are mandatory and all three arrays use the same `[y, mo, d, h, m, s]` offset shape; the example is 09:00 to 17:00 every 30 minutes. Identical at every branch on every day, which is exactly the difference from `bookingSlot`. Miss one prop and the option list is silently empty.',
  },

  {
    name: 'year',
    renders: 'form-select (clearable)',
    minimalNode: `{ "name": "year", "min": 2015, "max": 2026, "localization": { "en": { "label": "Model year", "placeholder": "Choose a year" } } }`,
    needs: [],
    notes:
      'Plain numeric range; `min` defaults to the current year minus 20 and `max` to the current year. `firstOption` and `lastOption` prepend/append an extra entry, and both their label AND their value are keyed by language: `{ "firstOption": { "label": { "en": "Older" }, "value": { "en": "older" } } }`.',
  },

  {
    name: 'bookingSlot',
    renders: 'branch-slot-picker (day strip + time grid)',
    minimalNode: `{
  "name": "bookingSlot",
  "calendarApi": "https://example.invalid/api/calendar",
  "daysAhead": 30,
  "disabledWeekdays": [5, 6],
  "localization": { "en": { "label": "Date & time", "placeholder": "Choose a slot" } }
}`,
    needs: ['calendarApi', 'a companyBranchId field in the same form'],
    notes:
      'The chosen slot is NOT sent with the form — the trigger input carries no `name`, so `FormHook.getValues` never reads it; listen for the `slotChange` event on the form element and send the value yourself through `extraPayload`. Otherwise it asks the selected branch what is actually open, instead of assuming. Company, branch, department and brand ids are resolved from the `companyBranchId` option `meta`; `departmentPreference` (default `["showroom"]`) picks among the branch departments, and explicit `departmentId` / `brandId` node props override the resolution. The field stays disabled until a branch is chosen. `disabledWeekdays` (`0` Sunday … `6` Saturday) and `disabledDates` (`YYYY-MM-DD`) accept an array or a comma string; matching days stay visible but unselectable. Submits `YYYY-MM-DDTHH:mm`. See trap 6.',
  },

  {
    name: 'bookingDate',
    renders: 'branch-date-picker (month calendar)',
    minimalNode: `{
  "name": "bookingDate",
  "calendarApi": "https://example.invalid/api/calendar",
  "daysAhead": 30,
  "localization": { "en": { "label": "Date & time", "placeholder": "Choose a slot" } }
}`,
    needs: ['calendarApi', 'a companyBranchId field in the same form'],
    notes:
      'Never sent with the form, exactly like `bookingSlot` — read it from the `slotChange` event instead. The month-calendar layout is the only difference: same props, same data, same event payload. The two are interchangeable; pick whichever your audience reads faster. Same caveats as `bookingSlot`, including trap 6.',
  },

  {
    name: 'ownVehicle',
    renders: 'form-select',
    minimalNode: `{ "name": "ownVehicle", "localization": { "en": { "label": "Do you own a vehicle?", "yes": "Yes", "no": "No" } } }`,
    needs: ['localization[lang].yes', 'localization[lang].no'],
    notes:
      'A two-option select whose values are the literal strings `yes` and `no`. The option LABELS come from `localization[lang].yes` / `.no` — not from anything shared — so a missing block gives you two blank rows. Four other fields branch on this value in the schema.',
  },

  {
    name: 'currentVehicleBrand',
    renders: 'form-select (searchable)',
    minimalNode: `{ "name": "currentVehicleBrand", "localization": { "en": { "label": "Current brand", "placeholder": "Choose a brand" } } }`,
    needs: ['structure.data.currentVehiclesApi'],
    notes:
      'The one field mapper that reads its endpoint from `structure.data` rather than from the node. Expects `{ ID, Name, Models: [{ ID, Name }] }`, appends a locale-provided `Other` option, and keeps each brand in `meta` so `currentVehicleModel` can read `Models` without a second request. Required in the schema when `ownVehicle === "yes"` — `requiredContext` does not reach it. Note the API filter tests lowercase `vehicle.name` while the mapping reads `vehicle.Name`, so an `Other` row coming from the endpoint is not actually filtered out and you get two.',
  },

  {
    name: 'currentVehicleModel',
    renders: 'form-select (searchable)',
    minimalNode: `{ "name": "currentVehicleModel", "localization": { "en": { "label": "Current model", "placeholder": "Choose a model" } } }`,
    needs: ['a currentVehicleBrand field in the same form'],
    notes:
      'Reads `Models` off the selected brand `meta`; no request of its own. Disabled until a brand is chosen and again when the brand is `Other`. Required when `ownVehicle === "yes"` and the brand is not `Other`.',
  },

  {
    name: 'conditionalCurrentVehicleBrand',
    renders: 'form-select (searchable)',
    minimalNode: `{ "name": "conditionalCurrentVehicleBrand", "brandApi": "https://example.invalid/api/vehicle-brands", "useNamedValue": true, "localization": { "en": { "label": "Current brand" } } }`,
    needs: ['brandApi', 'an ownVehicle field in the same form'],
    notes:
      'Same as `currentVehicleBrand` but takes its endpoint from the node (`brandApi`) and hard-gates on `ownVehicle === "yes"` for both enabled state and required state. `useNamedValue` submits the brand name instead of its id. Carries the same `name`/`Name` filter quirk.',
  },

  {
    name: 'conditionalCurrentVehicleModel',
    renders: 'form-select (searchable)',
    minimalNode: `{ "name": "conditionalCurrentVehicleModel", "useNamedValue": true, "localization": { "en": { "label": "Current model" } } }`,
    needs: ['a conditionalCurrentVehicleBrand field in the same form'],
    notes: 'The gated twin of `currentVehicleModel`, reading `conditionalCurrentVehicleBrandList` from the form context.',
  },

  {
    name: 'generalTicketType',
    renders: 'form-select (clearable) — general-inquiry-form ONLY',
    minimalNode: `{
  "name": "generalTicketType",
  "options": [
    { "value": "support", "en": "Support", "ar": "دعم" },
    { "value": "sales", "en": "Sales", "ar": "مبيعات" }
  ],
  "localization": { "en": { "label": "What is this about?", "placeholder": "Choose a topic", "require": "Please choose a topic." } }
}`,
    needs: ['options'],
    notes:
      "The one field `general-inquiry-form` adds to the shared table, in both the mapper and the schema. Options are inline on the node — no request — with the label read from a per-language key on each option object (`option[language]`), not from a `label` field. Because it is in that component's schema, `requiredContext.generalTicketType` works. On the other four components the name is unknown and the node renders nothing.",
  },
];

// ---------------------------------------------------------------------------
// Steps
// ---------------------------------------------------------------------------

export const stepsReference = {
  summary:
    'Adding `structure.steps` turns one render into n+1. `form-structure` runs `renderStructure` once with `currentStep === -1` — the chrome pass, drawn outside the sliding panels — and then once per step with `currentStep === 1 … n`, each into its own transform-animated panel. `step` on a node decides which passes it survives. Validation follows the same partition: `submitForm` omits every schema field that `hasItemInStructure` cannot find in the CURRENT step, so each step validates only its own fields; values accumulate in `stepFormValues`, and the POST happens only when `steps.length === currentStep`.',
  example: `{
  "data": { "requestUrl": "https://example.invalid/api/tickets" },
  "requiredContext": { "name": true, "email": true, "vehicle": true },
  "steps": [
    {
      "en": { "title": "Details", "stepCell": "1", "stepTitle": "Your details", "submitButton": "Continue" },
      "ar": { "title": "البيانات", "stepCell": "١", "stepTitle": "بياناتك", "submitButton": "متابعة" }
    },
    {
      "en": { "title": "Vehicle", "stepCell": "2", "stepTitle": "Your vehicle", "submitButton": "Book", "back": "Back" },
      "ar": { "title": "المركبة", "stepCell": "٢", "stepTitle": "مركبتك", "submitButton": "احجز", "back": "رجوع" }
    }
  ],
  "tag": "div",
  "id": "container",
  "children": [
    { "name": "formStepper", "step": -1 },
    {
      "tag": "div",
      "id": "step-one",
      "step": 1,
      "children": [
        { "name": "name", "localization": { "en": { "label": "Full name" } } },
        { "name": "email", "localization": { "en": { "label": "Email", "placeholder": "you@example.invalid" } } },
        { "name": "stepperSubmit", "step": 1 }
      ]
    },
    {
      "tag": "div",
      "id": "step-two",
      "step": 2,
      "children": [
        { "name": "vehicle", "vehicleApi": "https://example.invalid/api/models", "localization": { "en": { "label": "Model" } } },
        { "tag": "div", "id": "step-two-actions", "children": [{ "name": "back", "step": 2 }, { "name": "stepperSubmit", "step": 2 }] }
      ]
    }
  ]
}`,
  keys: [
    {
      key: 'title',
      summary: 'Declared as the one REQUIRED key on the `Step` type.',
      note: 'TRAP 9. Nothing reads it. No component, no helper — `getStepLabels` returns it in the object and every consumer takes `stepCell`, `stepTitle`, `submitButton` or `back` instead. TypeScript will make you write it; the rendered form will not show it. Treat it as a comment for whoever maintains the JSON.',
    },
    {
      key: 'stepCell',
      summary: 'The text inside the rail circle. Usually the step number — as a string, so it can be localised digits.',
      note: 'Read by `form-stepper` from `structure.steps` directly, so the rail shows every step regardless of which pass it renders in.',
    },
    {
      key: 'stepTitle',
      summary: 'The caption under the rail circle.',
      note: 'Also `form-stepper` only. Absent gives you a numbered circle with no caption.',
    },
    {
      key: 'submitButton',
      summary: 'Label for the `stepperSubmit` button on this step — "Continue" on the way through, "Book" at the end.',
      note: 'Sits between the node localisation and the form-wide `submit` key in the fallback chain. The button reads it via its OWN `step`, not the current step, so give the node a `step`.',
    },
    {
      key: 'back',
      summary: 'Label for the `back` button on this step.',
      note: 'Read from the CURRENT step, and there is no node-level override. Leave it out of step 1 (there is nothing to go back to) and put it on every later step.',
    },
  ],
  traps: [
    'TRAP 4 — the one that actually bites. A node with NO `step` under a stepped root renders in the chrome pass AND in every step pass: n+1 copies. That means n+1 elements with the same `name`, n+1 identical ids, and n+1 `form.subscribe()` calls under one key. `getValues` keeps whichever copy the DOM walk reaches last, and a single `unsubscribe` removes all of them. Every node under a stepped root wants a `step`.',
    '`step: -1` is the chrome pass and is where the stepper rail belongs. `step: 0` is falsy, so the guard treats it as "no step" during the step passes but still excludes it from the chrome pass — almost certainly not what you meant.',
    'Leaving `step` on a node after deleting `structure.steps` makes the node disappear: the single pass runs with `currentStep === -2`, which no positive step ever equals.',
    '`step` is not stripped before the props are forwarded, so a container node ships a literal `step="1"` attribute into the DOM.',
    "A field in another step is excluded from THIS step's validation by the same `hasItemInStructure` mechanism that `isHidden` uses — that is the feature, but it means a mis-stepped required field is never enforced anywhere.",
    'TRAP 10 — `stepChangeCallback` is declared as a `@Prop` on all five form components and typed on `FormHookInterface`, and is never invoked anywhere in the codebase. `updateStep` force-updates the back buttons and re-renders; it calls nothing. Do not wire analytics to it. To observe step changes today you have to poll `(await el.getForm()).formStructure.currentStep`.',
  ],
};

// ---------------------------------------------------------------------------
// Required
// ---------------------------------------------------------------------------

export const requiredMechanics = {
  knobs: [
    {
      id: 'requiredContext',
      title: 'requiredContext — the one structure-level switch',
      detail:
        'Every entry becomes a yup context variable with `Required` appended (`{"email": true}` → `$emailRequired`), and each base-schema field branches on its own variable through `.when("$<field>Required", { is: true, then: required, otherwise: optional })`. Read once, in the `FormHook` constructor. It works for exactly the fields that are IN the base schema: name, lastName, message, email, vin, vehicle, vacancyId, companyBranchId, cityId, date, year, time, ownVehicle, gender — plus generalTicketType on general-inquiry-form.',
      example: `{ "requiredContext": { "name": true, "email": true, "companyBranchId": true, "date": true, "time": true } }`,
      appliesTo: 'base-schema field names only',
    },

    {
      id: 'required-prop',
      title: 'required — a component prop, not a structure concept',
      detail:
        'Only `form-file` and `form-phone-number` read it. On a file field it adds the "at least one file" test. Written on `email` or `name` it does nothing at all — those fields listen to `requiredContext`.',
      example: `{ "name": "file", "required": true, "maxSize": 5, "signUrl": "https://example.invalid/api/sign" }`,
      appliesTo: 'file, fileUploader, phone',
    },

    {
      id: 'cross-field',
      title: 'Value-driven requirements, hard-coded in the schema',
      detail:
        '`currentVehicleBrand` and `conditionalCurrentVehicleBrand` are required whenever the `ownVehicle` value is the literal string `yes`. `currentVehicleModel` and `conditionalCurrentVehicleModel` are required when `ownVehicle === "yes"` AND the chosen brand is not `Other`. These use `.when("ownVehicle", …)` on the VALUE, not on the context — so `requiredContext` cannot turn them on or off.',
      example: `{
  "tag": "div",
  "children": [
    { "name": "ownVehicle", "localization": { "en": { "label": "Do you own a vehicle?", "yes": "Yes", "no": "No" } } },
    { "name": "currentVehicleBrand" },
    { "name": "currentVehicleModel" }
  ]
}`,
      appliesTo: 'currentVehicleBrand, currentVehicleModel, conditionalCurrentVehicleBrand, conditionalCurrentVehicleModel',
    },

    {
      id: 'isHidden-escape',
      title: 'isHidden — the de-facto "do not validate this" switch',
      detail:
        '`submitForm` builds `excludedFields` from every schema field that `hasItemInStructure` cannot find, and `hasItemInStructure` refuses to match a node carrying `isHidden`. The field is then `omit`ed from the schema for that submit. The VALUE still goes out, because yup keeps unknown keys.',
      example: `{ "name": "companyBranchId", "isHidden": true, "staticValue": { "value": "12", "label": "Central branch" }, "branchApi": "https://example.invalid/api/branches" }`,
      appliesTo: 'any field — but see trap 3: only select-backed fields are actually hidden',
    },

    {
      id: 'step-membership',
      title: 'Step membership',
      detail:
        "The same `hasItemInStructure` walk bails out on `!!step && step != currentStep`, so a field belonging to another step is excluded from this step's validation. That is how per-step validation works — and how a mis-stepped field escapes validation entirely.",
      example: `{ "tag": "div", "step": 2, "children": [{ "name": "vehicle" }] }`,
      appliesTo: 'every field in a stepped form',
    },

    {
      id: 'star-vs-enforcement',
      title: 'The star and the enforcement are computed separately',
      detail:
        'The asterisk comes from `FormHook.getInputState`, which probes the resolved schema with `validateSyncAt(name, { [name]: undefined })` and sets `isRequired = true` if ANYTHING throws. Enforcement comes from the omit-and-validate pass at submit. A field that is not in the schema at all makes `validateSyncAt` throw `The schema does not contain the path: <name>` — a permanent star that nothing enforces. The result is also CACHED on first call, so it never re-computes.',
      example: `{ "name": "secondaryEmail", "type": "email", "localization": { "en": { "label": "Second email" } } }`,
      appliesTo: 'bookingSlot, bookingDate, and any field name invented via `type`',
    },
  ],

  exceptions: [
    {
      field: 'phone',
      behaviour:
        'TRAP 5. Effectively always required, and there is no structure key that changes it. `phone` is not in the base schema, so `requiredContext.phone` is inert. Its schema comes from the component: a `require` test that passes when neither `required` nor `externalRequired` is set, followed by a `format` test that calls `value.replace(…)` unconditionally. `getInputState` probes with `undefined`, the format test throws a TypeError, the bare `catch` sets `isRequired = true`, and that value is cached AND fed straight back into the component as `externalRequired` — which is what makes the require test start enforcing. It is a loop that closes on the first render.',
      escape:
        'None that leaves the field usable. `{ "name": "phone", "required": false }` and `{ "requiredContext": { "phone": false } }` both do nothing. The nearest thing is `{ "name": "phone", "isHidden": true }`, which drops it from the submit-time schema — but on a phone field `isHidden` does not hide anything, so you get a visible, starred, live-erroring field that merely stops blocking submit. If a form genuinely needs an optional phone number, leave the node out.',
    },

    {
      field: 'bookingSlot / bookingDate',
      behaviour:
        'TRAP 6. Always starred, never enforced. Both pickers implement `FormElement` but define no `validate()` and no `partialValidation()`, so `form.subscribe` shapes nothing into the schema and the name is simply absent from it. `getInputState` then throws on the missing path and returns `isRequired: true` forever, while `submitForm` has no field to check. An empty booking submits cleanly.',
      escape:
        'There is no structure key for either half. `{ "name": "bookingSlot", "isRequired": false }` is a real prop on the picker, but the render reads `state?.isRequired || this.isRequired` and `state.isRequired` is already `true`, so it changes nothing. Enforcing a booking has to happen server-side or in a `middleware`/`successCallback` on the host page.',
    },

    {
      field: 'file',
      behaviour:
        'Honest, and the only field whose required state is genuinely node-driven. `{"required": true}` adds the count test and the star follows; without it the schema is `.optional()` and there is no star.',
      escape: `{ "name": "file", "required": false, "maxSize": 5, "signUrl": "https://example.invalid/api/sign" }`,
    },

    {
      field: 'gender',
      behaviour:
        'TRAP 8. Present in the base schema as a `number()` with a full `$genderRequired` branch — and absent from the mapper table, on every one of the five components. `elementMapper["gender"]` is `undefined`, so `renderStructure` falls through and returns `false`: no field, no error, no warning. `requiredContext.gender` is therefore unreachable, because `hasItemInStructure` also cannot find it and `submitForm` omits it.',
      escape:
        'Reuse a widget under that name: `{ "name": "gender", "type": "ownVehicle", "localization": { "en": { "label": "Gender", "yes": "Female", "no": "Male" } } }` renders a two-option select — but it submits the strings `yes`/`no` into a `number()` field, so the schema will reject it. In practice, do not use `gender`.',
    },

    {
      field: 'any name invented with `type`',
      behaviour:
        'The value is collected and sent — `getValues` reads the DOM, and yup preserves unknown keys through `validate` — but the name is in no schema, so nothing validates it and `getInputState` throws on the missing path, leaving a permanent asterisk beside a field that can be submitted empty.',
      escape:
        'Accept the star, and validate on the server — the value does arrive:\n' +
        '{ "name": "secondaryEmail", "type": "email", "localization": { "en": { "label": "Second email", "placeholder": "you@example.invalid" } } }\n' +
        'posts as { "secondaryEmail": "someone@example.invalid" }. Adding the name to the schema would mean editing `src/components/forms/defaults/validation.ts` and shipping a new package version; there is no host-side hook. Same story for a new field TYPE — see trap 7 under `nodeProps` → `type`.',
    },

    {
      field: 'generalTicketType',
      behaviour:
        'Fully wired — mapper and schema — but only on `general-inquiry-form`. On `general-form`, `service-booking-form`, `ssc-lookup-form` and `test-drive-form` the name is unknown to both tables, so the node renders nothing and `requiredContext.generalTicketType` is inert.',
      escape: `{ "component": "general-inquiry-form", "requiredContext": { "generalTicketType": true } }`,
    },
  ],
};

// ---------------------------------------------------------------------------
// Marketing / UTM
// ---------------------------------------------------------------------------

export const utm = {
  keys: ['utm_source', 'utm_medium', 'utm_campaign', 'utm_term', 'utm_content', 'gclid', 'fbclid'],

  readFrom: 'window.location.search on the page hosting the form, read at SUBMIT time — not at load. Only parameters actually present are collected; empty values are skipped.',

  storedWhere:
    'Nowhere. There is no cookie, no localStorage, no sessionStorage, no hidden field — `getMarketingValues` builds a fresh object from the live URL every time submit runs. Attribution therefore survives only as long as the query string does: if the visitor lands on a campaign URL and then navigates to a clean /contact page before filling the form, the parameters are gone. If you need them to survive navigation, the host page has to carry them, or feed them back through the `extraPayload` prop.',

  sentAs:
    'Merged into the JSON body for every method except GET and HEAD, where they are merged into the HEADERS instead (`["get", "head"].includes(method.toLowerCase())`). The merge happens after `extraPayload`, so a marketing key wins over a same-named key you set yourself.',

  offSwitches: [
    {
      id: 'data.disableUTMLog',
      detail: 'A truthy value in the structure returns `{}` before the URL is even read. The permanent, per-form switch.',
      example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets", "disableUTMLog": true } }`,
    },
    {
      id: 'disable_utm_log=true',
      detail:
        'A URL query parameter on the hosting page, checked as an exact string match against `"true"`. Per-visit, and it does not need a structure change — handy for testing.',
      example: `{ "url": "https://example.invalid/contact?utm_source=newsletter&disable_utm_log=true", "collected": {} }`,
    },
  ],

  example: `{
  "url": "https://example.invalid/contact?utm_source=newsletter&utm_campaign=spring&gclid=abc123&ref=ignored",
  "collectedValues": {
    "utm_source": "newsletter",
    "utm_campaign": "spring",
    "gclid": "abc123"
  },
  "postBody": {
    "name": "Ada Lovelace",
    "email": "ada@example.invalid",
    "message": "Please call me back.",
    "utm_source": "newsletter",
    "utm_campaign": "spring",
    "gclid": "abc123"
  }
}`,
};

// ---------------------------------------------------------------------------
// The wire
// ---------------------------------------------------------------------------

export const wireFormat = {
  headers: [
    {
      name: 'Content-Type',
      value: 'application/json',
      when: 'Always — including on a GET, where no body is sent.',
      example: `{ "Content-Type": "application/json" }`,
    },
    {
      name: 'Brand',
      value: 'structure.data.brandId',
      when: 'Always. The key is written unconditionally, so with no `brandId` the header goes out as the literal string "undefined" rather than being omitted.',
      example: `{ "Brand": "demo-brand" }`,
    },
    {
      name: 'Accept-Language',
      value: 'the form\'s current language, falling back to "en"',
      when: 'Always. Single value, never a weighted list.',
      example: `{ "Accept-Language": "ar" }`,
    },
    {
      name: 'Recaptcha-Token',
      value: 'grecaptcha.execute(data.recaptchaKey, { action: "submit" })',
      when: 'Browser mode only, and only when `data.recaptchaKey` is set. Omit the key and this header never appears — which is what every example on this page does.',
      example: `{ "data": { "requestUrl": "https://example.invalid/api/tickets" } }`,
    },
    {
      name: 'Authorization',
      value: 'the value returned by the getMobileToken prop',
      when: 'Mobile mode, when that token starts (case-insensitively) with `bearer`. The endpoint then comes from `data.requestAppUrl`.',
      example: `{ "data": { "isMobileForm": true, "requestAppUrl": "https://example.invalid/api/app/tickets" } }`,
    },
    {
      name: 'verification-token',
      value: 'the value returned by the getMobileToken prop',
      when: 'Mobile mode, when the token does NOT start with `bearer`. The endpoint then comes from `data.requestAppCheckUrl`.',
      example: `{ "data": { "isMobileForm": true, "requestAppCheckUrl": "https://example.invalid/api/appcheck/tickets" } }`,
    },
    {
      name: '<your own>',
      value: 'extraHeader prop, then structure.data.extraHeader',
      when: 'Whenever set. Note the order: the STRUCTURE is applied last and wins. The payload merge is the other way round, where the PROP wins.',
      example: `{ "data": { "extraHeader": { "X-Source": "docs-site", "X-Tenant": "demo" } } }`,
    },
    {
      name: '<marketing keys>',
      value: 'utm_source, utm_medium, utm_campaign, utm_term, utm_content, gclid, fbclid',
      when: 'Only when `data.requestMethod` is `get` or `head`. On every other method they go into the body instead.',
      example: `{ "data": { "requestMethod": "GET", "requestUrl": "https://example.invalid/api/tickets" } }`,
    },
  ],

  successPath:
    '`response.ok` → the body is parsed with `response.json().catch(() => ({}))`, so a non-JSON 200 becomes `{}` rather than an error. The result is handed to `setSuccessCallback` → `formSuccessHandler`, which calls your `successCallback(data, message)` if you supplied one. THE RETURN VALUE OF THAT CALLBACK DECIDES THE DIALOG: `openDefaultDialog = !!(await successCallback(...))`, so a callback that returns nothing SUPPRESSES the built-in success dialog. Return `true` to keep it. With no callback at all the dialog always opens. Then the page scrolls the form into view (unless `disableScrollToTop`), and 100 ms later the form resets and re-renders.',

  errorPath:
    'Not `response.ok` → if the response `content-type` includes `application/json`, the message is read from `parsedResponse.message.body ?? parsedResponse.Message.Body ?? ""`; otherwise the raw `response.text()` is used. That string is thrown as an `Error` and caught, then `formErrorHandler` resolves the display message as `error.message || error.Message.Body || error.message.body || sharedFormLocales.errors.wildCard`. So an error body in any other shape degrades to the generic wildcard string. `errorCallback` follows the same suppression rule as the success one: return a truthy value to keep the built-in error dialog. `isLoading` is cleared in a `finally`, so a thrown error never leaves the button spinning.',

  example: `{
  "request": {
    "url": "https://example.invalid/api/tickets",
    "method": "POST",
    "headers": {
      "Content-Type": "application/json",
      "Brand": "demo-brand",
      "Accept-Language": "en",
      "X-Source": "docs-site"
    },
    "body": {
      "name": "Ada Lovelace",
      "email": "ada@example.invalid",
      "phone": "+964 750 000 0000",
      "date": "2026-09-14",
      "time": "09:30",
      "preferredDateTime": "2026-09-14T09:30:00+03:00",
      "additionalData": { "customerNotes": "Please call me back." },
      "utm_source": "newsletter"
    }
  },
  "successResponse": { "status": 200, "body": { "id": "TCK-1024" } },
  "errorResponse": { "status": 400, "body": { "message": { "body": "That branch is closed on the selected day." } } }
}`,
};
