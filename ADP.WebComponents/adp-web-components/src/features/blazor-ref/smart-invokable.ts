import { BlazorInvokableFunction } from './interface';

export async function smartInvokable<T>(functionRef?: BlazorInvokableFunction<(...prop: any) => T>, ...payloads: any): Promise<T> {
  if (functionRef) {
    if (typeof functionRef === 'string') {
      if (this?.blazorRef) return await this.blazorRef?.invokeMethodAsync(functionRef, ...payloads);
    } else {
      return (await functionRef(...payloads)) as T;
    }
  }
}
