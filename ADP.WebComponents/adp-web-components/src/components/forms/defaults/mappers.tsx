import { h } from '@stencil/core';
import { getPhoneValidator } from '~features/form-hook';

export const getDefaultMappers = (stateObject: Record<string, any>) => ({
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => <form-input {...props} />,

  lastName: ({ props }) => <form-input {...props} />,

  email: ({ props }) => <form-input type="email" {...props} />,

  message: ({ props }) => <form-text-area {...props} />,

  vin: ({ props }) => <form-vin-input {...props} />,

  phone: ({ props, isLoading }) => {
    if (!stateObject.phoneValidator) {
      stateObject.phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={stateObject.phoneValidator.default} {...props} isLoading={isLoading} validator={stateObject.phoneValidator} />;
  },
});
