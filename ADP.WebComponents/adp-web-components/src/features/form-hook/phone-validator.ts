import { AsYouType, CountryCode } from 'libphonenumber-js';

export type PhoneValidator = AsYouType & {
  default: string;
  formattedOutput: string;
  metadata: {
    numberingPlan: { metadata: [string] };
  };
  geIsValidPhoneNumber: () => boolean;
};

export const getPhoneValidator = (countryCode?: CountryCode | CountryCode[]): PhoneValidator => {
  const phoneValidator = new AsYouType(Array.isArray(countryCode) ? undefined : countryCode) as PhoneValidator;

  if (Array.isArray(countryCode)) {
    phoneValidator.geIsValidPhoneNumber = () => {
      const raw = phoneValidator?.formattedOutput || '';

      if (!raw.trim()) return false;

      for (const country of countryCode) {
        try {
          const phone = new AsYouType(country);
          phone?.input(raw);
          if (phone?.isValid()) return true;
        } catch {}
      }
      return false;
    };
  } else if (countryCode) {
    try {
      phoneValidator.default = '+' + phoneValidator.metadata.numberingPlan.metadata[0];
      phoneValidator.input(phoneValidator.default);
    } catch (error) {
      // If metadata is not available, set empty default
      phoneValidator.default = '';
    }
    phoneValidator.geIsValidPhoneNumber = () => phoneValidator?.isValid();
  } else {
    phoneValidator.default = '';
    phoneValidator.geIsValidPhoneNumber = () => phoneValidator?.formattedOutput.length > 7;
  }

  return phoneValidator;
};
