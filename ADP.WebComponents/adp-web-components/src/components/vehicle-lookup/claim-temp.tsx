import { Component, Host, Prop, State, Watch, h } from '@stencil/core';

import MultiLingual from '~types/interfaces/multi-lingual';
import { LanguageKeys } from '~types/locale';

import ComponentLocale from '~lib/component-locale';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';
import { getLocaleLanguage, getSharedLocal, sharedLocalesSchema } from '~lib/get-local-language';

import { VehicleInfoLayoutInterface } from '../components/vehicle-info-layout';

@Component({
  tag: 'claim-temp',
  styleUrl: 'claim-temp.css',
  shadow: true,
})
export class ClaimTemp implements MultiLingual, VehicleInfoLayoutInterface {
  // ====== Start Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof dynamicClaimSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...dynamicClaimSchema.getDefault() };

  async componentWillLoad() {
    await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'vehicleLookup.claimableItems', dynamicClaimSchema)]);
    this.locale = { sharedLocales, ...locale };
  }
  // ====== End Localization

  // ====== Start Vehicle info layout prop
  @Prop() coreOnly: boolean = false;
  // ====== End Vehicle info layout prop

  render() {
    return (
      <Host>
        {this.locale.claim}
        <slot></slot>
      </Host>
    );
  }
}
