import { FunctionalComponent, h } from '@stencil/core';
import cn from '~lib/cn';

interface FormInputLabelProps {
  name?: string;
  label: string;
  isRequired: boolean;
}

export const FormInputLabel: FunctionalComponent<FormInputLabelProps> = ({ label, isRequired, name }) => {
  if (!label) return false;

  return (
    <div
      part={cn('form-input-label', {
        [`${name}-label`]: !!name,
      })}
      class="form-input-label"
    >
      {label}
      {isRequired && (
        <span
          part={cn('form-input-label-required-star', {
            [`${name}-label-required-star`]: !!name,
          })}
          class="form-input-label-required-star"
        >
          *
        </span>
      )}
    </div>
  );
};
