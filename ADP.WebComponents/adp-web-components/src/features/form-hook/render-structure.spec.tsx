import { renderStructure } from './render-structure';

describe('renderStructure', () => {
  it('forwards the shared page clock to mapped form elements', () => {
    const date = jest.fn(() => false);

    renderStructure(
      { name: 'appointmentDate', type: 'date' },
      { date },
      {
        form: {} as never,
        isLoading: false,
        language: 'en',
        locale: {},
        props: { today: '2026-09-01' },
      },
      {},
      -2,
    );

    expect(date).toHaveBeenCalledWith(expect.objectContaining({ props: expect.objectContaining({ name: 'appointmentDate', today: '2026-09-01' }) }));
  });
});
