import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import saleInformationSchema from '~locales/vehicleLookup/saleInformation/type';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { LookupHeadWait, PanelVerdict, StatusBadge } from '~features/vehicle-info-layout';

import { BADGE_GLYPHS } from './glyphs';

export type SaleLocale = InferType<typeof saleInformationSchema>;

export type SaleRecord = Pick<VehicleLookupDTO, 'vin' | 'isAuthorized' | 'saleInformation'>;

export type SaleConfig = { chain: boolean; hidden: ReadonlySet<string> };

export type SaleRole = 'code' | 'figure' | 'word' | 'statement';

export type SaleCell = { key: string; label: string; role: SaleRole; text: string; note?: string; noteValue?: string; noteDir?: 'ltr'; column?: 1 | 2 };

export type LegKind = 'distributor' | 'intermediary' | 'dealer' | 'broker' | 'endCustomer';

export type SaleLeg = { key: string; kind: LegKind; index?: number; role: string; party?: SaleCell; sub: string; cells: SaleCell[] };

export type EndCustomerStatement = { kind: 'present' | 'none' | 'brokerStock' | 'silent'; text: string; broker?: string };

export type SaleHead = { grammar: 'value' | 'name'; value: string; branch: string; branchSlot: boolean };

export type RetainedLegs = { legs: SaleLeg[]; chainEnd?: EndCustomerStatement };

const BLANK = ' ';

const END_CUSTOMER_FIELDS = ['endCustomerName', 'endCustomerPhone', 'endCustomerIdNumber'];

export const parseHiddenFields = (raw?: string): Set<string> =>
  new Set(
    (raw ?? '')
      .split(',')
      .map(field => field.trim())
      .filter(Boolean),
  );

export const sameConfig = (a: SaleConfig, b: SaleConfig): boolean => a.chain === b.chain && a.hidden.size === b.hidden.size && [...a.hidden].every(name => b.hidden.has(name));

export const normalise = (raw: string | number | null | undefined): string => (raw === null || raw === undefined ? '' : String(raw).trim());

export const asDate = (raw?: string | null): string => normalise(raw).slice(0, 10);

export const same = (a: string, b: string): boolean => !!a && a.trim().toUpperCase() === b.trim().toUpperCase();

export const hasRecords = (record?: SaleRecord): boolean => {
  const sale = record?.saleInformation;
  if (!sale) return false;

  const legValues = (leg?: { companyName?: string; branchName?: string; invoiceNumber?: string; invoiceDate?: string; cityName?: string }) =>
    leg ? [leg.companyName, leg.branchName, leg.invoiceNumber, leg.invoiceDate, leg.cityName] : [];

  const values = [
    sale.companyName,
    sale.branchName,
    sale.countryName,
    sale.cityName,
    sale.invoiceNumber,
    sale.invoiceDate,
    sale.warrantyActivationDate,
    sale.customerAccountNumber,
    sale.customerID,
    sale.broker?.brokerName,
    sale.broker?.nonOfficialBrokerName,
    sale.broker?.invoiceNumber,
    sale.broker?.invoiceDate,
    sale.broker?.cityName,
    ...legValues(sale.distributor),
    ...(sale.intermediaries ?? []).flatMap(legValues),
    sale.endCustomer?.id,
    sale.endCustomer?.name,
    sale.endCustomer?.phone,
    sale.endCustomer?.idNumber,
  ];

  return values.some(value => !!normalise(value));
};

export const readable = (record: SaleRecord | undefined, error?: string): boolean => !!record?.vin && !error && record.isAuthorized !== false;

export const saleNotice = (state: { authorized?: boolean }, busy: boolean, locale: SaleLocale): { open: boolean; tone: 'neutral'; message: string } => ({
  open: !busy && state.authorized === false,
  tone: 'neutral',
  message: locale.unauthorizedNotice,
});

export const brokerInStock = (record?: SaleRecord): boolean => {
  const broker = record?.saleInformation?.broker;
  return !!broker && !asDate(broker.invoiceDate) && !normalise(broker.invoiceNumber);
};

export const headStatement = (record: SaleRecord | undefined, config: SaleConfig, canRead: boolean): SaleHead => {
  if (config.hidden.has('companyName')) return { grammar: 'name', value: '', branch: '', branchSlot: false };

  const sale = canRead ? record?.saleInformation : undefined;
  const branchSlot = !config.hidden.has('branchName');

  return { grammar: 'value', value: normalise(sale?.companyName), branch: branchSlot ? normalise(sale?.branchName) : '', branchSlot };
};

export const sameHead = (a: SaleHead, b: SaleHead): boolean => a.grammar === b.grammar && a.value === b.value && a.branch === b.branch && a.branchSlot === b.branchSlot;

export const saleCells = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale, canRead: boolean): SaleCell[] => {
  const sale = canRead ? record?.saleInformation : undefined;
  const shown = (name: string) => !config.hidden.has(name);
  const cells: SaleCell[] = [];

  if (!shown('companyName') && shown('branchName')) cells.push({ key: 'branchName', label: locale.branchName, role: 'word', text: normalise(sale?.branchName) });

  if (shown('countryName') || shown('cityName')) {
    const parts = [shown('countryName') ? normalise(sale?.countryName) : '', shown('cityName') ? normalise(sale?.cityName) : ''];
    cells.push({ key: 'location', label: locale.location, role: 'word', text: parts.filter(Boolean).join(' · ') });
  }

  if (!config.chain) {
    if (shown('invoiceNumber')) cells.push({ key: 'invoiceNumber', label: locale.invoiceNumber, role: 'code', text: normalise(sale?.invoiceNumber) });
    if (shown('invoiceDate')) cells.push({ key: 'invoiceDate', label: locale.invoiceDate, role: 'figure', text: asDate(sale?.invoiceDate) });
  }

  if (shown('warrantyActivationDate'))
    cells.push({ key: 'warrantyActivationDate', label: locale.warrantyActivationDate, role: 'figure', text: asDate(sale?.warrantyActivationDate) });
  if (shown('customerAccountNumber')) cells.push({ key: 'customerAccountNumber', label: locale.customerAccountNumber, role: 'code', text: normalise(sale?.customerAccountNumber) });
  if (shown('customerID')) cells.push({ key: 'customerID', label: locale.customerID, role: 'code', text: normalise(sale?.customerID) });

  return cells;
};

export const endCustomerStatement = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale, canRead: boolean): EndCustomerStatement => {
  if (!canRead || END_CUSTOMER_FIELDS.every(name => config.hidden.has(name))) return { kind: 'silent', text: '' };

  const customer = record?.saleInformation?.endCustomer;
  if (customer && [customer.id, customer.name, customer.phone, customer.idNumber].some(value => !!normalise(value))) return { kind: 'present', text: '' };

  const broker = normalise(record?.saleInformation?.broker?.brokerName);
  if (brokerInStock(record) && !config.hidden.has('brokerName') && broker) return { kind: 'brokerStock', text: locale.inBrokerStock.replace('{broker}', broker), broker };

  return { kind: 'none', text: locale.noEndCustomer };
};

// endCustomer.id is the dealer's customerID, so it is only worth a note when that cell is not already saying it.
const customerIdNote = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale): Pick<SaleCell, 'note' | 'noteValue' | 'noteDir'> => {
  const id = normalise(record?.saleInformation?.endCustomer?.id);
  if (!id || config.hidden.has('endCustomerId')) return {};
  if (!config.hidden.has('customerID') && same(id, normalise(record?.saleInformation?.customerID))) return {};
  return { note: `${locale.endCustomerId} ${id}`, noteValue: id, noteDir: 'ltr' };
};

export const endCustomerCells = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale, canRead: boolean): SaleCell[] | null => {
  if (config.chain || END_CUSTOMER_FIELDS.every(name => config.hidden.has(name))) return null;

  const statement = endCustomerStatement(record, config, locale, canRead);
  const customer = statement.kind === 'present' ? record?.saleInformation?.endCustomer : undefined;
  const shown = (name: string) => !config.hidden.has(name);
  const cells: SaleCell[] = [];

  if (shown('endCustomerName'))
    cells.push({
      key: 'endCustomerName',
      label: locale.endCustomerName,
      role: 'word',
      text: normalise(customer?.name),
      ...(customer ? customerIdNote(record, config, locale) : {}),
    });
  if (shown('endCustomerPhone')) cells.push({ key: 'endCustomerPhone', label: locale.endCustomerPhone, role: 'figure', text: normalise(customer?.phone) });
  if (shown('endCustomerIdNumber')) cells.push({ key: 'endCustomerIdNumber', label: locale.endCustomerIdNumber, role: 'code', text: normalise(customer?.idNumber) });

  if ((statement.kind === 'none' || statement.kind === 'brokerStock') && cells.length)
    cells[0] = { key: cells[0].key, label: cells[0].label, role: 'statement', text: statement.text, note: undefined };

  return cells;
};

type LegSource = { companyName?: string; branchName?: string; invoiceNumber?: string; invoiceDate?: string; cityName?: string };

export const saleLegs = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale, canRead: boolean): SaleLeg[] => {
  const sale = canRead ? record?.saleInformation : undefined;
  if (!sale) return [];

  const shown = (name: string) => !config.hidden.has(name);
  const city = (raw?: string) => (shown('cityName') ? normalise(raw) : '');
  const sub = (...parts: string[]) => parts.filter(Boolean).join(' · ');
  const party = (key: string, label: string, text: string, note: Pick<SaleCell, 'note' | 'noteValue' | 'noteDir'> = {}): SaleCell | undefined =>
    shown(key)
      ? { key, label, role: 'word', text, note: note.note, ...(note.noteValue ? { noteValue: note.noteValue } : {}), ...(note.noteDir ? { noteDir: note.noteDir } : {}) }
      : undefined;
  const invoice = (numberKey: string, dateKey: string, source: { invoiceNumber?: string | number; invoiceDate?: string }): SaleCell[] => [
    ...(shown(numberKey) ? [{ key: numberKey, label: locale.invoiceNumber, role: 'code' as const, text: normalise(source.invoiceNumber), column: 1 as const }] : []),
    ...(shown(dateKey) ? [{ key: dateKey, label: locale.invoiceDate, role: 'figure' as const, text: asDate(source.invoiceDate), column: 2 as const }] : []),
  ];
  const carries = (leg: SaleLeg) => !!leg.party?.text || !!leg.party?.note || !!leg.sub || leg.cells.some(cell => !!cell.text);

  const companyLeg = (
    key: string,
    kind: LegKind,
    role: string,
    source: LegSource,
    names: { name: string; branch: string; number: string; date: string },
    index?: number,
  ): SaleLeg => ({
    key,
    kind,
    index,
    role,
    party: party(names.name, role, normalise(source.companyName)),
    sub: sub(shown(names.branch) ? normalise(source.branchName) : '', city(source.cityName)),
    cells: invoice(names.number, names.date, source),
  });

  const legs: SaleLeg[] = [];

  const distributor = sale.distributor
    ? companyLeg('distributor', 'distributor', locale.distributorName, sale.distributor, {
        name: 'distributorName',
        branch: 'distributorBranchName',
        number: 'distributorInvoiceNumber',
        date: 'distributorInvoiceDate',
      })
    : undefined;
  if (distributor && carries(distributor)) legs.push(distributor);

  if (shown('intermediaries'))
    (sale.intermediaries ?? []).forEach((intermediary, index) => {
      if (!intermediary) return;
      const leg = companyLeg(
        `intermediary-${index}`,
        'intermediary',
        locale.intermediaryName,
        intermediary,
        { name: 'intermediaryName', branch: 'intermediaryBranchName', number: 'intermediaryInvoiceNumber', date: 'intermediaryInvoiceDate' },
        index,
      );
      if (carries(leg)) legs.push(leg);
    });

  if (config.chain) {
    const dealer = companyLeg('dealer', 'dealer', locale.companyName, sale, { name: 'companyName', branch: 'branchName', number: 'invoiceNumber', date: 'invoiceDate' });
    // Folding must not hide a dealer value the distributor leg cannot show under this hiddenFields.
    const foldable = (['companyName:distributorName', 'invoiceNumber:distributorInvoiceNumber', 'invoiceDate:distributorInvoiceDate'] as const).every(pair => {
      const [dealerField, distributorField] = pair.split(':');
      return !shown(dealerField) || shown(distributorField);
    });
    const soldDirect =
      foldable &&
      legs[0]?.kind === 'distributor' &&
      same(normalise(sale.companyName), normalise(sale.distributor?.companyName)) &&
      normalise(sale.invoiceNumber) === normalise(sale.distributor?.invoiceNumber) &&
      asDate(sale.invoiceDate) === asDate(sale.distributor?.invoiceDate);
    if (!soldDirect && carries(dealer)) legs.push(dealer);
  }

  if (sale.broker) {
    const nonOfficial = shown('nonOfficialBrokerName') ? normalise(sale.broker.nonOfficialBrokerName) : '';
    const broker: SaleLeg = {
      key: 'broker',
      kind: 'broker',
      role: locale.brokerName,
      party: party(
        'brokerName',
        locale.brokerName,
        normalise(sale.broker.brokerName),
        nonOfficial ? { note: `${locale.nonOfficialBrokerName}: ${nonOfficial}`, noteValue: nonOfficial } : {},
      ),
      sub: city(sale.broker.cityName),
      cells: invoice('brokerInvoiceNumber', 'brokerInvoiceDate', sale.broker),
    };
    if (carries(broker)) legs.push(broker);
  }

  if (config.chain && endCustomerStatement(record, config, locale, canRead).kind === 'present') {
    const customer = sale.endCustomer;
    const customerLeg: SaleLeg = {
      key: 'endCustomer',
      kind: 'endCustomer',
      role: locale.endCustomer,
      party: party('endCustomerName', locale.endCustomer, normalise(customer?.name), customerIdNote(record, config, locale)),
      sub: '',
      cells: [
        ...(shown('endCustomerPhone')
          ? [{ key: 'endCustomerPhone', label: locale.endCustomerPhone, role: 'figure' as const, text: normalise(customer?.phone), column: 1 as const }]
          : []),
        ...(shown('endCustomerIdNumber')
          ? [{ key: 'endCustomerIdNumber', label: locale.endCustomerIdNumber, role: 'code' as const, text: normalise(customer?.idNumber), column: 2 as const }]
          : []),
      ],
    };
    if (carries(customerLeg)) legs.push(customerLeg);
  }

  return legs;
};

export const chainEnd = (record: SaleRecord | undefined, config: SaleConfig, locale: SaleLocale, canRead: boolean): EndCustomerStatement | undefined => {
  if (!config.chain || !hasRecords(record)) return undefined;
  const statement = endCustomerStatement(record, config, locale, canRead);
  return statement.kind === 'none' || statement.kind === 'brokerStock' ? statement : undefined;
};

const StatementText: FunctionalComponent<{ statement?: EndCustomerStatement; text: string }> = ({ statement, text }) => {
  const broker = statement?.kind === 'brokerStock' ? statement.broker : undefined;
  const at = broker ? text.indexOf(broker) : -1;
  if (!broker || at < 0) return <span>{text}</span>;

  return (
    <span>
      {text.slice(0, at)}
      <bdi>{broker}</bdi>
      {text.slice(at + broker.length)}
    </span>
  );
};

const SaleValue: FunctionalComponent<{ cell: SaleCell; statement?: EndCustomerStatement }> = ({ cell, statement }) => {
  if (!cell.text && !cell.note) return <span>—</span>;

  const text =
    cell.role === 'code' || cell.role === 'figure' ? (
      <bdi dir="ltr">{cell.text}</bdi>
    ) : cell.role === 'statement' ? (
      <StatementText statement={statement} text={cell.text} />
    ) : (
      cell.text || '—'
    );

  return (
    <span>
      <span class="sale-value-text">{text}</span>
      {!!cell.note && (
        <span class="sale-value-note">
          <NoteText cell={cell} />
        </span>
      )}
    </span>
  );
};

const NoteText: FunctionalComponent<{ cell: SaleCell }> = ({ cell }) => {
  const note = cell.note ?? '';
  const value = cell.noteValue;
  if (!value || !note.endsWith(value)) return <span>{note}</span>;

  return (
    <span>
      {note.slice(0, note.length - value.length)}
      <bdi dir={cell.noteDir ?? null}>{value}</bdi>
    </span>
  );
};

const valueRole = (cell: SaleCell) => (cell.text || cell.note ? cell.role : 'empty');

const SaleCellView: FunctionalComponent<{ cell: SaleCell; blank: boolean; statement?: EndCustomerStatement; key?: string }> = ({ cell, blank, statement }) => (
  <div class="sale-cell" data-field={cell.key} data-label={cell.label}>
    <span class="sale-cell-label">{cell.label}</span>
    <span class="sale-value shift-skeleton resize-settle" data-role={valueRole(cell)}>
      <span class="sale-value-content" data-empty={blank ? 'true' : 'false'}>
        {blank ? BLANK : <SaleValue cell={cell} statement={statement} />}
      </span>
    </span>
  </div>
);

const SaleLegView: FunctionalComponent<{ leg: SaleLeg; chain: boolean; key?: string }> = ({ leg, chain }) => (
  <li class="sale-leg" data-leg={leg.kind} data-index={leg.index === undefined ? null : String(leg.index)}>
    {chain && <span class="sale-leg-marker" aria-hidden="true" />}
    <div class="sale-leg-party">
      <span class="sale-leg-role">{leg.role}</span>
      {!!leg.party && (
        <span class="sale-value" data-role={valueRole(leg.party)} data-field={leg.party.key}>
          <span class="sale-value-content">
            <SaleValue cell={leg.party} />
          </span>
        </span>
      )}
      {!!leg.sub && <span class="sale-leg-sub">{leg.sub}</span>}
    </div>
    {leg.cells.map(cell => (
      <div class="sale-cell sale-leg-cell" key={cell.key} data-field={cell.key} data-label={cell.label} data-column={String(cell.column ?? 1)}>
        <span class="sale-cell-label">{cell.label}</span>
        <span class="sale-value" data-role={valueRole(cell)}>
          <span class="sale-value-content">
            <SaleValue cell={cell} />
          </span>
        </span>
      </div>
    ))}
  </li>
);

type Props = {
  locale: SaleLocale;
  record?: SaleRecord;
  error?: string;
  loading: boolean;
  reconfiguring: boolean;
  headLeaving: boolean;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  config: SaleConfig;
  legs: SaleLeg[];
  chainEnd?: EndCustomerStatement;
  retained: RetainedLegs;
};

export const VehicleSaleInformationPanel: FunctionalComponent<Props> = props => {
  const { locale, record, error, loading, reconfiguring, headLeaving, verdict, config, legs, retained } = props;

  const vehicleLoaded = !!record?.vin && !error;
  const canRead = readable(record, error);
  const notice = saleNotice({ authorized: record?.isAuthorized }, loading, locale);
  const head = headStatement(record, config, canRead);
  const cells = saleCells(record, config, locale, canRead);
  const statement = endCustomerStatement(record, config, locale, canRead);
  const customerCells = endCustomerCells(record, config, locale, canRead);
  const blank = !vehicleLoaded;

  const hasLegs = legs.length > 0 || !!props.chainEnd;
  const shownLegs = hasLegs ? legs : retained.legs;
  const shownEnd = hasLegs ? props.chainEnd : retained.chainEnd;
  const legsOpen = hasLegs && !loading;

  return (
    <section
      class="sale-card"
      data-verdict={verdict.state}
      data-phase={loading ? 'busy' : 'settled'}
      data-layout={config.chain ? 'chain' : 'standard'}
      data-head={headLeaving ? 'leaving' : 'settled'}
    >
      <header class="sale-head lookup-head-band">
        <div class="sale-main lookup-head-content">
          <p class="sale-title">
            {head.grammar === 'name' ? (
              <strong class="sale-title-name">{locale.vehicleSaleInformation}</strong>
            ) : (
              [<span class="sale-title-label">{locale.companyName}:</span>, ' ', <strong class="sale-title-value">{head.value || '—'}</strong>]
            )}
          </p>

          {/* In every configuration, so a hiddenFields change never resizes the head band. */}
          <div class="sale-branch-slot" data-empty={head.branch ? 'false' : 'true'} aria-hidden={head.branch ? null : 'true'}>
            <p class="sale-branch" title={head.branch || null}>
              {head.branch || BLANK}
            </p>
          </div>
        </div>

        <div class="lookup-summary lookup-head-content">
          <StatusBadge state={verdict.state} text={verdict.text} />
        </div>

        <LookupHeadWait />
      </header>

      <div class="lookup-slide-clip">
        <div class="sale-under-head lookup-slide">
          <div class="sale-notice collapsible" data-open={notice.open ? 'true' : 'false'} data-tone={notice.tone} aria-hidden={notice.open ? null : 'true'}>
            <div class="collapsible-body">
              <p class="sale-notice-body" role="status">
                <svg class="notice-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
                  <path fill="currentColor" d={BADGE_GLYPHS.question} />
                </svg>
                <span>{notice.message}</span>
              </p>
            </div>
          </div>

          <div class="sale-lead">
            <span class="sale-lead-caption-label" id="sale-caption" aria-hidden="true">
              {locale.sale}
            </span>
          </div>

          <div class="sale-body collapsible" data-open={reconfiguring ? 'false' : 'true'} aria-hidden={reconfiguring ? 'true' : null}>
            <div class="collapsible-body">
              <div class="sale-grid" role="group" aria-labelledby="sale-caption">
                {cells.map(cell => (
                  <SaleCellView key={cell.key} cell={cell} blank={blank} />
                ))}
              </div>

              {!!customerCells && (
                <section class="sale-group" data-group="endCustomer" role="group" aria-labelledby="sale-end-customer-label">
                  <span class="sale-group-label" id="sale-end-customer-label" aria-hidden="true">
                    {locale.endCustomer}
                  </span>
                  <div class="sale-ec-grid">
                    {customerCells.map(cell => (
                      <SaleCellView key={cell.key} cell={cell} blank={blank} statement={statement} />
                    ))}
                  </div>
                </section>
              )}

              <div class="sale-legs collapsible" data-open={legsOpen ? 'true' : 'false'} data-empty={hasLegs ? 'false' : 'true'} aria-hidden={legsOpen ? null : 'true'}>
                <div class="collapsible-body">
                  <div class="sale-legs-band">
                    <span class="sale-group-label" id="sale-supply-chain-label" aria-hidden="true">
                      {locale.supplyChain}
                    </span>
                    <ol class="sale-leg-list" aria-labelledby="sale-supply-chain-label">
                      {shownLegs.map(leg => (
                        <SaleLegView key={leg.key} leg={leg} chain={config.chain} />
                      ))}
                    </ol>
                    {config.chain && !!shownEnd && (
                      <p class="sale-chain-end" data-kind={shownEnd.kind}>
                        <StatementText statement={shownEnd} text={shownEnd.text} />
                      </p>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
