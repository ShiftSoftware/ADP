import { h, FunctionalComponent } from '@stencil/core';

import cn from '~lib/cn';

import { LoaderIcon } from '~assets/loader-icon';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import { ComponentLocale } from '~features/multi-lingual';

type ClaimableTraceModalProps = {
  isOpen: boolean;
  fadingOut: boolean;
  isLoading: boolean;
  errorMessage?: string;
  vin?: string;
  traceHtml?: string;
  locale: ComponentLocale<typeof dynamicClaimSchema>;
  onClose: () => void;
  /** Handed back so the owner can showModal()/close() it — see openTraceModal. */
  dialogRef: (element: HTMLDialogElement) => void;
  onCancel: (event: Event) => void;
};

export const ClaimableTraceModal: FunctionalComponent<ClaimableTraceModalProps> = ({
  isOpen,
  fadingOut,
  isLoading,
  errorMessage,
  vin,
  traceHtml,
  locale,
  onClose,
  dialogRef,
  onCancel,
}) => {
  const titleSuffix = vin ? ` — ${vin}` : '';

  return (
    // A real dialog rather than role="dialog": showModal() puts it in the top layer, so it clears
    // the host page's stacking contexts and inerts everything behind it. The element supplies the
    // modal semantics and aria-hidden that were being declared by hand here.
    <dialog
      ref={el => dialogRef(el as HTMLDialogElement)}
      onCancel={onCancel}
      aria-label={`${locale.traceTitle}${titleSuffix}`}
      dir={locale.sharedLocales.direction}
      class={cn('claimable-trace-modal', { 'open': isOpen, 'fading-out': fadingOut })}
    >
      <div class="trace-modal-header">
        <span class="trace-modal-title">
          {locale.traceTitle}
          {titleSuffix}
        </span>
        <button type="button" class="trace-modal-close" aria-label="Close" onClick={onClose}>
          ×
        </button>
      </div>
      <div class="trace-modal-body">
        {isLoading && (
          <div class="trace-modal-status">
            <LoaderIcon class="size-[40px] animate-spin text-[#3071a9]" />
            <span>{locale.traceLoading}</span>
          </div>
        )}
        {!isLoading && errorMessage && (
          <div class="trace-modal-status trace-modal-error">
            <span>{errorMessage}</span>
          </div>
        )}
        {!isLoading && !errorMessage && traceHtml && (
          // sandbox: allow-scripts is needed for Mermaid to run from CDN inside the trace HTML.
          // Omitting allow-same-origin keeps the iframe in a null origin so it can't reach
          // localStorage/cookies of the host page — defensive given this loads a third-party CDN script.
          <iframe class="trace-modal-iframe" srcdoc={traceHtml} sandbox="allow-scripts" title={locale.traceTitle} />
        )}
      </div>
    </dialog>
  );
};
