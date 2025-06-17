import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import { MockJson } from '~types/components';
import { LanguageKeys } from '~types/locale';
import MultiLingual from '~types/interfaces/multi-lingual';
import VehicleLookupComponent from '~types/interfaces/vehicle-lookup-component';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import ComponentLocale from '~lib/component-locale';
import { ErrorKeys, getLocaleLanguage, getSharedLocal, sharedLocalesSchema } from '~lib/get-local-language';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

import { VehicleInfoLayoutInterface, VehicleInfoLayout } from '../components/vehicle-info-layout';
import { setVehicleLookupData, setVehicleLookupErrorState } from '~api/vehicleInformation';

import { ClaimableItem } from './components/claimable-item';
import cn from '~lib/cn';

import { EmptyTableIcon } from '~assets/empty-table-icon';

@Component({
  tag: 'claim-temp',
  styleUrl: 'claim-temp.css',
  shadow: true,
})
export class ClaimTemp implements MultiLingual, VehicleInfoLayoutInterface, VehicleLookupComponent {
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

  // ====== Start Vehicle Lookup Component Shared Logic
  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() vehicleLookup?: VehicleLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    this.mockData = newMockData;
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    // Parse group information
    const beforeAssignment = async response => {
      if (response?.serviceItems?.length) {
        let orderedGroups: VehicleServiceItemDTO['group'][] = [];
        const unOrderedGroups: VehicleServiceItemDTO['group'][] = [];

        response.serviceItems.forEach(({ group }) => {
          if (!group?.name) return;

          if ([...orderedGroups, ...unOrderedGroups].find(g => g?.name === group?.name)) return;

          if (group?.isDefault) this.activeTab = group?.name;

          if (typeof group?.tabOrder === 'number') orderedGroups.push(group);
          else unOrderedGroups.push(group);
        });

        if (!!unOrderedGroups.length || !!orderedGroups.length) {
          orderedGroups = orderedGroups.sort((a, b) => a.tabOrder - b.tabOrder);
          this.tabs = [...orderedGroups, ...unOrderedGroups];
          if (!this.activeTab) this.activeTab = this.tabs[0].name;
        } else {
          this.tabs = [];
          this.activeTab = '';
        }
      } else {
        this.tabs = [];
        this.activeTab = '';
      }

      return response;
    };

    await setVehicleLookupData(this, newData, headers, { beforeAssignment });
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  @Method()
  async fetchData(requestedVin: string, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }
  // ====== End Vehicle Lookup Component Shared Logic

  // ====== Start Component Logic
  @State() activeTab: string = '';
  @State() tabs: VehicleServiceItemDTO['group'][] = [];

  private getServiceItems = (): VehicleLookupDTO['serviceItems'] => {
    if (!this.vehicleLookup?.serviceItems?.length) return [];

    if (!this.tabs?.length) return this.vehicleLookup?.serviceItems;

    return this.vehicleLookup?.serviceItems.filter(serviceItem => serviceItem?.group?.name === this.activeTab);
  };
  // ====== End Component Logic

  render() {
    const serviceItems = this.getServiceItems();

    const isNoServicesAvailable = !this.isLoading && this.vehicleLookup && !serviceItems.length;

    return (
      <Host>
        <VehicleInfoLayout
          isError={this.isError}
          coreOnly={this.coreOnly}
          isLoading={this.isLoading}
          vin={this.vehicleLookup?.vin}
          direction={this.locale.sharedLocales.direction}
          errorMessage={this.locale.sharedLocales.errors[this.errorMessage] || this.locale.sharedLocales.errors.wildCard}
        >
          <div dir="ltr" class={cn('relative h-[320px] transition-all duration-300', { loading: this.isLoading })}>
            <div class="px-[30px] relative overflow-x-scroll [&_*]:shrink-0 gap-[250px] justify-between h-full overflow-y-hidden flex items-center">
              <div
                dir={this.locale.sharedLocales.direction}
                class={cn('absolute top-0 left-0 size-full box-content flex flex-col justify-center opacity-0 transition duration-500 items-center text-slate-700', {
                  'opacity-100 scale-100': isNoServicesAvailable,
                })}
              >
                <EmptyTableIcon class="size-[90px]" />
                <div class="text-[22px]">{this.locale.sharedLocales.errors.noServiceAvailable}</div>
              </div>

              <div class="ml-[-125px]" />

              {serviceItems.map(item => (
                <ClaimableItem item={item} />
              ))}

              <div class="ml-[-125px]" />
            </div>
          </div>
        </VehicleInfoLayout>
      </Host>
    );
  }
}
