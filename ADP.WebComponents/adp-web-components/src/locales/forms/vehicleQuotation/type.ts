import { object } from 'yup';
import yupTypeMapper from '~lib/yup-type-mapper';

const vehicleQuotation = object({}).concat(
  yupTypeMapper([
    'Vehicle',
    'submit',
    'Please select a Vehicle',
    'This field is required.',
    'Choose',
    'Contact Information',
    'Full name',
    'Full name is required',
    'Full name minimum',
    'Phone number',
    'Phone number is required',
    'Phone number format invalid',
    'Dealer',
    'Select a dealer',
    'Dealer is required',
    'Preferred purchasing method',
    'Select the purchasing method',
    'Cash',
    'Flexible',
    'Your current car',
    'Do you own a vehicle?',
    'Please answer this field',
    'No',
    'Yes',
    'Vehicle Model',
    'Your current vehicle',
    'Other',
    'Form submitted successfully.',
    'Preferred contact time',
    'Please select a time',
    'Morning',
    'Noon',
    'Afternoon',
    'Installments',
  ]),
);

export default vehicleQuotation;
