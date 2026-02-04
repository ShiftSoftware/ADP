import { h } from '@stencil/core';

import type { VehicleLaborDTO } from '~types/generated/vehicle-lookup/vehicle-labor-dto';
import type { VehiclePartDTO } from '~types/generated/vehicle-lookup/vehicle-part-dto';
import type { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import type { InformationTableColumn } from '../../components/information-table';
import { ComponentLocale } from '~features/multi-lingual';
import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

export type Props = {
  row: VehicleServiceHistoryDTO;
  locale: ComponentLocale<typeof ServiceHistorySchema>;
};

export const ServiceHistorySubRow = ({ locale, row }: Props) => {
  const laborLines: VehicleLaborDTO[] = row?.laborLines || [];
  const partLines: VehiclePartDTO[] = row?.partLines || [];

  const laborHeaders: InformationTableColumn[] = [
    { key: 'laborCode', label: locale.laborCode },
    { key: 'packageCode', label: locale.packageCode },
    { key: 'serviceCode', label: locale.serviceCode },
    { key: 'serviceDescription', label: locale.serviceDescription, centeredHorizontally: false },
  ];

  const partHeaders: InformationTableColumn[] = [
    { key: 'partNumber', label: locale.partNumber },
    { key: 'qty', label: locale.qty },
    { key: 'packageCode', label: locale.packageCode },
    { key: 'partDescription', label: locale.partDescription, centeredHorizontally: false },
  ];

  return (
    <div class="service-history-subrow [&_.information-table-wrapper]:!w-fsull [&_.information-table-wrapper]:!mx-0" dir={locale.sharedLocales.direction}>
      <section class="service-history-subsection">
        <div class="service-history-subsection-header">
          <div class="service-history-subsection-title">{locale.laborLines}</div>
          <div class="service-history-subsection-meta">{laborLines.length}</div>
        </div>

        {laborLines.length ? (
          <div class="service-history-subsection-table">
            <information-table isRaw headers={laborHeaders} rows={laborLines} size="small" allowAutoWidth />
          </div>
        ) : (
          <div class="service-history-empty">{locale.noData}</div>
        )}
      </section>

      <section class="service-history-subsection">
        <div class="service-history-subsection-header">
          <div class="service-history-subsection-title">{locale.partLines}</div>
          <div class="service-history-subsection-meta">{partLines.length}</div>
        </div>

        {partLines.length ? (
          <div class="service-history-subsection-table">
            <information-table isRaw headers={partHeaders} rows={partLines} size="small" allowAutoWidth />
          </div>
        ) : (
          <div class="service-history-empty">{locale.noData}</div>
        )}
      </section>
    </div>
  );
};
