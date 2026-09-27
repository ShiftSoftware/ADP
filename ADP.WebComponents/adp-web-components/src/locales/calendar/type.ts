import { array, object, string } from 'yup';

const names = (length: number) => array().of(string().required()).length(length).required().default([]);

const calendarSchema = object({
  calendar: string().required().default(''),
  previousMonth: string().required().default(''),
  nextMonth: string().required().default(''),
  today: string().required().default(''),
  chooseMonth: string().required().default(''),
  chooseYear: string().required().default(''),
  previousYear: string().required().default(''),
  nextYear: string().required().default(''),
  previousYears: string().required().default(''),
  nextYears: string().required().default(''),
  dateLabel: string().required().default(''),
  months: names(12),
  monthsInDate: names(12),
  weekdays: names(7),
  weekdaysShort: names(7),
  weekdaysNarrow: names(7),
});

export default calendarSchema;
