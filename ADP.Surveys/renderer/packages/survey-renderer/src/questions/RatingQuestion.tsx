import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

/** Star rating from 1..max. `allowHalf` support is deferred — the first slice ships
 *  integer ratings only; half-steps require a finer pointer surface that'd bloat
 *  this component. Authors who need halves can fall back to Nps with a smaller max. */
export function RatingQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const max = Number(question['max'] ?? 5);
  const selected = answers[id];

  const stars: number[] = [];
  for (let i = 1; i <= max; i++) stars.push(i);

  return (
    <fieldset className="survey-question survey-question--rating">
      <legend className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </legend>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <div className="survey-question__rating-scale" role="radiogroup">
        {stars.map((n) => {
          const isSelected = typeof selected === 'number' && n <= selected;
          return (
            <button
              type="button"
              role="radio"
              aria-checked={selected === n}
              aria-label={`${n}`}
              key={n}
              className={
                'survey-question__rating-star' +
                (isSelected ? ' survey-question__rating-star--selected' : '')
              }
              onClick={() => setAnswer(id, n)}
            >
              <span aria-hidden="true">★</span>
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}
