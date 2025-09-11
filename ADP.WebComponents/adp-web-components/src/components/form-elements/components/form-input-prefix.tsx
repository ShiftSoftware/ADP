import { FunctionalComponent, h } from '@stencil/core';
import cn from '~lib/cn';

interface FormInputPrefixProps {
  name?: string;
  prefix: string;
  direction: string;
}

export const FormInputPrefix: FunctionalComponent<FormInputPrefixProps> = ({ direction, prefix, name }) => {
  if (!prefix) return false;

  return (
    <div
      dir={direction}
      class="form-input-prefix"
      part={cn('form-input-prefix', {
        [`${name}-prefix`]: !!name,
      })}
    >
      {prefix}
    </div>
  );
};
