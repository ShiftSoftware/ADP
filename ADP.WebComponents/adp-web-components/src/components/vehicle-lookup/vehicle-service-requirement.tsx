import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';
import { ComponentLocale } from '~features/multi-lingual';
import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import { VehicleServiceItemPrerequisiteDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-prerequisite-dto';
import { demoteOverlay, supportsPopover } from '~lib/overlay';
import { POPOVER_WIDTH } from './components/claimable-item-popover';

/** Informational milestone only: deliberately has no service item or claim callback. */
@Component({ tag: 'vehicle-service-requirement', shadow: true, styleUrl: 'vehicle-service-requirement.css' })
export class VehicleServiceRequirement {
  @Element() el: HTMLElement;
  @Prop() prerequisite: VehicleServiceItemPrerequisiteDTO;
  @Prop() locale: ComponentLocale<typeof dynamicClaimSchema>;
  @Prop() busy = false;
  @Event() evidenceVisibilityChange: EventEmitter<boolean>;
  @State() expanded = false;
  @State() position = { left: 0, top: 0, width: POPOVER_WIDTH };
  private trigger: HTMLButtonElement;
  private detail: HTMLDivElement;
  private closeTimer: ReturnType<typeof setTimeout>;
  private demoteTimer: ReturnType<typeof setTimeout>;
  private pinned = false;
  private hoverDismissed = false;

  connectedCallback() {
    document.addEventListener('pointerdown', this.onOutsidePointer);
    document.addEventListener('keydown', this.onEscape);
    window.addEventListener('resize', this.reposition);
    window.addEventListener('scroll', this.reposition, true);
  }

  disconnectedCallback() {
    clearTimeout(this.closeTimer);
    clearTimeout(this.demoteTimer);
    document.removeEventListener('pointerdown', this.onOutsidePointer);
    document.removeEventListener('keydown', this.onEscape);
    window.removeEventListener('resize', this.reposition);
    window.removeEventListener('scroll', this.reposition, true);
    demoteOverlay(this.detail);
  }

  @Watch('busy')
  onBusyChange() {
    if (this.busy && this.expanded) this.close();
  }

  private reposition = () => {
    if (!this.trigger) return;
    const anchor = this.trigger.getBoundingClientRect();
    const root = this.el.getRootNode();
    const panel = root instanceof ShadowRoot ? root.host.getBoundingClientRect() : undefined;
    // Keep evidence within the visible panel on a narrow embedded column, and within the
    // viewport on mobile. An offscreen/zero-width host falls back to the viewport bounds.
    const panelLeft = Math.max(12, (panel?.left ?? 0) + 12);
    const panelRight = Math.min(window.innerWidth - 12, (panel?.right ?? window.innerWidth) - 12);
    const leftBound = panelRight > panelLeft ? panelLeft : 12;
    const rightBound = panelRight > panelLeft ? panelRight : window.innerWidth - 12;
    const width = Math.min(POPOVER_WIDTH, rightBound - leftBound);
    const height = this.detail?.getBoundingClientRect().height || 220;
    const position = {
      left: Math.max(leftBound, Math.min((anchor.left + anchor.right - width) / 2, rightBound - width)),
      top: Math.max(12, Math.min(anchor.bottom + 8, window.innerHeight - height - 12)),
      width,
    };
    if (position.left !== this.position.left || position.top !== this.position.top || position.width !== this.position.width) this.position = position;
  };

  private open = () => {
    if (this.busy) return;
    clearTimeout(this.closeTimer);
    clearTimeout(this.demoteTimer);
    if (!this.expanded) this.evidenceVisibilityChange.emit(true);
    // The marker sits inside a scrolling rail. Its evidence must clear that clipping and
    // the stacking contexts of subsequent panels, even when no ancestor has a transform.
    if (supportsPopover && !this.detail.hasAttribute('popover')) {
      this.detail.setAttribute('popover', 'manual');
      this.detail.showPopover();
    }
    this.reposition();
    this.expanded = true;
    requestAnimationFrame(this.reposition);
  };

  private close = () => {
    clearTimeout(this.closeTimer);
    clearTimeout(this.demoteTimer);
    this.pinned = false;
    const wasExpanded = this.expanded;
    this.expanded = false;
    if (wasExpanded) this.evidenceVisibilityChange.emit(false);
    this.demoteTimer = setTimeout(() => demoteOverlay(this.detail), 320);
  };

  @Method()
  async closeEvidence() {
    this.hoverDismissed = true;
    this.close();
  }

  private scheduleClose = () => {
    if (this.pinned || this.el.shadowRoot?.activeElement) return;
    this.closeTimer = setTimeout(this.close, 200);
  };

  private onFocusLeave = () => {
    this.closeTimer = setTimeout(() => {
      if (!this.el.shadowRoot?.activeElement) this.close();
    }, 200);
  };

  private onOutsidePointer = (event: PointerEvent) => {
    if (this.expanded && !event.composedPath().includes(this.el)) this.close();
  };

  private onEscape = (event: KeyboardEvent) => {
    if (this.expanded && event.key === 'Escape') {
      this.hoverDismissed = true;
      this.close();
      this.trigger.focus({ preventScroll: true });
    }
  };

  render() {
    const requirement = this.prerequisite;
    const locale = this.locale;
    if (!requirement || !locale) return null;
    const evidence = requirement.satisfied ? requirement.evidence : undefined;
    const status = requirement.satisfied ? locale.requirementCompleted : locale.pending;
    const fact = (label: string, value: string | number, columns = 1, code = false) => ({ label, value, columns, code });
    const rows = evidence
      ? [
          [
            fact(locale.requirementInvoiceDate, evidence.invoiceDate?.slice(0, 10)),
            fact(locale.requirementOdometer, evidence.odometer),
            fact(locale.invoiceNumber, evidence.invoiceNumber),
          ],
          [fact(locale.jobNumber, evidence.jobNumber), fact(locale.packageCode, evidence.packageCode, 2, true)],
          [fact(locale.requirementServiceCode, evidence.serviceCode), fact(locale.requirementLaborCode, evidence.laborCode)],
          [fact(locale.requirementWork, evidence.serviceDescription, 3)],
          [fact(locale.requirementJobDescription, evidence.jobDescription, 3)],
        ]
          .map(row => row.filter(({ value }) => value !== undefined && value !== null && value !== ''))
          .filter(row => row.length)
      : [];

    return (
      <Host translate="no" data-complete={String(requirement.satisfied)} data-busy={String(this.busy)} aria-hidden={this.busy ? 'true' : undefined}>
        <button
          ref={el => (this.trigger = el)}
          type="button"
          class="requirement-trigger"
          dir={locale.sharedLocales.direction}
          disabled={this.busy}
          aria-expanded={String(this.expanded)}
          aria-controls="requirement-evidence"
          aria-label={`${requirement.label} · ${locale.requiredService} · ${status} · ${locale.requirementNotClaimable}`}
          onPointerEnter={event => event.pointerType !== 'touch' && !this.hoverDismissed && this.open()}
          onPointerLeave={() => {
            this.hoverDismissed = false;
            this.scheduleClose();
          }}
          onClick={() => {
            if (this.pinned) {
              this.hoverDismissed = true;
              this.close();
            } else {
              this.hoverDismissed = false;
              this.pinned = true;
              this.open();
            }
          }}
          onBlur={this.onFocusLeave}
        >
          <span class="requirement-status">
            <span aria-hidden="true">{requirement.satisfied ? '✓' : '○'}</span> {status}
          </span>
          <span class="requirement-circle" aria-hidden="true">
            {requirement.satisfied ? '✓' : ''}
          </span>
          <span class="requirement-label" dir="ltr">
            {requirement.label}
          </span>
          <span class="requirement-role">{locale.requiredService}</span>
          <span class="requirement-note">{locale.requirementNotClaimable}</span>
        </button>
        <div
          id="requirement-evidence"
          ref={el => (this.detail = el)}
          class="requirement-detail claim-detail-surface claim-detail-card"
          dir={locale.sharedLocales.direction}
          role="region"
          tabIndex={this.expanded ? 0 : -1}
          aria-label={`${requirement.label} · ${locale.requiredService}`}
          aria-hidden={String(!this.expanded)}
          data-open={String(this.expanded)}
          style={{ left: `${this.position.left}px`, top: `${this.position.top}px`, width: `${this.position.width}px` }}
          onPointerEnter={() => this.expanded && this.open()}
          onPointerLeave={this.scheduleClose}
          onBlur={this.onFocusLeave}
        >
          <div class="requirement-detail-heading">
            <p class="requirement-detail-title">
              {requirement.label} · {locale.requiredService}
            </p>
            <p class="requirement-detail-status">
              {status} · {locale.requirementNotClaimable}
            </p>
          </div>
          <div class="requirement-detail-facts">
            {rows.map(row => (
              <dl class="popover-specs">
                {row.map(({ label, value, columns, code }) => (
                  <div class={{ 'requirement-fact': true, 'popover-spec': true, 'pkg': code, 'full': columns === 3 || (columns > 1 && row.length === 1) }}>
                    <dt class="lab">{label}</dt>
                    <dd class="val" dir={columns === 3 ? 'auto' : 'ltr'}>
                      {String(value)}
                    </dd>
                  </div>
                ))}
              </dl>
            ))}
          </div>
          {!rows.length && <p class="requirement-empty">{requirement.satisfied ? locale.requirementEvidenceUnavailable : locale.requirementAwaiting}</p>}
        </div>
      </Host>
    );
  }
}
