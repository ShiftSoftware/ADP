import { FunctionalComponent, h } from '@stencil/core';
import { JSXBase } from '@stencil/core/internal';
import cn from '~lib/cn';

interface MaterialCardProps {
  title: string;
  desc?: string;
  width?: string;
  class?: string;
  maxWidth?: string;
  minWidth?: string;
}

export const MaterialCard: FunctionalComponent<MaterialCardProps> = (props, children = []) => {
  const wrapperStyle: JSXBase.HTMLAttributes<HTMLDivElement>['style'] = {
    flex: '1',
    display: 'flex',
    width: props.class,
    flexDirection: 'column',
    minWidth: props.minWidth,
    maxWidth: props.maxWidth,
  };

  const titleStyle: JSXBase.HTMLAttributes<HTMLDivElement>['style'] = {
    padding: '10px 0',
    fontWeight: 'bold',
    marginBottom: '8px',
    borderBottom: '1px solid gray',
  };

  return (
    <div style={wrapperStyle} class={props.class}>
      <strong style={titleStyle}>{props.title}</strong>
      <flexible-container>
        {!children.length ? <MaterialCardChildren hidden={['...', ''].includes(props?.desc?.trim())}>{props.desc || '...'}</MaterialCardChildren> : children}
      </flexible-container>
    </div>
  );
};

type MaterialCardChildrenProps = {
  class?: string;
  hidden: boolean;
};

export const MaterialCardChildren: FunctionalComponent<MaterialCardChildrenProps> = (props, children) => {
  const wrapperStyles: JSXBase.HTMLAttributes<HTMLDivElement>['style'] = {
    width: '100%',
  };

  const contentStyles: JSXBase.HTMLAttributes<HTMLDivElement>['style'] = {
    'transition-duration': '0ms !important',
    'opacity': props.hidden ? '0' : '1',
  };
  return (
    <div style={wrapperStyles} class={cn('shift-skeleton', props.class)}>
      <div style={contentStyles}>{children}</div>
    </div>
  );
};
