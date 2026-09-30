/**
 * bookingSlot: the wrapper around <shift-booking-calendar>. The component itself is
 * mocked out (it fetches availability and has its own specs in adp-web-components),
 * so these tests drive the element the way it drives the wrapper: by reading the
 * properties set on it and by dispatching `slotChange`.
 */

import { describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';

vi.mock('adp-web-components/components/shift-booking-calendar', () => ({}));

type CalendarElement = HTMLElement & {
  calendarApi?: string;
  branchId?: string;
  departmentId?: string;
  brandId?: string;
  label?: string;
  isRequired?: boolean;
  showStatus?: boolean;
  defaultValue?: string;
};

/** branch (dropdown) → slot (bookingSlot on the picked branch) → done. */
function fixture(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      {
        id: 'where',
        title: { en: 'Where?' },
        questions: [
          {
            type: 'dropdown',
            id: 'branch',
            title: { en: 'Branch' },
            required: true,
            options: [
              { id: 'Xr8pQ', label: { en: 'North branch' } },
              { id: 'Pw3xQ', label: { en: 'South branch' } },
            ],
          },
        ],
        nextScreen: 'when',
      },
      {
        id: 'when',
        title: { en: 'When?' },
        questions: [
          {
            type: 'bookingSlot',
            id: 'slot',
            title: { en: 'Preferred date and time' },
            required: true,
            calendarApi: 'https://calendar.example/api/calendar',
            branchId: '{{answers.branch}}',
            departmentId: 'service-center',
            brandId: 'BRAND',
          },
        ],
        nextScreen: 'done',
      },
      { id: 'done', title: { en: 'Booked {{answers.slot.label}} at {{answers.branch.label}}' }, questions: [] },
    ],
  };
}

const next = () => screen.getByRole('button', { name: /^(Next|Submit)$/ });
const back = () => screen.getByRole('button', { name: 'Back' });
const calendar = () => document.querySelector('shift-booking-calendar') as CalendarElement;

const pickSlot = (value: string) =>
  act(() => {
    calendar().dispatchEvent(
      new CustomEvent('slotChange', { detail: { date: value.slice(0, 10), raw: '', value }, bubbles: true, composed: true }),
    );
  });

async function toSlotScreen(user: ReturnType<typeof userEvent.setup>, branch: string) {
  await user.selectOptions(screen.getByRole('combobox'), branch);
  await user.click(next());
  screen.getByRole('heading', { name: 'When?' });
}

describe('bookingSlot question', () => {
  it('hands the calendar its target from the earlier answer, and shows the title once', async () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    const user = userEvent.setup();
    await toSlotScreen(user, 'Xr8pQ');

    const el = calendar();
    expect(el.calendarApi).toBe('https://calendar.example/api/calendar');
    expect(el.branchId).toBe('Xr8pQ');
    expect(el.departmentId).toBe('service-center');
    expect(el.brandId).toBe('BRAND');
    expect(el.isRequired).toBe(true);
    // Loading and a failure with its retry are the calendar's to show; the survey shows neither.
    expect(el.showStatus).toBe(true);
    // The component names its group with the title; the survey's copy is the visible one.
    expect(el.label).toBe('Preferred date and time');
    expect(screen.getAllByText('Preferred date and time')).toHaveLength(1);
  });

  it('is required until a slot is picked, then submits the wall-clock value', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await toSlotScreen(user, 'Xr8pQ');

    await user.click(next());
    expect(screen.getByRole('alert')).toHaveTextContent(/required/i);

    // Next from the last question screen submits and shows the end screen.
    await pickSlot('2026-10-07T09:00');
    await user.click(next());
    screen.getByRole('heading', { name: /^Booked .*2026.* at North branch$/ });
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers).toEqual({ branch: 'Xr8pQ', slot: '2026-10-07T09:00' });
  });

  it('keeps the slot when the respondent goes back and keeps the branch', async () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    const user = userEvent.setup();
    await toSlotScreen(user, 'Xr8pQ');
    await pickSlot('2026-10-07T09:00');

    await user.click(back());
    await user.click(next());

    expect(calendar().defaultValue).toBe('2026-10-07T09:00');
    await user.click(next());
    screen.getByRole('heading', { name: /^Booked / });
  });

  it('clears the slot when the respondent goes back and picks another branch', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await toSlotScreen(user, 'Xr8pQ');
    await pickSlot('2026-10-07T09:00');

    await user.click(back());
    await toSlotScreen(user, 'Pw3xQ');

    expect(calendar().branchId).toBe('Pw3xQ');
    expect(calendar().defaultValue).toBe('');
    await user.click(next());
    expect(screen.getByRole('alert')).toHaveTextContent(/required/i);

    await pickSlot('2026-10-08T10:00');
    await user.click(next());
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers).toEqual({ branch: 'Pw3xQ', slot: '2026-10-08T10:00' });
  });
});
