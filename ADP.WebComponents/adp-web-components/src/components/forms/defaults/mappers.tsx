import { h } from '@stencil/core';
import { FormSelectFetcher, FormSelectItem, getPhoneValidator } from '~features/form-hook';
import { VehicleImageViewer } from '../../form-elements/VehicleImageViewer';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';
import { Clock8Icon } from '~assets/clock-8-icon';
import { format, isAfter, isBefore, isEqual, startOfDay } from 'date-fns';
import { decodeTimeOffset } from '~lib/decode-time-offset';

export const getDefaultMappers = (stateObject: Record<string, any>) => ({
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => <form-input {...props} />,

  lastName: ({ props }) => <form-input {...props} />,

  email: ({ props }) => <form-input type="email" {...props} />,

  message: ({ props }) => <form-text-area {...props} />,

  vin: ({ props }) => <form-vin-input {...props} />,

  vehicleImage: ({ form }) => <VehicleImageViewer form={form} />,

  phone: ({ props, isLoading }) => {
    if (!stateObject.phoneValidator) {
      stateObject.phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={stateObject.phoneValidator.default} {...props} isLoading={isLoading} validator={stateObject.phoneValidator} />;
  },

  vehicle: ({ form, language, props }) => {
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

  companyBranchId: ({ form, language, props }) => {
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

  date: ({ props }) => <form-picker-input type="date" {...props} icon={<CalendarDaysIcon />} />,

  time: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async (): Promise<FormSelectItem[]> => {
      const options: FormSelectItem[] = [];

      if (Array.isArray(props.span) && Array.isArray(props.min) && Array.isArray(props.max) && props.format) {
        let tempDate = decodeTimeOffset({ offsets: props.min }) as Date;
        let maxDate = decodeTimeOffset({ offsets: props.max }) as Date;

        while (isBefore(tempDate, maxDate) || isEqual(tempDate, maxDate)) {
          options.push({
            value: format(tempDate, props.format),
            label: format(tempDate, props.format),
          });

          tempDate = decodeTimeOffset({ offsets: props.span, date: tempDate }) as Date;
        }
      }

      return options;
    };

    return <form-select {...props} clearable fetcher={fetcher} language={language} />;
  },
});
