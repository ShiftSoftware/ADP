import { InferType } from 'yup';
import { h } from '@stencil/core';

import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import StatusCard from './StatusCard';

import RejectIcon from '~assets/x-mark.svg';

import warrantySchema from '~locales/vehicleLookup/warranty/type';
import cn from '~lib/cn';

type Props = {
  isLoading: boolean;
  isAuthorized: boolean;
  unInvoicedByBrokerName?: string;
  vehicleInformation: VehicleLookupDTO;
  warrantyLocale: InferType<typeof warrantySchema>;
};

export default function CardsContainer({ isLoading, vehicleInformation, isAuthorized, unInvoicedByBrokerName, warrantyLocale }: Props) {
  return (
    <div class="tags-container mx-auto">
      <flexible-container isOpened={!vehicleInformation || !!vehicleInformation?.saleInformation?.companyName}>
        <div class={!(!vehicleInformation || !!vehicleInformation?.saleInformation?.companyName) && 'loading'} style={{ paddingBottom: '12px' }}>
          <div class="shift-skeleton !rounded-[4px]">
            <div class="card warning-card">
              <p class="no-padding flex gap-2">
                <span class="font-semibold">{warrantyLocale.dealer}:</span> {vehicleInformation?.saleInformation?.companyName || ''}{' '}
                {vehicleInformation?.saleInformation?.countryName && `(${vehicleInformation?.saleInformation?.countryName})`}
              </p>
            </div>
          </div>
        </div>
      </flexible-container>

      <flexible-container classes={cn({ loading: isLoading || !unInvoicedByBrokerName })} isOpened={!!unInvoicedByBrokerName}>
        <div style={{ paddingBottom: '12px' }}>
          <div class="shift-skeleton">
            <div class="card warning-card">
              <img src={RejectIcon} />

              <p>{unInvoicedByBrokerName ? warrantyLocale.notInvoiced + unInvoicedByBrokerName : ''}</p>
            </div>
          </div>
        </div>
      </flexible-container>

      <div class="warranty-tags">
        <StatusCard
          state={vehicleInformation ? (isAuthorized ? 'success' : 'reject') : 'idle'}
          desc={vehicleInformation ? (isAuthorized ? warrantyLocale.authorized : warrantyLocale.unauthorized) : ''}
        />

        <StatusCard
          state={vehicleInformation ? (vehicleInformation?.warranty?.hasActiveWarranty ? 'success' : 'reject') : 'idle'}
          desc={vehicleInformation ? (vehicleInformation?.warranty?.hasActiveWarranty ? warrantyLocale.activeWarranty : warrantyLocale.notActiveWarranty) : ''}
        />

        <StatusCard
          from
          icon={false}
          fromDesc={warrantyLocale.from}
          desc={vehicleInformation?.warranty?.warrantyStartDate || ''}
          opened={!!vehicleInformation?.warranty?.warrantyStartDate || !vehicleInformation}
          state={!!vehicleInformation ? (vehicleInformation?.warranty?.hasActiveWarranty ? 'success' : 'reject') : 'idle'}
        />

        <StatusCard
          to
          icon={false}
          toDesc={warrantyLocale.to}
          desc={vehicleInformation?.warranty?.warrantyEndDate || ''}
          opened={!!vehicleInformation?.warranty?.warrantyEndDate || !vehicleInformation}
          state={!!vehicleInformation ? (vehicleInformation?.warranty?.hasActiveWarranty ? 'success' : 'reject') : 'idle'}
        />

        <StatusCard
          from
          icon={false}
          fromDesc={warrantyLocale.extendedWarrantyFrom}
          desc={vehicleInformation?.warranty?.extendedWarrantyStartDate || ''}
          opened={(!!vehicleInformation && !!vehicleInformation?.warranty?.extendedWarrantyStartDate)}
          state={(vehicleInformation?.warranty?.hasExtendedWarranty ? 'info' : 'reject')}
        />

        <StatusCard
          to
          icon={false}
          toDesc={warrantyLocale.extendedWarrantyTo}
          desc={vehicleInformation?.warranty?.extendedWarrantyEndDate || ''}
          opened={(!!vehicleInformation && !!vehicleInformation?.warranty?.extendedWarrantyEndDate)}
          state={(vehicleInformation?.warranty?.hasExtendedWarranty ? 'info' : 'reject')}
        />
      </div>
    </div>
  );
}
