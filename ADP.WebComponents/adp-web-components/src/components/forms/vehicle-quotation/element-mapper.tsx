import { forceUpdate, h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem } from '~features/form-hook';

import { VehicleQuotation, VehicleQuotationFormLocale, phoneValidator } from './validations';
import { VehicleImageViewer } from './VehicleImageViewer';

type AdditionalFields = 'vehicleImage' | 'submit' | 'choose' | 'contact information' | 'current car';

export const vehicleQuotationElements: FormElementMapper<VehicleQuotation, VehicleQuotationFormLocale, AdditionalFields> = {
  'submit': ({ form, props }) => <form-submit {...props} form={form} />,

  'name': ({ props, form }) => {
    console.log('name', props);

    return <form-input {...props} form={form} name="name" />;
  },

  'phone': ({ form, props, isLoading }) => (
    <form-phone-number {...props} form={form} name="phone" isLoading={isLoading} defaultValue={phoneValidator.default} validator={phoneValidator} />
  ),

  'vehicle': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
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

  'dealer': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
      const dealerEndpoint = form.context.structure?.data.dealerApi as string;

      const response = await fetch(dealerEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['toyotaDealerList'] = (await response.json()).map(dealer => ({
        label: dealer.Name,
        value: `${dealer.ID}`,
      })) as FormSelectItem[];

      return form.context['toyotaDealerList'];
    };

    return <form-select {...props} name="dealer" clearable searchable form={form} fetcher={fetcher} language={language} />;
  },

  'paymentType': ({ form, language, props, locale }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'Cash',
          label: locale?.Cash,
        },
        {
          value: 'Flexible',
          label: locale.Flexible,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} name="paymentType" clearable form={form} fetcher={fetcher} language={language} />;
  },

  'ownVehicle': ({ form, language, props, locale }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'yes',
          label: locale?.Yes,
        },
        {
          value: 'no',
          label: locale?.No,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} name="ownVehicle" form={form} fetcher={fetcher} language={language} />;
  },

  'currentVehicleBrand': ({ form, language, props, locale }) => {
    form.addWatcher('ownVehicle');
    const ownVehicle = form.getValue<VehicleQuotation>('ownVehicle') === 'yes';

    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
      const currentVehiclesEndpoint = form.context.structure?.data.currentVehiclesApi as string;

      const response = await fetch(currentVehiclesEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['currentVehiclesList'] = await response.json();

      return [
        ...form.context['currentVehiclesList'].map(vehicle => ({
          label: vehicle.name,
          value: `${vehicle.id}`,
        })),
        {
          value: 'others',
          label: locale.Others,
        },
      ] as FormSelectItem[];
    };

    return (
      <form-select
        {...props}
        searchable
        form={form}
        fetcher={fetcher}
        language={language}
        resetKey={ownVehicle}
        isRequired={ownVehicle}
        isDisabled={!ownVehicle}
        name="currentVehicleBrand"
      />
    );
  },

  'currentVehicleModel': ({ form, language, props, locale }) => {
    form.addWatcher('ownVehicle');
    form.addWatcher('currentVehicleBrand');

    const ownVehicle = form.getValue<VehicleQuotation>('ownVehicle') === 'yes';
    const currentVehicleBrand = form.getValue<VehicleQuotation>('currentVehicleBrand');

    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({}): Promise<FormSelectItem[]> => {
      if (!currentVehicleBrand) return [];

      const selectedBrand = form.context['currentVehiclesList']?.find(vehicle => `${vehicle?.id}` === currentVehicleBrand);

      if (!selectedBrand) return [];

      return [
        ...selectedBrand?.models.map(model => ({ label: model.name, value: `${model.id}` })),
        {
          value: 'others',
          label: locale.Others,
        },
      ] as FormSelectItem[];
    };

    return (
      <form-select
        {...props}
        searchable
        form={form}
        fetcher={fetcher}
        language={language}
        name="currentVehicleModel"
        resetKey={currentVehicleBrand}
        fetcherKey={currentVehicleBrand}
        isRequired={ownVehicle && currentVehicleBrand !== 'others'}
        isDisabled={!currentVehicleBrand || currentVehicleBrand === 'others'}
      />
    );
  },

  'choose': ({ locale }) => <h1 class="section-title">{locale.Choose}</h1>,
  'vehicleImage': ({ form }) => <VehicleImageViewer form={form} />,
  'contact information': ({ locale }) => <h1 class="section-title">{locale['Contact Information']}</h1>,
  'current car': ({ locale }) => <h1 class="section-title">{locale['Your current car']}</h1>,
} as const;

export type vehicleQuotationElementNames = keyof typeof vehicleQuotationElements;
