import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function DateTimeQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const minDateTime = question['minDateTime'] as string | undefined;
  const maxDateTime = question['maxDateTime'] as string | undefined;
  const value = (answers[id] as string | undefined) ?? '';

  // `datetime-local` needs `YYYY-MM-DDTHH:mm` — if the schema/bounds include a
  // timezone suffix, strip it before passing to the input (the input is tz-naive).
  const toLocalInput = (v: string | undefined): string | undefined => {
    if (!v) return undefined;
    const match = v.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
    return match?.[1] ?? undefined;
  };

  return (
    <div className="survey-question survey-question--datetime">
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
        type="datetime-local"
        value={toLocalInput(value) ?? ''}
        required={required}
        min={toLocalInput(minDateTime)}
        max={toLocalInput(maxDateTime)}
        onChange={(e) => setAnswer(id, e.target.value || null)}
      />
    </div>
  );
}
