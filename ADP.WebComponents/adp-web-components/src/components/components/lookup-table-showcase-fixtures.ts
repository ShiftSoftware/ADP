import type { LanguageKeys } from '~features/multi-lingual';

/**
 * Fixtures and words for the lookup table's dev showcase (`lookup-table-showcase`). Everything here
 * is fictional and client-agnostic. The words are the showcase's own (it is a dev page, not shipped
 * copy, so they are not locale files): English and Arabic, because Arabic is what proves the right-to-left
 * flip. Russian and Kurdish fall back to English here.
 */

export type Visit = { branch: string | null; invoice: string | null; date: string | null; type: string | null; odometer: number | string | null };

export type LaborLine = { code: string | null; description: string | null; packageCode: string | null; serviceCode: string | null };

export type PartLine = { partNumber: string | null; partDescription: string | null; packageCode: string | null; qty: number | string | null };

export const VISITS: Visit[] = [
  { branch: 'Central', invoice: 'INV-100482', date: '2026-09-14', type: 'Periodic service, 60,000 km', odometer: 60112 },
  {
    branch: 'Riverside',
    invoice: 'INV-100377',
    date: '2026-04-02T09:30:00',
    type: 'Brake pads replaced front axle and rear axle, brake fluid renewed, wheel alignment checked and adjusted',
    odometer: 54870,
  },
  { branch: 'Central', invoice: 'INV-100119', date: '2025-11-21', type: 'Air conditioning service', odometer: 49004 },
  { branch: 'Harbour', invoice: 'INV-099871', date: '2025-06-30', type: 'Periodic service, 40,000 km', odometer: 40231 },
  { branch: 'Central', invoice: 'INV-099430', date: '2025-01-09', type: 'Battery replacement', odometer: 31590 },
  { branch: 'Riverside', invoice: 'INV-098812', date: '2024-08-17', type: 'Periodic service, 20,000 km', odometer: 20418 },
];

/** Dates, a reading and words missing, as a record can have them: every empty cell is the grey dash. */
export const BLANK_VISITS: Visit[] = [
  { branch: null, invoice: 'INV-200001', date: null, type: 'Part-only invoice', odometer: null },
  { branch: 'Central', invoice: 'INV-200002', date: '2026-02-11', type: null, odometer: 0 },
  { branch: 'Harbour', invoice: null, date: '2026-01-05', type: 'No invoice number on record', odometer: 'n/a' },
];

/**
 * The visits a second lookup of the same vehicle brings back, for the showcase's "look up again":
 * `KEEP` is the same vehicle with a new visit on top (the open invoice moved one slot down, the oldest
 * left), `DROP` is another vehicle (the open invoice is not among them).
 */
export const RELOOKUP_KEEP: Visit[] = [
  { branch: 'Harbour', invoice: 'INV-100611', date: '2026-10-20', type: 'Wiper blades replaced', odometer: 62340 },
  ...VISITS.slice(0, VISITS.length - 1),
];

export const RELOOKUP_DROP: Visit[] = [
  { branch: 'Harbour', invoice: 'INV-400210', date: '2026-08-02', type: 'Periodic service, 30,000 km', odometer: 30120 },
  { branch: 'Central', invoice: 'INV-400144', date: '2026-02-17', type: 'Tyre replacement', odometer: 24877 },
  { branch: 'Riverside', invoice: 'INV-400031', date: '2025-09-09', type: 'Periodic service, 20,000 km', odometer: 19950 },
  { branch: 'Central', invoice: 'INV-399875', date: '2025-03-26', type: 'Brake inspection', odometer: 12064 },
  { branch: 'Harbour', invoice: 'INV-399702', date: '2024-12-01', type: 'First service', odometer: 5032 },
  { branch: 'Riverside', invoice: 'INV-399650', date: '2024-10-14', type: 'Delivery inspection', odometer: 12 },
];

const BRANCHES = ['Central', 'Riverside', 'Harbour'];

/** Twenty-eight visits, newest first, so the pager has several pages at every size. */
export const MANY_VISITS: Visit[] = Array.from({ length: 28 }, (_, index) => ({
  branch: BRANCHES[index % BRANCHES.length],
  invoice: `INV-${String(300000 - index * 37).padStart(6, '0')}`,
  date: `20${26 - Math.floor(index / 12)}-${String(12 - (index % 12)).padStart(2, '0')}-${String(1 + ((index * 7) % 27)).padStart(2, '0')}`,
  type: index % 4 === 0 ? 'Periodic service' : index % 4 === 1 ? 'Tyre rotation and balancing' : index % 4 === 2 ? 'Oil and filter change' : 'Inspection',
  odometer: 62000 - index * 2150,
}));

export const LABOR_LINES: LaborLine[] = [
  { code: 'LB-1001', description: 'Engine oil and filter replaced', packageCode: 'PKG-S60', serviceCode: 'SV-10' },
  { code: 'LB-1042', description: 'Brake system inspection', packageCode: 'PKG-S60', serviceCode: 'SV-12' },
  { code: 'LB-2210', description: 'Wheel alignment, four wheels', packageCode: null, serviceCode: 'SV-31' },
  { code: 'LB-3300', description: 'Cabin air filter replaced', packageCode: '', serviceCode: 'SV-10' },
];

/** The description column is empty on every line (the service never fills it): the embedded table hides it by itself. */
export const PART_LINES: PartLine[] = [
  { partNumber: 'PT-48201', partDescription: null, packageCode: 'PKG-S60', qty: 1 },
  { partNumber: 'PT-48210', partDescription: null, packageCode: 'PKG-S60', qty: 4.2 },
  { partNumber: 'PT-77015', partDescription: null, packageCode: null, qty: 1 },
  { partNumber: 'PT-90333', partDescription: '  ', packageCode: null, qty: 0.05 },
];

/** Every column has something on some line: nothing is hidden. */
export const PART_LINES_FULL: PartLine[] = [
  { partNumber: 'PT-48201', partDescription: 'Oil filter', packageCode: 'PKG-S60', qty: 1 },
  { partNumber: 'PT-48210', partDescription: 'Engine oil, five-litre can', packageCode: 'PKG-S60', qty: 4.2 },
  { partNumber: 'PT-77015', partDescription: 'Cabin air filter', packageCode: null, qty: 1 },
];

/**
 * What a visit's drawer holds: a meta line (only the facts that exist) and its labor and part lines.
 * Derived from the invoice number (not the position, so a visit keeps its drawer when a lookup moves it), so every drawer differs a little: the part table of
 * an even visit has no description on any line (its column drops), an odd one has; every third visit has
 * no labor lines at all ("no labor lines" is a statement about a record that exists, not a verdict).
 */
export type VisitDetails = { job: string | null; parentInvoice: string | null; account: string | null; labor: LaborLine[]; parts: PartLine[] };

export const visitDetails = (visit: Visit): VisitDetails => {
  const digits = (visit.invoice ?? '').replace(/\D/g, '');
  const index = Number(digits.slice(-3) || 0);

  return {
    job: `JB-${digits.slice(-5)}`,
    parentInvoice: index % 2 === 1 ? `INV-${String(Number(digits || 0) - 11).padStart(6, '0')}` : null,
    account: index % 3 === 0 ? '  ACC-5521 ' : null,
    labor: index % 3 === 2 ? [] : LABOR_LINES.slice(0, 2 + (index % 3)),
    parts: index % 2 === 0 ? PART_LINES : PART_LINES_FULL,
  };
};

export type ShowcaseStrings = {
  title: string;
  columnsLabel: string;
  noImage: string;
  imageFailed: string;
  openImage: string;
  expandRow: string;
  collapseRow: string;
  branch: string;
  invoice: string;
  date: string;
  serviceType: string;
  odometer: string;
  details: string;
  laborLines: string;
  partLines: string;
  jobNumber: string;
  parentInvoice: string;
  accountNumber: string;
  noLaborLines: string;
  lookUpAgain: string;
  laborCode: string;
  laborDescription: string;
  packageCode: string;
  serviceCode: string;
  partNumber: string;
  partDescription: string;
  quantity: string;
  rowsPerPage: string;
  pageAll: string;
  pageRange: string;
  previousPage: string;
  nextPage: string;
  idleHint: string;
  emptyNotice: string;
  errorNotice: string;
};

const EN: ShowcaseStrings = {
  title: 'Lookup table showcase',
  columnsLabel: 'Visit list',
  noImage: 'No picture on record',
  imageFailed: 'Picture did not load',
  openImage: 'Open picture of',
  expandRow: 'Show details of',
  collapseRow: 'Hide details of',
  branch: 'Branch',
  invoice: 'Invoice no.',
  date: 'Date',
  serviceType: 'Service type',
  odometer: 'Odometer',
  details: 'Details',
  laborLines: 'Labor lines',
  partLines: 'Part lines',
  jobNumber: 'Job no.',
  parentInvoice: 'Parent invoice',
  accountNumber: 'Account no.',
  noLaborLines: 'No labor lines on this invoice',
  lookUpAgain: 'Look up again',
  laborCode: 'Labor code',
  laborDescription: 'Description',
  packageCode: 'Package code',
  serviceCode: 'Service code',
  partNumber: 'Part number',
  partDescription: 'Part description',
  quantity: 'Quantity',
  rowsPerPage: 'Rows per page',
  pageAll: 'All',
  pageRange: '{from}–{to} of {total}',
  previousPage: 'Previous page',
  nextPage: 'Next page',
  idleHint: 'Look a vehicle up',
  emptyNotice: 'No service visits on record',
  errorNotice: 'The lookup failed',
};

const AR: ShowcaseStrings = {
  title: 'عرض جدول البحث',
  columnsLabel: 'قائمة الزيارات',
  noImage: 'لا توجد صورة',
  imageFailed: 'تعذر تحميل الصورة',
  openImage: 'فتح صورة',
  expandRow: 'عرض تفاصيل',
  collapseRow: 'إخفاء تفاصيل',
  branch: 'الفرع',
  invoice: 'رقم الفاتورة',
  date: 'التاريخ',
  serviceType: 'نوع الخدمة',
  odometer: 'عداد المسافة',
  details: 'التفاصيل',
  laborLines: 'بنود العمالة',
  partLines: 'بنود القطع',
  jobNumber: 'رقم أمر العمل',
  parentInvoice: 'الفاتورة الأم',
  accountNumber: 'رقم الحساب',
  noLaborLines: 'لا توجد بنود عمالة في هذه الفاتورة',
  lookUpAgain: 'ابحث مرة أخرى',
  laborCode: 'رمز العمالة',
  laborDescription: 'الوصف',
  packageCode: 'رمز الباقة',
  serviceCode: 'رمز الخدمة',
  partNumber: 'رقم القطعة',
  partDescription: 'وصف القطعة',
  quantity: 'الكمية',
  rowsPerPage: 'صفوف في الصفحة',
  pageAll: 'الكل',
  pageRange: '{from}–{to} من {total}',
  previousPage: 'الصفحة السابقة',
  nextPage: 'الصفحة التالية',
  idleHint: 'ابحث عن مركبة',
  emptyNotice: 'لا توجد زيارات صيانة مسجلة',
  errorNotice: 'فشل البحث',
};

export const SHOWCASE_STRINGS: Record<LanguageKeys, ShowcaseStrings> = { en: EN, ar: AR, ku: EN, ru: EN };

/** The two languages that run right to left (the family's own rule: Arabic and Kurdish). */
export const showcaseDirection = (language: LanguageKeys): 'rtl' | 'ltr' => (language === 'ar' || language === 'ku' ? 'rtl' : 'ltr');
