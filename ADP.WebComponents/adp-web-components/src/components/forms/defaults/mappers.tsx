import { h } from '@stencil/core';
import { FormSelectFetcher, FormSelectItem, getPhoneValidator } from '~features/form-hook';
import { VehicleImageViewer } from '../../form-elements/VehicleImageViewer';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';
import { format, isBefore, isEqual } from 'date-fns';
import { decodeTimeOffset } from '~lib/decode-time-offset';
import { populateItems } from '~lib/populate-items';

export const getFormMappers = (stateObject: Record<string, any>, extraMappers: Record<string, (prop: any) => any> = {}) => ({
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

  vehicle: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal, context }): Promise<FormSelectItem[]> => {
      const params = new URLSearchParams(window.location.search);

      let paramValue = (params.get(props?.vehicleIdQueryParam) || '')?.toLowerCase();

      const vehicleEndpoint = props.vehicleApi as string;

      const response = await fetch(vehicleEndpoint, { signal, headers: { 'Accept-Language': language } });

      let options;
      let defaultIndex = -1;

      if (props?.dynamic) {
        const res = await response.json();
        options = populateItems(res, props, true) as FormSelectItem[];

        options.forEach((vehicle: FormSelectItem, idx) => {
          if (!props.defaultValue && paramValue && (vehicle?.label?.toLowerCase() === paramValue || `${vehicle.value}`?.toLowerCase() === paramValue)) defaultIndex = idx;
        });
      } else if (props?.vehiclesApiStrapiFormat) {
        const res = await response.json();

        options = res.data;

        options = options.map((vehicle, idx) => {
          if (!props.defaultValue && paramValue && (vehicle?.GradeName?.toLowerCase() === paramValue || `${vehicle.id}`?.toLowerCase() === paramValue)) defaultIndex = idx;

          return {
            label: vehicle.attributes.GradeName,
            value: props?.useNamedValue ? `${vehicle?.attributes?.GradeName}` : `${vehicle.id}`,
            meta: { ...vehicle, image: vehicle.attributes.Cover.data.attributes.url },
          };
        }) as FormSelectItem[];
      } else {
        options = await response.json();

        options = options.map((vehicle, idx) => {
          if (!props.defaultValue && paramValue && (vehicle?.Title?.toLowerCase() === paramValue || `${vehicle.ID}`?.toLowerCase() === paramValue)) defaultIndex = idx;

          return {
            label: vehicle.Title,
            value: props?.useNamedValue ? `${vehicle.Title}` : `${vehicle.ID}`,
            meta: { ...vehicle, image: vehicle.Image },
          };
        }) as FormSelectItem[];
      }

      if (!props?.defaultValue) {
        if (defaultIndex > -1) context.defaultValue = options[defaultIndex]?.value;
        else context.defaultValue = '';
      } else context.defaultValue = props?.defaultValue;

      return options;
    };

    return <form-select {...props} searchable fetcher={fetcher} language={language} />;
  },

  companyBranchId: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const dealerEndpoint = props?.branchApi as string;

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

  cityId: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const dealerEndpoint = props?.cityApi as string;

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

  currentVehicleBrand: ({ form, language, props, locale }) => {
    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const currentVehiclesEndpoint = form.context.structure?.data.currentVehiclesApi as string;

      const response = await fetch(currentVehiclesEndpoint, { signal, headers: { 'Accept-Language': 'en' } });

      const options = (await response.json()).filter(vehicle => vehicle.name !== 'Other');

      return [
        ...options.map(vehicle => ({
          label: vehicle.Name,
          value: `${vehicle.ID}`,
          meta: { ...vehicle },
        })),
        {
          value: 'Other',
          label: locale.Other,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} searchable fetcher={fetcher} language={language} />;
  },

  currentVehicleModel: ({ form, language, props, locale }) => {
    form.addWatcher('currentVehicleBrand');

    const currentVehicleBrand = form?.getValue('currentVehicleBrand');

    const fetcher: FormSelectFetcher = async ({}): Promise<FormSelectItem[]> => {
      if (!currentVehicleBrand) return [];

      const selectedBrand = form.context['currentVehicleBrandList']?.find(vehicle => vehicle.value === currentVehicleBrand)?.meta;

      if (!selectedBrand) return [];

      return [
        ...selectedBrand?.Models.map(model => ({ label: model.Name, value: `${model.ID}` })),
        {
          value: 'Other',
          label: locale.Other,
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
        isRequired={currentVehicleBrand !== 'Other'}
        isDisabled={!currentVehicleBrand || currentVehicleBrand === 'Other'}
      />
    );
  },

  ownVehicle: ({ language, props }) => {
    const fetcher: FormSelectFetcher = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'yes',
          label: props?.localization?.[language]?.yes,
        },
        {
          value: 'no',
          label: props?.localization?.[language]?.no,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} fetcher={fetcher} language={language} />;
  },

  conditionalCurrentVehicleBrand: ({ form, language, props, locale }) => {
    form.addWatcher('ownVehicle');
    const ownVehicle = form?.getValue('ownVehicle') === 'yes';

    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const currentVehiclesEndpoint = props?.brandApi as string;

      const response = await fetch(currentVehiclesEndpoint, { signal, headers: { 'Accept-Language': 'en' } });

      const options = (await response.json()).filter(vehicle => vehicle.name !== 'Other');

      return [
        ...options.map(vehicle => ({
          label: vehicle.Name,
          value: props?.useNamedValue ? vehicle.Name : `${vehicle.ID}`,
          meta: { ...vehicle },
        })),
        {
          value: 'Other',
          label: locale.Other,
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} searchable fetcher={fetcher} language={language} resetKey={ownVehicle} isRequired={ownVehicle} isDisabled={!ownVehicle} />;
  },

  conditionalCurrentVehicleModel: ({ form, language, props, locale }) => {
    form.addWatcher('ownVehicle');
    form.addWatcher('conditionalCurrentVehicleBrand');

    const ownVehicle = form?.getValue('ownVehicle') === 'yes';
    const currentVehicleBrand = form?.getValue('conditionalCurrentVehicleBrand');

    const fetcher: FormSelectFetcher = async ({}): Promise<FormSelectItem[]> => {
      if (!currentVehicleBrand) return [];

      const selectedBrand = form.context['conditionalCurrentVehicleBrandList']?.find(vehicle => vehicle.value === currentVehicleBrand)?.meta;

      if (!selectedBrand) return [];

      return [
        ...selectedBrand?.Models.map(model => ({ label: model.Name, value: props?.useNamedValue ? model.Name : `${model.ID}` })),
        {
          value: 'Other',
          label: locale.Other,
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
        isRequired={ownVehicle && currentVehicleBrand !== 'Other'}
        isDisabled={!currentVehicleBrand || currentVehicleBrand === 'Other'}
      />
    );
  },

  ...extraMappers,
});
