import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function TextQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const value = (answers[id] ?? '') as string;

  return (
    <div className="survey-question survey-question--text">
      <label className="survey-question__label" htmlFor={`q-${id}`}>
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </label>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <input
        id={`q-${id}`}
        className="survey-question__input"
        type="text"
        value={value}
        required={required}
        onChange={(e) => setAnswer(id, e.target.value)}
      />
    </div>
  );
}
