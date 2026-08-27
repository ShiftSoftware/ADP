import { Component, Element, Event, EventEmitter, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { ArrowUpIcon } from '~assets/arrow-up-icon';
import getCustomClassesForPortal from '~lib/get-custom-classes-for-portal';
import { isDayBlocked, parseDateList, parseWeekdayList } from '~lib/slot-day-rules';

import { FormHook } from '~features/form-hook/form-hook';
import { FormElement, FormInputLocalization, FormInputMeta, getInputLocalization } from '~features/form-hook';
import { LanguageKeys } from '~features/multi-lingual';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { BranchSlotDay } from './branch-slot-dropdown';
import { BranchDateStep } from './branch-date-dropdown';
import { BranchSlotSelection } from './branch-slot-picker';

/** One day as the calendar endpoint returns it. */
interface DayEntry {
  Date: string;
  Times: string[];
}

type Status = 'idle' | 'loading' | 'ready' | 'empty' | 'error';

const FALLBACK_COPY = {
  en: {
    date: 'Pick a date',
    time: 'Pick a time',
    slots: 'slots',
    back: 'Change date',
    noTimes: 'Nothing left on this day',
    closed: 'Closed on this day',
    idle: 'Choose a branch first',
    empty: 'No times available at this branch',
    error: "Couldn't load available times",
    retry: 'Try again',
    choose: 'Choose date & time',
    loading: 'Loading times…',
  },
  ar: {
    date: 'اختر التاريخ',
    time: 'اختر الوقت',
    slots: 'موعداً',
    back: 'تغيير التاريخ',
    noTimes: 'لا توجد مواعيد في هذا اليوم',
    closed: 'مغلق في هذا اليوم',
    idle: 'اختر الفرع أولاً',
    empty: 'لا توجد أوقات متاحة في هذا الفرع',
    error: 'تعذّر تحميل الأوقات المتاحة',
    retry: 'أعد المحاولة',
    choose: 'اختر التاريخ والوقت',
    loading: 'جارٍ تحميل الأوقات…',
  },
  ku: {
    date: 'بەروارێک هەڵبژێرە',
    time: 'کاتێک هەڵبژێرە',
    slots: 'کات',
    back: 'گۆڕینی بەروار',
    noTimes: 'هیچ کاتێک نەماوە لەم ڕۆژەدا',
    closed: 'داخراوە لەم ڕۆژەدا',
    idle: 'سەرەتا لقێک هەڵبژێرە',
    empty: 'هیچ کاتێکی بەردەست نییە لەم لقەدا',
    error: 'نەتوانرا کاتە بەردەستەکان باربکرێن',
    retry: 'دووبارە هەوڵ بدە',
    choose: 'بەروار و کات هەڵبژێرە',
    loading: 'کاتەکان باردەکرێن…',
  },
  ru: {
    date: 'Выберите дату',
    time: 'Выберите время',
    slots: 'слотов',
    back: 'Сменить дату',
    noTimes: 'На этот день ничего не осталось',
    closed: 'В этот день закрыто',
    idle: 'Сначала выберите филиал',
    empty: 'В этом филиале нет свободного времени',
    error: 'Не удалось загрузить свободное время',
    retry: 'Повторить',
    choose: 'Выберите дату и время',
    loading: 'Загрузка времени…',
  },
};

/**
 * Branch-aware booking slot field, month-calendar variant.
 *
 * Same contract and same data as `branch-slot-picker` — the same endpoint, the
 * same `slotChange` payload, the same standalone-without-a-form behaviour. The
 * difference is entirely in the panel: a month grid first, then that day's
 * times, on a sliding track. Customers who have never used a horizontal day
 * strip have used a month calendar, and the second step arrives already scoped
 * to one day rather than sitting under the first.
 *
 * The two are meant to be interchangeable, so a deployment can pick whichever
 * its audience reads faster without anything downstream changing.
 */
@Component({
  shadow: false,
  tag: 'branch-date-picker',
  styleUrl: 'branch-date-picker.css',
})
export class BranchDatePicker implements FormElement {
  /** Field name. Only meaningful when `form` is supplied. */
  @Prop() name: string = 'bookingDate';

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

  /**
   * Weekdays the branch never books on, `0` Sunday … `6` Saturday. Accepts an
   * array or a `"5,6"` string. Matching days stay on the grid, struck through,
   * rather than being dropped — see `~lib/slot-day-rules`.
   */
  @Prop() disabledWeekdays?: string | number[];

  /** One-off blackout dates as `YYYY-MM-DD`, same accepted shapes as above. */
  @Prop() disabledDates?: string | string[];

  /**
   * First column of the grid, `0` Sunday … `6` Saturday. Left unset it comes
   * from the active locale, which is right far more often than any constant.
   */
  @Prop() weekStartsOn?: number;

  /** Gap between the trigger and the panel, matching shift-select's default. */
  @Prop() gap: number = 8;

  /**
   * Floor for the panel width on desktop. The panel is portaled to `<body>` and
   * positioned fixed, so it is free to be wider than the field — and than the
   * form around it — which is the point: seven columns stop reading as a
   * calendar much below this, and form fields are routinely narrower.
   */
  @Prop() minPanelWidth: number = 380;

  /** Tallest the panel may get before its panes scroll. Also capped to the viewport. */
  @Prop() maxPanelHeight: number = 460;

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
   * The day whose times are on the second pane. Kept apart from `selectedDate`
   * so browsing the calendar and then dismissing the panel leaves the committed
   * selection untouched.
   */
  @State() draftDate: string = '';

  @State() step: BranchDateStep = 'date';

  /** Month on screen as `YYYY-MM`, and which way the last change travelled. */
  @State() monthKey: string = '';
  @State() monthDirection: number = 1;

  /** Fires on every confirmed day+time selection. The standalone contract. */
  @Event() slotChange: EventEmitter<BranchSlotSelection>;

  @Element() el: HTMLElement;

  /**
   * The response as it arrived, before the disable rules are layered on. Kept so
   * a rule change re-decorates without a refetch.
   */
  private fetchedDays: BranchSlotDay[] = [];

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
    this.dropdownAncestorClasses = `${this.name}-date-picker ${getCustomClassesForPortal(this.el)}`;
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
    // Only the sheet needs this. An anchored panel repositions on scroll and
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

  @Watch('disabledWeekdays')
  @Watch('disabledDates')
  onRulesChange() {
    this.days = this.applyDayRules(this.fetchedDays);

    // The committed slot may have just become unbookable, and a draft pointing
    // at a now-blocked day would leave the times pane empty with no way back.
    if (this.selectedDate && this.days.find(day => day.date === this.selectedDate)?.disabled) {
      this.selectedDate = '';
      this.selectedRaw = '';
    }

    if (!this.draftDate || this.days.find(day => day.date === this.draftDate)?.disabled) {
      this.draftDate = this.firstOpenDay()?.date ?? '';
      this.step = 'date';
    }

    if (this.status === 'ready' && !this.days.some(day => !day.disabled)) this.status = 'empty';
    else if (this.status === 'empty' && this.days.some(day => !day.disabled)) this.status = 'ready';
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
    this.step = 'date';
    this.isOpen = false;
    this.load();
  }

  /** FormElement contract. */
  reset(newValue?: unknown) {
    this.defaultValue = (newValue as string) ?? '';
    this.selectedDate = '';
    this.selectedRaw = '';
    this.draftDate = '';
    this.step = 'date';
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

    const landing = this.days.find(day => day.date === this.selectedDate && !day.disabled)?.date ?? this.firstOpenDay()?.date ?? '';

    this.draftDate = landing;
    this.monthKey = landing.slice(0, 7);
    // Reopening a committed slot lands on the times, where the change is most
    // likely to be; a first open has nothing to show there yet.
    this.step = this.selectedRaw ? 'time' : 'date';
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

  /**
   * `0` Sunday … `6` Saturday. `getWeekInfo` is the only thing that knows the
   * region's answer; it is missing on older WebViews, hence the guard and the
   * Monday default the rest of Intl assumes.
   */
  private get weekStart(): number {
    if (this.weekStartsOn !== undefined && this.weekStartsOn !== null) return ((+this.weekStartsOn % 7) + 7) % 7;

    try {
      const locale = new (Intl as any).Locale(this.intlLocales[0]);
      const info = locale.getWeekInfo?.() ?? locale.weekInfo;
      // ISO numbering: 1 is Monday, 7 is Sunday.
      if (info?.firstDay) return info.firstDay % 7;
    } catch {
      /* fall through to the default */
    }

    return 1;
  }

  private pad(n: number) {
    return String(n).padStart(2, '0');
  }

  private get todayIso(): string {
    const now = new Date();
    return `${now.getFullYear()}-${this.pad(now.getMonth() + 1)}-${this.pad(now.getDate())}`;
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
    // a day, which silently mislabels every cell.
    const [y, m, d] = date.split('-').map(x => parseInt(x, 10));
    return new Intl.DateTimeFormat(this.intlLocales, options).format(new Date(y, m - 1, d, 12));
  };

  private formatMonth = (monthKey: string): string => {
    if (!monthKey) return '';
    const [y, m] = monthKey.split('-').map(x => parseInt(x, 10));
    return new Intl.DateTimeFormat(this.intlLocales, { month: 'long', year: 'numeric' }).format(new Date(y, m - 1, 1, 12));
  };

  /** 7 January 2024 was a Sunday, so it indexes weekdays without a lookup table. */
  private formatWeekday = (weekday: number): string => {
    return new Intl.DateTimeFormat(this.intlLocales, { weekday: 'short' }).format(new Date(2024, 0, 7 + weekday, 12));
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
   * slot more than once, and a merged response is not necessarily ordered. Both
   * are fixed here rather than in the panel, so anything reading `days` gets
   * clean data.
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

  /** Layers the disable rules over the response without discarding anything. */
  private applyDayRules(days: BranchSlotDay[]): BranchSlotDay[] {
    const weekdays = parseWeekdayList(this.disabledWeekdays);
    const dates = parseDateList(this.disabledDates);

    if (!weekdays.length && !dates.length) return days.map(day => ({ ...day, disabled: false }));

    return days.map(day => ({ ...day, disabled: isDayBlocked(day.date, weekdays, dates) }));
  }

  private firstOpenDay(): BranchSlotDay | undefined {
    return this.days.find(day => !day.disabled);
  }

  private lastOpenDay(): BranchSlotDay | undefined {
    return [...this.days].reverse().find(day => !day.disabled);
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

      this.fetchedDays = this.normalise(await response.json());
      this.days = this.applyDayRules(this.fetchedDays);

      const firstOpen = this.firstOpenDay();

      // Every day blocked reads to a customer exactly as no days at all, so it
      // gets the empty copy rather than a month of dead cells.
      if (!firstOpen) {
        this.status = 'empty';
        return;
      }

      const preset = this.days.find(day => !day.disabled && this.defaultValue && this.defaultValue.startsWith(day.date));
      this.draftDate = (preset ?? firstOpen).date;
      this.monthKey = this.draftDate.slice(0, 7);
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
   * in sync with the media query in branch-date-dropdown.css.
   */
  private get isSheet(): boolean {
    return typeof window !== 'undefined' && window.matchMedia('(max-width: 599px)').matches;
  }

  /** Anchor to the trigger, flip up when space below is short. */
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

    // clientWidth, not innerWidth: innerWidth includes the classic scrollbar, so
    // measuring against it pushes the panel a scrollbar's width off the edge.
    const viewportWidth = document.documentElement.clientWidth;

    const width = Math.round(Math.min(Math.max(rect.width, this.minPanelWidth), viewportWidth - this.gap * 2));
    dropdown.style.setProperty('--branch-date-width', `${width}px`);

    const spaceBelow = window.innerHeight - rect.bottom - this.gap * 2;
    const spaceAbove = rect.top - this.gap * 2;
    const openUpwards = dropdown.offsetHeight > spaceBelow && spaceAbove > spaceBelow;

    // The body is a constant height, so this only ever clips it in a viewport
    // too short to hold the calendar — the panes scroll inside what is left.
    const room = Math.max(220, Math.min(this.maxPanelHeight, openUpwards ? spaceAbove : spaceBelow));
    dropdown.style.setProperty('--branch-date-max-height', `${room}px`);

    // Keep it on-screen horizontally — the panel has a min-width, so a field
    // near the right edge would otherwise hang off it.
    const left = Math.max(this.gap, Math.min(rect.left, viewportWidth - width - this.gap));
    dropdown.style.setProperty('--branch-date-left', `${left}px`);

    // Pinned by whichever edge touches the field. Anchoring upwards by `bottom`
    // rather than `top` means the animated body height cannot drag the panel
    // across the trigger as it grows.
    if (openUpwards) {
      dropdown.style.setProperty('--branch-date-top', 'auto');
      dropdown.style.setProperty('--branch-date-bottom', `${window.innerHeight - rect.top + this.gap}px`);
    } else {
      dropdown.style.setProperty('--branch-date-top', `${rect.bottom + this.gap}px`);
      dropdown.style.setProperty('--branch-date-bottom', 'auto');
    }
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
    // Escape steps back through the panel before it dismisses it — the times
    // pane is a drill-down, so leaving the field entirely would overshoot.
    if (event.key !== 'Escape') return;
    if (this.step === 'time') this.step = 'date';
    else this.isOpen = false;
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

  private get monthBounds(): { min: string; max: string } {
    return {
      min: (this.firstOpenDay()?.date ?? '').slice(0, 7),
      max: (this.lastOpenDay()?.date ?? '').slice(0, 7),
    };
  }

  /** Months with nothing bookable in them are not worth walking into. */
  private handleMonth = (delta: number) => {
    if (!this.monthKey) return;

    const [year, month] = this.monthKey.split('-').map(part => parseInt(part, 10));
    const moved = new Date(year, month - 1 + delta, 1, 12);
    const next = `${moved.getFullYear()}-${this.pad(moved.getMonth() + 1)}`;

    const { min, max } = this.monthBounds;
    if (next < min || next > max) return;

    this.monthDirection = delta < 0 ? -1 : 1;
    this.monthKey = next;
  };

  private handleDate = (date: string) => {
    if (this.days.find(day => day.date === date)?.disabled) return;
    this.draftDate = date;
    this.step = 'time';
  };

  private handleBack = () => {
    this.step = 'date';
    // Coming back from a day in another month should land on that day's month,
    // not on whatever the calendar was last scrolled to.
    if (this.draftDate) this.monthKey = this.draftDate.slice(0, 7);
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

    const { min, max } = this.monthBounds;

    const dropdownProps = {
      name: this.name,
      days: this.days,
      copy: this.copy,
      status: this.status,
      isOpen: this.isOpen,
      idleText: this.copy.idle,
      step: this.step,
      today: this.todayIso,
      weekStart: this.weekStart,
      monthKey: this.monthKey,
      monthDirection: this.monthDirection,
      canGoPrev: !!this.monthKey && this.monthKey > min,
      canGoNext: !!this.monthKey && this.monthKey < max,
      activeDate: this.draftDate,
      selectedRaw: this.selectedRaw,
      handleMonth: this.handleMonth,
      handleDate: this.handleDate,
      handleTime: this.handleTime,
      handleBack: this.handleBack,
      handleRetry: () => this.load(),
      handleDismiss: () => (this.isOpen = false),
      formatDay: this.formatDay,
      formatMonth: this.formatMonth,
      formatWeekday: this.formatWeekday,
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
              class="form-input-style form-input-select branch-date-trigger"
            />

            <div part={`${this.name}-select-icon-container form-input-select-icon-container`} class="form-input-select-icon-container">
              <ArrowUpIcon part={`${this.name}-arrow-icon select-arrow`} class="form-input-select-icon pointer-events-none! arrow cursor-pointer" />
            </div>

            {
              // Stencil only bundles a lazily-portaled component if it also sees
              // the tag in a template. Same guard shift-select uses.
              // @ts-ignore
              false && <branch-date-dropdown />
            }
            <shift-portal tag="branch-date-dropdown" inheritedClasses={this.dropdownAncestorClasses} componentProps={dropdownProps} />
          </div>

          <FormErrorMessage name={this.name} isError={!!state?.isError} errorMessage={localised?.errorTextMessage || ''} />
        </label>
      </Host>
    );
  }
}
