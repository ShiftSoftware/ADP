import { FunctionalComponent, h } from '@stencil/core';

interface FormInputPrefixProps {
  prefix: string;
  direction: string;
}

export const FormInputPrefix: FunctionalComponent<FormInputPrefixProps> = ({ direction, prefix }) => {
  if (!prefix) return false;

  return (
    <div part="form-input-prefix" dir={direction} class="form-input-prefix">
      {prefix}
    </div>
  );
};
