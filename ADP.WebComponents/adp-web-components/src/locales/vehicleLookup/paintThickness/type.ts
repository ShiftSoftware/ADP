import yupTypeMapper from '~lib/yup-type-mapper';

const paintThicknessSchema = yupTypeMapper(['paintThickness', 'noData', 'part', 'left', 'right', 'noImageGroups', 'expand']);

export default paintThicknessSchema;
