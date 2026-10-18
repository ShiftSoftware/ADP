// The global script runs once, on import; each test loads a fresh copy.
const FONTS = 'link[href^="https://fonts.googleapis.com/"]';

function load() {
  jest.isolateModules(() => {
    require('./middleware');
  });
}

describe('global script: fonts', () => {
  beforeEach(() => jest.spyOn(console, 'log').mockImplementation(() => undefined));

  afterEach(() => {
    document.head.querySelectorAll(FONTS).forEach(link => link.remove());
    delete (window as { adpWebComponentsFonts?: boolean }).adpWebComponentsFonts;
    jest.restoreAllMocks();
  });

  it('adds the font stylesheet once, however many times it loads', () => {
    load();
    load();

    expect(document.head.querySelectorAll(FONTS)).toHaveLength(1);
  });

  it('adds nothing when the host opts out before the first component loads', () => {
    (window as { adpWebComponentsFonts?: boolean }).adpWebComponentsFonts = false;
    load();

    expect(document.head.querySelectorAll(FONTS)).toHaveLength(0);
  });
});
