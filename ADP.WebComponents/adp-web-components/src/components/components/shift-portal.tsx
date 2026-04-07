import { Component, Prop, Watch } from '@stencil/core';

@Component({
  shadow: false,
  tag: 'shift-portal',
  styleUrl: 'shift-portal.css',
})
export class ShiftPortal {
  @Prop() tag: string = '';
  @Prop() inheritedClasses: string = '';
  @Prop() componentProps: Record<string, any> = {};

  private portaledElement: HTMLElement | null = null;

  setComponentsParams = async () => {
    if (!this.portaledElement) return;

    this.portaledElement.className = this.inheritedClasses;

    for (const [key, value] of Object.entries(this.componentProps ?? {})) {
      (this.portaledElement as any)[key] = value;
    }
  };

  @Watch('componentProps')
  @Watch('inheritedClasses')
  async updateComponentParams() {
    await this.setComponentsParams();
  }

  async componentWillLoad() {
    if (!this.tag) return;

    this.portaledElement = document.createElement(this.tag);
    document.body.appendChild(this.portaledElement);
    await this.setComponentsParams();
  }

  disconnectedCallback() {
    if (this.portaledElement && this.portaledElement.parentNode) {
      this.portaledElement.parentNode.removeChild(this.portaledElement);
    }
    this.portaledElement = null;
  }

  render() {
    return false;
  }
}
