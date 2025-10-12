## static content ( can be extended )

```typescript

  // #region Part Lookup Component Shared Logic

 @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: BlazorInvokableFunction<(errorMessage: ErrorKeys) => void>;
  @Prop() loadingStateChange?: BlazorInvokableFunction<(isLoading: boolean) => void>;
  @Prop() loadedResponse?: BlazorInvokableFunction<(response: PartLookupDTO) => void>;

  @State() searchString: string
  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() partLookup?: PartLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: PartLookupMock) {
    this.mockData = newMockData;
  }

  @Method()
  async getMockData() {
    return this.mockData;
  }

  @Watch('isDev')
  async onIsDevChange(isDev) {
    if (!isDev) return;

    const mockData = await getMockFile<PartLookupDTO>('part-lookup');

    await this.setMockData(mockData);
  }

  @Method()
  async fetchData(newData: PartLookupDTO | string, headers: any = {}) {
    await setPartLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setPartLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    smartInvokable.bind(this)(this.loadingStateChange, newValue);
  }

 // #endregion
```
