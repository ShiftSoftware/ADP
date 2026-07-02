import { useEffect, useState } from 'react';
import {
  buildOptionsUrl,
  fetchOptions,
  type FetchedOption,
  type LocalizedString,
  type OptionsSource,
} from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionComponent } from './registry.js';

/**
 * Fetch lifecycle for `optionsSource` questions. Renders a loading placeholder
 * while the external endpoint is in flight, an inline error + Retry on failure,
 * and the real question component with a *materialized* clone (fetched options
 * substituted in) once loaded. Fetches happen lazily — a sourced question on a
 * branch the respondent never visits is never fetched.
 *
 * The cache is session-scoped and keyed by locale + resolved URL, so revisiting
 * a screen doesn't refetch but switching locale does (endpoints localize via
 * the Accept-Language header the SDK sends).
 */

const optionsCache = new Map<string, FetchedOption[]>();

/** Test hook — clears the module-level session cache. */
export function clearSourcedOptionsCache(): void {
  optionsCache.clear();
}

type GateState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; options: FetchedOption[] };

export function SourcedOptionsGate({
  question,
  Component,
}: {
  question: Record<string, unknown>;
  Component: QuestionComponent;
}) {
  const { locale, schema, ui } = useSurveyContext();
  const source = question['optionsSource'] as OptionsSource;
  const cacheKey = `${locale}|${buildOptionsUrl(source)}`;
  const [state, setState] = useState<GateState>(() => {
    const cached = optionsCache.get(cacheKey);
    return cached ? { status: 'ready', options: cached } : { status: 'loading' };
  });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const cached = optionsCache.get(cacheKey);
    if (cached) {
      setState({ status: 'ready', options: cached });
      return;
    }
    let cancelled = false;
    setState({ status: 'loading' });
    fetchOptions(source, { locale })
      .then((options) => {
        optionsCache.set(cacheKey, options);
        if (!cancelled) setState({ status: 'ready', options });
      })
      .catch((e: unknown) => {
        if (!cancelled) setState({ status: 'error', message: (e as Error).message ?? String(e) });
      });
    return () => {
      cancelled = true;
    };
    // `source` is derived from the question object; cacheKey already encodes
    // its request-relevant parts (url + params) plus the locale.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cacheKey, attempt]);

  const title = question['title'] as LocalizedString | undefined;
  const heading = title ? (
    <span className="survey-question__label">{localize(title, locale, schema.defaultLocale)}</span>
  ) : null;

  if (state.status === 'loading') {
    return (
      <div className="survey-question survey-question--options-loading" role="status">
        {heading}
        <p className="survey-question__options-status">{ui.loadingOptions}</p>
      </div>
    );
  }

  if (state.status === 'error') {
    return (
      <div className="survey-question survey-question--options-error">
        {heading}
        <p className="survey-question__options-status" role="alert">
          {ui.optionsLoadError}
        </p>
        <button
          type="button"
          className="survey-button survey-button--retry"
          onClick={() => setAttempt((n) => n + 1)}
        >
          {ui.retry}
        </button>
      </div>
    );
  }

  // Materialize: hand the ordinary component an inline-shaped clone. Labels are
  // wrapped as single-locale LocalizedStrings ({ [locale]: label }) since the
  // endpoint already localized via Accept-Language; navigationList options all
  // share the source's single nextScreen.
  const isNavigationList = question['type'] === 'navigationList';
  const materialized: Record<string, unknown> = {
    ...question,
    options: state.options.map((o) => ({
      id: o.id,
      label: { [locale]: o.label },
      ...(isNavigationList && source.nextScreen ? { nextScreen: source.nextScreen } : {}),
    })),
  };
  return <Component question={materialized} />;
}
