import { Component, h, Prop } from '@stencil/core';
import cn from '~lib/cn';

@Component({
  shadow: false,
  tag: 'shift-tabs',
  styleUrl: 'shift-tabs.css',
})
export class ShiftTabs {
  @Prop() tabs: string[];
  @Prop() tabClasses?: string;
  @Prop() activeTabIndex: number;
  @Prop() activeTabLabel: string;
  @Prop() containerClasses?: string;
  @Prop() changeActiveTab: (activeStatus: { label: string; idx: number }) => void;

  render() {
    return (
      <div class={this.containerClasses}>
        <div class={cn('border-b relative flex mb-[6px] ps-[12px] gap-[4px]', this.tabClasses)}>
          {this.tabs.map((label, idx) => (
            <button
              key={label + idx}
              onClick={() => !(this.activeTabIndex === idx || this.activeTabLabel === label) && this.changeActiveTab({ label, idx })}
              class={cn(
                'px-[16px] cursor-pointer after:w-full py-[6px] after:h-[1px] after:left-0 after:bottom-0 after:absolute after:bg-white after:opacity-0 after:translate-y-full after:z-10 after:duration-500 after:transition text-[17px] relative transition-colors !border-b-0 duration-500 font-medium text-black bg-[#f6f6f6] border border-gray-200 rounded-t-[4px] focus:outline-none',
                {
                  'bg-white cursor-default after:opacity-100': this.activeTabIndex === idx || this.activeTabLabel === label,
                },
              )}
            >
              {label}
            </button>
          ))}
        </div>
        <flexible-container>
          <slot></slot>
        </flexible-container>
      </div>
    );
  }
}
