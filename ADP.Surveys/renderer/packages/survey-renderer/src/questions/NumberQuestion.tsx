import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function NumberQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const min = question['min'] as number | undefined;
  const max = question['max'] as number | undefined;
  const step = question['step'] as number | undefined;
  const unit = question['unit'] as LocalizedString | undefined;
  const raw = answers[id];
  const value = raw === undefined || raw === null ? '' : String(raw);

  return (
    <div className="survey-question survey-question--number">
      <label className="survey-question__label" htmlFor={`q-${id}`}>
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </label>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <div className="survey-question__number-wrap">
        <input
          id={`q-${id}`}
          className="survey-question__input"
          type="number"
          value={value}
          required={required}
          min={min}
          max={max}
          step={step}
          onChange={(e) => {
            const s = e.target.value;
            setAnswer(id, s === '' ? null : Number(s));
          }}
        />
        {unit && (
          <span className="survey-question__unit">
            {localize(unit, locale, schema.defaultLocale)}
          </span>
        )}
      </div>
    </div>
  );
}
