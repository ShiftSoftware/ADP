import { AsYouType } from 'libphonenumber-js';

export const getPhoneValidator = (useDefault = true) => {
  const phoneValidator = new AsYouType('IQ') as AsYouType & {
    default: string;
    metadata: {
      numberingPlan: { metadata: [string] };
    };
  };

  if (useDefault) {
    phoneValidator.default = '+' + phoneValidator.metadata.numberingPlan.metadata[0];

    phoneValidator.input(phoneValidator.default);
  }

  return phoneValidator;
};
