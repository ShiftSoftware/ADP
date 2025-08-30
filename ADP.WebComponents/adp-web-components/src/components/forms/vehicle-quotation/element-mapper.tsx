import { forceUpdate, h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem } from '~features/form-hook';

import { VehicleQuotation, VehicleQuotationFormLocale, phoneValidator } from './validations';
import { VehicleImageViewer } from './VehicleImageViewer';

type AdditionalFields = 'vehicleImage' | 'submit' | 'choose' | 'contact information';

export const vehicleQuotationElements: FormElementMapper<VehicleQuotation, VehicleQuotationFormLocale, AdditionalFields> = {
  'submit': ({ form, isLoading, props }) => <form-submit {...props} form={form} isLoading={isLoading} />,

  'name': ({ props, form }) => <form-input {...props} form={form} name="name" />,

  'phone': ({ form, props }) => <form-phone-number {...props} form={form} name="phone" defaultValue={phoneValidator.default} validator={phoneValidator} />,

  'vehicle': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<VehicleQuotation> = async ({ signal }): Promise<FormSelectItem[]> => {
      const vehicleEndpoint = form.context.structure?.data.vehicleApi as string;

      const response = await fetch(vehicleEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['toyotaVehicleList'] = (await response.json()).map(vehicle => ({
        label: vehicle.Title,
        value: `${vehicle.ID}`,
        meta: { image: vehicle.Image },
      })) as FormSelectItem[];

      forceUpdate(form.formStructure);
      return form.context['toyotaVehicleList'];
    };

    return <form-select {...props} name="vehicle" searchable form={form} fetcher={fetcher} language={language} />;
  },

  'choose': ({ locale }) => <h1 class="section-title">{locale.Choose}</h1>,
  'vehicleImage': ({ form }) => <VehicleImageViewer form={form} />,
  'contact information': ({ locale }) => <h1 class="section-title">{locale['Contact Information']}</h1>,
} as const;

export type vehicleQuotationElementNames = keyof typeof vehicleQuotationElements;
