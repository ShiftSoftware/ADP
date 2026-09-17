import { createContext, useContext } from 'react';
import type { AnswerMap, LabelledOption, Survey, TokenContext } from '@shiftsoftware/survey-sdk';
import type { UiStrings } from './i18n.js';

/** Runtime state shared by every question component in the renderer tree.
 *  Kept small on purpose — extended in later slices (validation errors,
 *  postMessage bridge, localStorage resume). */
export interface SurveyContextValue {
  schema: Survey;
  locale: string;
  direction: 'ltr' | 'rtl';
  ui: UiStrings;
  answers: AnswerMap;
  setAnswer(questionId: string, value: unknown): void;
  /** Resolves `{{answers.*}}` tokens against the current answers — the copy of the
   *  screen being rendered has already been through it; sourced questions run
   *  their request fields through it right before fetching. */
  answerContext: TokenContext;
  /** Lets a sourced question report the options it fetched, so
   *  `{{answers.<id>.label}}` can name what the respondent picked from an endpoint. */
  registerSourcedOptions(questionId: string, options: readonly LabelledOption[]): void;
}

const SurveyContext = createContext<SurveyContextValue | null>(null);

export const SurveyContextProvider = SurveyContext.Provider;

export function useSurveyContext(): SurveyContextValue {
  const ctx = useContext(SurveyContext);
  if (!ctx) {
    throw new Error(
      'useSurveyContext must be used inside <SurveyRenderer>. Question components rely on survey state from the enclosing provider.',
    );
  }
  return ctx;
}
