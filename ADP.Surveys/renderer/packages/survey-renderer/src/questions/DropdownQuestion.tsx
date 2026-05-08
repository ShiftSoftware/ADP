import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

interface Option {
  id: string;
  label?: LocalizedString;
}

export function DropdownQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer, ui } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const options = (question['options'] as Option[] | undefined) ?? [];
  const placeholder = question['placeholder'] as LocalizedString | undefined;
  const value = (answers[id] as string | undefined) ?? '';
  const placeholderText = placeholder
    ? localize(placeholder, locale, schema.defaultLocale)
    : ui.selectPlaceholder;

  return (
    <div className="survey-question survey-question--dropdown">
      <label className="survey-question__label" htmlFor={`q-${id}`}>
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </label>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <select
        id={`q-${id}`}
        className="survey-question__select"
        value={value}
        required={required}
        onChange={(e) => setAnswer(id, e.target.value || null)}
      >
        <option value="">{placeholderText}</option>
        {options.map((o) => (
          <option key={o.id} value={o.id}>
            {localize(o.label, locale, schema.defaultLocale)}
          </option>
        ))}
      </select>
    </div>
  );
}
