import { object } from 'yup';
import sscSchema from './ssc/type';
import accessoriesSchema from './accessories/type';
import specificationSchema from './specification/type';
import paintThicknessSchema from './paintThickness/type';
import ServiceHistorySchema from './serviceHistory/type';
import claimableItemsSchema from './claimableItems/type';

const vehicleLookupWrapperSchema = object({
  ssc: sscSchema,
  accessories: accessoriesSchema,
  specification: specificationSchema,
  claimableItems: claimableItemsSchema,
  paintThickness: paintThicknessSchema,
  serviceHistory: ServiceHistorySchema,
});

export default vehicleLookupWrapperSchema;
