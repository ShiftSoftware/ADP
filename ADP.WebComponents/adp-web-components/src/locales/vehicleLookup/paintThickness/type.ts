import yupTypeMapper from '~lib/yup-type-mapper';

const paintThicknessSchema = yupTypeMapper(['paintThickness', 'noData', 'panel', 'position', 'left', 'right', 'inspectionDate', 'noImageGroups', 'expand']);

export default paintThicknessSchema;
