import { object } from 'yup';
import warrantySchema from './warranty/type';
import accessoriesSchema from './accessories/type';
import specificationSchema from './specification/type';
import paintThicknessSchema from './paintThickness/type';
import ServiceHistorySchema from './serviceHistory/type';
import claimableItemsSchema from './claimableItems/type';

const vehicleLookupWrapperSchema = object({
  warranty: warrantySchema,
  accessories: accessoriesSchema,
  specification: specificationSchema,
  claimableItems: claimableItemsSchema,
  paintThickness: paintThicknessSchema,
  serviceHistory: ServiceHistorySchema,
});

export default vehicleLookupWrapperSchema;
