import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

interface Option {
  id: string;
  label?: LocalizedString;
}

export function SingleChoiceQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const options = (question['options'] as Option[] | undefined) ?? [];
  const selected = answers[id] as string | undefined;

  return (
    <fieldset className="survey-question survey-question--single">
      <legend className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </legend>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <div className="survey-question__options">
        {options.map((o) => (
          <label key={o.id} className="survey-question__option">
            <input
              type="radio"
              name={`q-${id}`}
              value={o.id}
              checked={selected === o.id}
              onChange={() => setAnswer(id, o.id)}
            />
            <span>{localize(o.label, locale, schema.defaultLocale)}</span>
          </label>
        ))}
      </div>
    </fieldset>
  );
}
