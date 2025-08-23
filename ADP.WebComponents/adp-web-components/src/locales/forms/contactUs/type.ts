import { object } from 'yup';
import yupTypeMapper from '~lib/yup-type-mapper';

const ticketTypes = yupTypeMapper(['complaint', 'generalInquiry']);

const contactUsSchema = object({
  ticketTypes,
}).concat(
  yupTypeMapper([
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
  ]),
);

export default contactUsSchema;
