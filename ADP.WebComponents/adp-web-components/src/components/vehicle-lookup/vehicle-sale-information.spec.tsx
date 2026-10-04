import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleSaleInformation } from './vehicle-sale-information';
import { VehicleLookup } from './vehicle-lookup';
import {
  SaleConfig,
  SaleLocale,
  asDate,
  brokerInStock,
  chainEnd,
  endCustomerCells,
  endCustomerStatement,
  hasRecords,
  headStatement,
  normalise,
  parseHiddenFields,
  readable,
  same,
  sameConfig,
  sameHead,
  saleCells,
  saleLegs,
  saleNotice,
} from './components/VehicleSaleInformationPanel';

import englishLocale from '../../locales/vehicleLookup/saleInformation/en.json';
import arabicLocale from '../../locales/vehicleLookup/saleInformation/ar.json';
import kurdishLocale from '../../locales/vehicleLookup/saleInformation/ku.json';
import russianLocale from '../../locales/vehicleLookup/saleInformation/ru.json';
import englishWarranty from '../../locales/vehicleLookup/warrantyTimeline/en.json';
import arabicWarranty from '../../locales/vehicleLookup/warrantyTimeline/ar.json';
import kurdishWarranty from '../../locales/vehicleLookup/warrantyTimeline/ku.json';
import russianWarranty from '../../locales/vehicleLookup/warrantyTimeline/ru.json';
import englishShared from '../../locales/en.json';
import arabicShared from '../../locales/ar.json';

import allocationMarketMocks from '../../features/mocks/data/generated/allocation-market/vehicle-lookup.json';
import brokerDealerMocks from '../../features/mocks/data/generated/broker-dealer/vehicle-lookup.json';
import brokerMarketMocks from '../../features/mocks/data/generated/broker-market/vehicle-lookup.json';
import edgeCaseMocks from '../../features/mocks/data/generated/edge-cases/vehicle-lookup.json';
import standardDealerMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

const DEFAULT_HIDDEN =
  'customerAccountNumber,customerID,brokerInvoiceNumber,brokerInvoiceDate,distributorInvoiceNumber,distributorInvoiceDate,warrantyActivationDate,invoiceDate,invoiceNumber,intermediaryInvoiceNumber,intermediaryInvoiceDate';

const SHOW_ALL = 'hidden-fields=""';
const CHAIN = 'show-supply-chain="true"';

const LOCALES = { en: englishLocale, ar: arabicLocale, ku: kurdishLocale, ru: russianLocale } as Record<'en' | 'ar' | 'ku' | 'ru', Record<string, string>>;
const WARRANTY = { en: englishWarranty, ar: arabicWarranty, ku: kurdishWarranty, ru: russianWarranty } as Record<'en' | 'ar' | 'ku' | 'ru', Record<string, string>>;

const FULL_CHAIN_VIN = 'ZT9DW6MP5S6570632';
const BROKER_STOCK_VIN = 'ZS8S63PG5RJ137144';
const NO_END_CUSTOMER_VIN = 'ZS8AJAYC9R6971589';
const SALE_NULL_VIN = 'ZU8ZL7VAXG2426255';
const UNAUTHORIZED_VIN = 'ZV8GHHHP37P214642';

const locale = englishLocale as unknown as SaleLocale;

const FULL_SALE = {
  countryID: '1',
  countryName: 'Meridia',
  cityID: '9',
  cityName: 'Port Vale',
  regionID: '2',
  companyID: '10',
  companyName: 'Sample Dealer',
  branchID: '100',
  branchName: 'Main Showroom',
  customerAccountNumber: 'AC-100',
  customerID: '900',
  invoiceNumber: '40257845',
  invoiceDate: '2025-07-13T00:00:00',
  warrantyActivationDate: '2025-07-20T13:45:00.000Z',
  distributor: {
    companyID: '5',
    companyName: 'Sample Distributor',
    branchID: '500',
    branchName: 'Distributor HQ',
    cityID: '8',
    cityName: 'Harbour City',
    invoiceNumber: '55120938',
    invoiceDate: '2025-06-12T08:00:00',
  },
  intermediaries: [
    { companyID: '7', companyName: 'First Importer', branchID: '700', branchName: 'Importer Hub', invoiceNumber: 'INT-1', invoiceDate: '2025-06-20' },
    { companyID: '8', companyName: 'Second Importer', invoiceNumber: 'INT-2', invoiceDate: '2025-06-25' },
  ],
  broker: {
    brokerID: 9,
    brokerName: 'Sample Broker',
    nonOfficialBrokerName: 'Street Trader',
    cityID: '4',
    cityName: 'Broker Town',
    invoiceNumber: 641,
    invoiceDate: '2025-07-23T00:00:00',
  },
  endCustomer: { id: '5', name: 'Sample Customer', phone: '+999 655 448 0379', idNumber: 'ID-417' },
};

const record = (sale: object | null = FULL_SALE, extra: object = {}) => ({ vin: 'ZS8TESTVIN0000001', isAuthorized: true, saleInformation: sale, ...extra }) as any;
const config = (hidden = '', chain = false): SaleConfig => ({ chain, hidden: parseHiddenFields(hidden) });

beforeAll(() => {
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async (mocks: unknown = brokerMarketMocks, attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleSaleInformation],
    html: `<vehicle-sale-information is-dev="true" ${attributes}></vehicle-sale-information>`,
  });
  await (page.rootInstance as VehicleSaleInformation).setMockData(mocks as any);
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleSaleInformation;
const state = (page: Page) => page.rootInstance as unknown as { leaving: boolean; reconfiguring: boolean; headLeaving: boolean };
const shadow = (page: Page) => page.root.shadowRoot;
const squash = (value?: string | null) => value?.replace(/\s+/g, ' ').trim();
const text = (page: Page, selector: string) => squash(shadow(page).querySelector(selector)?.textContent);
const all = (page: Page, selector: string) => Array.from(shadow(page).querySelectorAll(selector));
const attr = (page: Page, selector: string, name: string) => shadow(page).querySelector(selector)?.getAttribute(name);

const load = async (page: Page, vin: string | object) => {
  await owner(page).fetchVin(vin as any);
  await page.waitForChanges();
};

const settle = async (page: Page) => {
  await new Promise(resolve => setTimeout(resolve, 700));
  await page.waitForChanges();
};

const verdict = (page: Page) => attr(page, '.lookup-card', 'data-verdict');
const pill = (page: Page) => text(page, '.lookup-summary .status-badge > span');
const gridKeys = (page: Page) => all(page, '.sale-grid .sale-cell').map(cell => cell.getAttribute('data-field'));
const gridValue = (page: Page, field: string) => text(page, `.sale-grid .sale-cell[data-field="${field}"] .sale-value`);
const customerValue = (page: Page, field: string) => text(page, `.sale-group[data-group="endCustomer"] .sale-cell[data-field="${field}"] .sale-value`);
const legKinds = (page: Page) => all(page, '.sale-leg').map(leg => leg.getAttribute('data-leg'));
const leg = (page: Page, kind: string, index = 0) => all(page, `.sale-leg[data-leg="${kind}"]`)[index];
const legParty = (page: Page, kind: string, index = 0) => squash(leg(page, kind, index)?.querySelector('.sale-leg-party .sale-value-text')?.textContent);
const legNote = (page: Page, kind: string, index = 0) => squash(leg(page, kind, index)?.querySelector('.sale-leg-party .sale-value-note')?.textContent);
const legSub = (page: Page, kind: string, index = 0) => squash(leg(page, kind, index)?.querySelector('.sale-leg-sub')?.textContent);
const legCell = (page: Page, kind: string, field: string, index = 0) =>
  squash(leg(page, kind, index)?.querySelector(`.sale-leg-cell[data-field="${field}"] .sale-value`)?.textContent);

const everythingSaid = (page: Page): string => {
  const said: string[] = [shadow(page).textContent ?? ''];

  shadow(page)
    .querySelectorAll('*')
    .forEach(element => {
      const attributes = (element as Element).attributes;
      for (let index = 0; index < (attributes?.length ?? 0); index++) {
        const attribute = attributes.item(index);
        if (attribute) said.push(`${attribute.name}="${attribute.value}"`);
      }
    });

  return said.join('\n');
};

describe('vehicle-sale-information — the derivations', () => {
  it('parses hiddenFields by comma, trims, drops blanks and matches case-sensitively', () => {
    expect([...parseHiddenFields(' companyName , branchName ,, customerID ')]).toEqual(['companyName', 'branchName', 'customerID']);
    expect(parseHiddenFields('').size).toBe(0);
    expect(parseHiddenFields(undefined).size).toBe(0);
    expect(parseHiddenFields(' , ').size).toBe(0);
    expect(parseHiddenFields('CompanyName').has('companyName')).toBe(false);
    expect(parseHiddenFields(DEFAULT_HIDDEN).size).toBe(11);
  });

  it('normalises to trimmed text, keeps sentinels and zeroes as sent, and reads dates as ISO days', () => {
    expect(normalise(null)).toBe('');
    expect(normalise(undefined)).toBe('');
    expect(normalise('  ')).toBe('');
    expect(normalise(' AJ39529 ')).toBe('AJ39529');
    expect(normalise(641)).toBe('641');
    expect(normalise(0)).toBe('0');
    expect(normalise('0')).toBe('0');
    expect(normalise('UNKNOWN')).toBe('UNKNOWN');

    expect(asDate('2025-07-13T10:22:33.000Z')).toBe('2025-07-13');
    expect(asDate('2025-07-13T00:00:00')).toBe('2025-07-13');
    expect(asDate('2025-07-13')).toBe('2025-07-13');
    expect(asDate(' 2025-07-13 ')).toBe('2025-07-13');
    expect(asDate(null)).toBe('');
    expect(asDate(undefined)).toBe('');
    expect(asDate('  ')).toBe('');

    expect(same('Solmara Imports', ' solmara imports ')).toBe(true);
    expect(same('', '')).toBe(false);
    expect(same('A', 'B')).toBe(false);
  });

  it('counts values, not objects, for hasRecords — IDs, {} and blanks are not a record', () => {
    expect(hasRecords(undefined)).toBe(false);
    expect(hasRecords(record(null))).toBe(false);
    expect(hasRecords({ vin: 'X', isAuthorized: true } as any)).toBe(false);
    expect(hasRecords(record({}))).toBe(false);
    expect(hasRecords(record({ intermediaries: [] }))).toBe(false);
    expect(
      hasRecords(
        record({
          countryID: '1',
          companyID: '2',
          branchID: '3',
          regionID: '4',
          cityID: '5',
          broker: { brokerID: 6, cityID: '7' },
          distributor: { companyID: '8', branchID: '9', cityID: '10' },
          intermediaries: [{ companyID: '11', branchID: '12', cityID: '13' }],
          endCustomer: {},
        }),
      ),
    ).toBe(false);
    expect(hasRecords(record({ companyName: '  ', customerAccountNumber: ' ', endCustomer: { name: ' ' } }))).toBe(false);

    expect(hasRecords(record({ customerID: '0' }))).toBe(true);

    const single: object[] = [
      { companyName: 'X' },
      { branchName: 'X' },
      { countryName: 'X' },
      { cityName: 'X' },
      { invoiceNumber: 'X' },
      { invoiceDate: '2025-01-01' },
      { warrantyActivationDate: '2025-01-01' },
      { customerAccountNumber: 'X' },
      { customerID: 'X' },
      { broker: { brokerName: 'X' } },
      { broker: { nonOfficialBrokerName: 'X' } },
      { broker: { invoiceNumber: 641 } },
      { broker: { invoiceDate: '2025-01-01' } },
      { broker: { cityName: 'X' } },
      ...['companyName', 'branchName', 'invoiceNumber', 'invoiceDate', 'cityName'].flatMap(field => [
        { distributor: { [field]: 'X' } },
        { intermediaries: [{ companyID: '1' }, { [field]: 'X' }] },
      ]),
      { endCustomer: { id: 'X' } },
      { endCustomer: { name: 'X' } },
      { endCustomer: { phone: 'X' } },
      { endCustomer: { idNumber: 'X' } },
    ];
    single.forEach(sale => expect([sale, hasRecords(record(sale))]).toEqual([sale, true]));
  });

  it('reads a record only when it has a VIN, no error and is not unauthorized', () => {
    expect(readable(undefined)).toBe(false);
    expect(readable({ vin: '' } as any)).toBe(false);
    expect(readable({ vin: 'X' } as any)).toBe(true);
    expect(readable({ vin: 'X', isAuthorized: true } as any)).toBe(true);
    expect(readable({ vin: 'X', isAuthorized: false } as any)).toBe(false);
    expect(readable({ vin: 'X', isAuthorized: true } as any, 'Wrong response format')).toBe(false);
  });

  it('opens the notice only for an unauthorized vehicle, and never while busy', () => {
    const open = (authorized: boolean | undefined, busy = false) => saleNotice({ authorized }, busy, locale).open;

    expect(open(undefined)).toBe(false);
    expect(open(true)).toBe(false);
    expect(open(false)).toBe(true);
    expect(open(false, true)).toBe(false);
    expect(open(undefined, true)).toBe(false);
    expect(saleNotice({ authorized: false }, false, locale)).toEqual({ open: true, tone: 'neutral', message: englishLocale.unauthorizedNotice });
  });

  it('says a broker holds the vehicle only when the broker has neither an invoice number nor a date', () => {
    expect(brokerInStock(undefined)).toBe(false);
    expect(brokerInStock(record({ companyName: 'X' }))).toBe(false);
    expect(brokerInStock(record({ broker: { brokerName: 'Quartz Trading' } }))).toBe(true);
    expect(brokerInStock(record({ broker: {} }))).toBe(true);
    expect(brokerInStock(record({ broker: { brokerName: 'Q', invoiceNumber: ' ', invoiceDate: '  ' } }))).toBe(true);
    expect(brokerInStock(record({ broker: { brokerName: 'Q', invoiceNumber: 641 } }))).toBe(false);
    expect(brokerInStock(record({ broker: { brokerName: 'Q', invoiceNumber: 0 } }))).toBe(false);
    expect(brokerInStock(record({ broker: { brokerName: 'Q', invoiceDate: '2025-06-23' } }))).toBe(false);
  });

  it('follows the head grammar table', () => {
    const rich = record({ companyName: ' Harbor Auto ', branchName: 'Clearwater' });

    expect(headStatement(rich, config(''), true)).toEqual({ grammar: 'value', value: 'Harbor Auto', branch: 'Clearwater', branchSlot: true });
    expect(headStatement(rich, config('branchName'), true)).toEqual({ grammar: 'value', value: 'Harbor Auto', branch: '', branchSlot: false });
    expect(headStatement(rich, config('companyName'), true)).toEqual({ grammar: 'name', value: '', branch: '', branchSlot: false });
    expect(headStatement(rich, config('companyName,branchName'), true)).toEqual({ grammar: 'name', value: '', branch: '', branchSlot: false });
    expect(headStatement(record({ branchName: 'Clearwater' }), config(''), true)).toEqual({ grammar: 'value', value: '', branch: 'Clearwater', branchSlot: true });
    expect(headStatement(rich, config(''), false)).toEqual({ grammar: 'value', value: '', branch: '', branchSlot: true });
    expect(headStatement(undefined, config(''), false)).toEqual({ grammar: 'value', value: '', branch: '', branchSlot: true });

    const head = headStatement(rich, config(''), true);
    expect(sameHead(head, { ...head })).toBe(true);
    expect(sameHead(head, { ...head, branch: 'Glenmore' })).toBe(false);
    expect(sameHead(head, headStatement(rich, config('customerID'), true))).toBe(true);
    expect(sameHead(head, headStatement(rich, config('companyName'), true))).toBe(false);
  });

  it('builds the sale grid in order, with roles, and drops each cell its name hides', () => {
    const cells = saleCells(record(), config(''), locale, true);
    expect(cells.map(cell => cell.key)).toEqual(['location', 'invoiceNumber', 'invoiceDate', 'warrantyActivationDate', 'customerAccountNumber', 'customerID']);
    expect(cells.map(cell => cell.role)).toEqual(['word', 'code', 'figure', 'figure', 'code', 'code']);
    expect(cells.map(cell => cell.label)).toEqual([
      englishLocale.location,
      englishLocale.invoiceNumber,
      englishLocale.invoiceDate,
      englishLocale.warrantyActivationDate,
      englishLocale.customerAccountNumber,
      englishLocale.customerID,
    ]);
    expect(cells.map(cell => cell.text)).toEqual(['Meridia · Port Vale', '40257845', '2025-07-13', '2025-07-20', 'AC-100', '900']);

    expect(saleCells(record(), config('', true), locale, true).map(cell => cell.key)).toEqual(['location', 'warrantyActivationDate', 'customerAccountNumber', 'customerID']);
    expect(saleCells(record(), config('companyName'), locale, true).map(cell => cell.key)).toEqual([
      'branchName',
      'location',
      'invoiceNumber',
      'invoiceDate',
      'warrantyActivationDate',
      'customerAccountNumber',
      'customerID',
    ]);
    expect(saleCells(record(), config('companyName'), locale, true)[0]).toEqual({ key: 'branchName', label: englishLocale.branchName, role: 'word', text: 'Main Showroom' });
    expect(saleCells(record(), config('companyName,branchName'), locale, true)[0].key).toBe('location');
    expect(saleCells(record(), config('countryName'), locale, true)[0].text).toBe('Port Vale');
    expect(saleCells(record(), config('cityName'), locale, true)[0].text).toBe('Meridia');
    expect(saleCells(record(), config('countryName,cityName'), locale, true)[0].key).toBe('invoiceNumber');
    expect(saleCells(record(), config(DEFAULT_HIDDEN), locale, true).map(cell => cell.key)).toEqual(['location']);
    expect(saleCells(record(), config('countryName,cityName,invoiceNumber,invoiceDate,warrantyActivationDate,customerAccountNumber,customerID'), locale, true)).toEqual([]);

    expect(saleCells(record(), config(''), locale, false).every(cell => cell.text === '')).toBe(true);
    expect(saleCells(record(), config(''), locale, false)).toHaveLength(6);
  });

  it('states the end customer by the table: silent, present, broker stock or none', () => {
    const statement = (sale: object | null, hidden = '', canRead = true) => endCustomerStatement(record(sale), config(hidden), locale, canRead);
    const allThree = 'endCustomerName,endCustomerPhone,endCustomerIdNumber';

    expect(statement(FULL_SALE, '', false)).toEqual({ kind: 'silent', text: '' });
    expect(statement({ companyName: 'X' }, allThree)).toEqual({ kind: 'silent', text: '' });
    expect(statement({ broker: { brokerName: 'Quartz Trading' } }, allThree)).toEqual({ kind: 'silent', text: '' });
    expect(statement({ endCustomer: { name: 'A' } })).toEqual({ kind: 'present', text: '' });
    expect(statement({ endCustomer: { id: '5' } })).toEqual({ kind: 'present', text: '' });
    expect(statement({ endCustomer: { phone: '1' }, broker: { brokerName: 'Quartz Trading' } })).toEqual({ kind: 'present', text: '' });
    expect(statement({ broker: { brokerName: 'Quartz Trading' } })).toEqual({ kind: 'brokerStock', text: 'In stock at Quartz Trading', broker: 'Quartz Trading' });
    expect(statement({ broker: { brokerName: 'Quartz Trading' } }, 'brokerName')).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(statement({ broker: { brokerName: '  ' } })).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(statement({ broker: { brokerName: 'Delta', invoiceNumber: 641 } })).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(statement({ endCustomer: { name: ' ', phone: '' } })).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(statement(null)).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(statement({ companyName: 'X' }, 'endCustomerName,endCustomerPhone').kind).toBe('none');
  });

  it('builds the end-customer cells, notes an id only when it is not already the customer ID, and carries the statement in the first present cell', () => {
    const cells = (sale: object | null, hidden = '', canRead = true, chain = false) => endCustomerCells(record(sale), config(hidden, chain), locale, canRead);

    expect(cells(FULL_SALE, '', true, true)).toBeNull();
    expect(cells(FULL_SALE, 'endCustomerName,endCustomerPhone,endCustomerIdNumber')).toBeNull();

    const present = cells(FULL_SALE);
    expect(present.map(cell => [cell.key, cell.role, cell.text, cell.label])).toEqual([
      ['endCustomerName', 'word', 'Sample Customer', englishLocale.endCustomerName],
      ['endCustomerPhone', 'figure', '+999 655 448 0379', englishLocale.endCustomerPhone],
      ['endCustomerIdNumber', 'code', 'ID-417', englishLocale.endCustomerIdNumber],
    ]);
    expect(present[0].note).toBe('Customer ID 5');

    const matching = { ...FULL_SALE, customerID: '5' };
    expect(cells(matching)[0].note).toBeUndefined();
    expect(cells(matching, 'customerID')[0].note).toBe('Customer ID 5');
    expect(cells(FULL_SALE, 'endCustomerId')[0].note).toBeUndefined();

    const none = cells({ companyName: 'X' });
    expect(none.map(cell => [cell.role, cell.text])).toEqual([
      ['statement', englishLocale.noEndCustomer],
      ['figure', ''],
      ['code', ''],
    ]);

    const movedToPhone = cells({ companyName: 'X' }, 'endCustomerName');
    expect(movedToPhone.map(cell => [cell.key, cell.role, cell.text])).toEqual([
      ['endCustomerPhone', 'statement', englishLocale.noEndCustomer],
      ['endCustomerIdNumber', 'code', ''],
    ]);

    const stock = cells({ broker: { brokerName: 'Quartz Trading' } });
    expect(stock[0]).toEqual({ key: 'endCustomerName', label: englishLocale.endCustomerName, role: 'statement', text: 'In stock at Quartz Trading', note: undefined });

    const silent = cells(FULL_SALE, '', false);
    expect(silent.map(cell => [cell.role, cell.text, cell.note])).toEqual([
      ['word', '', undefined],
      ['figure', '', undefined],
      ['code', '', undefined],
    ]);
  });

  it('orders the legs for both layouts and fills each from its source', () => {
    const standard = saleLegs(record(), config(''), locale, true);
    expect(standard.map(leg => leg.key)).toEqual(['distributor', 'intermediary-0', 'intermediary-1', 'broker']);

    const chain = saleLegs(record(), config('', true), locale, true);
    expect(chain.map(leg => leg.key)).toEqual(['distributor', 'intermediary-0', 'intermediary-1', 'dealer', 'broker', 'endCustomer']);
    expect(chain.map(leg => leg.role)).toEqual([
      englishLocale.distributorName,
      englishLocale.intermediaryName,
      englishLocale.intermediaryName,
      englishLocale.companyName,
      englishLocale.brokerName,
      englishLocale.endCustomer,
    ]);
    expect(chain.map(leg => leg.index)).toEqual([undefined, 0, 1, undefined, undefined, undefined]);

    const [distributor, first, second, dealer, broker, customer] = chain;
    expect(distributor.party).toEqual({ key: 'distributorName', label: englishLocale.distributorName, role: 'word', text: 'Sample Distributor', note: undefined });
    expect(distributor.sub).toBe('Distributor HQ · Harbour City');
    expect(distributor.cells.map(cell => [cell.key, cell.role, cell.text, cell.column])).toEqual([
      ['distributorInvoiceNumber', 'code', '55120938', 1],
      ['distributorInvoiceDate', 'figure', '2025-06-12', 2],
    ]);
    expect([first.party?.text, first.sub, first.cells.map(cell => cell.key)]).toEqual(['First Importer', 'Importer Hub', ['intermediaryInvoiceNumber', 'intermediaryInvoiceDate']]);
    expect([second.party?.text, second.sub]).toEqual(['Second Importer', '']);
    expect([dealer.party?.key, dealer.party?.text, dealer.sub, dealer.cells.map(cell => cell.text)]).toEqual([
      'companyName',
      'Sample Dealer',
      'Main Showroom · Port Vale',
      ['40257845', '2025-07-13'],
    ]);
    expect([broker.party?.text, broker.party?.note, broker.sub, broker.cells.map(cell => [cell.key, cell.text])]).toEqual([
      'Sample Broker',
      'Non-official broker: Street Trader',
      'Broker Town',
      [
        ['brokerInvoiceNumber', '641'],
        ['brokerInvoiceDate', '2025-07-23'],
      ],
    ]);
    expect([customer.party?.key, customer.party?.text, customer.party?.note, customer.sub]).toEqual(['endCustomerName', 'Sample Customer', 'Customer ID 5', '']);
    expect(customer.cells.map(cell => [cell.key, cell.role, cell.text, cell.column])).toEqual([
      ['endCustomerPhone', 'figure', '+999 655 448 0379', 1],
      ['endCustomerIdNumber', 'code', 'ID-417', 2],
    ]);

    expect(saleLegs(record({ ...FULL_SALE, broker: { brokerName: 'B', invoiceNumber: 0 } }), config(''), locale, true).find(leg => leg.kind === 'broker')?.cells[0].text).toBe('0');
    expect(saleLegs(record({ ...FULL_SALE, intermediaries: [...FULL_SALE.intermediaries].reverse() }), config(''), locale, true)[1].party?.text).toBe('Second Importer');
  });

  it('folds the dealer leg into the distributor when the distributor sold it direct', () => {
    const direct = {
      companyName: 'solmara imports',
      invoiceNumber: '74670718',
      invoiceDate: '2023-01-23T00:00:00',
      distributor: { companyName: 'Solmara Imports ', invoiceNumber: '74670718', invoiceDate: '2023-01-23' },
    };
    expect(saleLegs(record(direct), config('', true), locale, true).map(leg => leg.kind)).toEqual(['distributor']);
    expect(saleLegs(record({ ...direct, invoiceNumber: '1' }), config('', true), locale, true).map(leg => leg.kind)).toEqual(['distributor', 'dealer']);
    expect(saleLegs(record({ ...direct, invoiceDate: '2023-01-24' }), config('', true), locale, true).map(leg => leg.kind)).toEqual(['distributor', 'dealer']);
    expect(saleLegs(record({ ...direct, companyName: 'Harbor Auto' }), config('', true), locale, true).map(leg => leg.kind)).toEqual(['distributor', 'dealer']);
  });

  it('keeps the dealer leg when folding it would hide a dealer value the distributor leg cannot show', () => {
    const direct = {
      companyName: 'Solmara Imports',
      invoiceNumber: '74670718',
      invoiceDate: '2023-01-23',
      distributor: { companyName: 'Solmara Imports', invoiceNumber: '74670718', invoiceDate: '2023-01-23' },
    };
    const kinds = (hidden: string) => saleLegs(record(direct), config(hidden, true), locale, true).map(leg => leg.kind);

    expect(kinds('distributorInvoiceNumber')).toEqual(['distributor', 'dealer']);
    expect(kinds('distributorInvoiceDate')).toEqual(['distributor', 'dealer']);
    expect(kinds('distributorName')).toEqual(['distributor', 'dealer']);
    expect(kinds('distributorInvoiceNumber,invoiceNumber')).toEqual(['distributor']);
    expect(kinds('invoiceNumber,invoiceDate')).toEqual(['distributor']);
  });

  it('leaves a leg out when its source is absent, carries only IDs, or has every visible field hidden', () => {
    const kinds = (sale: object, hidden = '', chain = true) => saleLegs(record(sale), config(hidden, chain), locale, true).map(leg => leg.key);

    expect(kinds({ companyName: 'D' })).toEqual(['dealer']);
    expect(kinds({ distributor: { companyID: '3', branchID: '4', cityID: '5' }, intermediaries: [{ companyID: '4' }, null], broker: { brokerID: 9, cityID: '1' } })).toEqual([]);
    expect(kinds({ endCustomer: {} })).toEqual([]);

    expect(kinds(FULL_SALE, 'intermediaries')).toEqual(['distributor', 'dealer', 'broker', 'endCustomer']);
    expect(kinds(FULL_SALE, 'distributorName,distributorBranchName,distributorInvoiceNumber,distributorInvoiceDate,cityName')).toEqual([
      'intermediary-0',
      'intermediary-1',
      'dealer',
      'broker',
      'endCustomer',
    ]);
    expect(kinds(FULL_SALE, 'companyName,branchName,invoiceNumber,invoiceDate,cityName')).toEqual(['distributor', 'intermediary-0', 'intermediary-1', 'broker', 'endCustomer']);
    expect(kinds(FULL_SALE, 'brokerName,nonOfficialBrokerName,brokerInvoiceNumber,brokerInvoiceDate,cityName')).toEqual([
      'distributor',
      'intermediary-0',
      'intermediary-1',
      'dealer',
      'endCustomer',
    ]);
    expect(kinds(FULL_SALE, 'endCustomerName,endCustomerPhone,endCustomerIdNumber')).toEqual(['distributor', 'intermediary-0', 'intermediary-1', 'dealer', 'broker']);
    expect(kinds({ ...FULL_SALE, endCustomer: null })).toEqual(['distributor', 'intermediary-0', 'intermediary-1', 'dealer', 'broker']);

    const roleOnly = saleLegs(record(FULL_SALE), config('distributorName'), locale, true)[0];
    expect([roleOnly.kind, roleOnly.party, roleOnly.cells.length]).toEqual(['distributor', undefined, 2]);

    const noCity = saleLegs(record(FULL_SALE), config('cityName', true), locale, true);
    expect(noCity.map(leg => leg.sub)).toEqual(['Distributor HQ', 'Importer Hub', '', 'Main Showroom', '', '']);

    const byDefault = saleLegs(record(FULL_SALE), config(DEFAULT_HIDDEN, true), locale, true);
    expect(byDefault.map(leg => leg.cells.map(cell => cell.key))).toEqual([[], [], [], [], [], ['endCustomerPhone', 'endCustomerIdNumber']]);

    expect(saleLegs(record(FULL_SALE), config(''), locale, false)).toEqual([]);
    expect(saleLegs(record(null), config(''), locale, true)).toEqual([]);
    expect(saleLegs(undefined, config(''), locale, true)).toEqual([]);
  });

  it('ends the chain with a statement only in the chain layout, and only for none or broker stock', () => {
    expect(chainEnd(record({ companyName: 'X' }), config(''), locale, true)).toBeUndefined();
    expect(chainEnd(record({ companyName: 'X' }), config('', true), locale, true)).toEqual({ kind: 'none', text: englishLocale.noEndCustomer });
    expect(chainEnd(record({ broker: { brokerName: 'Quartz Trading' } }), config('', true), locale, true)?.kind).toBe('brokerStock');
    expect(chainEnd(record(FULL_SALE), config('', true), locale, true)).toBeUndefined();
    expect(chainEnd(record({ companyName: 'X' }), config('endCustomerName,endCustomerPhone,endCustomerIdNumber', true), locale, true)).toBeUndefined();
    expect(chainEnd(record({ companyName: 'X' }), config('', true), locale, false)).toBeUndefined();
  });

  it('compares configs by layout and by set, not by order', () => {
    expect(sameConfig(config('a,b'), config(' b , a '))).toBe(true);
    expect(sameConfig(config('a,b'), config('a,b', true))).toBe(false);
    expect(sameConfig(config('a,b'), config('a'))).toBe(false);
    expect(sameConfig(config('a'), config('a,b'))).toBe(false);
    expect(sameConfig(config('a,c'), config('a,b'))).toBe(false);
    expect(sameConfig(config(''), config(''))).toBe(true);
  });
});

describe('vehicle-sale-information — the case matrix, vehicle by vehicle', () => {
  it('[4] says nothing but "not in distributor records" for every unauthorized fixture', async () => {
    for (const [mocks, vin] of [
      [brokerMarketMocks, 'ZV8GHHHP37P214642'],
      [allocationMarketMocks, 'ZV9LMW6D75T151181'],
      [standardDealerMocks, 'UNKNOWN_VIN_12345'],
    ] as const) {
      const page = await newPage(mocks, SHOW_ALL);
      await load(page, vin);

      expect(verdict(page)).toBe('neutral');
      expect(pill(page)).toBe(englishShared.notInRecords);
      expect(attr(page, '.sale-notice', 'data-open')).toBe('true');
      expect(text(page, '.sale-title-value')).toBe('—');
      expect(all(page, '.sale-grid .sale-value').every(value => squash(value.textContent) === '—')).toBe(true);
      expect(customerValue(page, 'endCustomerName')).toBe('—');
      expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
      expect(all(page, '.sale-leg')).toHaveLength(0);
    }
  });

  it('[5] greys "no records" for an authorized vehicle with no sale record', async () => {
    for (const [mocks, vin] of [
      [brokerMarketMocks, SALE_NULL_VIN],
      [allocationMarketMocks, 'ZS8WJEXK206451985'],
    ] as const) {
      const page = await newPage(mocks, SHOW_ALL);
      await load(page, vin);

      expect(verdict(page)).toBe('idle');
      expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-idle')).toBe(true);
      expect(pill(page)).toBe(englishShared.noRecords);
      expect(attr(page, '.sale-notice', 'data-open')).toBe('false');
      expect(text(page, '.sale-title-value')).toBe('—');
      expect(customerValue(page, 'endCustomerName')).toBe(englishLocale.noEndCustomer);
      expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
    }
  });

  it('[6] reads a sale record with nothing in it as no records', async () => {
    const page = await newPage(brokerDealerMocks, SHOW_ALL);
    await load(page, 'ZW8FL4NBXS6659473');

    expect(verdict(page)).toBe('idle');
    expect(pill(page)).toBe(englishShared.noRecords);
    expect(all(page, '.sale-grid .sale-value').every(value => value.getAttribute('data-role') === 'empty')).toBe(true);
    expect(attr(page, '.sale-legs', 'data-empty')).toBe('true');
  });

  it('[7][25][28] a dealer sale with the end customer not looked up and an activation date only', async () => {
    const page = await newPage(allocationMarketMocks, SHOW_ALL);
    await load(page, 'ZT8LY7CZ0S2959088');

    expect(verdict(page)).toBe('positive');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(text(page, '.sale-title-label')).toBe('Dealer:');
    expect(text(page, '.sale-title-value')).toBe('Lakeside Auto');
    expect(text(page, '.sale-branch')).toBe('Marlow Street');
    expect(gridValue(page, 'location')).toBe('Vessandria');
    expect(gridValue(page, 'warrantyActivationDate')).toBe('2026-02-21');
    expect(gridValue(page, 'invoiceDate')).toBe('—');
    expect(customerValue(page, 'endCustomerName')).toBe(englishLocale.noEndCustomer);
    expect(attr(page, '.sale-group .sale-cell[data-field="endCustomerName"] .sale-value', 'data-role')).toBe('statement');
    expect(customerValue(page, 'endCustomerPhone')).toBe('—');
    expect(legKinds(page)).toEqual(['distributor']);
    expect(attr(page, '.sale-legs', 'data-open')).toBe('true');
  });

  it('[8][28] a customer the records do not have, and no country on the record', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, NO_END_CUSTOMER_VIN);

    expect(customerValue(page, 'endCustomerName')).toBe(englishLocale.noEndCustomer);
    expect(gridValue(page, 'location')).toBe('—');
    expect(gridValue(page, 'customerID')).toBe('68908');
  });

  it('[9] a full end customer, with the id not repeated when it is the customer ID', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, 'ZS8HN4KB3S6512083');

    expect(customerValue(page, 'endCustomerName')).toBe('Elena Whitfield');
    expect(customerValue(page, 'endCustomerPhone')).toBe('+9994471203958');
    expect(customerValue(page, 'endCustomerIdNumber')).toBe('SM-000-417-829');
    expect(shadow(page).querySelector('.sale-group .sale-value-note')).toBeNull();
    expect(shadow(page).querySelector('.sale-group .sale-cell[data-field="endCustomerPhone"] bdi')?.getAttribute('dir')).toBe('ltr');
  });

  it('[10][26][11] partial end customers dash the missing field and nothing else', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);

    await load(page, 'ZS8SV9KXXST918958');
    expect([customerValue(page, 'endCustomerName'), customerValue(page, 'endCustomerPhone'), customerValue(page, 'endCustomerIdNumber')]).toEqual([
      'Bianca Underhill',
      '+9990090852034',
      '—',
    ]);
    expect(gridValue(page, 'invoiceDate')).toBe('2025-07-13');
    expect(gridValue(page, 'warrantyActivationDate')).toBe('—');

    await load(page, 'ZT8RM7DC8S6524196');
    expect([customerValue(page, 'endCustomerName'), customerValue(page, 'endCustomerPhone'), customerValue(page, 'endCustomerIdNumber')]).toEqual([
      '—',
      '+9994471205512',
      'SM-000-420-316',
    ]);
    expect(attr(page, '.sale-group .sale-cell[data-field="endCustomerName"] .sale-value', 'data-role')).toBe('empty');

    await load(page, 'ZU8PX3LAXS6537204');
    expect([customerValue(page, 'endCustomerName'), customerValue(page, 'endCustomerPhone'), customerValue(page, 'endCustomerIdNumber')]).toEqual([
      'Hugo Pendleton',
      '—',
      'SM-000-423-770',
    ]);

    await load(page, 'ZV8EB5GW5S6541378');
    expect(customerValue(page, 'endCustomerName')).toBe('Karim Draper');
    expect(shadow(page).querySelector('.sale-group .sale-value-note')).toBeNull();
  });

  it('[12] notes an end-customer id that differs from the customer ID, in both layouts', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, record({ ...FULL_SALE, customerID: '900', endCustomer: { id: '5', name: 'Sample Customer' } }));

    expect(text(page, '.sale-group .sale-cell[data-field="endCustomerName"] .sale-value-text')).toBe('Sample Customer');
    expect(text(page, '.sale-group .sale-cell[data-field="endCustomerName"] .sale-value-note')).toBe('Customer ID 5');

    page.root.showSupplyChain = true;
    await page.waitForChanges();
    await settle(page);
    expect(legNote(page, 'endCustomer')).toBe('Customer ID 5');
  });

  it('[13] says a broker holds the vehicle when the broker has no invoice', async () => {
    for (const [mocks, vin, broker] of [
      [brokerMarketMocks, BROKER_STOCK_VIN, 'Quartz Trading'],
      [brokerDealerMocks, 'JTEHF21A0Y0123456', 'Summit Auto Broker'],
    ] as const) {
      const page = await newPage(mocks, SHOW_ALL);
      await load(page, vin);

      expect(customerValue(page, 'endCustomerName')).toBe(`In stock at ${broker}`);
      expect(text(page, '.sale-group .sale-cell[data-field="endCustomerName"] bdi')).toBe(broker);
      expect(legKinds(page)).toContain('broker');
      expect(legParty(page, 'broker')).toBe(broker);
      expect(legCell(page, 'broker', 'brokerInvoiceNumber')).toBe('—');
    }
  });

  it('[14] a broker that sold it on, with the customer from its invoice', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(page, 'ZS8EU2ZF6ST814892');

    expect(legKinds(page)).toEqual(['distributor', 'dealer', 'broker', 'endCustomer']);
    expect(legCell(page, 'broker', 'brokerInvoiceNumber')).toBe('641');
    expect(legCell(page, 'broker', 'brokerInvoiceDate')).toBe('2025-06-23');
    expect(legParty(page, 'endCustomer')).toBe('gavin dario fletcher');
    expect(shadow(page).querySelector('.sale-chain-end')).toBeNull();
  });

  it('[15][16] an invoiced broker with no end customer says "none", never broker stock', async () => {
    for (const [mocks, vin] of [
      [brokerMarketMocks, 'ZW8CT2HN3S6553461'],
      [brokerDealerMocks, 'JTMHF33P5D5012345'],
    ] as const) {
      const page = await newPage(mocks, SHOW_ALL);
      await load(page, vin);
      expect(customerValue(page, 'endCustomerName')).toBe(englishLocale.noEndCustomer);
      expect(text(page, '.sale-card')).not.toContain('In stock');
    }
  });

  it('[17] notes a non-official broker on the broker leg', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, 'ZS9LK8FV6S6562597');

    expect(legParty(page, 'broker')).toBe('Keystone Showroom');
    expect(legNote(page, 'broker')).toBe('Non-official broker: Linden Park Car Traders');
  });

  it('[18][19][20] distributor only, a direct distributor sale, and one intermediary', async () => {
    const allocation = await newPage(allocationMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(allocation, 'ZT8LY7CZ3S2547425');
    expect(legKinds(allocation)).toEqual(['distributor', 'dealer']);

    await load(allocation, 'ZS8EU8AK206842177');
    expect(legKinds(allocation)).toEqual(['distributor', 'intermediary', 'dealer']);
    expect(legParty(allocation, 'intermediary')).toBe('Coastal Vehicle Distribution');

    const market = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(market, 'ZS8AJAYC9P6174790');
    expect(text(market, '.sale-title-value')).toBe('Solmara Vehicle Imports');
    expect(legKinds(market)).toEqual(['distributor']);
    expect(attr(market, '.sale-chain-end', 'data-kind')).toBe('none');
  });

  it('[21] three intermediaries, in the order delivered', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, 'ZU9GR4ZC4S6583745');

    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'intermediary']);
    expect([0, 1, 2].map(index => legParty(page, 'intermediary', index))).toEqual([
      'Coastal Plain Vehicle Logistics',
      'Northern Coast Motor Traders',
      'Eastern Marches Auto Wholesale',
    ]);
    expect(all(page, '.sale-leg[data-leg="intermediary"]').map(item => item.getAttribute('data-index'))).toEqual(['0', '1', '2']);
    expect(all(page, '.sale-leg[data-leg="intermediary"] .sale-leg-role').map(role => role.textContent)).toEqual(['Intermediary', 'Intermediary', 'Intermediary']);
  });

  it('[22] the whole chain, every leg invoiced', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(page, FULL_CHAIN_VIN);

    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'dealer', 'broker', 'endCustomer']);
    expect(legParty(page, 'dealer')).toBe('Harbor Auto');
    expect(legSub(page, 'dealer')).toBe('Glenmore');
    expect(legCell(page, 'dealer', 'invoiceNumber')).toBe('52117730');
    expect(legCell(page, 'dealer', 'invoiceDate')).toBe('2025-12-20');
    expect(legCell(page, 'intermediary', 'intermediaryInvoiceNumber', 1)).toBe('NCT-7731');
    expect(legCell(page, 'endCustomer', 'endCustomerIdNumber')).toBe('SM-000-731-560');
    expect(all(page, '.sale-leg-marker')).toHaveLength(6);
    expect(all(page, '.sale-leg-marker').every(marker => marker.getAttribute('aria-hidden') === 'true')).toBe(true);
    expect(text(page, `#${attr(page, '.sale-leg-list', 'aria-labelledby')}`)).toBe(englishLocale.supplyChain);
  });

  it('[23] legs with gaps show a dash in the empty slot and keep the rest', async () => {
    const page = await newPage(edgeCaseMocks, SHOW_ALL);
    await load(page, 'ZU8MC2VH8T6636219');

    expect(legCell(page, 'distributor', 'distributorInvoiceNumber')).toBe('55120938');
    expect(legCell(page, 'distributor', 'distributorInvoiceDate')).toBe('—');
    expect(legSub(page, 'distributor')).toBe('Distributor HQ');
    expect(legCell(page, 'intermediary', 'intermediaryInvoiceNumber')).toBe('—');
    expect(legCell(page, 'intermediary', 'intermediaryInvoiceDate')).toBe('2026-04-02');

    const allocation = await newPage(allocationMarketMocks, SHOW_ALL);
    await load(allocation, 'ZS8EU8AK406452424');
    expect([legCell(allocation, 'intermediary', 'intermediaryInvoiceNumber'), legCell(allocation, 'intermediary', 'intermediaryInvoiceDate')]).toEqual(['—', '—']);
    expect(legParty(allocation, 'intermediary')).toBe('Coastal Vehicle Distribution');
  });

  it('[24][32] no dealer on record keeps the legs; a dealer without a branch empties the slot', async () => {
    const page = await newPage(allocationMarketMocks, SHOW_ALL);

    await load(page, 'ZS88F5CL80J407317');
    expect(verdict(page)).toBe('positive');
    expect(text(page, '.sale-title-label')).toBe('Dealer:');
    expect(text(page, '.sale-title-value')).toBe('—');
    expect(legKinds(page)).toEqual(['distributor', 'intermediary']);

    await load(page, 'ZS8WJEXK506960999');
    expect(text(page, '.sale-title-value')).toBe('Lakeside Auto');
    expect(attr(page, '.sale-branch-slot', 'data-empty')).toBe('true');
    expect(attr(page, '.sale-branch-slot', 'aria-hidden')).toBe('true');
    expect(legCell(page, 'distributor', 'distributorInvoiceNumber')).toBe('—');
  });

  it('[27] shows the invoice and activation dates together', async () => {
    const page = await newPage(edgeCaseMocks, SHOW_ALL);
    await load(page, 'ZT8WA9PD8T6621184');

    expect(gridValue(page, 'invoiceDate')).toBe('2026-03-10');
    expect(gridValue(page, 'warrantyActivationDate')).toBe('2026-03-14');
    expect(attr(page, '.sale-grid .sale-cell[data-field="invoiceDate"] .sale-value', 'data-role')).toBe('figure');
  });

  it('[29] shows sentinels verbatim and dashes only what is blank', async () => {
    const page = await newPage(edgeCaseMocks, SHOW_ALL);
    await load(page, 'ZV8KS6EX6T6648350');

    expect(gridValue(page, 'invoiceNumber')).toBe('UNKNOWN');
    expect(gridValue(page, 'customerAccountNumber')).toBe('0');
    expect(gridValue(page, 'customerID')).toBe('—');
    expect(legCell(page, 'distributor', 'distributorInvoiceNumber')).toBe('N/A');

    const market = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(market, 'ZT8AWXSJ0R4973290');
    expect(gridValue(market, 'invoiceNumber')).toBe('0');
    expect(gridValue(market, 'customerID')).toBe('0');
    expect(gridValue(market, 'customerAccountNumber')).toBe('—');
  });

  it('[30] keeps very long names whole, with the branch in a title', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, 'ZV9NF7XK1S6596810');

    expect(text(page, '.sale-title-value')).toBe('Northern Coast Commercial, Fleet and Passenger Vehicle Trading Company');
    expect(attr(page, '.sale-branch', 'title')).toBe('Kingfisher Row Retail, Fleet Sales and Commercial Vehicle Showroom');
    expect(legNote(page, 'broker')).toBe('Non-official broker: Riverlands Family Car Sales, Part-Exchange and Vehicle Finance Consultancy');
    expect(customerValue(page, 'endCustomerName')).toBe('Regional Public Works and Municipal Infrastructure Maintenance Department');
    expect(gridValue(page, 'invoiceNumber')).toBe('SLM-RTL-2026-0000004417-A');
  });

  it('[31] renders Arabic names inside a Latin record and an all-Arabic record', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);

    await load(page, 'ZW9BH3TLXS6604927');
    expect(text(page, '.sale-title-value')).toBe('معرض الواحة للسيارات');
    expect(text(page, '.sale-branch')).toBe('فرع الميناء');
    expect(customerValue(page, 'endCustomerName')).toBe('ليلى الخطيب');

    await load(page, 'ZS8AJAYC6N6601694');
    expect(customerValue(page, 'endCustomerName')).toBe('لينا هدى العامري');

    await load(page, 'ZS8YJ5RE0S6617052');
    expect(gridValue(page, 'location')).toBe('إستاريا');
    expect(legParty(page, 'intermediary')).toBe('شركة الساحل لتوزيع المركبات');
    expect(legNote(page, 'broker')).toBe('Non-official broker: معرض النجمة للسيارات');
  });

  it('[33] shows a city on the sale and on each leg that carries one', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(page, record());

    expect(gridValue(page, 'location')).toBe('Meridia · Port Vale');
    expect(legSub(page, 'distributor')).toBe('Distributor HQ · Harbour City');
    expect(legSub(page, 'dealer')).toBe('Main Showroom · Port Vale');
    expect(legSub(page, 'broker')).toBe('Broker Town');
  });
});

const LEAKY_SALE = {
  countryID: 'COUNTRY-ID-LEAK',
  countryName: 'COUNTRY-LEAK',
  cityID: 'SALE-CITY-ID-LEAK',
  cityName: 'SALE-CITY-LEAK',
  regionID: 'REGION-ID-LEAK',
  companyID: 'COMPANY-ID-LEAK',
  companyName: 'COMPANY-LEAK',
  branchID: 'SALE-BRANCH-ID-LEAK',
  branchName: 'SALE-BRANCH-LEAK',
  customerAccountNumber: 'ACCOUNT-LEAK',
  customerID: 'CUSTOMER-ID-LEAK',
  invoiceNumber: 'INVOICE-LEAK',
  invoiceDate: '2031-01-01T00:00:00',
  warrantyActivationDate: '2031-01-02T00:00:00',
  distributor: {
    companyID: 'DIST-ID-LEAK',
    companyName: 'DIST-NAME-LEAK',
    branchID: 'DIST-BRANCH-ID-LEAK',
    branchName: 'DIST-BRANCH-LEAK',
    cityID: 'DIST-CITY-ID-LEAK',
    cityName: 'DIST-CITY-LEAK',
    invoiceNumber: 'DIST-INVOICE-LEAK',
    invoiceDate: '2031-01-03',
  },
  intermediaries: [
    {
      companyID: 'INTER-ID-LEAK',
      companyName: 'INTER-NAME-LEAK',
      branchID: 'INTER-BRANCH-ID-LEAK',
      branchName: 'INTER-BRANCH-LEAK',
      cityID: 'INTER-CITY-ID-LEAK',
      cityName: 'INTER-CITY-LEAK',
      invoiceNumber: 'INTER-INVOICE-LEAK',
      invoiceDate: '2031-01-04',
    },
    { companyID: 'SECOND-ID-LEAK', companyName: 'SECOND-NAME-LEAK', invoiceNumber: 'SECOND-INVOICE-LEAK', invoiceDate: '2031-01-05' },
  ],
  broker: {
    brokerID: 424242,
    brokerName: 'BROKER-NAME-LEAK',
    nonOfficialBrokerName: 'NONOFFICIAL-LEAK',
    cityID: 'BROKER-CITY-ID-LEAK',
    cityName: 'BROKER-CITY-LEAK',
    invoiceNumber: 868686,
    invoiceDate: '2031-01-06',
  },
  endCustomer: { id: 'EC-ID-LEAK', name: 'EC-NAME-LEAK', phone: 'EC-PHONE-LEAK', idNumber: 'EC-IDNUMBER-LEAK' },
};

const SAID_VALUES = [
  'COUNTRY-LEAK',
  'SALE-CITY-LEAK',
  'COMPANY-LEAK',
  'SALE-BRANCH-LEAK',
  'ACCOUNT-LEAK',
  'CUSTOMER-ID-LEAK',
  'INVOICE-LEAK',
  '2031-01-01',
  '2031-01-02',
  'DIST-NAME-LEAK',
  'DIST-BRANCH-LEAK',
  'DIST-CITY-LEAK',
  'DIST-INVOICE-LEAK',
  '2031-01-03',
  'INTER-NAME-LEAK',
  'INTER-BRANCH-LEAK',
  'INTER-CITY-LEAK',
  'INTER-INVOICE-LEAK',
  '2031-01-04',
  'SECOND-NAME-LEAK',
  'SECOND-INVOICE-LEAK',
  '2031-01-05',
  'BROKER-NAME-LEAK',
  'NONOFFICIAL-LEAK',
  'BROKER-CITY-LEAK',
  '868686',
  '2031-01-06',
  'EC-ID-LEAK',
  'EC-NAME-LEAK',
  'EC-PHONE-LEAK',
  'EC-IDNUMBER-LEAK',
];

const NEVER_SAID = [
  'COUNTRY-ID-LEAK',
  'SALE-CITY-ID-LEAK',
  'REGION-ID-LEAK',
  'COMPANY-ID-LEAK',
  'SALE-BRANCH-ID-LEAK',
  'DIST-ID-LEAK',
  'DIST-BRANCH-ID-LEAK',
  'DIST-CITY-ID-LEAK',
  'INTER-ID-LEAK',
  'INTER-BRANCH-ID-LEAK',
  'INTER-CITY-ID-LEAK',
  'SECOND-ID-LEAK',
  '424242',
  'BROKER-CITY-ID-LEAK',
];

const OLD_POSSESSION_SENTENCES = [
  'The vehicle is in the possession of the authorized dealer/trader.',
  'المركبة بحوزة الوكيل/التاجر المعتمد.',
  'ئۆتۆمبێلەکە لەبەردەستی بریکار/بازرگانی ڕێگەپێدراودایە.',
  'Автомобиль находится во владении авторизованного дилера/торговца.',
];

const POSSESSION_WORDS = ['possession', 'بحوزة', 'لەبەردەستی', 'владении', 'Vehicle has no end customer.'];

describe('vehicle-sale-information — the invariants', () => {
  it('says nothing from a fully populated sale record for an unauthorized vehicle, in either layout', async () => {
    for (const chain of [false, true]) {
      const page = await newPage(brokerMarketMocks, `${SHOW_ALL} show-supply-chain="${chain}"`);
      await load(page, { vin: 'ZZ9NOTOURS0000001', isAuthorized: false, saleInformation: LEAKY_SALE });

      expect(verdict(page)).toBe('neutral');
      expect(pill(page)).toBe(englishShared.notInRecords);
      const notice = shadow(page).querySelector('.sale-notice');
      expect(notice?.getAttribute('data-open')).toBe('true');
      expect(notice?.getAttribute('data-tone')).toBe('neutral');
      expect(notice?.querySelector('.sale-notice-body')?.getAttribute('role')).toBe('status');
      expect(squash(notice?.textContent)).toBe(englishLocale.unauthorizedNotice);
      expect(all(page, '.sale-grid .sale-value').every(value => value.getAttribute('data-role') === 'empty')).toBe(true);
      expect(all(page, '.sale-leg')).toHaveLength(0);
      expect(shadow(page).querySelector('.sale-chain-end')).toBeNull();
      expect(text(page, '.sale-card')).not.toContain(englishLocale.noEndCustomer);

      const said = everythingSaid(page);
      [...SAID_VALUES, ...NEVER_SAID].forEach(leak => expect([chain, leak, said.includes(leak)]).toEqual([chain, leak, false]));
    }
  });

  it('proves the audit reaches every value: an authorized copy says all of them, and still no ID', async () => {
    for (const chain of [false, true]) {
      const page = await newPage(brokerMarketMocks, `${SHOW_ALL} show-supply-chain="${chain}"`);
      await load(page, { vin: 'ZZ9NOTOURS0000001', isAuthorized: true, saleInformation: LEAKY_SALE });

      const said = everythingSaid(page);
      SAID_VALUES.forEach(value => expect([chain, value, said.includes(value)]).toEqual([chain, value, true]));
      NEVER_SAID.forEach(value => expect([chain, value, said.includes(value)]).toEqual([chain, value, false]));
      expect(said).toContain(`data-label="${englishLocale.invoiceDate}"`);
      expect(said).toContain('title="SALE-BRANCH-LEAK"');
      expect(said.split('\n').length).toBeGreaterThan(50);
    }
  });

  it('drops the previous vehicle s legs once an unauthorized one has settled', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, { vin: 'ZT9TESTVIN0000001', isAuthorized: true, saleInformation: LEAKY_SALE });
    expect(all(page, '.sale-leg').length).toBeGreaterThan(0);

    await load(page, { vin: 'ZZ9NOTOURS0000001', isAuthorized: false, saleInformation: { companyName: 'OTHER-LEAK' } });
    await settle(page);

    expect(all(page, '.sale-leg')).toHaveLength(0);
    const said = everythingSaid(page);
    [...SAID_VALUES, 'OTHER-LEAK'].forEach(leak => expect([leak, said.includes(leak)]).toEqual([leak, false]));
  });

  it('never claims where the vehicle is when there is no end customer, in any language', async () => {
    Object.entries(LOCALES).forEach(([lang, words]) => {
      const values = Object.values(words).join('\n');
      OLD_POSSESSION_SENTENCES.forEach(sentence => expect([lang, values.includes(sentence)]).toEqual([lang, false]));
      POSSESSION_WORDS.forEach(word => expect([lang, word, values.includes(word)]).toEqual([lang, word, false]));
      expect([lang, words.noEndCustomer.includes(words.companyName)]).toEqual([lang, false]);
      expect([lang, words.noEndCustomer.includes(WARRANTY[lang as 'en'].dealer)]).toEqual([lang, false]);
      expect([lang, words.inBrokerStock.includes(words.companyName)]).toEqual([lang, false]);
    });
    expect(englishLocale.noEndCustomer.toLowerCase()).not.toContain('dealer');
    expect(englishLocale.noEndCustomer.toLowerCase()).not.toContain('trader');

    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, NO_END_CUSTOMER_VIN);
    for (const lang of ['en', 'ar', 'ku', 'ru'] as const) {
      await owner(page).changeLanguage(lang);
      await page.waitForChanges();
      expect(customerValue(page, 'endCustomerName')).toBe(LOCALES[lang].noEndCustomer);
      const said = everythingSaid(page);
      [...OLD_POSSESSION_SENTENCES, ...POSSESSION_WORDS].forEach(word => expect([lang, word, said.includes(word)]).toEqual([lang, word, false]));
    }
  });

  it('says broker stock only when a broker is present with neither invoice number nor date', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    const stock = () => (customerValue(page, 'endCustomerName') ?? '').startsWith('In stock at');

    await load(page, BROKER_STOCK_VIN);
    expect(stock()).toBe(true);

    for (const vin of ['ZS8EU2ZF6ST814892', 'ZW8CT2HN3S6553461', NO_END_CUSTOMER_VIN, 'ZS9LK8FV6S6562597']) {
      await load(page, vin);
      expect([vin, stock()]).toEqual([vin, false]);
    }

    await load(page, record({ companyName: 'D', broker: { brokerName: 'Quartz Trading', invoiceNumber: 12 } }));
    expect(stock()).toBe(false);
    await load(page, record({ companyName: 'D', broker: { brokerName: 'Quartz Trading', invoiceDate: '2025-01-01' } }));
    expect(stock()).toBe(false);
    await load(page, record({ companyName: 'D', broker: { brokerName: 'Quartz Trading' } }));
    expect(stock()).toBe(true);
  });

  it('keeps the two empties apart: a blank before a lookup, a dash for a vehicle with nothing in the slot', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    const blanks = () => all(page, '.sale-body .sale-value-content[data-empty="true"]').length;

    expect(blanks()).toBe(9);
    expect(text(page, '.sale-body')).not.toContain('—');
    expect(text(page, '.sale-title-value')).toBe('—');

    await load(page, SALE_NULL_VIN);
    expect(blanks()).toBe(0);
    expect(gridValue(page, 'invoiceNumber')).toBe('—');

    await owner(page).clearData();
    await page.waitForChanges();
    expect(blanks()).toBe(9);
    expect(text(page, '.sale-body')).not.toContain('—');
  });

  it('keeps its anchors in every state and slides the notice above the strip', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    const anchors = () => {
      expect(text(page, '.sale-lead-caption-label')).toBe('Sale');
      expect(all(page, '.sale-head > .lookup-head-wait')).toHaveLength(1);
      expect(shadow(page).querySelector('.sale-head')?.lastElementChild?.classList.contains('lookup-head-wait')).toBe(true);
      expect(shadow(page).querySelector('.sale-legs')).not.toBeNull();
      expect(shadow(page).querySelector('.sale-body')).not.toBeNull();
      expect(all(page, '.sale-grid .sale-value.shift-skeleton.resize-settle').length).toBe(all(page, '.sale-grid .sale-value').length);
    };

    anchors();
    expect(attr(page, '.sale-notice', 'aria-hidden')).toBe('true');

    await load(page, FULL_CHAIN_VIN);
    anchors();

    await load(page, UNAUTHORIZED_VIN);
    anchors();
    expect(attr(page, '.sale-notice', 'aria-hidden')).toBeNull();
    const order = all(page, '.sale-notice, .sale-lead, .sale-body').map(element => element.className.split(' ')[0]);
    expect(order).toEqual(['sale-notice', 'sale-lead', 'sale-body']);

    await owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    anchors();
    expect(all(page, '.sale-leg-value.shift-skeleton, .sale-leg .shift-skeleton')).toHaveLength(0);
  });
});

describe('vehicle-sale-information — the bug fixes', () => {
  it('shows the invoice number as text, never through a date formatter', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, 'ZS8SV9KXXST918958');
    expect(gridValue(page, 'invoiceNumber')).toBe('40257845');
    expect(attr(page, '.sale-grid .sale-cell[data-field="invoiceNumber"] .sale-value', 'data-role')).toBe('code');

    const edge = await newPage(edgeCaseMocks, SHOW_ALL);
    await load(edge, 'ZV8KS6EX6T6648350');
    expect(gridValue(edge, 'invoiceNumber')).toBe('UNKNOWN');
  });

  it('formats every date as an ISO day from a full timestamp, in every language', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, record());

    for (const lang of ['en', 'ar', 'ku', 'ru'] as const) {
      await owner(page).changeLanguage(lang);
      await page.waitForChanges();
      expect([lang, gridValue(page, 'invoiceDate'), gridValue(page, 'warrantyActivationDate')]).toEqual([lang, '2025-07-13', '2025-07-20']);
      expect([lang, legCell(page, 'distributor', 'distributorInvoiceDate'), legCell(page, 'broker', 'brokerInvoiceDate')]).toEqual([lang, '2025-06-12', '2025-07-23']);
    }
    expect(attr(page, '.sale-grid .sale-cell[data-field="warrantyActivationDate"] .sale-value', 'data-role')).toBe('figure');
    expect(shadow(page).querySelector('.sale-grid .sale-cell[data-field="warrantyActivationDate"] bdi')?.getAttribute('dir')).toBe('ltr');
  });

  it('labels the end customer s name as a name, and trims every label', () => {
    expect(englishLocale.endCustomerName).toBe('Name');
    expect(russianLocale.endCustomerName).toBe('Имя');
    expect(englishLocale.endCustomerName.toLowerCase()).not.toContain('number');
    expect(russianLocale.endCustomerName).not.toContain('Номер');
    Object.entries(LOCALES).forEach(([lang, words]) =>
      Object.entries(words).forEach(([key, value]) => expect([lang, key, value === value.trim() && value.length > 0]).toEqual([lang, key, true])),
    );
  });

  it('calls the dealer by the warranty panel s word in every language, with no brand', () => {
    Object.keys(LOCALES).forEach(lang => expect([lang, LOCALES[lang as 'en'].companyName]).toEqual([lang, WARRANTY[lang as 'en'].dealer]));
    expect(arabicLocale.companyName).toBe('الوكيل');
    expect(arabicLocale.companyName.split(' ')).toHaveLength(1);
    expect(arabicLocale.brokerName).not.toBe(arabicLocale.intermediaryName);
  });

  it('uses no sentence as a key, and keeps type.ts and the four files in step', () => {
    const typeSource = readFileSync(join(__dirname, '../../locales/vehicleLookup/saleInformation/type.ts'), 'utf8');
    const schemaKeys = (typeSource.match(/yupTypeMapper\(\[([\s\S]*?)\]\)/)?.[1].match(/'([^']+)'/g) ?? []).map(key => key.slice(1, -1)).sort();

    expect(schemaKeys.length).toBeGreaterThan(20);
    Object.entries(LOCALES).forEach(([lang, words]) => {
      expect([lang, Object.keys(words).sort()]).toEqual([lang, schemaKeys]);
      Object.keys(words).forEach(key => expect([lang, key, /^[A-Za-z]+$/.test(key)]).toEqual([lang, key, true]));
    });
    expect(schemaKeys).toEqual(expect.arrayContaining(['noEndCustomer', 'inBrokerStock', 'unauthorizedNotice']));
    Object.values(LOCALES).forEach(words => expect(words.inBrokerStock).toContain('{broker}'));
  });
});

describe('vehicle-sale-information — hiddenFields', () => {
  const HIDES: [string, string, 'standard' | 'chain' | 'both'][] = [
    ['companyName', 'Harbor Auto', 'both'],
    ['branchName', 'Glenmore', 'both'],
    ['customerAccountNumber', 'HP90107', 'both'],
    ['customerID', '900107', 'both'],
    ['brokerName', 'Atlas Fleet Services', 'both'],
    ['brokerInvoiceNumber', '7107', 'both'],
    ['brokerInvoiceDate', '2026-01-14', 'both'],
    ['distributorName', 'Solmara Vehicle Imports', 'both'],
    ['distributorInvoiceNumber', '48218824', 'both'],
    ['distributorInvoiceDate', '2025-12-02', 'both'],
    ['invoiceDate', '2025-12-20', 'both'],
    ['invoiceNumber', '52117730', 'both'],
    ['endCustomerName', 'Omar Holloway', 'both'],
    ['endCustomerPhone', '+999 640 218 7733', 'both'],
    ['endCustomerIdNumber', 'SM-000-731-560', 'both'],
    ['intermediaryName', 'Coastal Plain Vehicle Logistics', 'both'],
    ['intermediaryBranchName', 'Harbour Lane', 'both'],
    ['intermediaryInvoiceNumber', 'CPL-25-10442', 'both'],
    ['intermediaryInvoiceDate', '2025-12-09', 'both'],
  ];

  it('keeps the default string exactly, with the two intermediary invoice names added', async () => {
    const page = await newPage();
    expect(owner(page).hiddenFields).toBe(DEFAULT_HIDDEN);
    expect(page.root.hiddenFields).toBe(DEFAULT_HIDDEN);
  });

  it('shows today s default output: location, the end customer and the parties without invoices', async () => {
    const page = await newPage(brokerMarketMocks);
    await load(page, FULL_CHAIN_VIN);

    expect(gridKeys(page)).toEqual(['location']);
    expect(all(page, '.sale-group .sale-cell').map(cell => cell.getAttribute('data-field'))).toEqual(['endCustomerName', 'endCustomerPhone', 'endCustomerIdNumber']);
    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'broker']);
    expect(all(page, '.sale-leg-cell')).toHaveLength(0);
    expect(legSub(page, 'intermediary')).toBe('Harbour Lane');
    expect(text(page, '.sale-branch')).toBe('Glenmore');
  });

  for (const layout of ['standard', 'chain'] as const) {
    it(`hides what every name hid, and each new name, in the ${layout} layout`, async () => {
      const chain = layout === 'chain' ? CHAIN : '';

      const shown = await newPage(brokerMarketMocks, `${SHOW_ALL} ${chain}`);
      await load(shown, FULL_CHAIN_VIN);
      const fullText = text(shown, '.sale-card');

      for (const [name, value] of HIDES) {
        expect([name, fullText.includes(value)]).toEqual([name, true]);

        const page = await newPage(brokerMarketMocks, `hidden-fields="${name}" ${chain}`);
        await owner(page).fetchVin((brokerMarketMocks as Record<string, any>)[FULL_CHAIN_VIN]);
        await page.waitForChanges();
        expect([name, layout, text(page, '.sale-card').includes(value)]).toEqual([name, layout, false]);
        expect([name, all(page, `[data-field="${name}"]`).length]).toEqual([name, 0]);
      }
    });
  }

  it('hides the warranty activation date, the location parts and the whole intermediary legs', async () => {
    for (const chain of ['', CHAIN]) {
      const page = await newPage(edgeCaseMocks, `hidden-fields="warrantyActivationDate" ${chain}`);
      await load(page, 'ZT8WA9PD8T6621184');
      expect(text(page, '.sale-card')).not.toContain('2026-03-14');

      const cities = await newPage(brokerMarketMocks, `hidden-fields="cityName" ${chain}`);
      await load(cities, record());
      expect(gridValue(cities, 'location')).toBe('Meridia');
      expect(text(cities, '.sale-card')).not.toContain('Harbour City');
      expect(text(cities, '.sale-card')).not.toContain('Broker Town');

      const countries = await newPage(brokerMarketMocks, `hidden-fields="countryName" ${chain}`);
      await load(countries, record());
      expect(gridValue(countries, 'location')).toBe('Port Vale');

      const neither = await newPage(brokerMarketMocks, `hidden-fields="countryName,cityName" ${chain}`);
      await load(neither, record());
      expect(gridKeys(neither)).not.toContain('location');

      const intermediaries = await newPage(brokerMarketMocks, `hidden-fields="intermediaries" ${chain}`);
      await load(intermediaries, FULL_CHAIN_VIN);
      expect(legKinds(intermediaries)).not.toContain('intermediary');
      expect(text(intermediaries, '.sale-card')).not.toContain('Northern Coast Motor Traders');
    }
  });

  it('hides the new names: the non-official broker, the end-customer id and the distributor branch', async () => {
    for (const chain of ['', CHAIN]) {
      const broker = await newPage(brokerMarketMocks, `hidden-fields="nonOfficialBrokerName" ${chain}`);
      await load(broker, 'ZS9LK8FV6S6562597');
      expect(legParty(broker, 'broker')).toBe('Keystone Showroom');
      expect(legNote(broker, 'broker')).toBeUndefined();

      const id = await newPage(brokerMarketMocks, `hidden-fields="endCustomerId" ${chain}`);
      await load(id, record());
      expect(text(id, '.sale-card')).not.toContain('Customer ID 5');
      expect(text(id, '.sale-card')).toContain('Sample Customer');

      const branch = await newPage(edgeCaseMocks, `hidden-fields="distributorBranchName" ${chain}`);
      await load(branch, 'ZU8MC2VH8T6636219');
      expect(text(branch, '.sale-card')).not.toContain('Distributor HQ');
      expect(legParty(branch, 'distributor')).toBe('Sample Distributor');
    }
  });

  it('switches the head to the name grammar and moves the branch into the grid when the company is hidden', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="companyName"');
    await load(page, FULL_CHAIN_VIN);

    expect(text(page, '.sale-title-name')).toBe(englishLocale.vehicleSaleInformation);
    expect(shadow(page).querySelector('.sale-title-label')).toBeNull();
    expect(attr(page, '.sale-branch-slot', 'data-empty')).toBe('true');
    expect(attr(page, '.sale-branch-slot', 'aria-hidden')).toBe('true');
    expect(gridKeys(page)).toEqual(['branchName', 'location', 'invoiceNumber', 'invoiceDate', 'warrantyActivationDate', 'customerAccountNumber', 'customerID']);
    expect(gridValue(page, 'branchName')).toBe('Glenmore');

    const both = await newPage(brokerMarketMocks, 'hidden-fields="companyName,branchName"');
    await load(both, FULL_CHAIN_VIN);
    expect(text(both, '.sale-title-name')).toBe(englishLocale.vehicleSaleInformation);
    expect(gridKeys(both)).not.toContain('branchName');

    const branchOnly = await newPage(brokerMarketMocks, 'hidden-fields="branchName"');
    await load(branchOnly, FULL_CHAIN_VIN);
    expect(text(branchOnly, '.sale-title-value')).toBe('Harbor Auto');
    expect(attr(branchOnly, '.sale-branch-slot', 'data-empty')).toBe('true');
    expect(gridKeys(branchOnly)).not.toContain('branchName');
  });

  it('takes the end-customer group or leg away when all three of its names are hidden, and moves the statement when the name alone is', async () => {
    const allThree = 'endCustomerName,endCustomerPhone,endCustomerIdNumber';

    const standard = await newPage(brokerMarketMocks, `hidden-fields="${allThree}"`);
    await load(standard, NO_END_CUSTOMER_VIN);
    expect(shadow(standard).querySelector('.sale-group[data-group="endCustomer"]')).toBeNull();
    expect(text(standard, '.sale-card')).not.toContain(englishLocale.noEndCustomer);

    const chain = await newPage(brokerMarketMocks, `hidden-fields="${allThree}" ${CHAIN}`);
    await load(chain, NO_END_CUSTOMER_VIN);
    expect(shadow(chain).querySelector('.sale-chain-end')).toBeNull();
    await load(chain, FULL_CHAIN_VIN);
    expect(legKinds(chain)).not.toContain('endCustomer');

    const nameOnly = await newPage(brokerMarketMocks, 'hidden-fields="endCustomerName"');
    await load(nameOnly, NO_END_CUSTOMER_VIN);
    expect(customerValue(nameOnly, 'endCustomerPhone')).toBe(englishLocale.noEndCustomer);
    expect(attr(nameOnly, '.sale-group .sale-cell[data-field="endCustomerPhone"] .sale-value', 'data-role')).toBe('statement');
  });

  it('keeps a distributor leg with only its role when its name is hidden and its invoices are not', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="distributorName"');
    await load(page, FULL_CHAIN_VIN);

    const distributor = leg(page, 'distributor');
    expect(distributor).toBeDefined();
    expect(squash(distributor.querySelector('.sale-leg-role')?.textContent)).toBe(englishLocale.distributorName);
    expect(distributor.querySelector('.sale-leg-party .sale-value')).toBeNull();
    expect(legCell(page, 'distributor', 'distributorInvoiceNumber')).toBe('48218824');
  });

  it('leaves the grid empty under its caption when every sale-grid name is hidden', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="countryName,cityName,invoiceNumber,invoiceDate,warrantyActivationDate,customerAccountNumber,customerID"');
    await load(page, FULL_CHAIN_VIN);

    expect(all(page, '.sale-grid .sale-cell')).toHaveLength(0);
    expect(text(page, '.sale-lead-caption-label')).toBe('Sale');
    expect(verdict(page)).toBe('positive');
  });

  it('silences broker stock when the broker s name is hidden, and says "none" instead', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="brokerName"');
    await load(page, BROKER_STOCK_VIN);

    expect(customerValue(page, 'endCustomerName')).toBe(englishLocale.noEndCustomer);
    expect(text(page, '.sale-card')).not.toContain('Quartz Trading');
  });

  it('reads records by value whatever is hidden', async () => {
    const page = await newPage(
      allocationMarketMocks,
      'hidden-fields="companyName,branchName,countryName,warrantyActivationDate,distributorName,distributorInvoiceNumber,distributorInvoiceDate,endCustomerName,endCustomerPhone,endCustomerIdNumber"',
    );
    await load(page, 'ZT8LY7CZ0S2959088');

    expect(verdict(page)).toBe('positive');
    expect(pill(page)).toBeUndefined();
  });

  it('trims whitespace around names and matches them case-sensitively', async () => {
    const spaced = await newPage(brokerMarketMocks, 'hidden-fields="  companyName ,  branchName  "');
    await load(spaced, FULL_CHAIN_VIN);
    expect(text(spaced, '.sale-title-name')).toBe(englishLocale.vehicleSaleInformation);
    expect(gridKeys(spaced)).not.toContain('branchName');

    const cased = await newPage(brokerMarketMocks, 'hidden-fields="CompanyName,BRANCHNAME"');
    await load(cased, FULL_CHAIN_VIN);
    expect(text(cased, '.sale-title-value')).toBe('Harbor Auto');
    expect(text(cased, '.sale-branch')).toBe('Glenmore');
  });
});

describe('vehicle-sale-information — the supply chain flag', () => {
  it('defaults off and shows the standard layout', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    expect(owner(page).showSupplyChain).toBe(false);

    await load(page, FULL_CHAIN_VIN);
    expect(attr(page, '.sale-card', 'data-layout')).toBe('standard');
    expect(shadow(page).querySelector('.sale-group[data-group="endCustomer"]')).not.toBeNull();
    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'broker']);
    expect(all(page, '.sale-leg-marker')).toHaveLength(0);
    expect(gridKeys(page)).toContain('invoiceNumber');
    expect(text(page, '.sale-legs .sale-group-label')).toBe(englishLocale.supplyChain);
  });

  it('on, orders the chain distributor → intermediaries → dealer → broker → end customer', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(page, FULL_CHAIN_VIN);

    expect(attr(page, '.sale-card', 'data-layout')).toBe('chain');
    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'dealer', 'broker', 'endCustomer']);
    expect(all(page, '.sale-leg-role').map(role => squash(role.textContent))).toEqual([
      'Distributor',
      'Intermediary',
      'Intermediary',
      'Dealer',
      'Broker',
      englishLocale.endCustomer,
    ]);
    expect(shadow(page).querySelector('.sale-group[data-group="endCustomer"]')).toBeNull();
    expect(gridKeys(page)).toEqual(['location', 'warrantyActivationDate', 'customerAccountNumber', 'customerID']);
  });

  it('ends the chain with a statement, not a leg, when there is no end customer', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);

    await load(page, BROKER_STOCK_VIN);
    expect(attr(page, '.sale-chain-end', 'data-kind')).toBe('brokerStock');
    expect(text(page, '.sale-chain-end')).toBe('In stock at Quartz Trading');
    expect(text(page, '.sale-chain-end bdi')).toBe('Quartz Trading');
    expect(legKinds(page)).not.toContain('endCustomer');
    expect(shadow(page).querySelector('.sale-chain-end .sale-leg-marker')).toBeNull();

    await load(page, NO_END_CUSTOMER_VIN);
    expect(attr(page, '.sale-chain-end', 'data-kind')).toBe('none');
    expect(text(page, '.sale-chain-end')).toBe(englishLocale.noEndCustomer);

    await load(page, FULL_CHAIN_VIN);
    expect(shadow(page).querySelector('.sale-chain-end')).toBeNull();
  });

  it('reconfigures live: the body shuts over the old layout, swaps unseen and reopens, with no loading state', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    page.root.showSupplyChain = true;
    await page.waitForChanges();

    expect(state(page).reconfiguring).toBe(true);
    expect(attr(page, '.sale-card', 'data-layout')).toBe('standard');
    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'broker']);
    expect(attr(page, '.sale-body', 'data-open')).toBe('false');
    expect(attr(page, '.sale-body', 'aria-hidden')).toBe('true');
    expect(owner(page).isLoading).toBe(false);
    expect(state(page).leaving).toBe(false);
    expect(shadow(page).querySelector('.loading')).toBeNull();
    expect(attr(page, '.lookup-card', 'data-phase')).toBe('settled');
    expect(attr(page, '.sale-card', 'data-phase')).toBe('settled');
    expect(attr(page, '.sale-card', 'data-head')).toBe('settled');
    expect(attr(page, '.sale-legs', 'data-open')).toBe('true');

    await settle(page);

    expect(state(page).reconfiguring).toBe(false);
    expect(attr(page, '.sale-card', 'data-layout')).toBe('chain');
    expect(attr(page, '.sale-body', 'data-open')).toBe('true');
    expect(attr(page, '.sale-body', 'aria-hidden')).toBeNull();
    expect(legKinds(page)).toEqual(['distributor', 'intermediary', 'intermediary', 'dealer', 'broker', 'endCustomer']);
    expect(verdict(page)).toBe('positive');
    expect(heard).toEqual([]);

    page.root.showSupplyChain = false;
    await page.waitForChanges();
    expect(attr(page, '.sale-card', 'data-layout')).toBe('chain');
    await settle(page);
    expect(attr(page, '.sale-card', 'data-layout')).toBe('standard');
  });

  it('lets the head leave only when the head s words change', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);

    page.root.hiddenFields = 'customerID';
    await page.waitForChanges();
    expect(state(page).reconfiguring).toBe(true);
    expect(attr(page, '.sale-card', 'data-head')).toBe('settled');
    await settle(page);
    expect(gridKeys(page)).not.toContain('customerID');

    page.root.hiddenFields = 'customerID,companyName';
    await page.waitForChanges();
    expect(attr(page, '.sale-card', 'data-head')).toBe('leaving');
    expect(text(page, '.sale-title-value')).toBe('Harbor Auto');
    expect(owner(page).isLoading).toBe(false);
    await settle(page);
    expect(attr(page, '.sale-card', 'data-head')).toBe('settled');
    expect(text(page, '.sale-title-name')).toBe(englishLocale.vehicleSaleInformation);
  });

  it('cancels a reconfigure when the config returns to the applied one', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);

    page.root.showSupplyChain = true;
    await page.waitForChanges();
    expect(state(page).reconfiguring).toBe(true);

    page.root.showSupplyChain = false;
    await page.waitForChanges();
    expect(state(page).reconfiguring).toBe(false);
    expect(attr(page, '.sale-body', 'data-open')).toBe('true');

    await settle(page);
    expect(attr(page, '.sale-card', 'data-layout')).toBe('standard');
    expect(attr(page, '.sale-body', 'data-open')).toBe('true');

    page.root.hiddenFields = 'companyName';
    await page.waitForChanges();
    expect(state(page).headLeaving).toBe(true);
    page.root.hiddenFields = ' ';
    await page.waitForChanges();
    expect([state(page).reconfiguring, state(page).headLeaving]).toEqual([false, false]);
    await settle(page);
    expect(text(page, '.sale-title-value')).toBe('Harbor Auto');
  });

  it('does nothing for a hiddenFields change that parses to the same set', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="customerID,invoiceNumber"');
    await load(page, FULL_CHAIN_VIN);

    page.root.hiddenFields = ' invoiceNumber , customerID ,';
    await page.waitForChanges();
    expect(state(page).reconfiguring).toBe(false);
    expect(attr(page, '.sale-body', 'data-open')).toBe('true');
  });

  it('keeps the chain across the next lookup', async () => {
    const page = await newPage(brokerMarketMocks, `${SHOW_ALL} ${CHAIN}`);
    await load(page, FULL_CHAIN_VIN);
    await load(page, 'ZS8EU2ZF6ST814892');
    expect(attr(page, '.sale-card', 'data-layout')).toBe('chain');
    expect(legKinds(page)).toEqual(['distributor', 'dealer', 'broker', 'endCustomer']);
  });
});

describe('vehicle-sale-information — the owner', () => {
  it('opens idle, goes green on records, amber, idle on clear, red on error, and announces each verdict', async () => {
    const page = await newPage(brokerMarketMocks);
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    expect(verdict(page)).toBe('idle');
    expect(text(page, '.sale-title-label')).toBe('Dealer:');
    expect(text(page, '.sale-title-value')).toBe('—');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();

    await load(page, 'ZS8SV9KXXST918958');
    expect(verdict(page)).toBe('positive');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();

    await load(page, UNAUTHORIZED_VIN);
    expect(verdict(page)).toBe('neutral');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-neutral')).toBe(true);

    await owner(page).clearData();
    await page.waitForChanges();
    expect(verdict(page)).toBe('idle');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();

    await owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    expect(verdict(page)).toBe('negative');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-negative')).toBe(true);
    expect(pill(page)).toBe('Wrong response format');

    expect(heard).toEqual(['positive', 'neutral', 'idle', 'negative']);
  });

  it('greys "no records" without an accent', async () => {
    const page = await newPage(brokerMarketMocks);
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    await load(page, SALE_NULL_VIN);
    expect(pill(page)).toBe(englishShared.noRecords);
    expect(verdict(page)).toBe('idle');
    expect(heard).toEqual([]);
  });

  it('clears through the leave and empties unseen', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);

    const pending = owner(page).clearData();
    await page.waitForChanges();
    expect(state(page).leaving).toBe(true);
    expect(text(page, '.sale-title-value')).toBe('Harbor Auto');
    expect(attr(page, '.sale-card', 'data-phase')).toBe('busy');
    expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
    await pending;
    await page.waitForChanges();

    expect(state(page).leaving).toBe(false);
    expect(text(page, '.sale-title-value')).toBe('—');
    expect(all(page, '.sale-body .sale-value-content[data-empty="true"]')).toHaveLength(9);
    expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
    expect(verdict(page)).toBe('idle');
  });

  it('enters an error raised on a settled card through the leave', async () => {
    const page = await newPage(brokerMarketMocks);
    await load(page, FULL_CHAIN_VIN);

    const seen: boolean[] = [];
    const pending = owner(page).setErrorMessage('wrongResponseFormat');
    seen.push(state(page).leaving);
    seen.push(owner(page).isError);
    await pending;
    await page.waitForChanges();

    expect(seen).toEqual([true, false]);
    expect(state(page).leaving).toBe(false);
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-negative')).toBe(true);
    expect(all(page, '.sale-body .sale-value-content[data-empty="true"]')).toHaveLength(4);
  });

  it('lets a lookup started during a clear win, and settles on it', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);

    const clearing = owner(page).clearData();
    const fetching = owner(page).fetchVin('ZS8EU2ZF6ST814892');
    await Promise.all([clearing, fetching]);
    await settle(page);

    expect(text(page, '.sale-title-value')).toBe('Harbor Auto');
    expect(customerValue(page, 'endCustomerName')).toBe('gavin dario fletcher');
    expect(owner(page).isLoading).toBe(false);
    expect(state(page).leaving).toBe(false);
    expect(attr(page, '.sale-card', 'data-phase')).toBe('settled');
    expect(verdict(page)).toBe('positive');
  });

  it('shuts the legs band in flight over the outgoing legs, and drops them a settle after', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);
    expect(all(page, '.sale-leg')).toHaveLength(4);

    const pending = owner(page).fetchVin(SALE_NULL_VIN);
    await page.waitForChanges();
    expect(owner(page).isLoading).toBe(true);
    expect(attr(page, '.sale-card', 'data-phase')).toBe('busy');
    expect(shadow(page).querySelector('.lookup-panel')?.classList.contains('loading')).toBe(true);
    expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
    expect(attr(page, '.sale-legs', 'aria-hidden')).toBe('true');
    expect(all(page, '.sale-leg')).toHaveLength(4);
    expect(attr(page, '.sale-notice', 'data-open')).toBe('false');

    await pending;
    await page.waitForChanges();
    expect(attr(page, '.sale-legs', 'data-open')).toBe('false');
    expect(attr(page, '.sale-legs', 'data-empty')).toBe('true');
    expect(all(page, '.sale-leg')).toHaveLength(4);

    await settle(page);
    expect(all(page, '.sale-leg')).toHaveLength(0);
  });

  it('touches no loading flag when the language changes', async () => {
    const page = await newPage(brokerMarketMocks, SHOW_ALL);
    await load(page, FULL_CHAIN_VIN);

    await owner(page).changeLanguage('ar');
    await page.waitForChanges();

    expect(owner(page).isLoading).toBe(false);
    expect(state(page).leaving).toBe(false);
    expect(state(page).reconfiguring).toBe(false);
    expect(attr(page, '.sale-legs', 'data-open')).toBe('true');
    expect(attr(page, '.sale-body', 'data-open')).toBe('true');
    expect(text(page, '.sale-title-label')).toBe(`${arabicLocale.companyName}:`);
  });

  it('keeps the public surface: every prop and its default', async () => {
    const page = await newSpecPage({ components: [VehicleSaleInformation], html: '<vehicle-sale-information></vehicle-sale-information>' });
    const element = page.root as any;

    expect(element.language).toBe('en');
    expect(element.coreOnly).toBe(false);
    expect(element.isDev).toBeUndefined();
    expect(element.today).toBeUndefined();
    expect(element.disableVinValidation).toBe(false);
    expect(element.baseUrl).toBeUndefined();
    expect(element.headers).toEqual({});
    expect(element.queryString).toBe('');
    expect(element.errorCallback).toBeUndefined();
    expect(element.loadingStateChange).toBeUndefined();
    expect(element.loadedResponse).toBeUndefined();
    expect(element.hiddenFields).toBe(DEFAULT_HIDDEN);
    expect(element.showSupplyChain).toBe(false);
  });

  it('keeps the public surface: every method is there and async, and the callbacks fire', async () => {
    const page = await newPage(brokerMarketMocks);
    const element = page.root as any;

    for (const method of ['setBlazorRef', 'setMockData', 'fetchVin', 'setErrorMessage', 'clearData']) {
      expect([method, typeof element[method]]).toEqual([method, 'function']);
      expect([method, typeof (owner(page) as any)[method]]).toEqual([method, 'function']);
    }
    expect(owner(page).setBlazorRef(undefined as any)).toBeInstanceOf(Promise);
    expect(owner(page).setMockData(brokerMarketMocks as any)).toBeInstanceOf(Promise);
    expect(owner(page).clearData()).toBeInstanceOf(Promise);

    const loading: boolean[] = [];
    const loaded: string[] = [];
    element.loadingStateChange = (value: boolean) => loading.push(value);
    element.loadedResponse = (response: any) => loaded.push(response.vin);
    await page.waitForChanges();

    const fetching = owner(page).fetchVin('ZS8SV9KXXST918958');
    expect(fetching).toBeInstanceOf(Promise);
    await fetching;
    await page.waitForChanges();
    expect(loading).toEqual([true, false]);
    expect(loaded).toEqual(['ZS8SV9KXXST918958']);

    const erroring = owner(page).setErrorMessage('wrongResponseFormat');
    expect(erroring).toBeInstanceOf(Promise);
    await erroring;
  });

  it('marks itself untranslatable, carries the direction on the layout, and passes coreOnly through', async () => {
    const page = await newPage(brokerMarketMocks);
    expect(page.root.getAttribute('translate')).toBe('no');
    expect(attr(page, '.lookup-panel', 'dir')).toBe('ltr');
    expect(shadow(page).querySelector('.lookup-card')).not.toBeNull();
    expect(shadow(page).querySelector('.lookup-core')).toBeNull();

    const core = await newPage(brokerMarketMocks, 'core-only="true"');
    expect(shadow(core).querySelector('.lookup-core')).not.toBeNull();
    expect(shadow(core).querySelector('.lookup-panel')).toBeNull();
    expect(shadow(core).querySelector('.lookup-card')).toBeNull();
    expect(attr(core, '.lookup-core', 'dir')).toBe('ltr');
    expect(shadow(core).querySelector('.sale-card')).not.toBeNull();

    await load(core, FULL_CHAIN_VIN);
    expect(text(core, '.sale-title-value')).toBe('Harbor Auto');
  });
});

describe('vehicle-sale-information — direction and language', () => {
  for (const lang of ['en', 'ar', 'ku', 'ru'] as const) {
    it(`renders every label in ${lang}, ${lang === 'ar' || lang === 'ku' ? 'right to left' : 'left to right'}`, async () => {
      const words = LOCALES[lang];
      const page = await newPage(brokerMarketMocks, SHOW_ALL);
      await owner(page).changeLanguage(lang);
      await load(page, BROKER_STOCK_VIN);

      expect(attr(page, '.lookup-panel', 'dir')).toBe(lang === 'ar' || lang === 'ku' ? 'rtl' : 'ltr');
      expect(attr(page, '.sale-card', 'dir')).toBeNull();
      expect(text(page, '.sale-title-label')).toBe(`${words.companyName}:`);
      expect(text(page, '.sale-lead-caption-label')).toBe(words.sale);
      expect(text(page, `#${attr(page, '.sale-grid', 'aria-labelledby')}`)).toBe(words.sale);
      expect(gridKeys(page).map(field => attr(page, `.sale-grid .sale-cell[data-field="${field}"]`, 'data-label'))).toEqual([
        words.location,
        words.invoiceNumber,
        words.invoiceDate,
        words.warrantyActivationDate,
        words.customerAccountNumber,
        words.customerID,
      ]);
      expect(text(page, '.sale-group[data-group="endCustomer"] .sale-group-label')).toBe(words.endCustomer);
      expect(text(page, `#${attr(page, '.sale-group[data-group="endCustomer"]', 'aria-labelledby')}`)).toBe(words.endCustomer);
      expect(all(page, '.sale-group .sale-cell-label').map(label => label.textContent)).toEqual([words.endCustomerName, words.endCustomerPhone, words.endCustomerIdNumber]);
      expect(customerValue(page, 'endCustomerName')).toBe(words.inBrokerStock.replace('{broker}', 'Quartz Trading'));
      expect(text(page, '.sale-legs .sale-group-label')).toBe(words.supplyChain);
      expect(all(page, '.sale-leg-role').map(role => role.textContent)).toEqual([words.distributorName, words.brokerName]);
      expect(legCell(page, 'distributor', 'distributorInvoiceDate')).toBe('2024-10-19');
      expect(attr(page, '.sale-leg[data-leg="distributor"] .sale-leg-cell[data-field="distributorInvoiceNumber"]', 'data-label')).toBe(words.invoiceNumber);

      page.root.showSupplyChain = true;
      await page.waitForChanges();
      await settle(page);
      expect(all(page, '.sale-leg-role').map(role => role.textContent)).toEqual([words.distributorName, words.companyName, words.brokerName]);
      expect(text(page, '.sale-chain-end')).toBe(words.inBrokerStock.replace('{broker}', 'Quartz Trading'));

      page.root.hiddenFields = 'companyName';
      await page.waitForChanges();
      await settle(page);
      expect(text(page, '.sale-title-name')).toBe(words.vehicleSaleInformation);
    });
  }

  it('says the unauthorized notice and the pills in the locale s words', async () => {
    const page = await newPage(brokerMarketMocks);

    await owner(page).changeLanguage('ar');
    await load(page, UNAUTHORIZED_VIN);
    expect(squash(shadow(page).querySelector('.sale-notice')?.textContent)).toBe(arabicLocale.unauthorizedNotice);
    expect(pill(page)).toBe(arabicShared.notInRecords);

    await load(page, SALE_NULL_VIN);
    expect(pill(page)).toBe(arabicShared.noRecords);
    expect(customerValue(page, 'endCustomerName')).toBe(arabicLocale.noEndCustomer);

    await owner(page).changeLanguage('ru');
    await page.waitForChanges();
    expect(customerValue(page, 'endCustomerName')).toBe(russianLocale.noEndCustomer);
    expect(text(page, '.sale-title-label')).toBe(`${russianLocale.companyName}:`);
  });
});

describe('vehicle-sale-information — under vehicle-lookup', () => {
  it('receives showSupplyChain and hiddenFields through children-props', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup, VehicleSaleInformation],
      html: `<vehicle-lookup active-element="vehicle-sale-information" children-props='{"vehicle-sale-information":{"showSupplyChain":true,"hiddenFields":""}}'></vehicle-lookup>`,
    });

    const panel = page.root.shadowRoot.getElementById('vehicle-sale-information') as any;
    expect(panel.showSupplyChain).toBe(true);
    expect(panel.hiddenFields).toBe('');
    expect(panel.shadowRoot.querySelector('.sale-card')?.getAttribute('data-layout')).toBe('chain');
    expect(panel.coreOnly).toBe(true);

    const plain = await newSpecPage({
      components: [VehicleLookup, VehicleSaleInformation],
      html: '<vehicle-lookup active-element="vehicle-sale-information"></vehicle-lookup>',
    });
    const standard = plain.root.shadowRoot.getElementById('vehicle-sale-information') as any;
    expect(standard.showSupplyChain).toBe(false);
    expect(standard.hiddenFields).toBe(DEFAULT_HIDDEN);
    expect(standard.shadowRoot.querySelector('.sale-card')?.getAttribute('data-layout')).toBe('standard');
  });
});
