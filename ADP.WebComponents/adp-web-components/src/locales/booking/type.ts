import { array, object, string } from 'yup';

const bookingSchema = object({
  times: string().required().default(''),
  noTimes: string().required().default(''),
  loadingTimes: string().required().default(''),
  timesCount: string().required().default(''),
  am: string().required().default(''),
  pm: string().required().default(''),
  bookingCalendar: string().required().default(''),
  idle: string().required().default(''),
  loading: string().required().default(''),
  empty: string().required().default(''),
  error: string().required().default(''),
  retry: string().required().default(''),
  pickDay: string().required().default(''),
  pickTime: string().required().default(''),
  selected: string().required().default(''),
  dayTimes: string().required().default(''),
  required: string().required().default(''),
  changeDate: string().required().default(''),
  dayTitle: string().required().default(''),
  monthsShort: array().of(string().required()).length(12).required().default([]),
  timesOn: string().required().default(''),
  chooseSlot: string().required().default(''),
  labelFormat: string().required().default(''),
  timesCountOne: string().required().default(''),
  daySummary: string().required().default(''),
  emptyTitle: string().required().default(''),
});

export default bookingSchema;
