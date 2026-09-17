import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type {
  AnswerMap,
  LabelledOption,
  LocalizedString,
  NavigationOption,
  Survey,
  SurveySubmission,
  SubmissionMeta,
} from '@shiftsoftware/survey-sdk';
import {
  computeNext,
  replayPathTo,
  createAnswerContext,
  personalizeScreen,
  resolveNavigationListTarget,
  validateAnswerValue,
  validatePresentAnswers,
  type AnswerValidationError,
} from '@shiftsoftware/survey-sdk';
import { brandingToCssVars } from './branding.js';
import { SurveyContextProvider } from './SurveyContext.js';
import { localize } from './locale.js';
import {
  resolveLocaleConfig,
  formatUi,
  localeDisplayName,
  type LocaleConfig,
  type UiStrings,
} from './i18n.js';
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
  /** Override the schema's `defaultLocale` — typically from a route param or UI control.
   *  Treated as the STARTING locale, not a lock: when the schema declares more than one
   *  locale the respondent can switch, and a later change to this prop re-seeds their
   *  choice. Pass `showLocalePicker={false}` to keep the locale fixed. */
  locale?: string;
  /** Called when the respondent picks a different language. Lets a host persist the
   *  choice or mirror it into its own chrome; the renderer switches either way. */
  onLocaleChange?: (locale: string) => void;
  /** Force the language picker on or off. Default: shown whenever the schema declares
   *  more than one locale, since a multi-lingual survey with no way to switch strands
   *  every respondent whose language isn't the default. */
  showLocalePicker?: boolean;
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

  /** Changes to re-issue a jump to the SAME `activeScreenId`. Without it the jump is
   *  once-per-distinct-id: an author who walked the preview forward, then re-clicked the
   *  screen they were already on in the editor, got nothing. Any changing value works —
   *  the host just needs it to differ per request. */
  activeScreenJumpToken?: number;
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
  onLocaleChange,
  showLocalePicker,
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
  activeScreenJumpToken,
}: SurveyRendererProps) {
  // The respondent's own choice, once they make one. Null means "follow the host":
  // the `locale` prop, else the schema's default. Keeping it null until they pick
  // is what lets a host re-seed the locale (builder preview pushing a new one, a
  // route param changing) without fighting a choice nobody made.
  const [pickedLocale, setPickedLocale] = useState<string | null>(null);
  const hostLocale = locale ?? schema.defaultLocale ?? 'en';

  // A picked locale the schema no longer declares (schema swapped underneath us in
  // the builder preview) must not strand the respondent on a language with no copy.
  const pickedIsValid =
    pickedLocale !== null &&
    (schema.locales?.includes(pickedLocale) ?? false);
  const effectiveLocale = pickedIsValid ? pickedLocale! : hostLocale;

  // Re-seed on a genuine host change, so `locale` still behaves like a control the
  // host owns until the respondent overrides it.
  const lastHostLocaleRef = useRef(hostLocale);
  useEffect(() => {
    if (lastHostLocaleRef.current === hostLocale) return;
    lastHostLocaleRef.current = hostLocale;
    setPickedLocale(null);
  }, [hostLocale]);

  const effectiveRegistry = registry ?? defaultRegistry;
  const localeConfig = useMemo(
    () => resolveLocaleConfig(effectiveLocale, schema.defaultLocale, uiLocales),
    [effectiveLocale, schema.defaultLocale, uiLocales],
  );

  const offeredLocales = schema.locales ?? [];
  const localePickerVisible = showLocalePicker ?? offeredLocales.length > 1;

  const handleLocaleChange = useCallback(
    (next: string) => {
      setPickedLocale(next);
      onLocaleChange?.(next);
    },
    [onLocaleChange],
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
    if (!screenStillExists) return { ...snap, currentScreenId: schema.screens[0]?.id ?? null, history: [] };
    // State saved before histories were persisted: replay the walk from the
    // answers so Back (and the on-path answer set) still work after the upgrade.
    if (!snap.history && snap.currentScreenId) {
      return { ...snap, history: replayPathTo(schema, snap.answers, snap.currentScreenId) ?? [] };
    }
    return snap;
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
  // The screens the respondent came through to reach the current one, oldest
  // first. Back pops it; forward navigation pushes the screen being left. It is
  // also what defines the respondent's PATH: answers on screens that are neither
  // in it nor current are parked — kept so the input is still filled if the
  // respondent walks that way again, but invisible to routing, tokens and the
  // submission (see `effectiveAnswers`).
  const [history, setHistory] = useState<readonly string[]>(() => resumeSnapshot?.history ?? []);

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

  // Builder-preview jump: when the host pushes a new jump request, snap to it.
  // Ref-guarded so each request fires exactly once — re-renders carrying the same
  // values don't fight the user's own navigation. The guard key includes the token
  // so the host can re-request the screen the preview is already sitting on.
  const lastAppliedJumpRef = useRef<string | undefined>(undefined);
  useEffect(() => {
    if (activeScreenId === undefined) return;
    const jumpKey = `${activeScreenJumpToken ?? ''}:${activeScreenId ?? ''}`;
    if (lastAppliedJumpRef.current === jumpKey) return;
    lastAppliedJumpRef.current = jumpKey;
    if (activeScreenId === null || done) return;
    if (!schema.screens.some((s) => s.id === activeScreenId)) return;
    setRequiredFlags(new Set());
    setConstraintFlags(new Set());
    // A jump to a screen already walked is a rewind to it — the screens after
    // it leave the path, exactly as if Back had been pressed that many times.
    // Any other target is a forward hop from wherever the preview is sitting.
    setHistory((prev) => {
      const walked = prev.indexOf(activeScreenId);
      if (walked >= 0) return prev.slice(0, walked);
      return currentScreenId && currentScreenId !== activeScreenId ? [...prev, currentScreenId] : prev;
    });
    setCurrentScreenId(activeScreenId);
  }, [activeScreenId, activeScreenJumpToken, schema, done, currentScreenId]);
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

  // Which screen each question lives on, for the path filter below.
  const screenOfQuestion = useMemo(() => {
    const map = new Map<string, string>();
    for (const screen of schema.screens) {
      for (const q of (screen.questions ?? []) as Array<Record<string, unknown>>) {
        const id = q['id'];
        if (typeof id === 'string') map.set(id, screen.id);
      }
    }
    return map;
  }, [schema]);

  // The answers on the respondent's path: history plus the current screen. An
  // answer given on a branch the respondent then backed out of is parked, not
  // deleted — it must not route a logic rule, fill a token or reach the server,
  // but it should still be in the input if they come back that way. Answers to
  // ids no screen declares (host-seeded values) are never on a wrong path.
  const effectiveAnswers = useMemo<AnswerMap>(() => {
    const onPath = new Set(history);
    if (currentScreenId) onPath.add(currentScreenId);
    const kept: AnswerMap = {};
    for (const [questionId, value] of Object.entries(answers)) {
      const screenId = screenOfQuestion.get(questionId);
      if (screenId === undefined || onPath.has(screenId)) kept[questionId] = value;
    }
    return kept;
  }, [answers, history, currentScreenId, screenOfQuestion]);

  // Options fetched by sourced questions, kept per renderer instance so
  // `{{answers.<id>.label}}` can name a pick from an endpoint even after that
  // question's screen has unmounted. A version counter turns a new registration
  // into a re-render — the map itself is a ref so registering never loops.
  const sourcedOptionsRef = useRef(new Map<string, readonly LabelledOption[]>());
  const [sourcedVersion, setSourcedVersion] = useState(0);
  const registerSourcedOptions = useCallback((questionId: string, options: readonly LabelledOption[]) => {
    if (sourcedOptionsRef.current.get(questionId) === options) return;
    sourcedOptionsRef.current.set(questionId, options);
    setSourcedVersion((n) => n + 1);
  }, []);

  // `{{answers.*}}` resolution for the copy on screen and for sourced requests.
  // Rebuilt whenever an answer changes, so a same-screen reference updates as the
  // respondent types.
  const answerContext = useMemo(
    () =>
      createAnswerContext({
        schema,
        answers: effectiveAnswers,
        lookupOptions: (questionId) => sourcedOptionsRef.current.get(questionId),
        yesNoLabels: { yes: localeConfig.strings.yes, no: localeConfig.strings.no },
      }),
    // sourcedVersion is the change signal for the ref-held map.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [schema, effectiveAnswers, localeConfig.strings.yes, localeConfig.strings.no, sourcedVersion],
  );

  // What actually renders: the current screen with answer tokens filled into its
  // copy. Navigation, validation and auto-submit keep using `currentScreen` — ids
  // and structure are identical, only display text differs.
  const shownScreen = useMemo(
    () => (currentScreen ? personalizeScreen(currentScreen, answerContext) : null),
    [currentScreen, answerContext],
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
      history: [...history],
      schemaVersion: schema.version,
    });
  }, [answers, currentScreenId, history, resumeKey, effectiveStorage, done, schema.version]);

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
      // A self-target (an option pointing at its own screen) must not leave a
      // Back that goes nowhere.
      if (currentScreenId && currentScreenId !== screenId) {
        setHistory((prev) => [...prev, currentScreenId]);
      }
      setCurrentScreenId(screenId);
    },
    [currentScreenId],
  );

  // Back returns to the most recent screen that still exists — a screen the
  // builder deleted mid-preview is skipped rather than snapping to the start.
  // Nothing is validated on the way out: the respondent is leaving to change
  // something, and their answers on this screen stay parked for their return.
  const goBack = useCallback(() => {
    let at = history.length - 1;
    while (at >= 0 && !schema.screens.some((s) => s.id === history[at])) at--;
    if (at < 0) return;
    const previous = history[at]!;
    setRequiredFlags(new Set());
    setConstraintFlags(new Set());
    setSubmissionError(null);
    setHistory(history.slice(0, at));
    setCurrentScreenId(previous);
  }, [history, schema]);
  const canGoBack = history.some((id) => schema.screens.some((s) => s.id === id));

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
        answers: effectiveAnswers,
        meta: {
          startedAt: submissionMeta?.startedAt ?? startedAtRef.current,
          completedAt: submissionMeta?.completedAt ?? new Date().toISOString(),
          ...(submissionMeta ?? {}),
        },
      });
      setDone(true);
      onCompleted?.(currentScreenId);
      hostBridgeRef.current?.completed({ screenId: currentScreenId, answers: effectiveAnswers });
    } catch (e) {
      setSubmissionError((e as Error).message ?? String(e));
    } finally {
      setSubmitting(false);
    }
  }, [schema.version, effectiveAnswers, submissionMeta, onSubmit, onCompleted, currentScreenId]);

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
    const step = computeNext(schema, currentScreenId, effectiveAnswers);
    if (step.kind === 'end') {
      void finishSurvey();
    } else {
      goTo(step.screenId);
    }
  }, [schema, currentScreenId, answers, effectiveAnswers, isAnswerMissing, goTo, finishSurvey]);

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
    const step = computeNext(schema, currentScreenId, effectiveAnswers);
    if (step.kind === 'end') {
      autoSubmittedFor.current = currentScreenId;
      void finishSurvey();
    }
  }, [currentScreenId, currentScreen, done, submitting, schema, effectiveAnswers, finishSurvey]);

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
      const nextAnswers = { ...effectiveAnswers, [detail.questionId]: detail.option.id };
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
  }, [effectiveAnswers, currentScreenId, schema, setAnswer, goTo, finishSurvey]);

  const contextValue = useMemo(
    () => ({
      schema,
      locale: effectiveLocale,
      direction: localeConfig.direction,
      ui: localeConfig.strings,
      answers,
      setAnswer,
      answerContext,
      registerSourcedOptions,
    }),
    [schema, effectiveLocale, localeConfig, answers, setAnswer, answerContext, registerSourcedOptions],
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

  // A native <select> rather than a custom dropdown: this is a mobile surface, and
  // the OS picker is the one control every respondent already knows, gets right at
  // any font size, and reads correctly to a screen reader without us reimplementing
  // it. Options list the schema's locales by endonym — a respondent finds their
  // language by recognising it, never by decoding a code. What SHOWS is compact —
  // a globe and the current code — because the header row has to hold Back, the
  // logo and this on a phone; the select sits transparent on top of that trigger,
  // so a tap still opens the OS picker with the endonyms in it.
  const localeOptions = offeredLocales.includes(effectiveLocale)
    ? offeredLocales
    : [effectiveLocale, ...offeredLocales];
  const localePicker = localePickerVisible ? (
    <label className="survey-locale">
      <span className="survey-locale__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8">
          <circle cx="12" cy="12" r="9" />
          <path d="M3 12h18M12 3a15 15 0 0 1 0 18a15 15 0 0 1 0-18" />
        </svg>
      </span>
      <span className="survey-locale__code" aria-hidden="true">
        {effectiveLocale.toUpperCase()}
      </span>
      <select
        className="survey-locale__select"
        aria-label={localeConfig.strings.language}
        value={effectiveLocale}
        onChange={(e) => handleLocaleChange(e.target.value)}
      >
        {localeOptions.map((code) => (
          <option key={code} value={code}>
            {localeDisplayName(code)}
          </option>
        ))}
      </select>
    </label>
  ) : null;

  // Back: the chevron in the top-start corner, the way a phone's own screens do
  // it. Offered wherever there is somewhere to go back to — including
  // navigationList screens, which have no Next — but not while a submission is
  // in flight, on a screen about to submit itself, or once the survey is done.
  // The chevron stays mounted whenever the survey can ever show it and is
  // toggled with a class, so it can fade and slide in and out — and the logo
  // slide along with it — the way a phone's own header does, instead of popping.
  // Hidden = aria-hidden + visibility:hidden after the fade, so it is out of the
  // accessibility tree and the tab order, not just invisible.
  const currentQuestions = (currentScreen?.questions as unknown[] | undefined) ?? [];
  const isAutoSubmitScreen = currentScreen !== null && currentQuestions.length === 0 && !currentScreen.nextScreen;
  const showBackButton = !done && canGoBack && !submitting && !isAutoSubmitScreen;
  const backButton = schema.screens.length > 1 ? (
    <button
      type="button"
      className={showBackButton ? 'survey-back' : 'survey-back survey-back--hidden'}
      aria-label={localeConfig.strings.back}
      aria-hidden={!showBackButton}
      tabIndex={showBackButton ? undefined : -1}
      onClick={goBack}
    >
      <svg
        className="survey-back__icon"
        viewBox="0 0 24 24"
        width="24"
        height="24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        <path d="M15 5l-7 7 7 7" />
      </svg>
    </button>
  ) : null;

  // One header row for every branch below — Back at the start, then the logo
  // (shrinking before anything else does), the picker at the end — so the picker
  // can't go missing on the thank-you or empty-schema screens. The row is there on
  // every screen of a survey that can ever show Back, so the title never moves
  // between the first screen, which has none, and the rest; the `--back` modifier
  // opens the Back slot, which is what slides the logo over.
  const chrome =
    brandLogo || localePicker || backButton ? (
      <div className={showBackButton ? 'survey-chrome survey-chrome--back' : 'survey-chrome'}>
        <div className="survey-chrome__start">{backButton}</div>
        {brandLogo}
        <div className="survey-chrome__end">{localePicker}</div>
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
        {chrome}
        <div className="survey-screen">
          <h2 className="survey-screen__title">
            {shownScreen?.title
              ? localize(shownScreen.title as LocalizedString, effectiveLocale, schema.defaultLocale)
              : localeConfig.strings.thankYou}
          </h2>
          {shownScreen?.description && (
            <p className="survey-screen__description">
              {localize(shownScreen.description as LocalizedString, effectiveLocale, schema.defaultLocale)}
            </p>
          )}
        </div>
      </div>
    );
  }

  if (!currentScreen || !shownScreen) {
    return (
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale} style={brandStyle}>
        {chrome}
        <div className="survey-screen"><em>{localeConfig.strings.noScreens}</em></div>
      </div>
    );
  }

  const questions = (shownScreen.questions as Array<Record<string, unknown>> | undefined) ?? [];
  // The Next button is hidden when the terminal question of the screen is a
  // navigationList — per Phase 3 Part B.1 the tap IS the transition.
  const hasTerminalNavList =
    questions.length > 0 &&
    (questions[questions.length - 1]?.['type'] as string | undefined) === 'navigationList';
  // Also hide it on zero-question screens that will auto-submit on arrival —
  // otherwise the user sees "Next" for the moment before the submission completes,
  // which is confusing UX.
  const showNextButton = !hasTerminalNavList && !isAutoSubmitScreen;
  // The press that ends the survey is labeled Submit, not Next — respondents
  // otherwise can't tell which press submits. Two shapes end it: computeNext
  // says 'end' outright, or it routes to a zero-question screen that will
  // auto-submit on arrival (the per-branch thank-you pattern) — from the
  // respondent's seat both are the committing press. Answer-aware via
  // computeNext so the label stays correct on branching flows.
  const nextStep =
    showNextButton && currentScreenId !== null ? computeNext(schema, currentScreenId, effectiveAnswers) : null;
  const nextWouldEnd =
    nextStep !== null &&
    (nextStep.kind === 'end' ||
      (nextStep.kind === 'screen' && screenAutoSubmitsOnArrival(schema, nextStep.screenId, effectiveAnswers)));

  return (
    <SurveyContextProvider value={contextValue}>
      <div ref={rootRef} className="survey-root" dir={localeConfig.direction} lang={effectiveLocale} style={brandStyle}>
        {chrome}
        <div className="survey-screen">
          {shownScreen.title && (
            <h2 className="survey-screen__title">
              {localize(shownScreen.title as LocalizedString, effectiveLocale, schema.defaultLocale)}
            </h2>
          )}
          {shownScreen.description && (
            <p className="survey-screen__description">
              {localize(shownScreen.description as LocalizedString, effectiveLocale, schema.defaultLocale)}
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
