import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type {
  AnswerMap,
  LocalizedString,
  NavigationOption,
  Survey,
  SurveySubmission,
  SubmissionMeta,
} from '@shiftsoftware/survey-sdk';
import {
  computeNext,
  resolveNavigationListTarget,
  validateAnswerValue,
  validatePresentAnswers,
  type AnswerValidationError,
} from '@shiftsoftware/survey-sdk';
import { brandingToCssVars } from './branding.js';
import { SurveyContextProvider } from './SurveyContext.js';
import { localize } from './locale.js';
import { resolveLocaleConfig, formatUi, type LocaleConfig, type UiStrings } from './i18n.js';
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

/** Map a constraint-validation code onto the locale's message templates.
 *  Codes without a dedicated template fall back to the generic string —
 *  they're type-shape errors the widgets themselves normally prevent. */
function localizeConstraintError(error: AnswerValidationError, ui: UiStrings): string {
  switch (error.code) {
    case 'minLength':
      return formatUi(ui.minLengthError, error.params);
    case 'maxLength':
      return formatUi(ui.maxLengthError, error.params);
    case 'pattern':
      return ui.patternError;
    case 'min':
      return formatUi(ui.minError, error.params);
    case 'max':
      return formatUi(ui.maxError, error.params);
    case 'range':
      return formatUi(ui.rangeError, error.params);
    case 'minSelected':
      return formatUi(ui.minSelectedError, error.params);
    case 'maxSelected':
      return formatUi(ui.maxSelectedError, error.params);
    default:
      return ui.invalidAnswerError;
  }
}

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
  /** Builder-preview hook: when this prop changes to a screen id that exists
   *  in the schema, the renderer jumps there (answer state preserved). A
   *  "jump signal", not a controlled value — the user can still navigate
   *  freely afterwards. `undefined` = feature unused. */
  activeScreenId?: string | null;
}

/** Mirrors the arrival-time auto-submit rule (zero questions + computeNext →
 *  'end'): a Next press that routes here commits the survey, so the button
 *  that triggers the hop must already read "Submit". */
function screenAutoSubmitsOnArrival(schema: Survey, screenId: string, answers: AnswerMap): boolean {
  const screen = schema.screens.find((s) => s.id === screenId);
  if (!screen) return false;
  const questions = (screen.questions as unknown[] | undefined) ?? [];
  if (questions.length > 0) return false;
  return computeNext(schema, screenId, answers).kind === 'end';
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
  activeScreenId,
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

  // Reconcile currentScreenId against the schema whenever the schema changes.
  // The lazy useState above seeds this once on mount; in the builder-preview
  // path the renderer can mount with `schema.screens === []` (e.g. an author
  // who sets locales before adding any screen), leaving currentScreenId
  // permanently null. When screens later appear, snap to the first one so the
  // renderer leaves the "No screens in this survey" branch. Also handles a
  // screen deletion mid-flow: if the active screen id no longer exists, fall
  // back to the first available rather than stranding the user.
  useEffect(() => {
    if (schema.screens.length === 0) {
      if (currentScreenId !== null) setCurrentScreenId(null);
      return;
    }
    const stillValid =
      currentScreenId !== null && schema.screens.some((s) => s.id === currentScreenId);
    if (!stillValid) {
      setCurrentScreenId(schema.screens[0]!.id);
    }
  }, [schema, currentScreenId]);
  const [submitting, setSubmitting] = useState(false);
  const [submissionError, setSubmissionError] = useState<string | null>(null);
  // Required questions the user tried to Next past on the current screen.
  // Flagged ids render an inline error; the flag set resets on navigation and
  // each entry hides itself as soon as its question gains an answer.
  const [requiredFlags, setRequiredFlags] = useState<ReadonlySet<string>>(new Set());
  // Same mechanics for per-type constraint violations (min/max/length/pattern —
  // the client-side AnswerValidator mirror). Flag on Next, re-validate live at
  // render so the error clears the moment the answer becomes valid.
  const [constraintFlags, setConstraintFlags] = useState<ReadonlySet<string>>(new Set());
  const [done, setDone] = useState(false);

  // Builder-preview jump: when the host pushes a distinct activeScreenId,
  // snap to it. Ref-guarded so each push fires exactly once — re-renders that
  // carry the same prop value don't fight the user's own navigation.
  const lastAppliedJumpRef = useRef<string | null | undefined>(undefined);
  useEffect(() => {
    if (activeScreenId === undefined) return;
    if (lastAppliedJumpRef.current === activeScreenId) return;
    lastAppliedJumpRef.current = activeScreenId;
    if (activeScreenId === null || done) return;
    if (!schema.screens.some((s) => s.id === activeScreenId)) return;
    setRequiredFlags(new Set());
    setCurrentScreenId(activeScreenId);
  }, [activeScreenId, schema, done]);
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
      setRequiredFlags(new Set());
      setConstraintFlags(new Set());
      setCurrentScreenId(screenId);
    },
    [],
  );

  /** A required question with no usable answer. Mirrors the server's presence
   *  check but is stricter on empties — '' and [] count as missing here. */
  const isAnswerMissing = useCallback(
    (question: Record<string, unknown>): boolean => {
      if (!question['required']) return false;
      const value = answers[question['id'] as string];
      if (value === undefined || value === null) return true;
      if (typeof value === 'string' && value.trim() === '') return true;
      if (Array.isArray(value) && value.length === 0) return true;
      return false;
    },
    [answers],
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
    // Required gate — block Next while the current screen has unanswered
    // required questions. The server enforces the same rule path-aware at
    // submit time; gating here surfaces it on the screen where it's fixable.
    const screen = schema.screens.find((s) => s.id === currentScreenId);
    const screenQuestions = (screen?.questions as Array<Record<string, unknown>> | undefined) ?? [];
    const missing = screenQuestions.filter(isAnswerMissing).map((q) => q['id'] as string);
    if (missing.length > 0) {
      setRequiredFlags(new Set(missing));
      return;
    }
    // Constraint gate — same server rules (AnswerValidator mirror), surfaced
    // inline on the screen where they're fixable instead of a 400 at submit.
    const constraintErrors = validatePresentAnswers(screen?.questions, answers);
    if (constraintErrors.length > 0) {
      setConstraintFlags(new Set(constraintErrors.map((e) => e.questionId)));
      return;
    }
    const step = computeNext(schema, currentScreenId, answers);
    if (step.kind === 'end') {
      void finishSurvey();
    } else {
      goTo(step.screenId);
    }
  }, [schema, currentScreenId, answers, isAnswerMissing, goTo, finishSurvey]);

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

  // Branding → CSS variable overrides on the root + optional logo header.
  // Served pre-merged by the API (deployment default ⊕ per-survey override).
  const brandStyle = useMemo(() => brandingToCssVars(schema.branding), [schema.branding]);
  const brandLogo = schema.branding?.logoUrl ? (
    <div className="survey-brand">
      <img
        className="survey-brand__logo"
        src={schema.branding.logoUrl}
        alt=""
        // A dead logo URL must degrade to "no logo", not a broken-image glyph.
        onError={(e) => {
          (e.currentTarget.parentElement as HTMLElement).style.display = 'none';
        }}
      />
    </div>
  ) : null;

  if (done) {
    return (
      <div
        ref={rootRef}
        className="survey-root survey-root--done"
        dir={localeConfig.direction}
        lang={effectiveLocale}
        style={brandStyle}
      >
        {brandLogo}
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
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale} style={brandStyle}>
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
  // The press that ends the survey is labeled Submit, not Next — respondents
  // otherwise can't tell which press submits. Two shapes end it: computeNext
  // says 'end' outright, or it routes to a zero-question screen that will
  // auto-submit on arrival (the per-branch thank-you pattern) — from the
  // respondent's seat both are the committing press. Answer-aware via
  // computeNext so the label stays correct on branching flows.
  const nextStep =
    showNextButton && currentScreenId !== null ? computeNext(schema, currentScreenId, answers) : null;
  const nextWouldEnd =
    nextStep !== null &&
    (nextStep.kind === 'end' ||
      (nextStep.kind === 'screen' && screenAutoSubmitsOnArrival(schema, nextStep.screenId, answers)));

  return (
    <SurveyContextProvider value={contextValue}>
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale} style={brandStyle}>
        {brandLogo}
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
            {questions.map((q, idx) => {
              const qid = q['id'] as string | undefined;
              // The flag self-clears visually once the question gains an answer —
              // no bookkeeping on answer change, just re-evaluate at render.
              const flagged = qid !== undefined && requiredFlags.has(qid) && isAnswerMissing(q);
              // Constraint flags re-validate live so the error disappears the
              // moment the value becomes valid. Required takes precedence.
              const constraintError =
                !flagged && qid !== undefined && constraintFlags.has(qid) && answers[qid] != null
                  ? validateAnswerValue(q, answers[qid])[0] ?? null
                  : null;
              const invalid = flagged || constraintError !== null;
              return (
                <div key={qid ?? idx} className={invalid ? 'survey-question-slot survey-question-slot--invalid' : 'survey-question-slot'}>
                  <QuestionHost question={q} registry={effectiveRegistry} />
                  {flagged && (
                    <p className="survey-question__required-error" role="alert">
                      {localeConfig.strings.requiredError}
                    </p>
                  )}
                  {constraintError && (
                    <p className="survey-question__required-error" role="alert">
                      {localizeConstraintError(constraintError, localeConfig.strings)}
                    </p>
                  )}
                </div>
              );
            })}
          </div>
          {showNextButton && (
            <div className="survey-screen__actions">
              <button
                type="button"
                className="survey-button survey-button--primary"
                disabled={submitting}
                onClick={advance}
              >
                {submitting
                  ? localeConfig.strings.submitting
                  : nextWouldEnd
                    ? localeConfig.strings.submit
                    : localeConfig.strings.next}
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
