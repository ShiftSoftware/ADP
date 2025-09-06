```typescript
  // #region Form Hook logic
  @State() isLoading: boolean;
  @State() errorMessage: string;

  @Prop() theme: string;
  @Prop() brandId: string;
  @Prop() language: LanguageKeys = 'en';
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() structure: FormStructure = { tag: 'div' };
  @Prop() loadingChanges: (loading: boolean) => void;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    this.isLoading = isLoading;
    if (this.loadingChanges) this.loadingChanges(true);
  }

  setErrorCallback(error: any) {
    if (error?.message) this.errorMessage = error.message;
    if (this.errorCallback) this.errorCallback(error);
  }

  setSuccessCallback(data: any) {
    if (this.successCallback) this.successCallback(data);
  }

  async formSubmit(formValues: Demo) {
    try {
      this.setIsLoading(true);

      const token = await grecaptcha.execute(this.recaptchaKey, { action: 'submit' });

      const response = await fetch(`${this.baseUrl}?${this.queryString}`, {
        method: 'post',
        body: JSON.stringify(formValues),
        headers: {
          'Brand': this.brandId,
          'Recaptcha-Token': token,
          'Accept-Language': this.language,
          'Content-Type': 'application/json',
        },
      });

      const data = await response.json();

      this.setSuccessCallback(data);

      this.form.openDialog();
      setTimeout(() => {
        this.form.reset();
      }, 1000);
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // #endregion  Form Hook logic
```
