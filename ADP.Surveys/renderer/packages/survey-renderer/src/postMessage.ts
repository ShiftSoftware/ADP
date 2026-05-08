/**
 * postMessage host-bridge for iframe-embedded surveys. The renderer emits
 * lifecycle events so the embedding host can auto-size the iframe, react to
 * completion, and log analytics. One-way for now (renderer → host); inbound
 * messages can be added in a later slice.
 *
 * Wire format (namespaced + versioned so hosts can route safely):
 *   { source: 'adp-surveys', version: 1, type: 'survey:loaded',
 *     payload: {...} }
 */

import type { AnswerMap } from '@shiftsoftware/survey-sdk';

export const HOST_MESSAGE_SOURCE = 'adp-surveys';
export const HOST_MESSAGE_VERSION = 1;

export type HostMessageType =
  | 'survey:loaded'
  | 'survey:screen-changed'
  | 'survey:completed'
  | 'survey:error'
  | 'survey:resize';

export interface HostMessage {
  source: typeof HOST_MESSAGE_SOURCE;
  version: typeof HOST_MESSAGE_VERSION;
  type: HostMessageType;
  payload: Record<string, unknown>;
}

export interface HostBridge {
  loaded(): void;
  screenChanged(screenId: string | null): void;
  completed(result: { screenId: string | null; answers: AnswerMap }): void;
  error(message: string): void;
  resize(height: number): void;
}

export interface HostBridgeOptions {
  /** Target window for `postMessage`. Defaults to `window.parent` (the embedding
   *  iframe host). Pass null to disable. */
  target?: Window | null;
  /** Target origin for `postMessage`. Defaults to `'*'` — tighten per deployment
   *  via this prop if the host origin is known. */
  targetOrigin?: string;
  /** Force-enable or force-disable, bypassing the iframe auto-detection.
   *  Useful for tests and for hosts that want explicit control. */
  enabled?: boolean;
}

/** Create a host bridge. When disabled (e.g., not in an iframe and no explicit
 *  `enabled: true`), every method is a no-op so callers never need to guard. */
export function createHostBridge(options: HostBridgeOptions = {}): HostBridge {
  const inBrowser = typeof window !== 'undefined';
  const inIframe = inBrowser && window.parent !== window;
  const enabled = options.enabled ?? inIframe;
  const target = options.target ?? (inBrowser ? window.parent : null);
  const targetOrigin = options.targetOrigin ?? '*';

  if (!enabled || !target) {
    return {
      loaded: () => {},
      screenChanged: () => {},
      completed: () => {},
      error: () => {},
      resize: () => {},
    };
  }

  const send = (type: HostMessageType, payload: Record<string, unknown>) => {
    const message: HostMessage = {
      source: HOST_MESSAGE_SOURCE,
      version: HOST_MESSAGE_VERSION,
      type,
      payload,
    };
    try {
      target.postMessage(message, targetOrigin);
    } catch {
      // Host went away / invalid target — ignore; the renderer must not crash
      // on host disconnection.
    }
  };

  return {
    loaded: () => send('survey:loaded', {}),
    screenChanged: (screenId) => send('survey:screen-changed', { screenId }),
    completed: (result) => send('survey:completed', result as unknown as Record<string, unknown>),
    error: (message) => send('survey:error', { message }),
    resize: (height) => send('survey:resize', { height }),
  };
}
