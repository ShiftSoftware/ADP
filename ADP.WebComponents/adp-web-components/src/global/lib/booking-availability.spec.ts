import { BookingOutcome, clearAvailabilityCache, createAvailabilityLoader, Fetcher, parseDays, parseSlot, queryUrl, REQUEST_DAYS, slotValue } from './booking-availability';

const TARGET = { url: 'https://calendar.example/api/public/calendar', branchId: 'Xr8pQ', departmentId: 'showroom', brandId: 'BRAND' };

const reply = (payload: unknown, status = 200) => Promise.resolve({ ok: status >= 200 && status < 300, status, json: () => Promise.resolve(payload) } as Response);

const day = (date: string, ...times: string[]) => ({ Date: date, Times: times.map(time => `${date} ${time}`) });

function recorder(respond: (url: string, init: RequestInit) => Promise<Response>) {
  const calls: { url: string; init: RequestInit }[] = [];
  const fetcher: Fetcher = (url, init) => {
    calls.push({ url, init });
    return respond(url, init);
  };

  return { calls, fetcher };
}

// Resolves only when the caller aborts, as fetch does.
const hanging: Fetcher = (_url, init) =>
  new Promise((_resolve, reject) => init.signal.addEventListener('abort', () => reject(Object.assign(new Error('aborted'), { name: 'AbortError' }))));

beforeEach(() => clearAvailabilityCache());

describe('parseSlot', () => {
  it('12-hour to 24-hour, including both 12 o’clocks', () => {
    expect(parseSlot('2026-10-01 12:00 AM')).toEqual({ date: '2026-10-01', time: '00:00' });
    expect(parseSlot('2026-10-01 12:30 AM')).toEqual({ date: '2026-10-01', time: '00:30' });
    expect(parseSlot('2026-10-01 01:00 AM')).toEqual({ date: '2026-10-01', time: '01:00' });
    expect(parseSlot('2026-10-01 11:59 AM')).toEqual({ date: '2026-10-01', time: '11:59' });
    expect(parseSlot('2026-10-01 12:00 PM')).toEqual({ date: '2026-10-01', time: '12:00' });
    expect(parseSlot('2026-10-01 01:00 PM')).toEqual({ date: '2026-10-01', time: '13:00' });
    expect(parseSlot('2026-10-01 11:00 PM')).toEqual({ date: '2026-10-01', time: '23:00' });
  });

  it('tolerates case, a single-digit hour and outer spaces', () => {
    expect(parseSlot(' 2026-10-01 9:05 pm ')).toEqual({ date: '2026-10-01', time: '21:05' });
    expect(parseSlot('2026-10-01 09:00AM')).toEqual({ date: '2026-10-01', time: '09:00' });
  });

  it('rejects what is not a slot', () => {
    for (const bad of ['', '2026-10-01', '2026-10-01 13:00 PM', '2026-10-01 00:00 AM', '2026-10-01 09:60 AM', '2026-10-01 09:00', '2026-02-30 09:00 AM', 'soon', null, 9, {}]) {
      expect(parseSlot(bad)).toBeNull();
    }
  });

  it('slotValue is the form value', () => {
    expect(slotValue('2026-10-01 01:00 PM')).toBe('2026-10-01T13:00');
    expect(slotValue('nonsense')).toBe('');
  });
});

describe('parseDays', () => {
  it('orders days and times, drops empty days and duplicates, keeps the raw text', () => {
    const days = parseDays([day('2026-10-02', '01:00 PM', '09:00 AM', '09:00 AM'), day('2026-10-01', '12:00 PM'), { Date: '2026-10-03', Times: [] }]);

    expect(days).toEqual([
      { date: '2026-10-01', times: [{ time: '12:00', raw: '2026-10-01 12:00 PM' }] },
      {
        date: '2026-10-02',
        times: [
          { time: '09:00', raw: '2026-10-02 09:00 AM' },
          { time: '13:00', raw: '2026-10-02 01:00 PM' },
        ],
      },
    ]);
  });

  it('skips malformed entries and times, and times dated another day', () => {
    const days = parseDays([
      null,
      'x',
      { Date: 'tomorrow', Times: ['2026-10-01 09:00 AM'] },
      { Date: '2026-10-01' },
      { Date: '2026-10-01', Times: 'no' },
      { Date: '2026-10-01T00:00:00', Times: ['2026-10-01 09:00 AM', 'bad', 7, '2026-10-02 10:00 AM'] },
      { Date: '2026-10-05', Times: ['nope'] },
    ]);

    expect(days).toEqual([{ date: '2026-10-01', times: [{ time: '09:00', raw: '2026-10-01 09:00 AM' }] }]);
  });

  it('merges a day that appears twice', () => {
    expect(parseDays([day('2026-10-01', '10:00 AM'), day('2026-10-01', '09:00 AM', '10:00 AM')])).toEqual([
      {
        date: '2026-10-01',
        times: [
          { time: '09:00', raw: '2026-10-01 09:00 AM' },
          { time: '10:00', raw: '2026-10-01 10:00 AM' },
        ],
      },
    ]);
  });

  it('null for a payload that is not a list', () => {
    expect(parseDays({ error: 'x' })).toBeNull();
    expect(parseDays(undefined)).toBeNull();
    expect(parseDays([])).toEqual([]);
  });
});

describe('query', () => {
  it('one window from today', () => {
    const url = new URL(queryUrl(TARGET, '2026-09-28'));

    expect(url.origin + url.pathname).toBe(TARGET.url);
    expect(Object.fromEntries(url.searchParams)).toEqual({
      from: '2026-09-28',
      to: '2026-10-28',
      branchId: 'Xr8pQ',
      departmentId: 'showroom',
      brandId: 'BRAND',
    });
    expect(REQUEST_DAYS).toBe(30);
  });

  it('appends to a URL that already has a query', () => {
    expect(queryUrl({ ...TARGET, url: 'https://calendar.example/api?code=k' }, '2026-09-28')).toMatch(/^https:\/\/calendar\.example\/api\?code=k&from=2026-09-28&/);
  });

  it('null until every input is present', () => {
    for (const key of Object.keys(TARGET)) expect(queryUrl({ ...TARGET, [key]: '' }, '2026-09-28')).toBeNull();
    expect(queryUrl(null)).toBeNull();
  });

  it('today falls back to the local date', () => {
    const from = new URL(queryUrl(TARGET)).searchParams.get('from');
    const now = new Date();

    expect(from).toBe(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`);
    expect(new URL(queryUrl(TARGET, 'not-a-date')).searchParams.get('from')).toBe(from);
  });
});

describe('loader', () => {
  it('ready: days in order plus what the calendar needs', async () => {
    const { calls, fetcher } = recorder(() => reply([day('2026-10-02', '09:00 AM'), day('2026-09-30', '08:00 AM', '09:00 AM')]));
    const outcome = await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28', language: 'ar' });

    expect(calls).toHaveLength(1);
    expect(calls[0].init.headers).toEqual({ 'Accept-Language': 'ar' });
    expect(outcome.status).toBe('ready');
    if (outcome.status !== 'ready') return;
    expect(outcome.availability.enabledDates).toEqual(['2026-09-30', '2026-10-02']);
    expect(outcome.availability.slotCounts).toEqual({ '2026-09-30': 2, '2026-10-02': 1 });
    expect([outcome.availability.first, outcome.availability.last]).toEqual(['2026-09-30', '2026-10-02']);
  });

  it('empty, http, payload and network outcomes', async () => {
    const run = (respond: () => Promise<Response>) => createAvailabilityLoader(() => respond()).load(TARGET, { today: '2026-09-28' });

    expect(await run(() => reply([]))).toEqual({ status: 'empty' });
    expect(await run(() => reply([{ Date: '2026-10-01', Times: [] }]))).toEqual({ status: 'empty' });
    clearAvailabilityCache();
    expect(await run(() => reply(null, 500))).toEqual({ status: 'error', error: { kind: 'http', status: 500 } });
    expect(await run(() => reply({ message: 'x' }))).toEqual({ status: 'error', error: { kind: 'payload' } });
    expect(
      await run(() =>
        Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.reject(new SyntaxError('bad json')),
        } as unknown as Response),
      ),
    ).toEqual({ status: 'error', error: { kind: 'payload' } });
    expect(await run(() => Promise.reject(new TypeError('Failed to fetch')))).toEqual({ status: 'error', error: { kind: 'network' } });
  });

  it('departmentIds: one request per department in parallel, slots merged once each and in order', async () => {
    const { calls, fetcher } = recorder(url =>
      url.includes('departmentId=satellite-1')
        ? reply([day('2026-10-01', '09:00 AM'), day('2026-09-30', '01:00 PM', '09:00 AM')])
        : reply([day('2026-09-30', '09:00 AM', '08:00 AM')]),
    );
    const target = { ...TARGET, departmentId: '', departmentIds: ['service-center', 'satellite-1'] };
    const outcome = await createAvailabilityLoader(fetcher).load(target, { today: '2026-09-28' });

    expect(calls.map(call => new URL(call.url).searchParams.get('departmentId'))).toEqual(['service-center', 'satellite-1']);
    expect(outcome.status).toBe('ready');
    if (outcome.status !== 'ready') return;
    expect(outcome.availability.days.map(({ date, times }) => [date, times.map(t => t.time)])).toEqual([
      ['2026-09-30', ['08:00', '09:00', '13:00']],
      ['2026-10-01', ['09:00']],
    ]);

    await createAvailabilityLoader(fetcher).load(target, { today: '2026-09-28' });
    expect(calls).toHaveLength(2);

    clearAvailabilityCache();
    const failing = (url: string) => (url.includes('satellite-1') ? reply(null, 500) : reply([]));
    expect(await createAvailabilityLoader(failing).load(target, { today: '2026-09-28' })).toEqual({ status: 'error', error: { kind: 'http', status: 500 } });
  });

  it('no request without every input', async () => {
    const { calls, fetcher } = recorder(() => reply([]));

    expect(await createAvailabilityLoader(fetcher).load({ ...TARGET, branchId: '' })).toEqual({ status: 'cancelled' });
    expect(calls).toHaveLength(0);
  });

  it('a new load cancels the stale one', async () => {
    let first: Fetcher = hanging;
    const loader = createAvailabilityLoader((url, init) => first(url, init));
    const stale = loader.load(TARGET, { today: '2026-09-28' });

    first = () => reply([day('2026-10-01', '09:00 AM')]);
    const fresh = loader.load({ ...TARGET, branchId: '43' }, { today: '2026-09-28' });

    expect(await stale).toEqual({ status: 'cancelled' });
    expect((await fresh).status).toBe('ready');
  });

  it('cancel() aborts the request in flight', async () => {
    const signals: AbortSignal[] = [];
    const loader = createAvailabilityLoader((url, init) => {
      signals.push(init.signal);
      return hanging(url, init);
    });
    const pending = loader.load(TARGET, { today: '2026-09-28' });

    loader.cancel();

    expect(await pending).toEqual({ status: 'cancelled' });
    expect(signals[0].aborted).toBe(true);
  });

  it('a response that lands after cancel is dropped, not cached', async () => {
    let release: (value: Response) => void;
    const { calls, fetcher } = recorder(() => (calls.length > 1 ? reply([]) : new Promise<Response>(resolve => (release = resolve))));
    const loader = createAvailabilityLoader(fetcher);
    const pending = loader.load(TARGET, { today: '2026-09-28' });

    loader.cancel();
    release({ ok: true, status: 200, json: () => Promise.resolve([day('2026-10-01', '09:00 AM')]) } as Response);

    expect(await pending).toEqual({ status: 'cancelled' });
    await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28' });
    expect(calls).toHaveLength(2);
  });

  it('caches ready and empty per input set for the page, not errors', async () => {
    let payload: unknown = [day('2026-10-01', '09:00 AM')];
    let status = 200;
    const { calls, fetcher } = recorder(() => reply(payload, status));
    const results: BookingOutcome[] = [];

    results.push(await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28' }));
    results.push(await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28' }));
    expect(calls).toHaveLength(1);
    expect(results[1]).toBe(results[0]);

    await createAvailabilityLoader(fetcher).load({ ...TARGET, departmentId: 'service-center' }, { today: '2026-09-28' });
    await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28', language: 'ku' });
    await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-29' });
    expect(calls).toHaveLength(4);

    payload = [];
    const other = { ...TARGET, branchId: '99' };
    await createAvailabilityLoader(fetcher).load(other, { today: '2026-09-28' });
    expect(await createAvailabilityLoader(fetcher).load(other, { today: '2026-09-28' })).toEqual({ status: 'empty' });
    expect(calls).toHaveLength(5);

    status = 503;
    const failing = { ...TARGET, branchId: '98' };
    await createAvailabilityLoader(fetcher).load(failing, { today: '2026-09-28' });
    await createAvailabilityLoader(fetcher).load(failing, { today: '2026-09-28' });
    expect(calls).toHaveLength(7);
  });

  it('never retries on its own', async () => {
    const { calls, fetcher } = recorder(() => Promise.reject(new TypeError('offline')));

    await createAvailabilityLoader(fetcher).load(TARGET, { today: '2026-09-28' });
    await new Promise(resolve => setTimeout(resolve, 20));
    expect(calls).toHaveLength(1);
  });
});
