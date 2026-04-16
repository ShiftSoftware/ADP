import { JSXBase } from '@stencil/core/internal';
import { Component, h, Prop, State, Watch, Element } from '@stencil/core';
import cn from '~lib/cn';
import { ArrowIcon } from '~assets/arrow-icon';

export type InformationTableColumn = {
  key: string;
  label: string;
  width?: number;
  maxWidth?: number;
  nowrap?: boolean;
  centeredVertically?: boolean;
  centeredHorizontally?: boolean;
  styles?: JSXBase.HTMLAttributes<HTMLDivElement>['style'];
};

@Component({
  shadow: false,
  tag: 'information-table',
  styleUrl: 'information-table.css',
})
export class InformationTable {
  @Prop() rows: object[] = [];
  @Prop() templateRow: object = {};
  @Prop() isLoading: boolean = false;
  @Prop() showHeader: boolean = true;
  @Prop() isRaw: boolean = false;
  @Prop() stripCells: boolean = true;
  @Prop() customSkeleton: boolean = false;
  @Prop() headers: InformationTableColumn[];

  @Prop() size: 'default' | 'small' = 'default';
  @Prop() allowAutoWidth: boolean = false;

  @Prop() expandColumnWidth: number = 48;
  @Prop() subRowRenderer?: (row: any) => any;
  @Prop() expandUsingEntireRow: boolean = false;
  @Prop() allowMultipleExpanded: boolean = false;
  @Prop() scrollExpandedIntoView: boolean = false;

  @State() tableRowHeight: number | 'auto' = 'auto';
  @State() expandedRowIndexes: number[] = [];

  @Element() el: HTMLElement;

  private getTableWidth = () => {
    if (this.allowAutoWidth) return 0;
    const headersWidth = this.headers?.reduce((sum, h) => sum + (typeof h?.width === 'number' ? h.width : 0), 0) || 0;
    return headersWidth + (this.subRowRenderer ? this.expandColumnWidth : 0);
  };

  private isExpanded = (rowIndex: number) => this.expandedRowIndexes.includes(rowIndex);

  private toggleExpanded = (rowIndex: number) => {
    const currentlyExpanded = this.isExpanded(rowIndex);

    if (currentlyExpanded) {
      this.expandedRowIndexes = this.expandedRowIndexes.filter(i => i !== rowIndex);
      return;
    }

    this.expandedRowIndexes = this.allowMultipleExpanded ? [...this.expandedRowIndexes, rowIndex] : [rowIndex];

    if (this.scrollExpandedIntoView) {
      requestAnimationFrame(() => {
        const rows = this.el.querySelectorAll('.info-table-data-tr, .information-table-row');
        const row = rows[rowIndex] as HTMLElement | undefined;
        if (row && typeof row.scrollIntoView === 'function') {
          row.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    }
  };

  @Watch('isLoading')
  async onIsLoadingChange() {
    const staticTableRowHeight = this.el.getElementsByClassName('information-table-row')[0]?.clientHeight;

    if (staticTableRowHeight) this.tableRowHeight = staticTableRowHeight;
    if (this.isLoading) {
      this.expandedRowIndexes = [];
    } else this.tableRowHeight = 'auto';
  }

  private renderCellContent = (value: any) => {
    if (value === null || value === undefined) return <div>&nbsp;</div>;
    if (typeof value === 'string' || typeof value === 'number') return value;
    if (typeof value === 'function') return value();
    return value;
  };

  private renderTableExpandCell = (rowIndex: number) => {
    const expandable = !!this.subRowRenderer && rowIndex >= 0;
    const expanded = expandable ? this.isExpanded(rowIndex) : false;
    const expandCellPaddingClass = this.size === 'small' ? 'px-[6px] py-[6px]' : 'px-[8px] py-[16px]';
    const expandPlaceholderSizeClass = this.size === 'small' ? 'size-[20px]' : 'size-[32px]';

    if (expandable) {
      return (
        <td style={{ width: `${this.expandColumnWidth}px` }} class={cn('info-table-td', expandCellPaddingClass)}>
          <div class="grid place-items-center size-full">
            <button
              type="button"
              disabled={this.expandUsingEntireRow}
              aria-expanded={expanded ? 'true' : 'false'}
              onClick={e => {
                e.stopPropagation();
                this.toggleExpanded(rowIndex);
              }}
              class={cn('size-full grid place-items-center shift-skeleton rounded transition duration-500', {
                'hover:bg-slate-200/70': !expanded,
                'bg-sky-100 hover:bg-sky-200': expanded,
                '!pointer-events-none': this.expandUsingEntireRow,
              })}
            >
              <ArrowIcon class={cn('text-slate-700 transition-transform duration-500', { 'rotate-180': expanded })} />
            </button>
          </div>
        </td>
      );
    }

    return (
      <td style={{ width: `${this.expandColumnWidth}px` }} class={cn('info-table-td', expandCellPaddingClass)}>
        <div class="grid place-items-center size-full">{!this.customSkeleton && <div class={cn('shift-skeleton', expandPlaceholderSizeClass)} />}</div>
      </td>
    );
  };

  private renderTableDataRow = (data: any, rowIndex: number) => {
    const isStripe = rowIndex >= 0 && rowIndex % 2 === 1 && this.stripCells;
    const cellPaddingClass = this.size === 'small' ? 'px-[12px] py-[6px]' : 'px-[16px] py-[16px]';
    const cellTextClass = this.size === 'small' ? 'text-[13px]' : '';

    return (
      <tr
        onClick={this.expandUsingEntireRow && rowIndex >= 0 ? () => this.toggleExpanded(rowIndex) : undefined}
        class={cn('info-table-tr info-table-data-tr information-table-row', {
          'info-table-tr-stripe': isStripe,
          'info-table-tr-clickable': this.expandUsingEntireRow && rowIndex >= 0,
        })}
      >
        {this.subRowRenderer && this.renderTableExpandCell(rowIndex)}
        {this.headers.map(({ key, label, centeredHorizontally = true, nowrap, maxWidth, styles = {} }, idx) => {
          const tdStyle = {
            ...(typeof maxWidth === 'number' && maxWidth > 0 ? { maxWidth: `${maxWidth}px` } : {}),
            ...styles,
          };
          return (
            <td
              key={key + label + idx}
              style={tdStyle}
              class={cn('info-table-td table-body-cell', cellPaddingClass, cellTextClass, {
                'text-center': centeredHorizontally,
                'whitespace-nowrap': nowrap,
              })}
            >
              <div class={cn({ 'shift-skeleton': !this.customSkeleton })}>{this.renderCellContent(data[key])}</div>
            </td>
          );
        })}
      </tr>
    );
  };

  private renderTableSubRow = (row: any, idx: number) => {
    const expanded = this.isExpanded(idx);
    const colSpan = (this.headers?.length || 0) + (this.subRowRenderer ? 1 : 0);
    return (
      <tr key={`sub-${idx}`} class="info-table-tr info-table-subrow-tr">
        <td class="info-table-subrow-td" colSpan={colSpan}>
          <flexible-container isOpened={expanded}>{this.subRowRenderer(row)}</flexible-container>
        </td>
      </tr>
    );
  };

  private renderRow = (data: any = {}, rowIndex: number = -1) => {
    const expandable = !!this.subRowRenderer && rowIndex >= 0;
    const expanded = expandable ? this.isExpanded(rowIndex) : false;

    const rowBg = rowIndex >= 0 && rowIndex % 2 === 1 && this.stripCells ? 'bg-slate-100' : '';

    const expandCellPaddingClass = this.size === 'small' ? 'px-[6px] py-[6px]' : 'px-[8px] py-[16px]';
    const expandPlaceholderSizeClass = this.size === 'small' ? 'size-[20px]' : 'size-[32px]';

    const expandCell = !this.subRowRenderer ? (
      false
    ) : expandable ? (
      <div style={{ width: `${this.expandColumnWidth}px` }} class={cn('grid place-items-center', expandCellPaddingClass)}>
        <button
          type="button"
          disabled={this.expandUsingEntireRow}
          aria-expanded={expanded ? 'true' : 'false'}
          onClick={() => this.toggleExpanded(rowIndex)}
          class={cn('size-full grid place-items-center shift-skeleton rounded transition duration-500', {
            'hover:bg-slate-200/70': !expanded,
            'bg-sky-100 hover:bg-sky-200': expanded,
            '!pointer-events-none': this.expandUsingEntireRow,
          })}
        >
          <ArrowIcon class={cn('text-slate-700 transition-transform duration-500', { 'rotate-180': expanded })} />
        </button>
      </div>
    ) : (
      <div style={{ width: `${this.expandColumnWidth}px` }} class={cn('grid place-items-center', expandCellPaddingClass)}>
        {!this.customSkeleton && <div class={cn('shift-skeleton', expandPlaceholderSizeClass)} />}
      </div>
    );

    const cellPaddingClass = this.size === 'small' ? 'px-[12px] py-[6px]' : 'px-[16px] py-[16px]';
    const cellTextClass = this.size === 'small' ? 'text-[13px]' : '';

    return (
      <div
        onClick={this.expandUsingEntireRow && (() => this.toggleExpanded(rowIndex))}
        class={cn('information-table-row flex hover:bg-sky-100/50 transition duration-300', rowBg, { 'hover:cursor-pointer': this.expandUsingEntireRow })}
      >
        {expandCell}
        {this.headers.map(({ key, label, width, centeredHorizontally = true, centeredVertically = true, styles = {} }, idx) => {
          const hasWidth = typeof width === 'number' && width > 0;
          const cellStyle = { ...(hasWidth ? { width: `${width}px` } : {}), ...styles };

          return (
            <div
              key={key + label + idx}
              style={cellStyle}
              class={cn('table-body-cell', cellPaddingClass, cellTextClass, {
                'text-center': centeredHorizontally,
                'my-auto': centeredVertically,
              })}
            >
              <div class={cn({ 'shift-skeleton': !this.customSkeleton })}>{this.renderCellContent(data[key])}</div>
            </div>
          );
        })}
      </div>
    );
  };

  private renderExpandableRow = (row: any, idx: number) => {
    const expanded = this.isExpanded(idx);
    const tableWidth = this.getTableWidth();

    return (
      <div key={String(idx)} style={{ width: tableWidth ? `${tableWidth}px` : undefined }} class={cn('border-b last:border-b-0')}>
        {this.renderRow(row, idx)}
        <flexible-container isOpened={expanded}>{this.subRowRenderer(row)}</flexible-container>
      </div>
    );
  };

  render() {
    const isGrid = this.allowAutoWidth;
    const rowCount = this.rows?.length || 0;
    const hasRows = rowCount > 0;

    if (isGrid) {
      const cellPaddingHeader = this.size === 'small' ? 'py-[8px] px-[12px] text-[13px]' : 'py-[16px] px-[16px]';
      const expandHeaderPadding = this.size === 'small' ? 'py-[10px] px-[6px]' : 'py-[16px] px-[8px]';

      return (
        <div class={cn('information-table-wrapper info-table-table-wrapper', { loading: this.isLoading })}>
          <table class="info-table-table">
            {this.showHeader && (
              <thead>
                <tr class="info-table-tr info-table-header-tr">
                  {!!this.subRowRenderer && <th style={{ width: `${this.expandColumnWidth}px` }} class={cn('info-table-th', expandHeaderPadding)} />}
                  {this.headers.map(({ label, width, maxWidth, centeredHorizontally = true, nowrap, styles = {} }, idx) => {
                    const hasWidth = typeof width === 'number' && width > 0;
                    const hasMaxWidth = typeof maxWidth === 'number' && maxWidth > 0;
                    const thStyle = {
                      ...(hasWidth ? { width: `${width}px` } : {}),
                      ...(hasMaxWidth ? { maxWidth: `${maxWidth}px` } : {}),
                      ...styles,
                    };
                    return (
                      <th
                        key={label + idx}
                        style={thStyle}
                        class={cn('info-table-th font-semibold table-header-cell', cellPaddingHeader, {
                          'text-center': centeredHorizontally,
                          'whitespace-nowrap': nowrap,
                        })}
                      >
                        {label}
                      </th>
                    );
                  })}
                </tr>
              </thead>
            )}
            <tbody>
              {!hasRows && this.renderTableDataRow(this.templateRow, -1)}
              {hasRows &&
                this.rows.map((row, idx) => [
                  this.renderTableDataRow(row, idx),
                  !!this.subRowRenderer && this.renderTableSubRow(row, idx),
                ])}
            </tbody>
          </table>
        </div>
      );
    }

    const headerRow = this.showHeader && (
      <div class="flex">
        {!!this.subRowRenderer && (
          <div
            style={{ width: `${this.expandColumnWidth}px` }}
            class={cn('border-b', this.size === 'small' ? 'py-[10px] px-[6px]' : 'py-[16px] px-[8px]')}
          />
        )}
        {this.headers.map(({ label, width, centeredHorizontally = true, styles = {} }, idx) => {
          const hasWidth = typeof width === 'number' && width > 0;
          const headerStyle = { ...(hasWidth ? { width: `${width}px` } : {}), ...styles };

          return (
            <div
              key={label + idx}
              style={headerStyle}
              class={cn('font-semibold table-header-cell border-b', this.size === 'small' ? 'py-[8px] px-[12px] text-[13px]' : 'py-[16px] px-[16px]', {
                'text-center': centeredHorizontally,
              })}
            >
              {label}
            </div>
          );
        })}
      </div>
    );

    return (
      <div class={cn('information-table-wrapper mx-auto w-fit', { loading: this.isLoading })}>
        {headerRow}

        {this.isRaw ? (
          <div style={{ height: `${this.tableRowHeight}px` }}>
            {!this.rows?.length && <div class={cn('border-b last:border-b-0')}>{this.renderRow(this.templateRow)}</div>}
            {!!this.rows?.length &&
              !this.subRowRenderer &&
              this.rows.map((row, idx) => (
                <div key={String(idx)} class={cn('border-b last:border-b-0')}>
                  {this.renderRow(row, idx)}
                </div>
              ))}
            {!!this.rows?.length && !!this.subRowRenderer && this.rows.map((row, idx) => this.renderExpandableRow(row, idx))}
          </div>
        ) : (
          <flexible-container height={this.tableRowHeight}>
            {!this.rows?.length && <div class={cn('border-b last:border-b-0')}>{this.renderRow(this.templateRow)}</div>}
            {!!this.rows?.length &&
              !this.subRowRenderer &&
              this.rows.map((row, idx) => (
                <div key={String(idx)} class={cn('border-b last:border-b-0')}>
                  {this.renderRow(row, idx)}
                </div>
              ))}
            {!!this.rows?.length && !!this.subRowRenderer && this.rows.map((row, idx) => this.renderExpandableRow(row, idx))}
          </flexible-container>
        )}
      </div>
    );
  }
}
