import { h } from '@stencil/core';
import { InferType } from 'yup';

import warrantySchema from '~locales/vehicleLookup/warranty/type';
import type { VehicleExtendedWarrantyDTO } from '~types/generated/vehicle-lookup/vehicle-extended-warranty-dto';

type Props = {
  extendedWarranties?: VehicleExtendedWarrantyDTO[];
  warrantyLocale: InferType<typeof warrantySchema>;
};

export default function ExtendedWarrantyCards({ extendedWarranties, warrantyLocale }: Props) {
  const coverages = extendedWarranties ?? [];

  if (coverages.length === 0) return null;

  return (
    <section class="extended-warranties" aria-label={warrantyLocale.extendedWarranties}>
      <h3 class="extended-warranties-title">{warrantyLocale.extendedWarranties}</h3>

      <div class="extended-warranties-grid">
        {coverages.map((coverage, index) => {
          const providerID = coverage?.providerCompanyID || '—';
          const coverageID = coverage?.id || '—';

          return (
            <article class="extended-warranty-card" key={coverage?.id || `${providerID}-${index}`}>
              <div class="extended-warranty-provider">
                {coverage?.providerCompanyLogo ? (
                  <img
                    class="extended-warranty-provider-logo"
                    src={coverage.providerCompanyLogo}
                    alt={`${warrantyLocale.extendedWarrantyProviderID}: ${providerID}`}
                    loading="lazy"
                  />
                ) : (
                  <div class="extended-warranty-provider-logo-placeholder" aria-hidden="true">
                    {providerID.slice(0, 2).toUpperCase()}
                  </div>
                )}

                <div class="extended-warranty-provider-details">
                  <span class="extended-warranty-field-label">{warrantyLocale.extendedWarrantyProviderID}</span>
                  <strong class="extended-warranty-field-value">{providerID}</strong>
                </div>
              </div>

              <dl class="extended-warranty-fields">
                <div class="extended-warranty-field extended-warranty-id">
                  <dt>{warrantyLocale.extendedWarrantyID}</dt>
                  <dd>{coverageID}</dd>
                </div>
                <div class="extended-warranty-field">
                  <dt>{warrantyLocale.extendedWarrantyStartDate}</dt>
                  <dd>{coverage?.startDate || '—'}</dd>
                </div>
                <div class="extended-warranty-field">
                  <dt>{warrantyLocale.extendedWarrantyEndDate}</dt>
                  <dd>{coverage?.endDate || '—'}</dd>
                </div>
              </dl>
            </article>
          );
        })}
      </div>
    </section>
  );
}
