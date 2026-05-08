import { ShiftSurveyElement } from './ShiftSurveyElement.js';

export { ShiftSurveyElement };

/** Register the `<shift-survey>` custom element. Idempotent — calling twice on
 *  the same page is a no-op. Consumers who want a different tag name can import
 *  `ShiftSurveyElement` directly and call `customElements.define(...)` with it. */
export function registerShiftSurvey(tagName = 'shift-survey'): void {
  if (typeof window === 'undefined' || typeof customElements === 'undefined') return;
  if (customElements.get(tagName)) return;
  customElements.define(tagName, ShiftSurveyElement);
}

// Auto-register on import so plain `<script type="module" src="...">` embeds
// Just Work™. Callers who want to rename the tag should import the class and
// register themselves BEFORE loading this module, or set the tag name before
// import. This default covers the 90% case.
registerShiftSurvey();
