// @ts-nocheck

import { Component, Element, Host, Method, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { scrollIntoContainerView } from '~lib/scroll-into-container-view';
import { ErrorKeys } from '~lib/get-local-language';

import { MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import expiredIcon from './assets/expired.svg';
import pendingIcon from './assets/pending.svg';
import cancelledIcon from './assets/cancelled.svg';
import processedIcon from './assets/processed.svg';
import activationRequiredIcon from './assets/activationRequired.svg';

import { getVehicleLookup, VehicleInformationInterface } from '~api/vehicleInformation';

import { ClaimFormPayload, VehicleItemClaimForm } from './vehicle-item-claim-form';

import { VehicleInfoLayout } from '../components/vehicle-info-layout';

let mockData: MockJson<VehicleLookupDTO> = {};

const icons = {
  expired: expiredIcon,
  pending: pendingIcon,
  processed: processedIcon,
  cancelled: cancelledIcon,
  activationRequired: activationRequiredIcon,
};

@Component({
  shadow: true,
  tag: 'vehicle-claimable-item',
  styleUrl: 'vehicle-claimable-item.css',
})
export class VehicleClaimableItem {
  @Prop() print?: (claimResponse: any) => void;
  @Prop() maximumDocumentFileSizeInMb: number = 30;
  @Prop() claimEndPoint: string = 'api/vehicle/swift-claim';

  @Prop() activate?: (vehicleInformation: VehicleLookupDTO) => void;

  @State() activeTab: string = '';

  @State() showPopup: boolean = false;
  @State() externalVin?: string = null;
  @State() showPrintBox: boolean = false;

  @State() tabAnimationLoading: boolean = false;
  @State() activePopupIndex: null | number = null;
  @State() tabs: VehicleServiceItemDTO['group'][] = [];
  @State() lastSuccessfulClaimResponse: any = null;

  pendingItemHighlighted = false;

  scrollListenerRef: () => void;
  timeoutRef: ReturnType<typeof setTimeout>;
  tabAnimationTimeoutRef: ReturnType<typeof setTimeout>;
}
