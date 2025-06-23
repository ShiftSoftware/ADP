import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import { getSharedLocal, LanguageKeys, SharedLocales, sharedLocalesSchema } from '~features/multi-lingual';

@Component({
  shadow: false,
  tag: 'form-structure-error',
  styleUrl: 'form-structure-error.css',
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
        <div dir={this.sharedLocales.direction} class="py-[16px] min-h-[100px] flex items-center">
          <div class="px-[16px] py-[8px] border text-[#58151c] border-[#f2aeb5] bg-[#f7d7d8] text-[20px] rounded-[8px] w-fit mx-auto">
            {this.sharedLocales.errors.wrongFormStructure}
          </div>
        </div>
      </Host>
    );
  }
}
