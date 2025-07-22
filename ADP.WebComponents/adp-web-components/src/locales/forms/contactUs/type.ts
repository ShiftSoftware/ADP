import yupTypeMapper from '~lib/yup-type-mapper';

const contactUsSchema = yupTypeMapper([
  'fullName',
  'fullNameIsRequired',
  'fullNameMinimum',
  'emailAddress',
  'emailAddressNotValid',
  'city',
  'selectCity',
  'cityIsRequired',
  'phoneNumber',
  'phoneNumberIsRequired',
  'phoneNumberFormatInvalid',
  'inquiryType',
  'selectInquiryType',
  'inquiryTypeIsRequired',
  'writeAMessage',
  'leaveUsMessage',
  'messageIsRequired',
]);

export default contactUsSchema;
