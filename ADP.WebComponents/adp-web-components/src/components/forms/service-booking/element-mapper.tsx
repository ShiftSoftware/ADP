import { h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem, getPhoneValidator, PhoneValidator } from '~features/form-hook';

import { ServiceBooking } from './validations';

export let phoneValidator: PhoneValidator;

type AdditionalFields = 'submit' | 'service details' | 'contact information';

export const serviceBookingElements: FormElementMapper<ServiceBooking, AdditionalFields> = {
  'submit': ({ props }) => <form-submit {...props} />,

  'name': ({ props }) => {
    return <form-input {...props} />;
  },

  'phone': ({ props, isLoading }) => {
    if (!phoneValidator) {
      phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={phoneValidator.default} {...props} isLoading={isLoading} validator={phoneValidator} />;
  },

  'vehicle': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const vehicleEndpoint = form.context.structure?.data.vehicleApi as string;

      const response = await fetch(vehicleEndpoint, { signal, headers: { 'Accept-Language': language } });

      if (form.context.structure?.data?.vehiclesApiStrapiFormat) {
        const res = await response.json();

        let options = res.data;

        return options.map(vehicle => ({
          label: vehicle.attributes.GradeName,
          value: `${vehicle.id}`,
          meta: { ...vehicle, image: vehicle.attributes.Cover.data.attributes.url },
        })) as FormSelectItem[];
      } else {
        const options = await response.json();

        return options.map(vehicle => ({
          label: vehicle.Title,
          value: `${vehicle.ID}`,
          meta: { ...vehicle, image: vehicle.Image },
        })) as FormSelectItem[];
      }
    };

    let defaultValue;

    if (!props.defaultValue) {
      const params = new URLSearchParams(window.location.search);

      defaultValue = params.get(form.context.structure.data?.vehicleIdQueryParam);
    } else {
      defaultValue = props.defaultValue;
    }

    return <form-select {...props} defaultValue={defaultValue} searchable fetcher={fetcher} language={language} />;
  },

  'dealer': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const dealerEndpoint = form.context.structure?.data.dealerApi as string;

      const response = await fetch(dealerEndpoint, { signal, headers: { 'Accept-Language': language } });

      const options = (await response.json()).map(dealer => ({
        label: dealer.Name,
        value: `${dealer.ID}`,
        meta: { ...dealer },
      })) as FormSelectItem[];

      return options;
    };

    return <form-select {...props} clearable searchable fetcher={fetcher} language={language} />;
  },

  'serviceType': ({ form, language, props, locale }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const serviceTypeEndpoint = form.context.structure?.data.serviceTypeApi as string;

      if (!serviceTypeEndpoint) {
        return [
          { value: 'PeriodicMaintenance', label: locale?.['Periodic Maintenance'] || 'Periodic Maintenance' },
          { value: 'GeneralRepair', label: locale?.['General Repair'] || 'General Repair' },
          { value: 'BodyAndPaint', label: locale?.['Body and Paint'] || 'Body and Paint' },
          { value: 'TireService', label: locale?.['Tire Service'] || 'Tire Service' },
          { value: 'Other', label: locale?.Other || 'Other' },
        ] as FormSelectItem[];
      }

      const response = await fetch(serviceTypeEndpoint, { signal, headers: { 'Accept-Language': language } });

      const options = (await response.json()).map(service => ({
        label: service.Name,
        value: `${service.ID}`,
        meta: { ...service },
      })) as FormSelectItem[];

      return options;
    };

    return <form-select {...props} clearable searchable fetcher={fetcher} language={language} />;
  },

  'contactTime': ({ language, props, locale }) => {
    const fetcher: FormSelectFetcher = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'Morning',
          label: locale?.Morning,
        },
        {
          value: 'Noon',
          label: locale?.Noon,
        },
        {
          value: 'Afternoon',
          label: locale?.Afternoon,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} clearable fetcher={fetcher} language={language} />;
  },

  'preferredDate': ({ props }) => {
    return <form-input type="date" {...props} />;
  },

  'mileage': ({ props }) => {
    return <form-input type="number" {...props} />;
  },

  'additionalNotes': ({ props }) => {
    return <form-text-area {...props} />;
  },

  'city': ({ form, language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const cityEndpoint = form.context.structure?.data.cityApi as string;

      const response = await fetch(cityEndpoint, { signal, headers: { 'Accept-Language': language } });

      const options = (await response.json()).map(city => ({
        label: city.Name,
        value: `${city.ID}`,
        meta: { ...city },
      })) as FormSelectItem[];

      return options;
    };

    return <form-select {...props} clearable searchable fetcher={fetcher} language={language} />;
  },

  'service details': ({ locale }) => <h1 part="section-title">{locale?.['Service Details'] || 'Service Details'}</h1>,
  'contact information': ({ locale }) => <h1 part="section-title">{locale['Contact Information']}</h1>,
} as const;

export type serviceBookingElementNames = keyof typeof serviceBookingElements;
