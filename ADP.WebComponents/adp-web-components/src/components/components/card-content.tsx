import { FunctionalComponent, h } from '@stencil/core';
import cn from '~lib/cn';

interface CardContentProps {
  title: string;
  width?: string;
  classes?: string;
  maxWidth?: string;
  minWidth?: string;
}

export const CardContent: FunctionalComponent<CardContentProps> = ({ title, classes, maxWidth, minWidth, width }, children) => (
  <div style={{ 'min-width': minWidth, 'max-width': maxWidth, 'width': width }} class={cn('rounded-[4px] overflow-hidden border flex flex-col shadow bg-white', classes)}>
    <h2 class="text-[18px] py-[8px] px-[16px] bg-[#f6f6f6] border-b font-semibold shrink-0 mb-2">{title}</h2>
    <div class="text-gray-700 text-[16px] flex-1 flex items-center py-[8px] px-[16px]">{children}</div>
  </div>
);
