import { FunctionalComponent, h } from '@stencil/core';

interface FormInputLabelProps {
  label: string;
  isRequired: boolean;
}

export const FormInputLabel: FunctionalComponent<FormInputLabelProps> = ({ label, isRequired }) => {
  if (!label) return false;

  return (
    <div part="form-input-label" class="form-input-label">
      {label}
      {isRequired && (
        <span part="form-input-label-required-star" class="form-input-label-required-star">
          *
        </span>
      )}
    </div>
  );
};
