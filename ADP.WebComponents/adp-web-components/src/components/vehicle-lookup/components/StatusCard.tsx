import { h } from '@stencil/core';
import SuccessIcon from '~assets/check.svg';
import RejectIcon from '~assets/x-mark.svg';

import cn from '~lib/cn';

type Props = {
  desc: string;
  to?: boolean;
  from?: boolean;
  icon?: boolean;
  toDesc?: string;
  opened?: boolean;
  fromDesc?: string;
  className?: string;
  isLoading?: boolean;
  state?: 'idle' | 'warning' | 'success' | 'reject' | 'info';
};

export default function StatusCard({ isLoading = false, opened = true, desc, className, fromDesc, toDesc, from, to, state = 'idle', icon = true }: Props) {
  return (
    <flexible-container isOpened={opened} classes={cn('shift-skeleton', { loading: isLoading || !opened })}>
      <div class={cn('card', className, `${state}-card`)}>
        {icon && state === 'reject' && <img src={RejectIcon} />}
        {icon && state === 'warning' && <img src={RejectIcon} />}
        {icon && state === 'success' && <img src={SuccessIcon} />}
        {from && (
          <p class="no-padding flex gap-2">
            <span class="font-semibold">{fromDesc}:</span> {desc}
          </p>
        )}
        {to && (
          <p class="no-padding flex gap-2">
            <span class="font-semibold">{toDesc}:</span> {desc}
          </p>
        )}
        {!from && !to && <p class={state === 'idle' ? 'no-padding' : ''}>{desc}</p>}
      </div>
    </flexible-container>
  );
}
