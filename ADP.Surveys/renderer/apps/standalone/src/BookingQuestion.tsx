import { useEffect, useRef } from 'react';
import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { localize, useSurveyContext, type QuestionProps } from '@shiftsoftware/survey-renderer';

export interface BookingTarget {
  calendarApi: string;
  calendarApiVersion?: 'v1' | 'v2';
  companyId: string;
  branchId: string;
  departmentId: string;
  brandId: string;
}

type BookingCalendarElement = HTMLElement & Record<string, unknown>;

export function bookingQuestion(target: BookingTarget) {
  return function BookingQuestion({ question }: QuestionProps) {
    const { locale, schema, answers, setAnswer } = useSurveyContext();
    const ref = useRef<BookingCalendarElement>(null);
    const id = question['id'] as string;
    const title = question['title'] as LocalizedString | undefined;
    const help = question['help'] as LocalizedString | undefined;
    const required = Boolean(question['required']);
    const value = (answers[id] as string | undefined) ?? '';

    useEffect(() => {
      const el = ref.current;
      if (!el) return;
      Object.assign(el, target, { valueFormat: 'iso', hourCycle: 'h12', language: locale.startsWith('ar') ? 'ar' : 'en' });
      const onChange = (e: Event) => setAnswer(id, (e as CustomEvent<{ value: string }>).detail.value || null);
      el.addEventListener('pickerChange', onChange);
      return () => el.removeEventListener('pickerChange', onChange);
    }, [id, locale, setAnswer]);

    useEffect(() => {
      if (ref.current && ref.current['value'] !== value) ref.current['value'] = value;
    }, [value]);

    return (
      <div className="survey-question survey-question--datetime">
        <label className="survey-question__label">
          {localize(title, locale, schema.defaultLocale)}
          {required && <span aria-label="required" className="survey-question__required"> *</span>}
        </label>
        {help && <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>}
        <shift-booking-calendar ref={ref} show-status />
      </div>
    );
  };
}

declare module 'react' {
  namespace JSX {
    interface IntrinsicElements {
      'shift-booking-calendar': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & { 'show-status'?: boolean };
    }
  }
}
