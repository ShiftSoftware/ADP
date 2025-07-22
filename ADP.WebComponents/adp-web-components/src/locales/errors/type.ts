import yupTypeMapper from '~lib/yup-type-mapper';

const errorsSchema = yupTypeMapper([
  'noBaseUrl',
  'invalidVin',
  'vinNumberRequired',
  'partNumberRequired',
  'wrongResponseFormat',
  'noPartsFound',
  'noServiceAvailable',
  'wrongFormStructure',
  'wildCard',
  'requestFailedPleaseTryAgainLater',
]);

export default errorsSchema;
