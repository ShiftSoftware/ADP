import { JSXBase } from '@stencil/core/internal';
import { Component, h, Prop, State, Watch, Element } from '@stencil/core';
import cn from '~lib/cn';

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
  @Prop() headers: InformationTableColumn[];

  @State() tableRowHeight: number | 'auto' = 'auto';

  @Element() el: HTMLElement;

  @Watch('isLoading')
  async onIsLoadingChange() {
    const staticTableRowHeight = this.el.getElementsByClassName('information-table-row')[0]?.clientHeight;

    if (staticTableRowHeight) this.tableRowHeight = staticTableRowHeight;
    if (this.isLoading) {
    } else this.tableRowHeight = 'auto';
  }

  private renderRow = (data: object = {}) => (
    <div class={cn('border-b information-table-row flex even:bg-slate-100 hover:bg-sky-100/50 transition duration-300 last:border-b-0')}>
      {this.headers.map(({ key, label, width, centeredHorizontally = true, centeredVertically = true, styles = {} }, idx) => (
        <div
          key={key + label + idx}
          style={{ width: `${width}px`, ...styles }}
          class={cn('px-[16px] py-[16px]', { 'text-center': centeredHorizontally, 'my-auto': centeredVertically })}
        >
          <div class="shift-skeleton">
            {(data[key] === null || data[key] === undefined) && <div>&nbsp;</div>}
            {(data[key] !== null || data[key] !== undefined) && (typeof data[key] === 'string' || typeof data[key] === 'number') && data[key]}
            {(data[key] !== null || data[key] !== undefined) && typeof data[key] === 'function' && data[key]()}
          </div>
        </div>
      ))}
    </div>
  );

  render() {
    return (
      <div class={cn('mx-auto w-fit', { loading: this.isLoading })}>
        <div class="flex">
          {this.headers.map(({ label, width, centeredHorizontally = true, styles = {} }, idx) => (
            <div key={label + idx} style={{ width: `${width}px`, ...styles }} class={cn('py-[16px] px-[16px] font-semibold border-b', { 'text-center': centeredHorizontally })}>
              {label}
            </div>
          ))}
        </div>
        <flexible-container height={this.tableRowHeight}>
          {!this.rows.length && this.renderRow(this.templateRow)}
          {!!this.rows.length && this.rows.map(row => this.renderRow(row))}
        </flexible-container>
      </div>
    );
  }
}
