import { Component, h, Prop } from '@stencil/core';

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
        <div class={{ 'shift-tabs-bar': true, [this.tabClasses || '']: !!this.tabClasses }}>
          {this.tabs.map((label, idx) => (
            <button
              key={label + idx}
              onClick={() => !(this.activeTabIndex === idx || this.activeTabLabel === label) && this.changeActiveTab({ label, idx })}
              class={{
                'shift-tabs-tab': true,
                'shift-tabs-tab-active': this.activeTabIndex === idx || this.activeTabLabel === label,
              }}
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
