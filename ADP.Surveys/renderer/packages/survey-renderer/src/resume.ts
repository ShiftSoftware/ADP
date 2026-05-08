/**
 * localStorage-backed resume flow. The SDK persists the accumulated answer map +
 * current screen id per survey instance so a mid-flow reload lands the user on
 * the same screen with their answers intact. Cleared on successful submission.
 *
 * Server-side partials aren't part of this slice — per Phase 3 Part E.2 the
 * simpler local-only flow is enough until we have a real need (and a matching
 * API endpoint for partial submissions).
 */

import type { AnswerMap } from '@shiftsoftware/survey-sdk';

/** Minimum of the Storage API the renderer depends on. Makes it trivial to
 *  inject a mock in tests and swap to `sessionStorage` per-deployment. */
export interface ResumeStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

export interface ResumeState {
  answers: AnswerMap;
  currentScreenId: string | null;
  /** Unix ms. Future slices may expire stale state; current slice keeps forever. */
  savedAt: number;
  /** Schema version the state was captured against — useful for future "schema
   *  changed since save" UX. Today we key by instanceId alone; instance always
   *  pins a single version, so schema changes can't happen under our feet. */
  schemaVersion?: number;
}

export function storageKey(resumeKey: string): string {
  return `adp-surveys:resume:${resumeKey}`;
}

export function loadResumeState(storage: ResumeStorage, resumeKey: string): ResumeState | null {
  try {
    const raw = storage.getItem(storageKey(resumeKey));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as ResumeState;
    if (!parsed || typeof parsed !== 'object' || !parsed.answers) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function saveResumeState(
  storage: ResumeStorage,
  resumeKey: string,
  state: Omit<ResumeState, 'savedAt'>,
): void {
  try {
    const payload: ResumeState = { ...state, savedAt: Date.now() };
    storage.setItem(storageKey(resumeKey), JSON.stringify(payload));
  } catch {
    // Quota / serialization failure — don't block the render. Resume is a
    // best-effort UX feature; losing it is never worse than the user re-
    // starting the survey from scratch.
  }
}

export function clearResumeState(storage: ResumeStorage, resumeKey: string): void {
  try {
    storage.removeItem(storageKey(resumeKey));
  } catch {
    // Same as above — never crash the renderer on storage errors.
  }
}
