import { h, JSX } from '@stencil/core';
import { FormElementStructure, FormElementMapper, FormElementMapperFunctionProps } from '~features/form-hook';
import cn from '~lib/cn';

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
        <Tag {...props} part={cn(props?.id, props?.class, `element-${tag}`, tag)}>
          {Array.isArray(children) && children.map(child => renderStructure(child, elementMapper, { ...generaProps }))}
        </Tag>
      );
    }

    const newProps = {
      ...generaProps,
      props: {
        ...props,
        name,
        wrapperId: props?.id,
        form: generaProps.form,
        wrapperClass: props?.class,
        isLoading: generaProps.isLoading,
      },
    };

    if (typeof name === 'string' && name && elementMapper[name]) return elementMapper[name](newProps);
  }

  return false;
}
