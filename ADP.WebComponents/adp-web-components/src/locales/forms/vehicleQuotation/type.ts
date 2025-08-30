import yupTypeMapper from '~lib/yup-type-mapper';

const vehicleQuotationSchema = yupTypeMapper(['Vehicle', 'Please select a Vehicle', 'This field is required.', 'Choose', 'Contact Information', 'Full name']);

export default vehicleQuotationSchema;
