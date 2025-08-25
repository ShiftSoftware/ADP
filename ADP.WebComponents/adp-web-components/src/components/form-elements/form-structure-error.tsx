import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import { getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: false,
  tag: 'form-structure-error',
  styleUrl: 'form-inputs.css',
})
export class FormStructureError {
  @Prop() language: LanguageKeys = 'en';

  @State() sharedLocales: SharedLocales = sharedLocalesSchema.getDefault();

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    this.sharedLocales = await getSharedLocal(newLanguage);
  }

  render() {
    return (
      <Host>
        <div dir={this.sharedLocales.direction} part="form-structure-error-container" class="form-structure-error-container">
          <div part="form-structure-error-content" class="form-structure-error-content">
            {this.sharedLocales.errors.wrongFormStructure}
          </div>
        </div>
      </Host>
    );
  }
}
