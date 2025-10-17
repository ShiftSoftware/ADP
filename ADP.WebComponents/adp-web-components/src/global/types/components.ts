export type MockJson<T> = { [key: string]: T };

export type EndPoint = {
  url: string;
  body?: object;
  headers?: object;
  method?: string;
};
