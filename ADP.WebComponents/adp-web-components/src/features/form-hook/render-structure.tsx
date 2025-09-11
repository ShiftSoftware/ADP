import { h, JSX } from '@stencil/core';
import { FormElementStructure, FormElementMapper, FormElementMapperFunctionProps } from '~features/form-hook';

export function renderStructure(
  structure: FormElementStructure<any> | string,
  elementMapper: FormElementMapper<any, any>,
  generaProps: FormElementMapperFunctionProps<any>,
): JSX.Element | false {
  if (typeof structure === 'string') {
    if (typeof structure === 'string' && structure && elementMapper[structure]) {
      generaProps.props['name'] = structure;
      return elementMapper[structure](generaProps);
    }
  } else {
    const { tag, name, children, data, ...props } = structure;

    if (tag) {
      const Tag = tag as any;

      return (
        <Tag part={props?.id || ''} {...props}>
          {Array.isArray(children) && children.map(child => renderStructure(child, elementMapper, generaProps))}
        </Tag>
      );
    }

    generaProps.props = { wrapperId: props?.id, wrapperClass: props?.class, isLoading: generaProps.isLoading, form: generaProps.form };

    if (typeof name === 'string' && name && elementMapper[name]) return elementMapper[name](generaProps);
  }

  return false;
}
