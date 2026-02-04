import { JSXBase } from '@stencil/core/internal';
import { Component, h, Prop, State, Watch, Element } from '@stencil/core';
import cn from '~lib/cn';
import { ArrowIcon } from '~assets/arrow-icon';

export type InformationTableColumn = {
  key: string;
  label: string;
  width: number;
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
  @Prop() customSkeleton: boolean = false;
  @Prop() headers: InformationTableColumn[];

  @Prop() expandColumnWidth: number = 48;
  @Prop() subRowRenderer?: (row: any) => any;
  @Prop() allowMultipleExpanded: boolean = true;

  @State() tableRowHeight: number | 'auto' = 'auto';
  @State() expandedRowIndexes: number[] = [];

  @Element() el: HTMLElement;

  private getTableWidth = () => {
    const headersWidth = this.headers?.reduce((sum, h) => sum + (h?.width || 0), 0) || 0;
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
  };

  @Watch('isLoading')
  async onIsLoadingChange() {
    const staticTableRowHeight = this.el.getElementsByClassName('information-table-row')[0]?.clientHeight;

    if (staticTableRowHeight) this.tableRowHeight = staticTableRowHeight;
    if (this.isLoading) {
    } else this.tableRowHeight = 'auto';
  }

  private renderCellContent = (value: any) => {
    if (value === null || value === undefined) return <div>&nbsp;</div>;
    if (typeof value === 'string' || typeof value === 'number') return value;
    if (typeof value === 'function') return value();
    return value;
  };

  private renderRow = (data: any = {}, rowIndex: number = -1) => {
    const expandable = !!this.subRowRenderer && rowIndex >= 0;
    const expanded = expandable ? this.isExpanded(rowIndex) : false;

    const rowBg = rowIndex >= 0 && rowIndex % 2 === 1 ? 'bg-slate-100' : '';

    const expandCell = !this.subRowRenderer ? (
      false
    ) : expandable ? (
      <div style={{ width: `${this.expandColumnWidth}px` }} class={cn('px-[8px] py-[16px] grid place-items-center')}>
        <button
          type="button"
          aria-expanded={expanded ? 'true' : 'false'}
          onClick={() => this.toggleExpanded(rowIndex)}
          class={cn('size-full grid place-items-center shift-skeleton rounded transition duration-500', {
            'hover:bg-slate-200/70': !expanded,
            'bg-sky-100 hover:bg-sky-200': expanded,
          })}
        >
          <ArrowIcon class={cn('text-slate-700 transition-transform duration-500', { 'rotate-180': expanded })} />
        </button>
      </div>
    ) : (
      <div style={{ width: `${this.expandColumnWidth}px` }} class={cn('px-[8px] py-[16px] grid place-items-center')}>
        {!this.customSkeleton && <div class="shift-skeleton size-full" />}
      </div>
    );

    return (
      <div class={cn('information-table-row flex hover:bg-sky-100/50 transition duration-300', rowBg)}>
        {expandCell}
        {this.headers.map(({ key, label, width, centeredHorizontally = true, centeredVertically = true, styles = {} }, idx) => {
          return (
            <div
              key={key + label + idx}
              style={{ width: `${width}px`, ...styles }}
              class={cn('px-[16px] py-[16px]', { 'text-center': centeredHorizontally, 'my-auto': centeredVertically })}
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
    return (
      <div class={cn('information-table-wrapper mx-auto w-fit', { loading: this.isLoading })}>
        {this.showHeader && (
          <div class="flex">
            {!!this.subRowRenderer && <div style={{ width: `${this.expandColumnWidth}px` }} class={cn('py-[16px] px-[8px] border-b')} />}
            {this.headers.map(({ label, width, centeredHorizontally = true, styles = {} }, idx) => (
              <div key={label + idx} style={{ width: `${width}px`, ...styles }} class={cn('py-[16px] px-[16px] font-semibold border-b', { 'text-center': centeredHorizontally })}>
                {label}
              </div>
            ))}
          </div>
        )}
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
      </div>
    );
  }
}
