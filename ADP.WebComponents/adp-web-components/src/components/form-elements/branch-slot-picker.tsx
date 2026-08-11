import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { ArrowUpIcon } from '~assets/arrow-up-icon';
import getCustomClassesForPortal from '~lib/get-custom-classes-for-portal';

import { FormHook } from '~features/form-hook/form-hook';
import { FormElement, FormInputLocalization, FormInputMeta, getInputLocalization } from '~features/form-hook';
import { LanguageKeys } from '~features/multi-lingual';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { BranchSlotDay } from './branch-slot-dropdown';

/** One day as the calendar endpoint returns it. */
interface DayEntry {
  Date: string;
  Times: string[];
}

export interface BranchSlotSelection {
  /** `2026-08-13` */
  date: string;
  /** Exactly what the API sent, e.g. `2026-08-13 09:00 AM`. */
  raw: string;
  /** `2026-08-13T09:00` — what goes on the wire to the ticket. */
  value: string;
}

type Status = 'idle' | 'loading' | 'ready' | 'empty' | 'error';

const FALLBACK_COPY = {
  en: {
    day: 'Pick a day',
    time: 'Pick a time',
    days: 'days available',
    slots: 'slots',
    idle: 'Choose a branch first',
    empty: 'No times available at this branch',
    error: "Couldn't load available times",
    retry: 'Try again',
    choose: 'Choose date & time',
    loading: 'Loading times…',
  },
  ar: {
    day: 'اختر اليوم',
    time: 'اختر الوقت',
    days: 'يوماً متاحاً',
    slots: 'موعداً',
    idle: 'اختر الفرع أولاً',
    empty: 'لا توجد أوقات متاحة في هذا الفرع',
    error: 'تعذّر تحميل الأوقات المتاحة',
    retry: 'أعد المحاولة',
    choose: 'اختر التاريخ والوقت',
    loading: 'جارٍ تحميل الأوقات…',
  },
  ku: {
    day: 'ڕۆژێک هەڵبژێرە',
    time: 'کاتێک هەڵبژێرە',
    days: 'ڕۆژی بەردەست',
    slots: 'کات',
    idle: 'سەرەتا لقێک هەڵبژێرە',
    empty: 'هیچ کاتێکی بەردەست نییە لەم لقەدا',
    error: 'نەتوانرا کاتە بەردەستەکان باربکرێن',
    retry: 'دووبارە هەوڵ بدە',
    choose: 'بەروار و کات هەڵبژێرە',
    loading: 'کاتەکان باردەکرێن…',
  },
  ru: {
    day: 'Выберите день',
    time: 'Выберите время',
    days: 'дней доступно',
    slots: 'слотов',
    idle: 'Сначала выберите филиал',
    empty: 'В этом филиале нет свободного времени',
    error: 'Не удалось загрузить свободное время',
    retry: 'Повторить',
    choose: 'Выберите дату и время',
    loading: 'Загрузка времени…',
  },
};

/**
 * Branch-aware booking slot field.
 *
 * Presents as a normal dropdown — the same `form-input-select` trigger as every
 * other select in the form — and portals its panel onto `document.body` through
 * `shift-portal`, exactly as `shift-select` does.
 *
 * Standalone by design: `form` is optional. Drop `<branch-slot-picker>` on any
 * page, in a PWA or in a native WebView, give it the four ids, and listen to
 * `slotChange`.
 */
@Component({
  shadow: false,
  tag: 'branch-slot-picker',
  styleUrl: 'branch-slot-picker.css',
})
export class BranchSlotPicker implements FormElement {
  /** Field name. Only meaningful when `form` is supplied. */
  @Prop() name: string = 'bookingSlot';

  /** Optional — omit for standalone use and listen to `slotChange` instead. */
  @Prop() form?: FormHook<any>;

  /** Full URL of the calendar endpoint, e.g. `https://…/api/public/calendar`. */
  @Prop() calendarApi: string;

  /** Integration ids. The field stays idle until all four are present. */
  @Prop() companyId: string;
  @Prop() branchId: string;
  @Prop() departmentId: string;
  @Prop() brandId: string;

  /** How far ahead to ask. The endpoint caps its own response at 15 days. */
  @Prop() daysAhead: number = 30;

  /** Gap between the trigger and the panel, matching shift-select's default. */
  @Prop() gap: number = 8;

  @Prop() label?: string;
  @Prop() wrapperId: string;
  @Prop() wrapperClass: string;
  @Prop() isRequired: boolean = false;
  @Prop() isDisabled: boolean = false;
  @Prop() language: LanguageKeys = 'en';
  @Prop() localization?: FormInputLocalization = {};

  /** Preselects a slot, in the same `YYYY-MM-DDTHH:mm` form the field emits. */
  @Prop({ mutable: true }) defaultValue?: string;

  @State() status: Status = 'idle';
  @State() days: BranchSlotDay[] = [];
  @State() selectedDate: string = '';
  @State() selectedRaw: string = '';
  @State() isOpen: boolean = false;
  @State() dropdownAncestorClasses = '';

  /**
   * The day being browsed in the panel. Kept apart from `selectedDate` so that
   * opening the panel, scrubbing through days and then dismissing it leaves the
   * committed selection untouched.
   */
  @State() draftDate: string = '';

  /** Fires on every confirmed day+time selection. The standalone contract. */
  @Event() slotChange: EventEmitter<BranchSlotSelection>;

  @Element() el: HTMLElement;

  private abortController?: AbortController;
  private dropdownEl: HTMLElement | null = null;
  private lockedScrollY = 0;
  private boundKeyDown = (event: KeyboardEvent) => this.handleKeyDown(event);

  componentWillLoad() {
    this.form?.subscribe(this.name, this);
    this.load();
  }

  componentDidLoad() {
    document.addEventListener('click', this.closeOnOutsideClick);
    document.addEventListener('keydown', this.boundKeyDown);
    window.addEventListener('resize', this.handleResize);
    window.addEventListener('scroll', this.handleScroll, true);

    // The panel is portaled out of the form, so theme classes have to travel
    // with it or it renders unstyled against the host page.
    this.dropdownAncestorClasses = `${this.name}-slot-picker ${getCustomClassesForPortal(this.el)}`;
  }

  disconnectedCallback() {
    this.abortController?.abort();
    this.form?.unsubscribe(this.name);
    document.removeEventListener('click', this.closeOnOutsideClick);
    document.removeEventListener('keydown', this.boundKeyDown);
    window.removeEventListener('resize', this.handleResize);
    window.removeEventListener('scroll', this.handleScroll, true);
    // A teardown while the sheet is open would otherwise leave the page frozen.
    this.releaseScroll();
  }

  @Watch('isOpen')
  onOpenChange(isOpen: boolean) {
    // Only the sheet needs this. An anchored dropdown repositions on scroll and
    // freezing the page under it would be wrong.
    if (!this.isSheet) return;
    if (isOpen) this.lockScroll();
    else this.releaseScroll();
  }

  /**
   * Pinning the body position rather than just hiding overflow: on iOS,
   * `overflow: hidden` alone still lets the page rubber-band behind the sheet.
   */
  private lockScroll() {
    if (document.body.style.position === 'fixed') return;
    this.lockedScrollY = window.scrollY;
    Object.assign(document.body.style, { position: 'fixed', top: `-${this.lockedScrollY}px`, left: '0', right: '0', width: '100%' });
  }

  private releaseScroll() {
    if (document.body.style.position !== 'fixed') return;
    Object.assign(document.body.style, { position: '', top: '', left: '', right: '', width: '' });
    window.scrollTo(0, this.lockedScrollY);
  }

  @Watch('branchId')
  @Watch('departmentId')
  @Watch('brandId')
  @Watch('companyId')
  onTargetChange() {
    // A different branch invalidates the whole selection, not just the times.
    this.selectedDate = '';
    this.selectedRaw = '';
    this.draftDate = '';
    this.isOpen = false;
    this.load();
  }

  /** FormElement contract. */
  reset(newValue?: unknown) {
    this.defaultValue = (newValue as string) ?? '';
    this.selectedDate = '';
    this.selectedRaw = '';
    this.draftDate = '';
  }

  getValue() {
    return this.selectedRaw ? this.toFormValue(this.selectedRaw) : this.defaultValue || '';
  }

  /** Lets a host force a refetch, e.g. after a token refresh. */
  @Method()
  async refresh() {
    this.load();
  }

  @Method()
  async openDropdown() {
    if (this.status === 'idle') return;
    this.draftDate = this.selectedDate || this.days[0]?.date || '';
    this.adjustDropdownPosition();
    this.isOpen = true;
  }

  @Method()
  async closeDropdown() {
    this.isOpen = false;
  }

  private get isRtl() {
    return this.language === 'ar' || this.language === 'ku';
  }

  private get copy() {
    return FALLBACK_COPY[this.language] ?? FALLBACK_COPY.en;
  }

  private get intlLocales(): string[] {
    // An array lets Intl fall back on its own when a locale is unavailable —
    // ckb has patchy support in older Android WebViews.
    if (this.language === 'ar') return ['ar-IQ', 'ar', 'en-GB'];
    if (this.language === 'ku') return ['ckb-IQ', 'ckb', 'ar-IQ', 'en-GB'];
    if (this.language === 'ru') return ['ru-RU', 'en-GB'];
    return ['en-GB'];
  }

  private pad(n: number) {
    return String(n).padStart(2, '0');
  }

  /** `2026-08-13 09:00 AM` → minutes since midnight. */
  private minutesOf(raw: string): number {
    const parts = raw.split(' ');
    const clock = parts[1] ?? '00:00';
    const meridiem = (parts[2] ?? '').toUpperCase();
    const [h, m] = clock.split(':').map(x => parseInt(x, 10) || 0);
    let hour = h % 12;
    if (meridiem === 'PM') hour += 12;
    return hour * 60 + m;
  }

  private toFormValue(raw: string): string {
    const date = raw.split(' ')[0];
    const total = this.minutesOf(raw);
    return `${date}T${this.pad(Math.floor(total / 60))}:${this.pad(total % 60)}`;
  }

  private displayTime = (raw: string): string => {
    const total = this.minutesOf(raw);
    const date = new Date(2000, 0, 1, Math.floor(total / 60), total % 60);
    return new Intl.DateTimeFormat(this.intlLocales, { hour: 'numeric', minute: '2-digit' }).format(date);
  };

  private formatDay = (date: string, options: Intl.DateTimeFormatOptions): string => {
    // Midday, not midnight: a midnight Date in a negative-offset zone rolls back
    // a day, which silently mislabels every chip.
    const [y, m, d] = date.split('-').map(x => parseInt(x, 10));
    return new Intl.DateTimeFormat(this.intlLocales, options).format(new Date(y, m - 1, d, 12));
  };

  private queryUrl(): string | null {
    if (!this.calendarApi || !this.companyId || !this.branchId || !this.departmentId || !this.brandId) return null;

    const from = new Date();
    const to = new Date();
    to.setDate(to.getDate() + this.daysAhead);

    const iso = (d: Date) => `${d.getFullYear()}-${this.pad(d.getMonth() + 1)}-${this.pad(d.getDate())}`;

    const qs = new URLSearchParams({
      from: iso(from),
      to: iso(to),
      companyId: this.companyId,
      branchId: this.branchId,
      departmentId: this.departmentId,
      brandId: this.brandId,
    });

    return `${this.calendarApi}${this.calendarApi.includes('?') ? '&' : '?'}${qs.toString()}`;
  }

  /**
   * Overlapping work periods of equal priority make the endpoint emit the same
   * slot more than once (branch 45 returns every time twice), and a merged
   * response is not necessarily ordered. Both are fixed here rather than in the
   * panel, so anything reading `days` gets clean data.
   */
  private normalise(raw: DayEntry[]): BranchSlotDay[] {
    return raw
      .filter(entry => entry && entry.Date && Array.isArray(entry.Times) && entry.Times.length)
      .map(entry => ({
        date: entry.Date,
        times: Array.from(new Set(entry.Times)).sort((a, b) => this.minutesOf(a) - this.minutesOf(b)),
      }))
      .sort((a, b) => a.date.localeCompare(b.date));
  }

  private async load() {
    const url = this.queryUrl();

    this.abortController?.abort();

    if (!url) {
      this.status = 'idle';
      this.days = [];
      return;
    }

    this.abortController = new AbortController();
    this.status = 'loading';

    try {
      const response = await fetch(url, {
        signal: this.abortController.signal,
        headers: { 'Accept-Language': this.language },
      });

      if (!response.ok) throw new Error(`calendar ${response.status}`);

      this.days = this.normalise(await response.json());

      if (!this.days.length) {
        this.status = 'empty';
        return;
      }

      const preset = this.days.find(day => this.defaultValue && this.defaultValue.startsWith(day.date));
      this.draftDate = (preset ?? this.days[0]).date;
      this.status = 'ready';
    } catch (error) {
      if ((error as Error)?.name === 'AbortError') return;
      this.status = 'error';
    }
  }

  private setDropdownRef = (el: HTMLElement | null) => {
    this.dropdownEl = el;
  };

  private getTriggerEl(): HTMLElement | null {
    return this.el.getElementsByClassName('form-input-select')[0] as HTMLElement | null;
  }

  private isTriggerInView(rect: DOMRect): boolean {
    return rect.bottom > 0 && rect.top < window.innerHeight && rect.right > 0 && rect.left < window.innerWidth;
  }

  /**
   * Below this width the panel is a bottom sheet pinned to the viewport, so it
   * is not anchored to anything and none of the positioning below applies. Kept
   * in sync with the media query in branch-slot-dropdown.css.
   */
  private get isSheet(): boolean {
    return typeof window !== 'undefined' && window.matchMedia('(max-width: 599px)').matches;
  }

  /** Mirrors shift-select: anchor to the trigger, flip up when space is short. */
  private adjustDropdownPosition = () => {
    const dropdown = this.dropdownEl;
    const trigger = this.getTriggerEl();

    if (!dropdown || !trigger) return;
    // Sheet mode is viewport-anchored; CSS owns it entirely.
    if (this.isSheet) return;

    const rect = trigger.getBoundingClientRect();

    // Scrolled out of sight — a panel floating over unrelated content is worse
    // than no panel.
    if (!this.isTriggerInView(rect)) {
      this.isOpen = false;
      return;
    }

    const width = Math.max(rect.width, 288);
    dropdown.style.setProperty('--branch-slot-width', `${width}px`);

    const spaceBelow = window.innerHeight - rect.bottom - this.gap * 2;
    const spaceAbove = rect.top - this.gap * 2;
    const openUpwards = spaceBelow < dropdown.offsetHeight && spaceAbove > spaceBelow;

    // Cap the panel to the room that actually exists. Without this the fixed
    // 320px runs off-screen in landscape and with the keyboard up, and there is
    // nothing to scroll to reach it.
    dropdown.style.setProperty('--branch-slot-max-height', `${Math.max(160, Math.min(320, openUpwards ? spaceAbove : spaceBelow))}px`);

    const height = dropdown.offsetHeight;

    // Keep it on-screen horizontally — the panel has a min-width, so a field
    // near the right edge would otherwise hang off it.
    const left = Math.max(this.gap, Math.min(rect.left, window.innerWidth - width - this.gap));

    dropdown.style.setProperty('--branch-slot-left', `${left}px`);
    dropdown.style.setProperty('--branch-slot-top', `${openUpwards ? rect.top - height - this.gap : rect.bottom + this.gap}px`);
  };

  private handleResize = () => {
    if (this.isOpen) this.adjustDropdownPosition();
  };

  private handleScroll = (event: Event) => {
    if (!this.isOpen) return;
    // The sheet does not move with the page, and the page is locked anyway.
    if (this.isSheet) return;
    const target = event.target as Node;
    if (this.dropdownEl && (this.dropdownEl === target || this.dropdownEl.contains(target))) return;
    this.adjustDropdownPosition();
  };

  private handleKeyDown(event: KeyboardEvent) {
    if (!this.isOpen) return;
    if (event.key === 'Escape') this.isOpen = false;
  }

  private closeOnOutsideClick = (event: MouseEvent) => {
    const path = event.composedPath();
    if (path.includes(this.el)) return;
    if (this.dropdownEl && path.includes(this.dropdownEl)) return;
    this.isOpen = false;
  };

  private toggle = () => {
    if (this.isOpen) this.isOpen = false;
    else if (this.status === 'error') this.load();
    else this.openDropdown();
  };

  private handleDay = (date: string) => {
    this.draftDate = date;
  };

  private handleTime = (raw: string) => {
    this.selectedDate = raw.split(' ')[0];
    this.selectedRaw = raw;
    this.isOpen = false;

    // The form pulls values — it calls getValue() on every subscribed element at
    // submit time — so there is nothing to push. This only fires the public
    // event, which is what standalone hosts listen to.
    this.slotChange.emit({ date: this.selectedDate, raw, value: this.toFormValue(raw) });
  };

  /** What the trigger shows: the committed slot, or why it cannot be opened. */
  private triggerText(): string {
    if (this.status === 'idle') return this.copy.idle;
    if (this.status === 'loading') return this.copy.loading;
    if (this.status === 'empty') return this.copy.empty;
    if (this.status === 'error') return this.copy.error;
    if (!this.selectedRaw) return '';

    const day = this.formatDay(this.selectedDate, { weekday: 'short', day: 'numeric', month: 'short' });
    return `${day} · ${this.displayTime(this.selectedRaw)}`;
  }

  render() {
    // Inside a form, required/disabled/error come from the form's own state, so
    // this field validates and reports exactly like every other one. Standalone,
    // there is no form to ask and the props stand on their own.
    const state = this.form?.getInputState<FormInputMeta>(this.name);
    const localised = this.form ? getInputLocalization(this, state?.meta, state?.errorMessage) : null;

    const label = localised?.label || this.localization?.[this.language]?.label || this.label;
    const placeholder = localised?.placeholder || this.localization?.[this.language]?.placeholder || this.copy.choose;
    const isRequired = state?.isRequired || this.isRequired;

    // Idle/loading/empty cannot be opened; error can, because the trigger then
    // doubles as the retry affordance.
    const blocked = this.isDisabled || state?.disabled || this.status === 'idle' || this.status === 'loading' || this.status === 'empty';

    const dropdownProps = {
      name: this.name,
      days: this.days,
      copy: this.copy,
      status: this.status,
      isOpen: this.isOpen,
      idleText: this.copy.idle,
      activeDate: this.draftDate,
      handleDay: this.handleDay,
      handleTime: this.handleTime,
      handleRetry: () => this.load(),
      handleDismiss: () => (this.isOpen = false),
      selectedRaw: this.selectedRaw,
      formatDay: this.formatDay,
      displayTime: this.displayTime,
      setElementRef: this.setDropdownRef,
      direction: this.isRtl ? 'rtl' : 'ltr',
    };

    return (
      <Host translate="no">
        <label part={`${this.name}`} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: blocked })}>
          <FormInputLabel name={this.name} label={label} isRequired={isRequired} />

          <div part={`${this.name}-container form-input-container`} class={cn('form-input-container', { open: this.isOpen, disabled: blocked })}>
            <input
              type="text"
              readOnly
              disabled={blocked}
              value={this.triggerText()}
              placeholder={placeholder}
              onClick={this.toggle}
              part={`${this.name}-input-select form-input-select`}
              class="form-input-style form-input-select branch-slot-trigger"
            />

            <div part={`${this.name}-select-icon-container form-input-select-icon-container`} class="form-input-select-icon-container">
              <ArrowUpIcon part={`${this.name}-arrow-icon select-arrow`} class="form-input-select-icon pointer-events-none! arrow cursor-pointer" />
            </div>

            {
              // Stencil only bundles a lazily-portaled component if it also sees
              // the tag in a template. Same guard shift-select uses.
              // @ts-ignore
              false && <branch-slot-dropdown />
            }
            <shift-portal tag="branch-slot-dropdown" inheritedClasses={this.dropdownAncestorClasses} componentProps={dropdownProps} />
          </div>

          <FormErrorMessage name={this.name} isError={!!state?.isError} errorMessage={localised?.errorTextMessage || ''} />
        </label>
      </Host>
    );
  }
}
