import { Component, h, Prop, State } from '@stencil/core';
import { JSX } from '@stencil/core/internal';
import cn from '~lib/cn';

export type AccordionItem = {
  label: string;
  content: JSX.Element | string;
};

@Component({
  shadow: false,
  tag: 'shift-accordion',
  styleUrl: 'shift-accordion.css',
})
export class ShiftAccordion {
  @Prop() items: AccordionItem[];
  @Prop() accordionClasses?: string;
  @Prop() accordionContentClasses?: string;

  @State() openedItems: AccordionItem[] = [];

  private toggleAccordion = (item: AccordionItem) => {
    if (this.openedItems.includes(item)) this.openedItems = this.openedItems.filter(openedItem => openedItem !== item);
    else this.openedItems = [...this.openedItems, item];
  };

  render() {
    return (
      <div class="flex w-full flex-col gap-[4px]">
        {this.items.map((item, idx) => (
          <div key={item.label + idx} class="w-full rounded border">
            <button onClick={() => this.toggleAccordion(item)} class="w-full flex flex-col items-start">
              <div class="flex items-center justify-between mx-auto text-[17px] py-[4px] w-[calc(100%-20px)]">
                <span>{item.label}</span>
                <svg
                  width="24"
                  height="24"
                  fill="none"
                  stroke-width="2"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  xmlns="http://www.w3.org/2000/svg"
                  class={cn('transition-all duration-500', { 'rotate-180': this.openedItems.includes(item) })}
                >
                  <path d="m6 9 6 6 6-6" />
                </svg>
              </div>
              <div class="transition-all duration-500" style={{ height: this.openedItems.includes(item) ? '6px' : '0px' }}></div>
              <flexible-container isOpened={this.openedItems.includes(item)}>{item.content}</flexible-container>
            </button>
          </div>
        ))}
      </div>
    );
  }
}
