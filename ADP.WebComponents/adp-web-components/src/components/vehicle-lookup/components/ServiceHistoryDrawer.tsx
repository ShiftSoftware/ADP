import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import serviceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';
import { VehicleLaborDTO } from '~types/generated/vehicle-lookup/vehicle-labor-dto';
import { VehiclePartDTO } from '~types/generated/vehicle-lookup/vehicle-part-dto';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

type Locale = InferType<typeof serviceHistorySchema>;

/** null for null, undefined, '' or whitespace, else the value as sent. */
const present = (value: string | number | null | undefined): string | null => {
  if (value === null || value === undefined) return null;
  const text = String(value);
  return text.trim() === '' ? null : text;
};

/** The parent invoice, only when it is a real one: the data sends `"0"` for a visit that has none. */
export const parentInvoice = (visit: VehicleServiceHistoryDTO): string | null => {
  const value = present(visit.parentInvoiceNumber);
  return value === null || value.trim() === '0' ? null : value;
};

type LineColumn<Line> = {
  field: string;
  label: string;
  kind: 'code' | 'text' | 'number';
  /** The line's name: Code type, uncaptioned on a narrow card, and kept even when blank on every line. */
  identity?: boolean;
  /** The column that takes the slack and wraps. */
  grow?: boolean;
  value: (line: Line) => string | null;
};

const laborColumns = (locale: Locale): LineColumn<VehicleLaborDTO>[] => [
  { field: 'laborCode', label: locale.laborCode, kind: 'code', identity: true, value: line => present(line.laborCode) },
  { field: 'serviceDescription', label: locale.serviceDescription, kind: 'text', grow: true, value: line => present(line.serviceDescription) },
  { field: 'serviceCode', label: locale.serviceCode, kind: 'text', value: line => present(line.serviceCode) },
  { field: 'packageCode', label: locale.packageCode, kind: 'text', value: line => present(line.packageCode) },
];

const partColumns = (locale: Locale): LineColumn<VehiclePartDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: line => present(line.partNumber) },
  { field: 'partDescription', label: locale.partDescription, kind: 'text', grow: true, value: line => present(line.partDescription) },
  // As sent, in Latin digits: 7.3 stays 7.3.
  { field: 'qty', label: locale.qty, kind: 'number', value: line => (typeof line.qty === 'number' && Number.isFinite(line.qty) ? String(line.qty) : null) },
  { field: 'packageCode', label: locale.packageCode, kind: 'text', value: line => present(line.packageCode) },
];

/** A column with no value on any of this visit's lines says nothing, so it is left out of this visit's list. */
const filledColumns = <Line,>(columns: LineColumn<Line>[], lines: Line[]): LineColumn<Line>[] =>
  columns.filter(column => column.identity || lines.some(line => column.value(line) !== null));

// Floors sized for a drawer, which is narrower than the card: a code like "VYOST416LPC 15K" at 13px, a
// figure like "0.0366", and each heading in small caps, with the cell's padding.
const TRACKS: Record<LineColumn<unknown>['kind'] | 'grow', string> = {
  code: 'minmax(120px, 1fr)',
  grow: 'minmax(180px, 2.4fr)',
  text: 'minmax(120px, 1fr)',
  number: 'minmax(80px, 0.6fr)',
};

const tracks = <Line,>(columns: LineColumn<Line>[]): string => columns.map(column => TRACKS[column.grow ? 'grow' : column.kind]).join(' ');

type ListProps<Line> = {
  label: string;
  none: string;
  columns: LineColumn<Line>[];
  lines: Line[];
};

const LineList = <Line,>({ label, none, columns, lines }: ListProps<Line>) => {
  const shown = filledColumns(columns, lines);

  return (
    <div class="service-history-lines">
      <p class="service-history-caption">
        <span>{label}</span>
        <span class="service-history-caption-count">{lines.length}</span>
      </p>

      {lines.length === 0 ? (
        <p class="service-history-none">{none}</p>
      ) : (
        <div class="service-history-list" role="table" aria-label={label} style={{ '--service-history-tracks': tracks(shown) }}>
          <div class="service-history-line service-history-line-head" role="row">
            {shown.map(column => (
              <span key={column.field} class="service-history-cell" role="columnheader" data-kind={column.kind}>
                {column.label}
              </span>
            ))}
          </div>
          {lines.map((line, index) => (
            <div key={index} class="service-history-line" role="row">
              {shown.map(column => {
                const value = column.value(line);
                return (
                  <span
                    key={column.field}
                    class="service-history-cell"
                    role="cell"
                    data-kind={column.kind}
                    data-label={column.label}
                    data-identity={column.identity ? 'true' : null}
                    data-grow={column.grow ? 'true' : null}
                  >
                    {value ?? <span class="service-history-blank">—</span>}
                  </span>
                );
              })}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

type Props = { visit: VehicleServiceHistoryDTO; locale: Locale };

/** What a visit carries beyond its row: the job, the account, a parent invoice, and its labor and part lines. */
export const ServiceHistoryDrawer: FunctionalComponent<Props> = ({ visit, locale }) => {
  const parent = parentInvoice(visit);
  const details: [string, string, string | null][] = [
    ['jobNumber', locale.jobNumber, present(visit.jobNumber)],
    ['accountNumber', locale.accountNumber, present(visit.accountNumber)],
  ];
  if (parent !== null) details.push(['parentInvoiceNumber', locale.parentInvoiceNumber, parent]);

  return (
    <div class="service-history-drawer">
      <dl class="service-history-details">
        {details.map(([field, label, value]) => (
          <div key={field} class="service-history-detail" data-field={field}>
            <dt>{label}</dt>
            <dd>{value ?? '—'}</dd>
          </div>
        ))}
      </dl>

      <LineList label={locale.laborLines} none={locale.noLaborLines} columns={laborColumns(locale)} lines={visit.laborLines ?? []} />
      <LineList label={locale.partLines} none={locale.noPartLines} columns={partColumns(locale)} lines={visit.partLines ?? []} />
    </div>
  );
};
