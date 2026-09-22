import { object } from 'yup';
import yupTypeMapper from '~lib/yup-type-mapper';

const specification = object({}).concat(
  yupTypeMapper([
    'vehicleSpecification',
    'noData',
    'model',
    'variant',
    'katashiki',
    'modelYear',
    'sfx',
    'productionDate',
    'modelCode',
    'recordYearNote',
    'exteriorColour',
    'interiorColour',
    'swatchCaveat',
    'identity',
    'powertrain',
    'body',
    'engine',
    'engineType',
    'cylinders',
    'fuel',
    'fuelCapacity',
    'litreUnit',
    'transmission',
    'class',
    'bodyType',
    'style',
    'doors',
    'steering',
    'vehicleType',
    'detailsOne',
    'detailsMany',
    'expandDetails',
    'collapseDetails',
    'unauthorizedNotice',
  ]),
);

export default specification;
