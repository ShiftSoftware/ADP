import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type {
  AnswerMap,
  LocalizedString,
  NavigationOption,
  Survey,
  SurveySubmission,
  SubmissionMeta,
} from '@shiftsoftware/survey-sdk';
import { computeNext, resolveNavigationListTarget } from '@shiftsoftware/survey-sdk';
import { SurveyContextProvider } from './SurveyContext.js';
import { localize } from './locale.js';
import { resolveLocaleConfig, type LocaleConfig } from './i18n.js';
import { createHostBridge, type HostBridge } from './postMessage.js';
import {
  clearResumeState,
  loadResumeState,
  saveResumeState,
  type ResumeStorage,
} from './resume.js';
import { QuestionHost } from './questions/QuestionHost.js';
import type { QuestionRegistry } from './questions/registry.js';
import { TextQuestion } from './questions/TextQuestion.js';
import { NpsQuestion } from './questions/NpsQuestion.js';
import {
  NavigationListQuestion,
  type NavigationListOptionSelectedDetail,
} from './questions/NavigationListQuestion.js';
import { ParagraphQuestion } from './questions/ParagraphQuestion.js';
import { NumberQuestion } from './questions/NumberQuestion.js';
import { RatingQuestion } from './questions/RatingQuestion.js';
import { SingleChoiceQuestion } from './questions/SingleChoiceQuestion.js';
import { MultiChoiceQuestion } from './questions/MultiChoiceQuestion.js';
import { DropdownQuestion } from './questions/DropdownQuestion.js';
import { DateQuestion } from './questions/DateQuestion.js';
import { DateTimeQuestion } from './questions/DateTimeQuestion.js';
import { FileQuestion } from './questions/FileQuestion.js';
import { SignatureQuestion } from './questions/SignatureQuestion.js';
import { YesNoQuestion } from './questions/YesNoQuestion.js';

/** Built-in registry — callers can override or extend via the `registry` prop.
 *  Add a new question type by landing a component + adding it here. */
export const defaultRegistry: QuestionRegistry = {
  text: TextQuestion,
  paragraph: ParagraphQuestion,
  number: NumberQuestion,
  rating: RatingQuestion,
  nps: NpsQuestion,
  singleChoice: SingleChoiceQuestion,
  multiChoice: MultiChoiceQuestion,
  dropdown: DropdownQuestion,
  date: DateQuestion,
  dateTime: DateTimeQuestion,
  file: FileQuestion,
  signature: SignatureQuestion,
  yesNo: YesNoQuestion,
  navigationList: NavigationListQuestion,
};

export interface SurveyRendererProps {
  schema: Survey;
  onSubmit(submission: SurveySubmission): Promise<void> | void;
  /** Seed answers — used by the resume flow (localStorage) and by builder preview. */
  initialAnswers?: AnswerMap;
  /** Override the schema's `defaultLocale` — typically from a route param or UI control. */
  locale?: string;
  /** Called whenever the active screen changes. Used later as the
   *  `postMessage('survey:screen-changed')` source for iframe embeds. */
  onScreenChange?: (screenId: string | null) => void;
  /** Override or extend the built-in question registry. */
  registry?: QuestionRegistry;
  /** Called when the user completes the survey. Receives the final screen id
   *  (if any) so hosts can render branch-specific confirmations. */
  onCompleted?: (screenId: string | null) => void;
  /** Meta to stamp on the submission (startedAt, agentId, etc.). `completedAt`
   *  is filled in automatically if missing. */
  submissionMeta?: SubmissionMeta;
  /** Extend or override the built-in UI-chrome locales (`en`, `ar`). Each entry
   *  must be a full `LocaleConfig` — merging is shallow at the locale level. */
  uiLocales?: Record<string, LocaleConfig>;
  /** Opaque key that scopes localStorage-backed resume state. Pass the survey's
   *  `publicId` for the standalone app path. When unset, no persistence. */
  resumeKey?: string;
  /** Storage adapter for resume state. Defaults to `globalThis.localStorage`;
   *  pass a mock in tests or `sessionStorage` for tab-scoped persistence. */
  storage?: ResumeStorage;
  /** Force-enable or force-disable the iframe host postMessage bridge.
   *  Default: auto-detect (enabled iff running inside an iframe). */
  emitHostMessages?: boolean;
  /** Target origin for postMessage events. Default `'*'` — tighten per
   *  deployment once the host origin is known. */
  hostMessageOrigin?: string;
  /** Override the host-message `target` (for tests). Default `window.parent`. */
  hostMessageTarget?: Window | null;
}

export function SurveyRenderer({
  schema,
  onSubmit,
  initialAnswers,
  locale,
  onScreenChange,
  onCompleted,
  registry,
  submissionMeta,
  uiLocales,
  resumeKey,
  storage,
  emitHostMessages,
  hostMessageOrigin,
  hostMessageTarget,
}: SurveyRendererProps) {
  const effectiveLocale = locale ?? schema.defaultLocale ?? 'en';
  const effectiveRegistry = registry ?? defaultRegistry;
  const localeConfig = useMemo(
    () => resolveLocaleConfig(effectiveLocale, schema.defaultLocale, uiLocales),
    [effectiveLocale, schema.defaultLocale, uiLocales],
  );

  // Resume state is resolved once on mount — it seeds both `answers` and
  // `currentScreenId` so there's no flash of empty state. Schema changes are
  // rare (version is pinned to an instance) so we don't try to reconcile.
  const effectiveStorage =
    storage ?? (typeof globalThis !== 'undefined' ? (globalThis.localStorage as ResumeStorage | undefined) : undefined);
  const resumeSnapshot = useMemo(() => {
    if (!resumeKey || !effectiveStorage) return null;
    const snap = loadResumeState(effectiveStorage, resumeKey);
    if (!snap) return null;
    // Guard against a saved screen id that's no longer in the schema.
    const screenStillExists =
      snap.currentScreenId === null || schema.screens.some((s) => s.id === snap.currentScreenId);
    return screenStillExists ? snap : { ...snap, currentScreenId: schema.screens[0]?.id ?? null };
    // Intentionally scoped to mount — we don't restore mid-session if the key
    // changes underneath us.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const [answers, setAnswers] = useState<AnswerMap>(() => ({
    ...(initialAnswers ?? {}),
    ...(resumeSnapshot?.answers ?? {}),
  }));
  const [currentScreenId, setCurrentScreenId] = useState<string | null>(
    () => resumeSnapshot?.currentScreenId ?? schema.screens[0]?.id ?? null,
  );
  const [submitting, setSubmitting] = useState(false);
  const [submissionError, setSubmissionError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const startedAtRef = useRef<string>(new Date().toISOString());

  // Host bridge — iframe embed protocol. Created once per mount; the
  // `emitHostMessages` / `hostMessageOrigin` / `hostMessageTarget` props let
  // callers override the defaults for tests and non-iframe hosts.
  const hostBridgeRef = useRef<HostBridge | null>(null);
  if (hostBridgeRef.current === null) {
    const bridgeOptions: Parameters<typeof createHostBridge>[0] = {};
    if (hostMessageTarget !== undefined) bridgeOptions.target = hostMessageTarget;
    if (hostMessageOrigin !== undefined) bridgeOptions.targetOrigin = hostMessageOrigin;
    if (emitHostMessages !== undefined) bridgeOptions.enabled = emitHostMessages;
    hostBridgeRef.current = createHostBridge(bridgeOptions);
  }

  // Resolve the current screen from the schema every render — keeps us aligned
  // with live schema updates from builder preview (Phase 4).
  const currentScreen = useMemo(
    () => (currentScreenId ? schema.screens.find((s) => s.id === currentScreenId) ?? null : null),
    [schema, currentScreenId],
  );

  // Emit screen-change events after every transition (both to the callback and
  // to the iframe host bridge, if enabled).
  useEffect(() => {
    onScreenChange?.(currentScreenId);
    hostBridgeRef.current?.screenChanged(currentScreenId);
  }, [currentScreenId, onScreenChange]);

  // Emit a single `survey:loaded` when we've mounted + have a first screen.
  const hasEmittedLoadedRef = useRef(false);
  useEffect(() => {
    if (hasEmittedLoadedRef.current || !currentScreenId) return;
    hasEmittedLoadedRef.current = true;
    hostBridgeRef.current?.loaded();
  }, [currentScreenId]);

  // Persist resume state on every answer / screen change. Cleared on done.
  useEffect(() => {
    if (!resumeKey || !effectiveStorage || done) return;
    saveResumeState(effectiveStorage, resumeKey, {
      answers,
      currentScreenId,
      schemaVersion: schema.version,
    });
  }, [answers, currentScreenId, resumeKey, effectiveStorage, done, schema.version]);

  useEffect(() => {
    if (done && resumeKey && effectiveStorage) {
      clearResumeState(effectiveStorage, resumeKey);
    }
  }, [done, resumeKey, effectiveStorage]);

  // Surface submission errors to the host bridge.
  useEffect(() => {
    if (submissionError) hostBridgeRef.current?.error(submissionError);
  }, [submissionError]);

  const setAnswer = useCallback((questionId: string, value: unknown) => {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
  }, []);

  const goTo = useCallback(
    (screenId: string | null) => {
      if (screenId === null) return;
      setCurrentScreenId(screenId);
    },
    [],
  );

  const finishSurvey = useCallback(async () => {
    setSubmitting(true);
    setSubmissionError(null);
    try {
      await onSubmit({
        schemaVersion: schema.version ?? 0,
        answers,
        meta: {
          startedAt: submissionMeta?.startedAt ?? startedAtRef.current,
          completedAt: submissionMeta?.completedAt ?? new Date().toISOString(),
          ...(submissionMeta ?? {}),
        },
      });
      setDone(true);
      onCompleted?.(currentScreenId);
      hostBridgeRef.current?.completed({ screenId: currentScreenId, answers });
    } catch (e) {
      setSubmissionError((e as Error).message ?? String(e));
    } finally {
      setSubmitting(false);
    }
  }, [schema.version, answers, submissionMeta, onSubmit, onCompleted, currentScreenId]);

  const advance = useCallback(() => {
    if (!currentScreenId) return;
    const step = computeNext(schema, currentScreenId, answers);
    if (step.kind === 'end') {
      void finishSurvey();
    } else {
      goTo(step.screenId);
    }
  }, [schema, currentScreenId, answers, goTo, finishSurvey]);

  // Auto-submit on arrival at a zero-question terminal screen — authors design
  // multiple thank-you pages per branch, and forcing the user to press Next once
  // more on a thanks screen is awkward UX. The screen's content still renders
  // via the `done` state once submission completes. Guard with `done` + a ref so
  // a re-render or a second arrival doesn't double-submit.
  const autoSubmittedFor = useRef<string | null>(null);
  useEffect(() => {
    if (done || submitting || !currentScreenId || !currentScreen) return;
    if (autoSubmittedFor.current === currentScreenId) return;
    const isZeroQ =
      !currentScreen.questions ||
      (currentScreen.questions as unknown[]).length === 0;
    if (!isZeroQ) return;
    const step = computeNext(schema, currentScreenId, answers);
    if (step.kind === 'end') {
      autoSubmittedFor.current = currentScreenId;
      void finishSurvey();
    }
  }, [currentScreenId, currentScreen, done, submitting, schema, answers, finishSurvey]);

  // NavigationList bridge — listen for selections bubbling up from a navigationList
  // option and route using the SDK's resolver. Scoped to this renderer instance via
  // a ref-captured element so multiple <SurveyRenderer>s can coexist on a page.
  const rootRef = useRef<HTMLDivElement>(null);

  // ResizeObserver → survey:resize for iframe auto-sizing. Safe no-op in test
  // environments that don't polyfill it.
  useEffect(() => {
    const el = rootRef.current;
    if (!el || typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) return;
      hostBridgeRef.current?.resize(Math.ceil(entry.contentRect.height));
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, []);
  useEffect(() => {
    const el = rootRef.current;
    if (!el) return;
    const handler = (e: Event) => {
      const detail = (e as CustomEvent<NavigationListOptionSelectedDetail>).detail;
      if (!detail || !currentScreenId) return;
      setAnswer(detail.questionId, detail.option.id);
      // Compute using the updated answer map (functional) — setAnswer is async.
      const nextAnswers = { ...answers, [detail.questionId]: detail.option.id };
      const step = resolveNavigationListTarget(
        detail.option as NavigationOption,
        schema,
        currentScreenId,
        nextAnswers,
      );
      if (step.kind === 'end') void finishSurvey();
      else goTo(step.screenId);
    };
    el.addEventListener('survey:navigationListSelect', handler as EventListener);
    return () => el.removeEventListener('survey:navigationListSelect', handler as EventListener);
  }, [answers, currentScreenId, schema, setAnswer, goTo, finishSurvey]);

  const contextValue = useMemo(
    () => ({
      schema,
      locale: effectiveLocale,
      direction: localeConfig.direction,
      ui: localeConfig.strings,
      answers,
      setAnswer,
    }),
    [schema, effectiveLocale, localeConfig, answers, setAnswer],
  );

  if (done) {
    return (
      <div
        ref={rootRef}
        className="survey-root survey-root--done"
        dir={localeConfig.direction}
        lang={effectiveLocale}
      >
        <div className="survey-screen">
          <h2 className="survey-screen__title">
            {currentScreen?.title
              ? localize(currentScreen.title as LocalizedString, effectiveLocale, schema.defaultLocale)
              : localeConfig.strings.thankYou}
          </h2>
          {currentScreen?.description && (
            <p className="survey-screen__description">
              {localize(currentScreen.description as LocalizedString, effectiveLocale, schema.defaultLocale)}
            </p>
          )}
        </div>
      </div>
    );
  }

  if (!currentScreen) {
    return (
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale}>
        <div className="survey-screen"><em>{localeConfig.strings.noScreens}</em></div>
      </div>
    );
  }

  const questions = (currentScreen.questions as Array<Record<string, unknown>> | undefined) ?? [];
  // The Next button is hidden when the terminal question of the screen is a
  // navigationList — per Phase 3 Part B.1 the tap IS the transition.
  const hasTerminalNavList =
    questions.length > 0 &&
    (questions[questions.length - 1]?.['type'] as string | undefined) === 'navigationList';
  // Also hide it on zero-question screens that will auto-submit on arrival —
  // otherwise the user sees "Next" for the moment before the submission completes,
  // which is confusing UX.
  const isAutoSubmitScreen = questions.length === 0 && !currentScreen.nextScreen;
  const showNextButton = !hasTerminalNavList && !isAutoSubmitScreen;

  return (
    <SurveyContextProvider value={contextValue}>
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale}>
        <div className="survey-screen">
          {currentScreen.title && (
            <h2 className="survey-screen__title">
              {localize(currentScreen.title as LocalizedString, effectiveLocale, schema.defaultLocale)}
            </h2>
          )}
          {currentScreen.description && (
            <p className="survey-screen__description">
              {localize(currentScreen.description as LocalizedString, effectiveLocale, schema.defaultLocale)}
            </p>
          )}
          <div className="survey-screen__questions">
            {questions.map((q, idx) => (
              <QuestionHost key={(q['id'] as string | undefined) ?? idx} question={q} registry={effectiveRegistry} />
            ))}
          </div>
          {showNextButton && (
            <div className="survey-screen__actions">
              <button
                type="button"
                className="survey-button survey-button--primary"
                disabled={submitting}
                onClick={advance}
              >
                {submitting ? localeConfig.strings.submitting : localeConfig.strings.next}
              </button>
            </div>
          )}
          {submissionError && (
            <p className="survey-screen__error" role="alert">
              {localeConfig.strings.couldNotSubmit} {submissionError}
            </p>
          )}
        </div>
      </div>
    </SurveyContextProvider>
  );
}
