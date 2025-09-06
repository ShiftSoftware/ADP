export type DotNetObjectReference = {
  invokeMethodAsync: (methodName: string, ...args: any[]) => Promise<any>;
};

export type BlazorInvokableFunction<T = () => void> = string | T;

export interface BlazorInvokable {
  blazorRef?: DotNetObjectReference;

  setBlazorRef: (newBlazorRef: DotNetObjectReference) => void;
}
