export type AppStates = 'idle' | 'loading' | 'data' | 'error' | 'error-loading' | 'data-loading';

export type MockJson<T> = { [key: string]: T };

export type DotNetObjectReference = {
  invokeMethodAsync: (methodName: string, ...args: any[]) => Promise<any>;
};
