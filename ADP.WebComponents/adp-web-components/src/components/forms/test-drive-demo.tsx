import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import testDriveDemoSchema from '~locales/forms/testDriveDemo/type';

import { testDriveDemoElementNames, testDriveDemoElements } from './test-drive-demo/element-mapper';
import { TestDriveDemo, TestDriveDemoFormLocale, testDriveDemoInputsValidation } from './test-drive-demo/validations';

import { FormHook } from '~features/form-hook/form-hook';
import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import { fetchJson } from '~lib/fetch-json';

@Component({
  shadow: true,
  tag: 'test-drive-demo-form',
  styleUrl: 'test-drive-demo/themes.css',
})
export class TestDriveDemoForm implements FormHookInterface<TestDriveDemo>, MultiLingual {
  // #region Localization
  @Prop({ mutable: true, reflect: true }) language: LanguageKeys = 'en';

  @State() locale: TestDriveDemoFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...testDriveDemoSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.testDriveDemo', testDriveDemoSchema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.localeLanguage = newLanguage;

    this.form.rerender({ rerenderAll: true });
  }
  // #endregion

  // #region Form Hook logic
  @State() errorMessage: string;
  @State() isLoading: boolean = false;
  @State() localeLanguage: LanguageKeys;

  @Prop() gistId?: string;
  @Prop() structureUrl?: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<testDriveDemoElementNames> | undefined;

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

  async componentWillLoad() {
    this.language = getLanguageFromUrl();

    if (this.structure) {
      await this.changeLanguage(this.language);
    } else {
      if (this.gistId) {
        const [newGistStructure] = await Promise.all([gistLoader(this.gistId), this.changeLanguage(this.language)]);
        this.structure = newGistStructure as FormElementStructure<testDriveDemoElementNames>;
      } else if (this.structureUrl) {
        await this.changeLanguage(this.language);
        const [newGistStructure] = await Promise.all([fetchJson<FormElementStructure<testDriveDemoElementNames>>(this.structureUrl), this.changeLanguage(this.language)]);
        this.structure = newGistStructure;
      }
    }
    this.localeLanguage = this.language;
  }

  async formSubmit(formValues: TestDriveDemo) {
    try {
      this.setIsLoading(true);

      const payload = {
        name: formValues.name,
        phone: formValues.phone,
        email: formValues?.email || '',
        companyBranchId: formValues.branch,
        ticketPriority: formValues.priority,
      };

      const requestUrl = this.structure?.data?.requestUrl as string;
      const functionKey = this.structure?.data['x-functions-key'] as string;

      const response = await fetch(requestUrl, {
        method: 'POST',
        headers: {
          'x-functions-key': functionKey,
          'Content-Type': 'application/json',
          'Accept-Language': this.localeLanguage || 'en',
        },
        body: JSON.stringify(payload),
      });

      console.log(response);

      if (response.ok) {
        const result = await response?.json();
        this.setSuccessCallback(result);
        this.form.openDialog();
        setTimeout(() => {
          this.form.reset();
          this.form.rerender({ rerenderForm: true, rerenderAll: true });
        }, 100);
      } else {
        const errorText = await response?.text();
        throw new Error(errorText);
      }
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // #endregion

  // #region Component Logic

  form = new FormHook(this, testDriveDemoInputsValidation);

  // #endregion
  render() {
    return (
      <Host>
        <div part={`test-drive-demo-${this.structure?.data?.theme}`}>
          <form-structure
            form={this.form}
            formLocale={this.locale}
            structure={this.structure}
            isLoading={this.isLoading}
            language={this.localeLanguage}
            errorMessage={this.errorMessage}
            formElementMapper={testDriveDemoElements}
            successMessage={this.locale['Form submitted successfully.']}
          >
            <slot></slot>
          </form-structure>
        </div>
      </Host>
    );
  }
}
