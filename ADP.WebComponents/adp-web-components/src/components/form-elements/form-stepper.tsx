import { Component, h, Prop, Host } from '@stencil/core';
import { FormElement, FormHook } from '~features/form-hook';
import cn from '~lib/cn';

const formStepperId = 'form-stepper';

@Component({
  shadow: false,
  tag: 'form-stepper',
  styleUrl: 'form-inputs.css',
})
export class FormStepper implements FormElement {
  @Prop() name?: string;
  @Prop() language?: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;

  reset: (newValue?: unknown) => void;

  private container?: HTMLElement;
  private line?: HTMLElement;
  private resizeObserver?: ResizeObserver;

  componentDidLoad() {
    this.form.subscribe(formStepperId, this);
    this.updateLine();

    this.resizeObserver = new ResizeObserver(() => {
      this.updateLine();
    });

    this.resizeObserver.observe(this.container);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(formStepperId);

    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  updateLine() {
    if (!this.container || !this.line) return;

    const buttons = this.container.querySelectorAll('button');

    if (buttons.length < 2) return;

    const first = buttons[0].getBoundingClientRect();
    const last = buttons[buttons.length - 1].getBoundingClientRect();
    const parent = this.container.getBoundingClientRect();

    const firstCenter = first.left + first.width / 2;
    const lastCenter = last.left + last.width / 2;

    const left = firstCenter - parent.left;
    const width = lastCenter - firstCenter;

    this.line.style.left = `${left}px`;
    this.line.style.width = `${width}px`;
  }

  render() {
    const [_, language] = this.form.getFormLocale();

    const steps = this?.form?.context?.structure?.steps || [];
    const currentStep = this.form?.formStructure?.currentStep;

    return (
      <Host>
        <div part={cn(formStepperId, this?.name)} class="w-full" id={this.wrapperId}>
          {/* <div part={cn(formStepperId + '-container', this?.name)} class={cn('flex ')}></div> */}
          <div ref={el => (this.container = el as HTMLElement)} class="relative flex items-center justify-between px-1 w-full">
            <div
              part={cn(formStepperId + '-line')}
              ref={el => (this.line = el as HTMLElement)}
              class="absolute w-full h-[1px] -translate-y-[50%] top-[16px]"
              style={{ background: 'repeating-linear-gradient(to right,black 0px,black 12px,transparent 6px,transparent 24px)' }}
            />
            {steps.map((step, i) => (
              <div key={i} class="flex items-center">
                <div
                  part={cn(formStepperId + '-step', formStepperId + '-step-' + (i + 1), {
                    [formStepperId + '-step-done']: i + 1 < currentStep,
                    [formStepperId + '-step-done' + (i + 1)]: i + 1 < currentStep,
                    [formStepperId + '-step-active']: i + 1 === currentStep,
                    [formStepperId + '-step-active' + (i + 1)]: i + 1 === currentStep,
                  })}
                  class="flex flex-col items-center"
                >
                  <button
                    type="button"
                    aria-expanded={(i + 1 === currentStep)?.toString()}
                    part={cn(formStepperId + '-step-indicator', formStepperId + '-step-indicator-' + (i + 1), {
                      [formStepperId + '-step-indicator-done']: i + 1 < currentStep,
                      [formStepperId + '-step-indicator-done' + (i + 1)]: i + 1 < currentStep,
                      [formStepperId + '-step-indicator-active']: i + 1 === currentStep,
                      [formStepperId + '-step-indicator-active' + (i + 1)]: i + 1 === currentStep,
                    })}
                    class="relative z-[10] before:absolute before:inset-0 before:size-full before:origin-center before:bg-black/25 before:transition-all before:duration-500 aria-expanded:before:scale-[1.4] size-[32px] flex bg-black cursor-default items-center justify-center"
                  >
                    <div
                      class="relative z-10 text-white items-center text-[16px]"
                      part={cn(formStepperId + '-step-indicator-text', formStepperId + '-step-indicator-text-' + (i + 1), {
                        [formStepperId + '-step-indicator-text-done']: i + 1 < currentStep,
                        [formStepperId + '-step-indicator-text-done' + (i + 1)]: i + 1 < currentStep,
                        [formStepperId + '-step-indicator-text-active']: i + 1 === currentStep,
                        [formStepperId + '-step-indicator-text-active' + (i + 1)]: i + 1 === currentStep,
                      })}
                    >
                      {step?.[language]?.stepCell}
                    </div>
                  </button>

                  <span
                    part={cn(formStepperId + '-step-title', formStepperId + '-step-title-' + (i + 1), {
                      [formStepperId + '-step-title-done']: i + 1 < currentStep,
                      [formStepperId + '-step-title-done' + (i + 1)]: i + 1 < currentStep,
                      [formStepperId + '-step-title-active']: i + 1 === currentStep,
                      [formStepperId + '-step-title-active' + (i + 1)]: i + 1 === currentStep,
                    })}
                    class="text-[14px] text-center mt-[8px] text-gray-600"
                  >
                    {step?.[language]?.stepTitle}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </Host>
    );
  }
}
