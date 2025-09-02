import { forceUpdate, h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem } from '~features/form-hook';

import { VehicleQuotation, VehicleQuotationFormLocale, phoneValidator } from './validations';
import { VehicleImageViewer } from './VehicleImageViewer';

type AdditionalFields = 'vehicleImage' | 'submit' | 'choose' | 'contact information' | 'current car';

export const vehicleQuotationElements: FormElementMapper<VehicleQuotation, VehicleQuotationFormLocale, AdditionalFields> = {
  'submit': ({ props }) => <form-submit {...props} />,

  'name': ({ props }) => <form-input {...props} />,

  'phone': ({ props, isLoading }) => <form-phone-number {...props} isLoading={isLoading} defaultValue={phoneValidator.default} validator={phoneValidator} />,

  'vehicle': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
      const vehicleEndpoint = form.context.structure?.data.vehicleApi as string;

      const response = await fetch(vehicleEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['vehicleList'] = await response.json();

      forceUpdate(form.formStructure);

      return form.context['vehicleList'].map(vehicle => ({
        label: vehicle.Title,
        value: `${vehicle.ID}`,
        meta: { image: vehicle.Image },
      })) as FormSelectItem[];
    };

    const params = new URLSearchParams(window.location.search);

    const defaultValue = params.get(form.context.structure.data?.vehicleIdQueryParam);

    return <form-select {...props} defaultValue={defaultValue} searchable fetcher={fetcher} language={language} />;
  },

  'dealer': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
      const dealerEndpoint = form.context.structure?.data.dealerApi as string;

      const response = await fetch(dealerEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['dealerList'] = (await response.json()).map(dealer => ({
        label: dealer.Name,
        value: `${dealer.ID}`,
      })) as FormSelectItem[];

      return form.context['dealerList'];
    };

    return <form-select {...props} clearable searchable fetcher={fetcher} language={language} />;
  },

  'paymentType': ({ language, props, locale }) => {
    const fetcher: FormSelectFetcher<VehicleQuotationFormLocale> = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'Cash',
          label: locale?.Cash,
        },
        {
          value: 'FlexiblePaymentPlan',
          label: locale.Flexible,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} clearable fetcher={fetcher} language={language} />;
  },

  'ownVehicle': ({ language, props, locale }) => {
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

    return <form-select {...props} fetcher={fetcher} language={language} />;
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

    return <form-select {...props} searchable fetcher={fetcher} language={language} resetKey={ownVehicle} isRequired={ownVehicle} isDisabled={!ownVehicle} />;
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
        fetcher={fetcher}
        language={language}
        resetKey={currentVehicleBrand}
        fetcherKey={currentVehicleBrand}
        isRequired={ownVehicle && currentVehicleBrand !== 'others'}
        isDisabled={!currentVehicleBrand || currentVehicleBrand === 'others'}
      />
    );
  },

  'vehicleImage': ({ form }) => <VehicleImageViewer form={form} />,
  'choose': ({ locale }) => <h1 class="section-title">{locale.Choose}</h1>,
  'current car': ({ locale }) => <h1 class="section-title">{locale['Your current car']}</h1>,
  'contact information': ({ locale }) => <h1 class="section-title">{locale['Contact Information']}</h1>,
} as const;

export type vehicleQuotationElementNames = keyof typeof vehicleQuotationElements;
