import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function ParagraphQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const placeholder = question['placeholder'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const minLength = question['minLength'] as number | undefined;
  const maxLength = question['maxLength'] as number | undefined;
  const value = (answers[id] ?? '') as string;

  return (
    <div className="survey-question survey-question--paragraph">
      <label className="survey-question__label" htmlFor={`q-${id}`}>
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </label>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <textarea
        id={`q-${id}`}
        className="survey-question__textarea"
        value={value}
        required={required}
        rows={5}
        minLength={minLength}
        maxLength={maxLength}
        placeholder={placeholder ? localize(placeholder, locale, schema.defaultLocale) : undefined}
        onChange={(e) => setAnswer(id, e.target.value)}
      />
    </div>
  );
}
