/**
 * Landing-page copy in the four languages the components themselves support,
 * plus the language state that goes with it.
 *
 * Loaded as a plain blocking script in <head> — deliberately not a module, for
 * the same reason as `harness-theme.js`: a module is deferred, and the page
 * would paint left-to-right before an RTL choice was applied. Direction is a
 * layout property; it cannot be corrected after first paint without a visible
 * jump.
 *
 * Scope is the landing page and the 404 page. Harness pages have their own
 * language control inside `harness()`, because there the language is an input to
 * the component under test rather than a property of the page. Both use the same
 * `?lang=` parameter, so a choice survives navigating between them.
 *
 * ── Accuracy ──────────────────────────────────────────────────────────────────
 * Several claims here were wrong in the first pass and were corrected against the
 * source. Keep them honest; an integrator finds a false claim within an hour and
 * then trusts nothing else on the page. In particular:
 *
 *   • Credentials are the HOST's job. The vehicle elements take a `headers`
 *     prop and `fetchVin(vin, headers)`; the part elements take an `endpoint` —
 *     a URL, or `{ url, headers, method, query }` — and `fetchData(partNumber)`.
 *     The element sends none of its own.
 *   • There is NO retry. A failed request stays failed.
 *   • The lookup elements reset inherited styles and are not host-themeable.
 *   • The form elements portal their dropdowns and dialogs onto document.body,
 *     so those DO inherit host styles.
 *   • `base-url` (vehicle elements) is a path prefix the element concatenates
 *     the VIN onto — the trailing slash matters. The part elements append
 *     `/<part number>` to `endpoint` themselves, so THAT one must not end in a
 *     slash. The quickstart is a vehicle element, so its copy says the former.
 *   • `fetchVin` validates the ISO 3779 check digit, so an invented 17-character
 *     string is rejected.
 *   • A document can register a custom element tag once. Two versions, or the
 *     whole-library bundle plus per-component modules, is a runtime failure.
 *
 * Translations are machine-written and want a native pass before this is shown
 * to anyone outside the team — the English is the source of truth.
 */
(function () {
  const KEY = 'adp-site-language';

  const LANGUAGES = [
    { code: 'en', label: 'English', dir: 'ltr' },
    { code: 'ar', label: 'العربية', dir: 'rtl' },
    { code: 'ku', label: 'کوردی', dir: 'rtl' },
    { code: 'ru', label: 'Русский', dir: 'ltr' },
  ];

  const STRINGS = {
    en: {
      'skip': 'Skip to content',
      'nav.components': 'Components',
      'nav.docs': 'Documentation',
      'nav.architecture': 'Architecture',
      'nav.home': 'Home',
      'nav.menu': 'Menu',
      'nav.close': 'Close',
      'nav.filter': 'Filter pages…',
      'nav.empty': 'No demo pages published yet — the elements ship in the package.',
      'nav.language': 'Language',
      'nav.theme': 'Theme',
      'theme.system': 'System',
      'theme.light': 'Light',
      'theme.dark': 'Dark',

      'hero.eyebrow': 'Shift Framework · Auto Distributor Platform',
      'hero.title': 'Embed ADP lookups and forms in your site',
      'hero.lede':
        'Custom elements that render Auto Distributor Platform vehicle, part and service data on any page — no framework, no build step. Load one module, place the element, give it the base URL your platform operator issued, then call one method.',
      'hero.primary': 'Quickstart',
      'hero.secondary': 'Installation',
      'hero.package': 'Package',
      'hero.version': 'Latest version',
      'hero.registry': 'Release history',

      'quickstart.title': 'Quickstart',
      'quickstart.lede': 'A working embed in three steps. This is the whole integration.',
      'quickstart.steps': [
        { name: 'Load the package', body: 'One module tag. It registers every element and lazy-loads each one’s code the first time that element appears on the page.' },
        { name: 'Place the element', body: 'Give it your base URL and the language the page is in. Keep the trailing slash — the element appends the identifier to it.' },
        { name: 'Ask it a question', body: 'Call the element’s method with a VIN or a part number. It owns the request, the loading state and the error state from there.' },
      ],
      'quickstart.note':
        'Your base URL and the access that goes with it are issued by the platform operator — ask before you start, and allow-list the origins you will embed on. The VIN must be real: fetchVin verifies the ISO check digit and rejects an invented 17-character string.',
      'quickstart.network':
        'Three hosts must be reachable from the browser, and they need two different Content-Security-Policy directives. In connect-src: your base URL, and the CDN, from which the element fetches its own translation files — including when you install from npm. In style-src and font-src: fonts.googleapis.com and fonts.gstatic.com, because the package appends a font stylesheet to your document head on load. Miss the first and the element renders with empty labels and no error; miss the second and it renders in the wrong typeface.',

      'install.title': 'Install',
      'install.lede':
        'Load the whole package from the CDN at @latest. It registers every element but downloads code only for the ones your page actually uses, when it uses them — so the recommended path is also the one you have to think about least.',
      'install.library': 'Whole package',
      'install.single': 'One element',
      'install.recommended': 'Recommended',
      'install.libraryNote':
        'One tag, nothing to build. Every element is registered, but code is fetched per element at runtime, the first time that element appears — a page using one component downloads one component.',
      'install.singleNote': 'The loader registers exactly the elements you name. Worth it only if you embed one element on a page where every kilobyte is argued over.',
      'install.version': 'Version',
      'install.pinned': 'Pinned',
      'install.latest': 'Always latest',
      'install.pinnedNote': 'Freezes behaviour at a version you tested. Choose this when a release has to be signed off before it can reach production.',
      'install.latestNote':
        'Recommended. Always the newest release, so a fix reaches your page without a redeploy. The one rule that still binds: a single document must never end up holding two versions — see below.',
      'install.bundler': 'Using a bundler? npm install adp-web-components — then keep every import in the application on one version.',
      'install.copy': 'Copy',
      'install.copied': 'Copied',
      'install.copyLabel': 'Copy code',

      'versions.title': 'Versions and compatibility',
      'versions.lede': 'Three rules. Breaking any of them fails at runtime, not at build time, and the symptom rarely points at the cause.',
      'versions.items': [
        {
          name: 'One version per document',
          body: 'A browser can register a custom element tag once and can never re-register it. If a page ends up holding two versions of this package, the second silently loses every tag it shares with the first — you get undefined rendered into the page, or a type error thrown while reading a form structure. In a single-page application every route shares one document, so they all move together.',
        },
        {
          name: 'Do not mix loading styles',
          body: 'The whole-package bundle and the per-element modules are two separate runtimes. Loading both in one document has exactly the same effect as loading two versions. Pick one and keep to it.',
        },
        {
          name: 'Pin deliberately',
          body: 'Pinning an exact version freezes behaviour; @latest moves when we publish. Either is supported — what is not supported is different parts of one page disagreeing about which they use.',
        },
      ],
      'versions.note': 'If one page genuinely cannot move to the same version as the rest, isolate it in an iframe. A separate document has its own element registry.',

      'overview.title': 'What the element does, and what you do',
      'overview.lede':
        'Ready-to-embed elements, not a toolkit. Each one is a finished piece of product: a VIN lookup that renders a whole service history, a part search across three sources, a form that raises a real ticket. You place it and hand it a base URL — it does the rest.',
      'overview.elementTitle': 'The element handles',
      'overview.elementItems': [
        'Calling the platform and reading what comes back',
        'Its own loading, empty and error states',
        'Formatting and text direction in four languages',
        'Rendering inside a shadow root, so your CSS stays yours',
        'Fetching its own code the first time it appears on a page',
      ],
      'overview.youTitle': 'You provide',
      'overview.youItems': ['A base URL, and the access issued with it', 'One line of markup where you want it to appear', 'One method call with a VIN or a part number'],
      'overview.body3':
        'That keeps the integration contract small: an element, a base URL, and the identifier you are asking about. The element owns the request, the response shape, the loading and error states, and the language. You own the base URL and the access it needs — and the element never retries, so a failed request stays failed until you ask again.',

      'features.title': 'What you get',
      'features.lede': 'What the element handles, and what it needs from your page.',
      'features.items': [
        {
          name: 'Framework-agnostic',
          body: 'Custom elements, not React or Blazor components — the same element works in plain HTML, React, Angular, Vue and Blazor. Strings and booleans pass as attributes anywhere. Object properties and methods such as fetchVin need a DOM reference, and Blazor needs setBlazorRef before callbacks fire.',
        },
        {
          name: 'Style-isolated',
          body: 'The elements you embed render in a shadow root: your CSS cannot break them, and their CSS stays out of your stylesheet. One exception to plan for — the form elements mount their dropdowns and dialogs on document.body so they can escape a clipping container, and those do inherit your page styles.',
        },
        {
          name: 'Right-to-left included',
          body: 'English, Arabic, Kurdish and Russian. Set the language and the element re-renders, text direction included — the direction is read from the locale, not guessed.',
        },
        {
          name: 'Fixed presentation',
          body: 'The lookup elements ship their own layout and type and reset inherited styles, so a host stylesheet cannot shift them — and cannot restyle them either. The form elements are the configurable ones: they expose named parts and take a theme class.',
        },
        {
          name: 'Build before you have data',
          body: 'Set is-dev on a lookup element and it answers from sample data instead of the network. The container elements ship their own; a standalone element takes yours through setMockData. On the form elements is-dev means something different — it points the submission at the non-production endpoint.',
        },
      ],

      'groups.vehicle-lookup': 'Vehicle lookup',
      'groups.part-lookup': 'Part lookup',
      'groups.forms': 'Ticket forms',
      'blurb.vehicle-lookup': 'VIN-driven lookups — specification, warranty, service history, paint thickness and claimable items.',
      'blurb.part-lookup': 'Part number lookups across distributor, dead stock and manufacturer sources.',
      'blurb.forms': 'Structure-driven forms that raise a ticket — inquiry, service booking, test drive and quotation.',

      'components.title': 'Components',
      'components.lede':
        'Grouped by the part of the platform they read from. Every element listed here ships in the package today — where a group has no demo page yet, it is the demo that is pending, not the code.',
      'components.empty': 'Demo pages are still being published. The elements are already in the package.',
      'components.soon': 'Demo pending',
      'components.page': 'page',
      'components.pages': 'pages',

      'stack.title': 'Where it sits',
      'stack.lede':
        'You call one package. It calls the ADP services over HTTPS, and everything behind that — data, rules, identity — stays the platform’s problem rather than your site’s.',
      'stack.items': [
        { name: 'Shift Framework', body: 'ShiftEntity, ShiftIdentity and TypeAuth — entities, identity and type-safe authorization, shared by every Shift product.' },
        { name: 'ADP services', body: 'Lookup, synchronisation and domain models built on that foundation. They own the data and the rules.' },
        { name: 'adp-web-components', body: 'This package. Custom elements that call those services and render what comes back.' },
        { name: 'Your site', body: 'A module tag and an element.' },
      ],
      'stack.here': 'This package',
      'stack.cta': 'See how it fits together',
      'chart.sources': {
        title: 'Dealer & distributor systems',
        body: 'The systems dealers already run — met where they are, read-only.',
        chips: ['Dealer management system', 'Distributor ERP', 'Manufacturer feeds'],
      },
      'chart.sync': { label: 'sync', note: 'direct database · REST, pushed or pulled · files (CSV, Parquet, JSON)' },
      'chart.adp': { title: 'ADP — Auto Distributor Platform', note: 'One repository · NuGet ShiftSoftware.ADP.*' },
      'chart.pipeline': [
        {
          name: 'Sync Agent',
          chip: 'ADP.SyncAgent',
          facts: ['CSV · EF Core · Cosmos DB · DuckDB'],
          body: 'Pipelines that move data in. A run counts only when every step completed; retries, batch-level errors and logging are built in.',
        },
        {
          name: 'Core platform',
          chip: 'microservices on Azure',
          facts: ['Identity · Vehicles · Parts · Services', 'Tickets · Customers · Menus · Surveys'],
          body: 'SQL per service; Cosmos DB and DuckDB / Parquet as the shared read layer. Rastgo watches freshness and correctness.',
        },
        {
          name: 'Lookup Services',
          chip: 'ADP.Lookup.Services',
          facts: ['Vehicle · Part · Service menu · SSC'],
          body: 'Read-only lookups with the platform’s rules applied — eligibility, warranty, recalls. Serverless HTTPS APIs.',
        },
      ],
      'chart.pipelineArrows': ['ingest', 'serve'],
      'chart.framework': {
        title: 'Built on the Shift Framework',
        note: 'NuGet ShiftSoftware.*',
        body: 'Entities · Identity · Authorization · Dashboards · Mapping — one set of rules for data, users and access, shared by every Shift product.',
      },
      'chart.https': { label: 'HTTPS · JSON', note: 'behind a base URL the platform operator issues' },
      'chart.elements': {
        title: 'adp-web-components',
        chip: 'npm package',
        body: 'Custom elements that call the lookups and render the result — request, states, layout and languages included.',
        families: ['Vehicle lookup', 'Part lookup', 'Ticket forms', 'VIN extractor'],
        note: 'Style-isolated · en ar ku ru · RTL · pinned version, one module per element',
      },
      'chart.embed': { label: 'embed', note: 'one tag, one element, one method call' },
      'chart.hosts': [
        { name: 'Dealer website', body: 'service booking, part search' },
        { name: 'Distributor portal', body: 'warranty desk, dealer network' },
        { name: 'Showroom & service kiosk', body: 'a plate, a screen, a result' },
        { name: 'Any page you already have', body: 'no build step, no framework' },
      ],

      'architecture.eyebrow': 'Architecture',
      'architecture.title': 'How it fits together',
      'architecture.lede':
        'One picture of the whole chain: the systems the data comes from, the platform that owns it, the services that serve it, and the package that puts it on your page. Read it top to bottom — the data flows the same way.',
      'architecture.layers': 'Layer by layer',
      'architecture.layersLede': 'The same chain as four layers, foundation first. You only ever touch the last two.',
      'architecture.more': 'Go deeper',
      'architecture.moreLede': 'The platform and the framework document themselves; these are the pages the elements lean on.',
      'architecture.links': [
        { name: 'ADP documentation', body: 'The platform behind the lookups — vehicles, parts, services, warranty, the Sync Agent and Rastgo.' },
        { name: 'Shift Framework', body: 'The foundation ADP stands on: entities, identity and type-safe authorization.' },
        { name: 'Vehicle lookup result', body: 'The response shape every vehicle element renders, field by field.' },
        { name: 'Part lookup result', body: 'The response shape the part elements render.' },
      ],
      'support.title': 'Next steps',
      'support.ecosystem': 'More from ShiftSoftware',
      'support.items': [
        { name: 'Documentation', body: 'Component reference, integration guides and the form structure schema.' },
        { name: 'Release history', body: 'Published versions and what shipped in each, on the registry.' },
        {
          name: 'Report a problem',
          body: 'Send the package version, the element tag, the browser, and what the network tab shows for the failing request. Version and origin are what make a report reproducible.',
        },
      ],
      'support.links': ['Open the documentation', 'View releases', null],

      'footer.tagline': 'Part of the Auto Distributor Platform by ShiftSoftware, built on the Shift Framework.',
      'footer.built': 'Built from',

      'notFound.title': 'This page is not published',
      'notFound.body': 'The page you asked for is not part of this site yet. It may still be in development, or it may have moved.',
      'notFound.home': 'Back to the landing page',
    },

    ar: {
      'skip': 'تخطّي إلى المحتوى',
      'nav.components': 'المكوّنات',
      'nav.docs': 'التوثيق',
      'nav.architecture': 'البنية',
      'nav.home': 'الرئيسية',
      'nav.menu': 'القائمة',
      'nav.close': 'إغلاق',
      'nav.filter': 'تصفية الصفحات…',
      'nav.empty': 'لم تُنشر صفحات تجريبية بعد — والعناصر نفسها موجودة في الحزمة.',
      'nav.language': 'اللغة',
      'nav.theme': 'المظهر',
      'theme.system': 'النظام',
      'theme.light': 'فاتح',
      'theme.dark': 'داكن',

      'hero.eyebrow': 'Shift Framework · Auto Distributor Platform',
      'hero.title': 'ضَع استعلامات ADP ونماذجها في موقعك',
      'hero.lede':
        'عناصر مخصّصة تعرض بيانات المركبات وقطع الغيار والخدمة في Auto Distributor Platform على أي صفحة — بلا إطار عمل وبلا خطوة بناء. حمّل وحدة واحدة، وضَع العنصر، وأعطه العنوان الأساسي الصادر لك من مشغّل المنصّة، ثم استدعِ دالة واحدة.',
      'hero.primary': 'البداية السريعة',
      'hero.secondary': 'التثبيت',
      'hero.package': 'الحزمة',
      'hero.version': 'أحدث إصدار',
      'hero.registry': 'سجلّ الإصدارات',

      'quickstart.title': 'البداية السريعة',
      'quickstart.lede': 'تضمين عامل في ثلاث خطوات. هذا هو التكامل كلّه.',
      'quickstart.steps': [
        { name: 'حمّل الحزمة', body: 'وسم وحدة واحد. يسجّل كل العناصر ويحمّل شيفرة كل عنصر عند أول ظهور له في الصفحة.' },
        { name: 'ضَع العنصر', body: 'أعطه عنوانك الأساسي ولغة الصفحة. وأبقِ الشرطة المائلة في آخره — فالعنصر يُلحق المعرّف به.' },
        { name: 'اطرح السؤال', body: 'استدعِ دالة العنصر بـ VIN أو رقم القطعة. ومن تلك اللحظة يتولّى الطلب وحالة التحميل وحالة الخطأ.' },
      ],
      'quickstart.note':
        'يصدر مشغّل المنصّة عنوانك الأساسي والصلاحية المرافقة له — اسأل قبل أن تبدأ، وأدرج النطاقات التي ستضمّن فيها في قائمة السماح. ويجب أن يكون VIN حقيقيًا: تتحقق fetchVin من رقم التدقيق وفق ISO وترفض أي سلسلة مؤلَّفة من 17 حرفًا.',
      'quickstart.network':
        'يجب أن تكون ثلاثة مضيفات قابلة للوصول من المتصفّح، وتحتاج إلى توجيهين مختلفين في سياسة أمان المحتوى. في connect-src: عنوانك الأساسي، و CDN الذي يجلب منه العنصر ملفات الترجمة الخاصة به — حتى عند التثبيت من npm. وفي style-src و font-src: fonts.googleapis.com و fonts.gstatic.com، لأن الحزمة تُلحق ورقة أنماط خطوط برأس مستندك عند التحميل. فإن أغفلت الأول ظهر العنصر بعناوين فارغة وبلا خطأ، وإن أغفلت الثاني ظهر بخط غير صحيح.',

      'install.title': 'التثبيت',
      'install.lede':
        'حمّل الحزمة كاملة من CDN على @latest. تسجّل كل العناصر، لكنها لا تنزّل شيفرة إلا للعناصر التي تستخدمها صفحتك فعلًا، وعند استخدامها — فالطريق الموصى به هو أقلّها حاجة إلى تفكير.',
      'install.library': 'الحزمة كاملة',
      'install.single': 'عنصر واحد',
      'install.recommended': 'موصى به',
      'install.libraryNote':
        'وسم واحد، وبلا بناء. تُسجَّل كل العناصر، لكن الشيفرة تُجلب لكل عنصر وقت التشغيل عند أول ظهور له — فالصفحة التي تستخدم مكوّنًا واحدًا تنزّل مكوّنًا واحدًا.',
      'install.singleNote': 'يسجّل المُحمِّل العناصر التي تسمّيها فقط. ولا يستحق العناء إلا إن ضمّنت عنصرًا واحدًا في صفحة يُحاسَب فيها كل كيلوبايت.',
      'install.version': 'الإصدار',
      'install.pinned': 'مثبّت',
      'install.latest': 'الأحدث دائمًا',
      'install.pinnedNote': 'يجمّد السلوك عند إصدار اختبرته. اختره حين يلزم اعتماد الإصدار قبل وصوله إلى الإنتاج.',
      'install.latestNote':
        'موصى به. أحدث إصدار دائمًا، فيصل الإصلاح إلى صفحتك بلا إعادة نشر. والقاعدة الوحيدة التي تبقى ملزمة: ألّا يحمل المستند الواحد إصدارين أبدًا — انظر أدناه.',
      'install.bundler': 'تستخدم مُجمِّعًا؟ npm install adp-web-components — ثم أبقِ كل عمليات الاستيراد في التطبيق على إصدار واحد.',
      'install.copy': 'نسخ',
      'install.copied': 'تم النسخ',
      'install.copyLabel': 'نسخ الشيفرة',

      'versions.title': 'الإصدارات والتوافق',
      'versions.lede': 'ثلاث قواعد. مخالفة أي منها تفشل وقت التشغيل لا وقت البناء، ونادرًا ما يدلّ العَرَض على السبب.',
      'versions.items': [
        {
          name: 'إصدار واحد لكل مستند',
          body: 'يسجّل المتصفّح وسم العنصر المخصّص مرة واحدة ولا يمكنه إعادة تسجيله أبدًا. فإن حملت الصفحة إصدارين من هذه الحزمة، فقد الثاني بصمت كل وسم يشترك فيه مع الأول — فتظهر undefined في الصفحة، أو يُرمى خطأ نوع أثناء قراءة بنية نموذج. وفي تطبيق أحادي الصفحة تتشارك كل المسارات مستندًا واحدًا، فتنتقل جميعها معًا.',
        },
        {
          name: 'لا تخلط أسلوبَي التحميل',
          body: 'حزمة المكتبة كاملة ووحدات العناصر المفردة زمنا تشغيل منفصلان. وتحميلهما معًا في مستند واحد أثره نفسه أثر تحميل إصدارين. اختر واحدًا والتزم به.',
        },
        {
          name: 'ثبّت عن قصد',
          body: 'تثبيت إصدار محدّد يجمّد السلوك، و@latest يتحرّك عند كل نشر. وكلاهما مدعوم — غير المدعوم هو أن تختلف أجزاء الصفحة الواحدة على أيّهما تستخدم.',
        },
      ],
      'versions.note': 'إن تعذّر فعلًا نقل صفحة واحدة إلى إصدار البقية، فاعزلها في إطار iframe. فللمستند المنفصل سجلّ عناصر خاص به.',

      'overview.title': 'ما يفعله العنصر، وما تفعله أنت',
      'overview.lede':
        'عناصر جاهزة للتضمين، لا مجموعة أدوات. كل عنصر منها منتج مكتمل: استعلام بـ VIN يعرض سجل خدمة كاملًا، وبحث عن قطعة عبر ثلاثة مصادر، ونموذج يُنشئ طلبًا حقيقيًا. أنت تضعه وتعطيه عنوانًا أساسيًا — وهو يتكفّل بالباقي.',
      'overview.elementTitle': 'العنصر يتكفّل بـ',
      'overview.elementItems': [
        'استدعاء المنصّة وقراءة ما يعود منها',
        'حالات التحميل والفراغ والخطأ الخاصة به',
        'التنسيق واتجاه النص في أربع لغات',
        'العرض داخل shadow root، فتبقى أنماطك لك',
        'جلب شيفرته عند أول ظهور له في الصفحة',
      ],
      'overview.youTitle': 'وأنت تقدّم',
      'overview.youItems': ['عنوانًا أساسيًا، والصلاحية الصادرة معه', 'سطرًا واحدًا من الوسوم حيث تريده أن يظهر', 'استدعاءً واحدًا للدالة بـ VIN أو رقم قطعة'],
      'overview.body3':
        'وهذا يُبقي عقد التكامل صغيرًا: عنصر، وعنوان أساسي، والمعرّف الذي تسأل عنه. العنصر يملك الطلب وشكل الاستجابة وحالات التحميل والخطأ واللغة. وأنت تملك العنوان الأساسي والصلاحية التي يحتاجها — والعنصر لا يعيد المحاولة أبدًا، فالطلب الفاشل يبقى فاشلًا حتى تسأل من جديد.',

      'features.title': 'ماذا تحصل عليه',
      'features.lede': 'ما يتكفّل به العنصر، وما يحتاجه من صفحتك.',
      'features.items': [
        {
          name: 'مستقل عن أطر العمل',
          body: 'عناصر مخصّصة، لا مكوّنات React أو Blazor — العنصر نفسه يعمل في HTML الصِّرف و React و Angular و Vue و Blazor. تُمرَّر النصوص والقيم المنطقية كسمات في كل مكان. أما الخصائص الكائنية والدوال مثل fetchVin فتحتاج مرجع DOM، و Blazor يحتاج setBlazorRef قبل أن تعمل ردود النداء.',
        },
        {
          name: 'معزول التنسيق',
          body: 'العناصر التي تضمّنها تُعرض داخل shadow root: لا تستطيع أنماطك كسرها، ولا تتسرّب أنماطها إلى ورقة أنماطك. مع استثناء واحد خطّط له — عناصر النماذج تركّب قوائمها ونوافذها على document.body لتفلت من حاوية قاصّة، وتلك ترث أنماط صفحتك.',
        },
        {
          name: 'يدعم الكتابة من اليمين',
          body: 'الإنجليزية والعربية والكردية والروسية. اضبط اللغة فيُعاد عرض العنصر، مع اتجاه النص — والاتجاه يُقرأ من ملف اللغة لا يُخمَّن.',
        },
        {
          name: 'عرض ثابت',
          body: 'عناصر الاستعلام تحمل تخطيطها وخطوطها وتعيد ضبط الأنماط الموروثة، فلا تستطيع ورقة أنماط مضيفة أن تزحزحها — ولا أن تعيد تنسيقها. أما عناصر النماذج فهي القابلة للضبط: تكشف أجزاءً مسمّاة وتقبل صنف مظهر.',
        },
        {
          name: 'ابنِ قبل أن تحصل على البيانات',
          body: 'اضبط is-dev على عنصر استعلام فيجيب من بيانات نموذجية بدل الشبكة. العناصر الحاوية تحمل بياناتها، والعنصر المفرد يأخذ بياناتك عبر setMockData. وعلى عناصر النماذج يعني is-dev شيئًا آخر — إذ يوجّه الإرسال إلى نقطة غير الإنتاج.',
        },
      ],

      'groups.vehicle-lookup': 'البحث عن المركبات',
      'groups.part-lookup': 'البحث عن قطع الغيار',
      'groups.forms': 'نماذج الطلبات',
      'blurb.vehicle-lookup': 'استعلامات بحسب VIN — المواصفات والضمان وسجل الخدمة وسماكة الطلاء والبنود القابلة للمطالبة.',
      'blurb.part-lookup': 'استعلامات بأرقام القطع عبر مصادر الموزّع والمخزون الراكد والمُصنّع.',
      'blurb.forms': 'نماذج مبنية على بنية معرّفة تُنشئ طلبًا — استفسار وحجز خدمة وقيادة تجريبية وعرض سعر.',

      'components.title': 'المكوّنات',
      'components.lede': 'مجمّعة حسب الجزء الذي تقرأ منه في المنصّة. وكل عنصر مذكور هنا موجود في الحزمة اليوم — فحيث لا توجد صفحة تجريبية بعد، فالمعلَّق هو العرض لا الشيفرة.',
      'components.empty': 'ما تزال الصفحات التجريبية قيد النشر. أما العناصر فهي في الحزمة أصلًا.',
      'components.soon': 'العرض قريبًا',
      'components.page': 'صفحة',
      'components.pages': 'صفحة',

      'stack.title': 'موقعها في المنظومة',
      'stack.lede': 'أنت تستدعي حزمة واحدة. وهي تستدعي خدمات ADP عبر HTTPS، وكل ما خلف ذلك — البيانات والقواعد والهوية — يبقى شأن المنصّة لا شأن موقعك.',
      'stack.items': [
        { name: 'Shift Framework', body: 'ShiftEntity و ShiftIdentity و TypeAuth — الكيانات والهوية والتفويض الآمن نوعيًا، وتشترك فيها جميع منتجات Shift.' },
        { name: 'خدمات ADP', body: 'الاستعلام والمزامنة ونماذج المجال المبنية على ذلك الأساس. وهي التي تملك البيانات والقواعد.' },
        { name: 'adp-web-components', body: 'هذه الحزمة. عناصر مخصّصة تستدعي تلك الخدمات وتعرض ما يعود منها.' },
        { name: 'موقعك', body: 'وسم وحدة وعنصر واحد.' },
      ],
      'stack.here': 'هذه الحزمة',
      'stack.cta': 'شاهد كيف تترابط الأجزاء',
      'chart.sources': {
        title: 'أنظمة الوكلاء والموزّع',
        body: 'الأنظمة التي يعمل بها الوكلاء أصلًا — نصل إليها حيث هي، للقراءة فقط.',
        chips: ['نظام إدارة الوكالة', 'نظام ERP للموزّع', 'بيانات الشركة المصنّعة'],
      },
      'chart.sync': { label: 'مزامنة', note: 'قاعدة بيانات مباشرة · REST دفعًا أو سحبًا · ملفات (CSV، Parquet، JSON)' },
      'chart.adp': { title: 'ADP — منصّة موزّعي السيارات', note: 'مستودع واحد · NuGet ShiftSoftware.ADP.*' },
      'chart.pipeline': [
        {
          name: 'Sync Agent',
          chip: 'ADP.SyncAgent',
          facts: ['CSV · EF Core · Cosmos DB · DuckDB'],
          body: 'خطوط أنابيب تنقل البيانات إلى الداخل. لا يُحتسب التشغيل إلا عند اكتمال كل خطوة؛ إعادة المحاولة وأخطاء الدُّفعات والتسجيل مضمّنة.',
        },
        {
          name: 'المنصّة الأساسية',
          chip: 'خدمات مصغّرة على Azure',
          facts: ['الهوية · المركبات · القطع · الخدمات', 'التذاكر · العملاء · القوائم · الاستبيانات'],
          body: 'قاعدة SQL لكل خدمة؛ Cosmos DB و DuckDB / Parquet طبقةَ قراءة مشتركة. يراقب Rastgo حداثة البيانات وصحّتها.',
        },
        {
          name: 'Lookup Services',
          chip: 'ADP.Lookup.Services',
          facts: ['المركبة · القطعة · قائمة الخدمة · SSC'],
          body: 'استعلامات للقراءة فقط مع تطبيق قواعد المنصّة — الأهلية والضمان والاستدعاءات. واجهات HTTPS بلا خوادم.',
        },
      ],
      'chart.pipelineArrows': ['استيعاب', 'تقديم'],
      'chart.framework': {
        title: 'مبنيّة على Shift Framework',
        note: 'NuGet ShiftSoftware.*',
        body: 'الكيانات · الهوية · التفويض · لوحات التحكّم · التحويل — مجموعة قواعد واحدة للبيانات والمستخدمين والصلاحيات تشترك فيها كل منتجات Shift.',
      },
      'chart.https': { label: 'HTTPS · JSON', note: 'خلف عنوان أساسي يصدره مشغّل المنصّة' },
      'chart.elements': {
        title: 'adp-web-components',
        chip: 'حزمة npm',
        body: 'عناصر مخصّصة تستدعي الاستعلامات وتعرض النتيجة — الطلب والحالات والتخطيط واللغات مضمّنة.',
        families: ['البحث عن مركبة', 'البحث عن قطعة', 'نماذج التذاكر', 'قارئ VIN'],
        note: 'معزولة الأنماط · en ar ku ru · RTL · إصدار مثبّت، وحدة لكل عنصر',
      },
      'chart.embed': { label: 'تضمين', note: 'وسم واحد، عنصر واحد، استدعاء واحد' },
      'chart.hosts': [
        { name: 'موقع الوكيل', body: 'حجز الخدمة، البحث عن القطع' },
        { name: 'بوّابة الموزّع', body: 'مكتب الضمان، شبكة الوكلاء' },
        { name: 'كشك المعرض والصيانة', body: 'لوحة، شاشة، نتيجة' },
        { name: 'أي صفحة لديك أصلًا', body: 'بلا خطوة بناء، بلا إطار عمل' },
      ],

      'architecture.eyebrow': 'البنية',
      'architecture.title': 'كيف تترابط الأجزاء',
      'architecture.lede':
        'صورة واحدة للسلسلة كاملة: الأنظمة التي تأتي منها البيانات، والمنصّة التي تملكها، والخدمات التي تقدّمها، والحزمة التي تضعها على صفحتك. اقرأها من الأعلى إلى الأسفل — فالبيانات تسير في الاتجاه نفسه.',
      'architecture.layers': 'طبقة طبقة',
      'architecture.layersLede': 'السلسلة نفسها في أربع طبقات، بدءًا من الأساس. أنت لا تلمس سوى الطبقتين الأخيرتين.',
      'architecture.more': 'تعمّق أكثر',
      'architecture.moreLede': 'المنصّة وإطار العمل يوثّقان نفسيهما؛ هذه هي الصفحات التي تعتمد عليها العناصر.',
      'architecture.links': [
        { name: 'توثيق ADP', body: 'المنصّة التي خلف الاستعلامات — المركبات وقطع الغيار والخدمات والضمان وSync Agent وRastgo.' },
        { name: 'Shift Framework', body: 'الأساس الذي تقوم عليه ADP: الكيانات والهوية والتفويض الآمن نوعيًا.' },
        { name: 'نتيجة استعلام المركبة', body: 'شكل الاستجابة الذي يعرضه كل عنصر من عناصر المركبات، حقلًا حقلًا.' },
        { name: 'نتيجة استعلام القطعة', body: 'شكل الاستجابة الذي تعرضه عناصر قطع الغيار.' },
      ],
      'support.title': 'الخطوات التالية',
      'support.ecosystem': 'المزيد من ShiftSoftware',
      'support.items': [
        { name: 'التوثيق', body: 'مرجع المكوّنات وأدلة التكامل ومخطّط بنية النماذج.' },
        { name: 'سجلّ الإصدارات', body: 'الإصدارات المنشورة وما صدر في كل منها، على السجلّ.' },
        {
          name: 'الإبلاغ عن مشكلة',
          body: 'أرسل إصدار الحزمة، ووسم العنصر، والمتصفّح، وما يظهره تبويب الشبكة للطلب الفاشل. فالإصدار والنطاق هما ما يجعل البلاغ قابلًا لإعادة الإنتاج.',
        },
      ],
      'support.links': ['افتح التوثيق', 'عرض الإصدارات', null],

      'footer.tagline': 'جزء من Auto Distributor Platform من ShiftSoftware، مبنيّة على Shift Framework.',
      'footer.built': 'مبني من',

      'notFound.title': 'هذه الصفحة غير منشورة',
      'notFound.body': 'الصفحة المطلوبة ليست جزءًا من هذا الموقع بعد. قد تكون قيد التطوير أو تكون قد نُقلت.',
      'notFound.home': 'العودة إلى الصفحة الرئيسية',
    },

    ku: {
      'skip': 'بازدان بۆ ناوەڕۆک',
      'nav.components': 'پێکهاتەکان',
      'nav.docs': 'بەڵگەنامە',
      'nav.architecture': 'پێکهاتە',
      'nav.home': 'سەرەتا',
      'nav.menu': 'پێڕست',
      'nav.close': 'داخستن',
      'nav.filter': 'پاڵاوتنی لاپەڕەکان…',
      'nav.empty': 'هێشتا هیچ لاپەڕەیەکی نموونە بڵاو نەکراوەتەوە — بەڵام توخمەکان لە پاکێجەکەدان.',
      'nav.language': 'زمان',
      'nav.theme': 'ڕووکار',
      'theme.system': 'سیستەم',
      'theme.light': 'ڕووناک',
      'theme.dark': 'تاریک',

      'hero.eyebrow': 'Shift Framework · Auto Distributor Platform',
      'hero.title': 'گەڕان و فۆرمەکانی ADP بخە ناو ماڵپەڕەکەت',
      'hero.lede':
        'توخمی تایبەت کە داتای ئۆتۆمبێل و پارچە و خزمەتگوزاری Auto Distributor Platform لە هەر لاپەڕەیەکدا پیشان دەدەن — بەبێ چوارچێوە، بەبێ هەنگاوی بنیاتنان. یەک مۆدیول باربکە، توخمەکە دابنێ، ئەو ناونیشانە بنەڕەتییەی بەڕێوەبەری پلاتفۆرم پێیداویت بیدەرێ، ئینجا یەک دەستەواژە بانگ بکە.',
      'hero.primary': 'دەستپێکی خێرا',
      'hero.secondary': 'دامەزراندن',
      'hero.package': 'پاکێج',
      'hero.version': 'دوایین وەشان',
      'hero.registry': 'مێژووی وەشانەکان',

      'quickstart.title': 'دەستپێکی خێرا',
      'quickstart.lede': 'یەکخستنێکی کارا بە سێ هەنگاو. ئەمە هەموو یەکخستنەکەیە.',
      'quickstart.steps': [
        { name: 'پاکێجەکە باربکە', body: 'یەک تاگی مۆدیول. هەموو توخمەکان تۆمار دەکات و کۆدی هەر توخمێک لە یەکەم دەرکەوتنیدا لە لاپەڕەکەدا دەهێنێت.' },
        { name: 'توخمەکە دابنێ', body: 'ناونیشانی بنەڕەتی و زمانی لاپەڕەکەی پێبدە. سلاشەکەی کۆتایی بهێڵەوە — چونکە توخمەکە ناسنامەکەی پێوە دەلکێنێت.' },
        { name: 'پرسیارەکە بکە', body: 'میسۆدی توخمەکە بە VIN یان ژمارەی پارچە بانگ بکە. لەوێوە خۆی داواکاری و دۆخی بارکردن و دۆخی هەڵە بەڕێوە دەبات.' },
      ],
      'quickstart.note':
        'ناونیشانی بنەڕەتی و ئەو دەستگەیشتنەی لەگەڵیدایە لەلایەن بەڕێوەبەری پلاتفۆرمەوە دەردەکرێن — پێش دەستپێکردن بپرسە، و ئەو سەرچاوانەی تێیاندا دایدەنێیت لە لیستی ڕێگەپێدراودا دابنێ. VIN دەبێت ڕاستەقینە بێت: fetchVin ژمارەی پشکنینی ISO تاقی دەکاتەوە و ڕیزێکی داتاشراوی ١٧ پیتی ڕەت دەکاتەوە.',
      'quickstart.network':
        'دەبێت سێ خانەخوێ لە وێبگەڕەوە بەردەست بن، و پێویستیان بە دوو ئاراستەی جیاوازی Content-Security-Policy هەیە. لە connect-src: ناونیشانی بنەڕەتیت، و CDN کە توخمەکە فایلە وەرگێڕدراوەکانی خۆی لێوە دەهێنێت — تەنانەت کاتێک لە npm دایدەمەزرێنیت. لە style-src و font-src: fonts.googleapis.com و fonts.gstatic.com، چونکە پاکێجەکە لە کاتی بارکردندا ورقەی شێوازی فۆنت بۆ سەری بەڵگەنامەکەت زیاد دەکات. ئەگەر یەکەمیان لەبیر بکەیت توخمەکە بە ناونیشانی بەتاڵ دەردەکەوێت، و ئەگەر دووەمیان لەبیر بکەیت بە فۆنتێکی هەڵە.',

      'install.title': 'دامەزراندن',
      'install.lede':
        'پاکێجەکە بە تەواوی لە CDN و لەسەر @latest باربکە. هەموو توخمەکان تۆمار دەکات، بەڵام تەنیا کۆدی ئەو توخمانە دادەبەزێنێت کە بەڕاستی لاپەڕەکەت بەکاریان دەهێنێت، لە کاتی بەکارهێنانیاندا — بۆیە ڕێگا پێشنیارکراوەکە هەر ئەوەیە کە کەمترین بیرکردنەوەی دەوێت.',
      'install.library': 'پاکێجی تەواو',
      'install.single': 'یەک توخم',
      'install.recommended': 'پێشنیارکراو',
      'install.libraryNote':
        'یەک تاگ، بەبێ هیچ بنیاتنانێک. هەموو توخمەکان تۆمار دەکرێن، بەڵام کۆد بۆ هەر توخمێک لە کاتی کارکردندا دەهێنرێت، لە یەکەم دەرکەوتنیدا — لاپەڕەیەک کە یەک پێکهاتە بەکاردەهێنێت یەک پێکهاتە دادەبەزێنێت.',
      'install.singleNote':
        'بارکەرەکە تەنیا ئەو توخمانە تۆمار دەکات کە ناویان دەبەیت. تەنیا کاتێک دەیەرزێت کە یەک توخم لە لاپەڕەیەکدا دابنێیت کە هەر کیلۆبایتێکی لەسەر مشتومڕ دەکرێت.',
      'install.version': 'وەشان',
      'install.pinned': 'بەستراو',
      'install.latest': 'هەمیشە نوێترین',
      'install.pinnedNote': 'ڕەفتار لەسەر ئەو وەشانە دەبەستێتەوە کە تاقیت کردووەتەوە. ئەمە هەڵبژێرە کاتێک پێویستە بڵاوکردنەوەیەک پەسەند بکرێت پێش ئەوەی بگاتە بەرهەمهێنان.',
      'install.latestNote':
        'پێشنیارکراوە. هەمیشە نوێترین بڵاوکردنەوە، بۆیە چاککردنێک بەبێ بڵاوکردنەوەی دووبارە دەگاتە لاپەڕەکەت. ئەو یاسایەی دەمێنێتەوە: نابێت یەک بەڵگەنامە هەرگیز دوو وەشانی تێدا بێت — لە خوارەوە بیبینە.',
      'install.bundler': 'بەستەرێک بەکاردەهێنیت؟ npm install adp-web-components — ئینجا هەموو هێنانەوەکانی ئەپەکە لەسەر یەک وەشان بهێڵەوە.',
      'install.copy': 'لەبەرگرتنەوە',
      'install.copied': 'لەبەرگیرایەوە',
      'install.copyLabel': 'کۆدەکە لەبەربگرەوە',

      'versions.title': 'وەشان و گونجاندن',
      'versions.lede': 'سێ یاسا. شکاندنی هەر یەکێکیان لە کاتی کارکردندا شکست دەهێنێت نەک لە کاتی بنیاتناندا، و بەزۆری نیشانەکە ئاماژە بە هۆکارەکە ناکات.',
      'versions.items': [
        {
          name: 'یەک وەشان بۆ هەر بەڵگەنامەیەک',
          body: 'وێبگەڕ تاگی توخمی تایبەت تەنیا جارێک تۆمار دەکات و هەرگیز ناتوانێت دووبارە تۆماری بکات. ئەگەر لاپەڕەیەک دوو وەشانی ئەم پاکێجەی تێدا بێت، وەشانی دووەم بێدەنگ هەموو ئەو تاگانە لەدەست دەدات کە لەگەڵ یەکەمدا هاوبەشن — ئەنجامەکەی undefined لە لاپەڕەکەدا دەردەکەوێت، یان هەڵەی جۆر لە کاتی خوێندنەوەی پێکهاتەی فۆرمێکدا دەدرێت. لە ئەپێکی تاک-لاپەڕەدا هەموو ڕێڕەوەکان یەک بەڵگەنامە هاوبەش دەکەن، بۆیە هەموویان پێکەوە دەجوڵێن.',
        },
        {
          name: 'شێوازی بارکردن تێکەڵ مەکە',
          body: 'پاکێجی تەواو و مۆدیولی توخمی تاک دوو کاتی کارکردنی جیاوازن. بارکردنی هەردووکیان لە یەک بەڵگەنامەدا هەمان کاریگەری بارکردنی دوو وەشانی هەیە. یەکێکیان هەڵبژێرە و لەسەری بمێنەوە.',
        },
        {
          name: 'بە ئەنقەست ببەستە',
          body: 'بەستنی وەشانێکی دیاریکراو ڕەفتار دەبەستێتەوە، و @latest لە هەر بڵاوکردنەوەیەکدا دەجوڵێت. هەردووکیان پشتگیری دەکرێن — ئەوەی پشتگیری ناکرێت ئەوەیە کە بەشەکانی یەک لاپەڕە لەسەر ئەوەی کامیان بەکاردەهێنن ناکۆک بن.',
        },
      ],
      'versions.note': 'ئەگەر بەڕاستی لاپەڕەیەک ناتوانێت بچێتە سەر هەمان وەشانی ئەوانی تر، لە iframe جیای بکەرەوە. بەڵگەنامەی جیاواز تۆمارگەی توخمی خۆی هەیە.',

      'overview.title': 'توخمەکە چی دەکات، و تۆ چی دەکەیت',
      'overview.lede':
        'توخمی ئامادە بۆ دانان، نەک کۆمەڵە ئامرازێک. هەر یەکێکیان بەرهەمێکی تەواوە: گەڕانێک بە VIN کە مێژووی خزمەتگوزاری تەواو پیشان دەدات، گەڕانی پارچە بەناو سێ سەرچاوەدا، و فۆرمێک کە داواکارییەکی ڕاستەقینە دروست دەکات. تۆ دایدەنێیت و ناونیشانێکی بنەڕەتی پێدەدەیت — ئەویش ئەوی تر دەکات.',
      'overview.elementTitle': 'توخمەکە ئەمانە بەڕێوە دەبات',
      'overview.elementItems': [
        'بانگکردنی پلاتفۆرم و خوێندنەوەی ئەوەی دەگەڕێتەوە',
        'دۆخەکانی بارکردن و بەتاڵی و هەڵەی خۆی',
        'ڕێکخستن و ئاراستەی نووسین بە چوار زمان',
        'پیشاندان لە ناو shadow root، بۆیە CSS-ی تۆ هی خۆت دەمێنێتەوە',
        'هێنانی کۆدی خۆی لە یەکەم دەرکەوتنیدا لە لاپەڕەکەدا',
      ],
      'overview.youTitle': 'تۆ ئەمانە دابین دەکەیت',
      'overview.youItems': [
        'ناونیشانێکی بنەڕەتی، و ئەو دەستگەیشتنەی لەگەڵیدا دەردەکرێت',
        'یەک دێڕ کۆد لەو شوێنەی دەتەوێت دەربکەوێت',
        'یەک بانگکردنی میسۆد بە VIN یان ژمارەی پارچە',
      ],
      'overview.body3':
        'ئەمە گرێبەستی یەکخستن بچووک ڕادەگرێت: توخمێک، ناونیشانێکی بنەڕەتی، و ئەو ناسنامەیەی پرسیاری لێدەکەیت. توخمەکە خاوەنی داواکاری و شێوەی وەڵام و دۆخی بارکردن و هەڵە و زمانە. تۆ خاوەنی ناونیشانی بنەڕەتی و ئەو دەستگەیشتنەیت کە پێویستی پێیەتی — و توخمەکە هەرگیز دووبارە هەوڵ نادات، بۆیە داواکارییەکی شکستخواردوو شکستخواردوو دەمێنێتەوە هەتا دووبارە داوا دەکەیت.',

      'features.title': 'چی وەردەگریت',
      'features.lede': 'توخمەکە چی بەڕێوە دەبات، و چی لە لاپەڕەکەت دەخوازێت.',
      'features.items': [
        {
          name: 'سەربەخۆ لە چوارچێوە',
          body: 'توخمی تایبەت، نەک پێکهاتەی React یان Blazor — هەمان توخم لە HTML-ی ساکار و React و Angular و Vue و Blazor کاردەکات. ڕشتە و بەهای بوولی لە هەموو شوێنێک وەک تایبەتمەندی دەگوازرێنەوە. بەڵام خاسیەتە تەنەکان و میسۆدەکان وەک fetchVin پێویستیان بە ئاماژەی DOM هەیە، و Blazor پێویستی بە setBlazorRef هەیە پێش ئەوەی بانگەوازەکان کار بکەن.',
        },
        {
          name: 'شێواز-جیاکراوە',
          body: 'ئەو توخمانەی دایاندەنێیت لە ناو shadow rootدا پیشان دەدرێن: CSS-ی تۆ ناتوانێت بیانشکێنێت، و CSS-ی ئەوانیش لە ورقەی شێوازەکەت دەرەوە دەمێنێتەوە. یەک ئیستیسنا پلانی بۆ دابنێ — توخمەکانی فۆرم لیستە و دیالۆگەکانیان لەسەر document.body دادەنێن تا لە هەڵگرێکی بڕاو دەرباز بن، و ئەوانە شێوازەکانی لاپەڕەکەت وەردەگرن.',
        },
        {
          name: 'ڕاست-بۆ-چەپیش تێدایە',
          body: 'ئینگلیزی و عەرەبی و کوردی و ڕووسی. زمانەکە دیاری بکە، توخمەکە دووبارە پیشان دەدرێت، لەگەڵ ئاراستەی نووسین — ئاراستەکە لە فایلی زمانەوە دەخوێنرێتەوە، نە خەمڵێنراوە.',
        },
        {
          name: 'پیشاندانی جێگیر',
          body: 'توخمەکانی گەڕان تەرح و فۆنتی خۆیان هەڵدەگرن و شێوازە وەرگیراوەکان ڕێک دەخەنەوە، بۆیە ورقەی شێوازی خانەخوێ ناتوانێت بیانجوڵێنێت — و ناشتوانێت دووبارە ڕایانبخات. توخمەکانی فۆرم ئەوانەن کە دەکرێن ڕێک بخرێن: بەشی ناودار دەردەخەن و پۆلی ڕووکار وەردەگرن.',
        },
        {
          name: 'بنیاتنان پێش هەبوونی داتا',
          body: 'is-dev لەسەر توخمێکی گەڕان دابنێ، ئەویش لە جیاتی تۆڕ لە داتای نموونەوە وەڵام دەداتەوە. توخمە هەڵگرەکان داتای خۆیان هەڵدەگرن؛ توخمی تاک داتای تۆ لە ڕێگەی setMockData وەردەگرێت. لەسەر توخمەکانی فۆرم is-dev واتایەکی جیاوازی هەیە — ناردنەکە ئاراستەی خاڵێکی نا-بەرهەمهێنان دەکات.',
        },
      ],

      'groups.vehicle-lookup': 'گەڕانی ئۆتۆمبێل',
      'groups.part-lookup': 'گەڕانی پارچە',
      'groups.forms': 'فۆرمی داواکاری',
      'blurb.vehicle-lookup': 'گەڕان بەپێی VIN — تایبەتمەندی، گەرەنتی، مێژووی خزمەتگوزاری، ئەستووری بۆیە و بڕگە داواکراوەکان.',
      'blurb.part-lookup': 'گەڕانی ژمارەی پارچە بەناو سەرچاوەکانی دابەشکەر و کۆگای وەستاو و بەرهەمهێنەردا.',
      'blurb.forms': 'فۆرمی پێکهاتەیی کە داواکاری دروست دەکەن — پرسیار، حجزی خزمەتگوزاری، لێخوڕینی تاقیکردنەوە و نرخاندن.',

      'components.title': 'پێکهاتەکان',
      'components.lede':
        'بەپێی ئەو بەشەی پلاتفۆرم کە لێی دەخوێننەوە کۆکراونەتەوە. هەر توخمێکی لێرە ناوبراو ئەمڕۆ لە پاکێجەکەدایە — لەو شوێنانەی هێشتا لاپەڕەی نموونە نییە، ئەوەی ماوە پیشاندانەکەیە نەک کۆدەکە.',
      'components.empty': 'لاپەڕە نموونەییەکان هێشتا لە بڵاوکردنەوەدان. بەڵام توخمەکان پێشتر لە پاکێجەکەدان.',
      'components.soon': 'نموونە بەم زووانە',
      'components.page': 'لاپەڕە',
      'components.pages': 'لاپەڕە',

      'stack.title': 'شوێنی لە زنجیرەکەدا',
      'stack.lede':
        'تۆ یەک پاکێج بانگ دەکەیت. ئەویش خزمەتگوزارییەکانی ADP بە HTTPS بانگ دەکات، و هەرچی لە پشتییەوەیە — داتا، یاسا، ناسنامە — کێشەی پلاتفۆرم دەمێنێتەوە نەک هی ماڵپەڕەکەت.',
      'stack.items': [
        { name: 'Shift Framework', body: 'ShiftEntity و ShiftIdentity و TypeAuth — ئێنتیتی و ناسنامە و مۆڵەتپێدانی جۆر-پارێزراو، هاوبەش لەنێوان هەموو بەرهەمەکانی Shift.' },
        { name: 'خزمەتگوزارییەکانی ADP', body: 'گەڕان و هاوکاتکردن و مۆدێلەکانی بواری، لەسەر ئەو بنەمایە بنیات نراون. ئەوان خاوەنی داتا و یاساکانن.' },
        { name: 'adp-web-components', body: 'ئەم پاکێجە. توخمی تایبەت کە ئەو خزمەتگوزاریانە بانگ دەکەن و ئەنجامەکە پیشان دەدەن.' },
        { name: 'ماڵپەڕەکەت', body: 'تاگێکی مۆدیول و توخمێک.' },
      ],
      'stack.here': 'ئەم پاکێجە',
      'stack.cta': 'ببینە چۆن پێکەوە دەگونجێن',
      'chart.sources': {
        title: 'سیستەمەکانی دیلەر و دابەشکەر',
        body: 'ئەو سیستەمانەی دیلەرەکان پێشتر بەکاریان دەهێنن — لە شوێنی خۆیان دەگەینە پێیان، تەنها بۆ خوێندنەوە.',
        chips: ['سیستەمی بەڕێوەبردنی دیلەر', 'ERPی دابەشکەر', 'داتای بەرهەمهێنەر'],
      },
      'chart.sync': { label: 'هاوکاتکردن', note: 'بنکەدراوەی ڕاستەوخۆ · REST بە پاڵنان یان ڕاکێشان · فایل (CSV، Parquet، JSON)' },
      'chart.adp': { title: 'ADP — پلاتفۆرمی دابەشکەری ئۆتۆمبێل', note: 'یەک کۆگا · NuGet ShiftSoftware.ADP.*' },
      'chart.pipeline': [
        {
          name: 'Sync Agent',
          chip: 'ADP.SyncAgent',
          facts: ['CSV · EF Core · Cosmos DB · DuckDB'],
          body: 'بۆرییەکان کە داتا دەهێننە ناوەوە. جێبەجێکردنێک تەنها کاتێک دەژمێردرێت کە هەموو هەنگاوەکان تەواو بووبن؛ دووبارەکردنەوە، هەڵەی بەستە و تۆمارکردن لەناویدان.',
        },
        {
          name: 'پلاتفۆرمی ناوەکی',
          chip: 'مایکرۆسێرڤیس لەسەر Azure',
          facts: ['ناسنامە · ئۆتۆمبێل · پارچە · خزمەتگوزاری', 'تکت · کڕیار · مێنیو · ڕاپرسی'],
          body: 'SQL بۆ هەر خزمەتگوزارییەک؛ Cosmos DB و DuckDB / Parquet وەک چینی خوێندنەوەی هاوبەش. Rastgo چاودێری تازەیی و ڕاستیی داتا دەکات.',
        },
        {
          name: 'Lookup Services',
          chip: 'ADP.Lookup.Services',
          facts: ['ئۆتۆمبێل · پارچە · مێنیوی خزمەتگوزاری · SSC'],
          body: 'گەڕانی تەنها-خوێندنەوە بە جێبەجێکردنی یاساکانی پلاتفۆرم — شایستەیی، گەرەنتی، بانگکردنەوە. APIی HTTPSی بێ‌سێرڤەر.',
        },
      ],
      'chart.pipelineArrows': ['وەرگرتن', 'پێشکەشکردن'],
      'chart.framework': {
        title: 'دروستکراو لەسەر Shift Framework',
        note: 'NuGet ShiftSoftware.*',
        body: 'ئێنتیتی · ناسنامە · دەسەڵاتدان · داشبۆرد · ماپینگ — یەک کۆمەڵە یاسا بۆ داتا و بەکارهێنەر و دەستپێگەیشتن، هاوبەش لەنێوان هەموو بەرهەمەکانی Shift.',
      },
      'chart.https': { label: 'HTTPS · JSON', note: 'لە پشت ناونیشانێکی بنەڕەتی کە بەڕێوەبەری پلاتفۆرم دەیدات' },
      'chart.elements': {
        title: 'adp-web-components',
        chip: 'پاکێجی npm',
        body: 'ئێلێمێنتی تایبەت کە گەڕانەکان بانگ دەکەن و ئەنجامەکە پیشان دەدەن — داواکاری، دۆخەکان، ڕێکخستن و زمانەکان لەناویدان.',
        families: ['گەڕانی ئۆتۆمبێل', 'گەڕانی پارچە', 'فۆڕمی تکت', 'دەرهێنەری VIN'],
        note: 'جیاکراوەی ستایل · en ar ku ru · RTL · وەشانی چەسپاو، یەک مۆدیول بۆ هەر ئێلێمێنتێک',
      },
      'chart.embed': { label: 'خستنەناو', note: 'یەک تاگ، یەک ئێلێمێنت، یەک بانگکردن' },
      'chart.hosts': [
        { name: 'ماڵپەڕی دیلەر', body: 'حیجزی خزمەتگوزاری، گەڕانی پارچە' },
        { name: 'پۆرتاڵی دابەشکەر', body: 'مێزی گەرەنتی، تۆڕی دیلەرەکان' },
        { name: 'کیۆسکی پیشانگا و خزمەتگوزاری', body: 'پلێتێک، شاشەیەک، ئەنجامێک' },
        { name: 'هەر پەڕەیەک کە پێشتر هەتە', body: 'بەبێ هەنگاوی بیلد، بەبێ فرەیموۆرک' },
      ],

      'architecture.eyebrow': 'پێکهاتە',
      'architecture.title': 'چۆن پێکەوە دەگونجێن',
      'architecture.lede':
        'یەک وێنە بۆ هەموو زنجیرەکە: ئەو سیستەمانەی داتا لێیانەوە دێت، ئەو پلاتفۆرمەی خاوەنیەتی، ئەو خزمەتگوزاریانەی پێشکەشی دەکەن، و ئەو پاکێجەی دەیخاتە سەر لاپەڕەکەت. لە سەرەوە بۆ خوارەوە بیخوێنەرەوە — داتا هەمان ڕێگا دەگرێت.',
      'architecture.layers': 'چین بە چین',
      'architecture.layersLede': 'هەمان زنجیرە وەک چوار چین، بنەما لە پێشەوە. تۆ تەنها دەست لە دوو چینی کۆتایی دەدەیت.',
      'architecture.more': 'قووڵتر بڕۆ',
      'architecture.moreLede': 'پلاتفۆرم و چوارچێوەکە خۆیان بەڵگەنامەی خۆیان دەنووسن؛ ئەمانە ئەو لاپەڕانەن کە توخمەکان پشتیان پێ دەبەستن.',
      'architecture.links': [
        { name: 'بەڵگەنامەی ADP', body: 'ئەو پلاتفۆرمەی لە پشت گەڕانەکانەوەیە — ئۆتۆمبێل، پارچە، خزمەتگوزاری، گەرەنتی، Sync Agent و Rastgo.' },
        { name: 'Shift Framework', body: 'ئەو بنەمایەی ADP لەسەری وەستاوە: ئێنتیتی، ناسنامە و مۆڵەتپێدانی جۆر-پارێزراو.' },
        { name: 'ئەنجامی گەڕانی ئۆتۆمبێل', body: 'شێوەی وەڵامەکە کە هەموو توخمێکی ئۆتۆمبێل پیشانی دەدات، خانە بە خانە.' },
        { name: 'ئەنجامی گەڕانی پارچە', body: 'شێوەی وەڵامەکە کە توخمەکانی پارچە پیشانی دەدەن.' },
      ],
      'support.title': 'هەنگاوەکانی داهاتوو',
      'support.ecosystem': 'زیاتر لە ShiftSoftware',
      'support.items': [
        { name: 'بەڵگەنامە', body: 'سەرچاوەی پێکهاتەکان و ڕێنمایی یەکخستن و پێکهاتەی فۆرمەکان.' },
        { name: 'مێژووی وەشانەکان', body: 'وەشانە بڵاوکراوەکان و ئەوەی لە هەر یەکێکیاندا هاتووە، لەسەر تۆمارگەکە.' },
        {
          name: 'ڕاپۆرتی کێشە',
          body: 'وەشانی پاکێج، تاگی توخم، وێبگەڕ، و ئەوەی تابی تۆڕ بۆ داواکارییە شکستخواردووەکە پیشانی دەدات بنێرە. وەشان و سەرچاوە ئەوانەن کە ڕاپۆرتێک دووبارە بەرهەمهێنراو دەکەن.',
        },
      ],
      'support.links': ['بەڵگەنامەکە بکەرەوە', 'وەشانەکان ببینە', null],

      'footer.tagline': 'بەشێکە لە Auto Distributor Platform لەلایەن ShiftSoftware، بنیات نراوە لەسەر Shift Framework.',
      'footer.built': 'بنیات نراوە لە',

      'notFound.title': 'ئەم لاپەڕەیە بڵاو نەکراوەتەوە',
      'notFound.body': 'ئەو لاپەڕەیەی داوات کرد هێشتا بەشێک نییە لەم ماڵپەڕە. لەوانەیە هێشتا لە گەشەپێداندا بێت، یان گواستراوەتەوە.',
      'notFound.home': 'گەڕانەوە بۆ لاپەڕەی سەرەکی',
    },

    ru: {
      'skip': 'Перейти к содержимому',
      'nav.components': 'Компоненты',
      'nav.docs': 'Документация',
      'nav.architecture': 'Архитектура',
      'nav.home': 'Главная',
      'nav.menu': 'Меню',
      'nav.close': 'Закрыть',
      'nav.filter': 'Фильтр страниц…',
      'nav.empty': 'Демо-страницы ещё не опубликованы — сами элементы есть в пакете.',
      'nav.language': 'Язык',
      'nav.theme': 'Тема',
      'theme.system': 'Системная',
      'theme.light': 'Светлая',
      'theme.dark': 'Тёмная',

      'hero.eyebrow': 'Shift Framework · Auto Distributor Platform',
      'hero.title': 'Встройте запросы и формы ADP в свой сайт',
      'hero.lede':
        'Пользовательские элементы, которые выводят данные Auto Distributor Platform по автомобилям, запчастям и сервису на любой странице — без фреймворка и без сборки. Подключите один модуль, поставьте элемент, задайте базовый URL, выданный оператором платформы, и вызовите один метод.',
      'hero.primary': 'Быстрый старт',
      'hero.secondary': 'Установка',
      'hero.package': 'Пакет',
      'hero.version': 'Последняя версия',
      'hero.registry': 'История релизов',

      'quickstart.title': 'Быстрый старт',
      'quickstart.lede': 'Рабочая вставка за три шага. Это и есть вся интеграция.',
      'quickstart.steps': [
        { name: 'Подключите пакет', body: 'Один тег модуля. Он регистрирует все элементы и подгружает код каждого при первом его появлении на странице.' },
        { name: 'Разместите элемент', body: 'Задайте базовый URL и язык страницы. Оставьте завершающий слеш — элемент дописывает к адресу идентификатор.' },
        { name: 'Задайте вопрос', body: 'Вызовите метод элемента с VIN или номером детали. Дальше он сам ведёт запрос, состояние загрузки и состояние ошибки.' },
      ],
      'quickstart.note':
        'Базовый URL и выданный вместе с ним доступ предоставляет оператор платформы — спросите до начала работы и добавьте в список разрешённых те домены, где будете встраивать. VIN должен быть настоящим: fetchVin проверяет контрольную цифру по ISO и отклоняет выдуманную строку из 17 символов.',
      'quickstart.network':
        'Из браузера должны быть доступны три хоста, и им нужны две разные директивы Content-Security-Policy. В connect-src: ваш базовый URL и CDN, откуда элемент берёт собственные файлы переводов, — в том числе при установке из npm. В style-src и font-src: fonts.googleapis.com и fonts.gstatic.com, потому что при загрузке пакет добавляет таблицу стилей шрифтов в head вашего документа. Пропустите первое — элемент отрисуется с пустыми подписями и без ошибки; пропустите второе — не тем шрифтом.',

      'install.title': 'Установка',
      'install.lede':
        'Подключите пакет целиком с CDN на @latest. Он регистрирует все элементы, но код скачивает только для тех, что действительно есть на странице, и в момент их появления — поэтому рекомендуемый путь и есть тот, о котором меньше всего нужно думать.',
      'install.library': 'Пакет целиком',
      'install.single': 'Один элемент',
      'install.recommended': 'Рекомендуется',
      'install.libraryNote':
        'Один тег, ничего собирать не нужно. Регистрируются все элементы, но код подгружается поэлементно во время работы, при первом появлении, — страница с одним компонентом скачивает один компонент.',
      'install.singleNote':
        'Загрузчик регистрирует ровно те элементы, которые вы назвали. Оправданно, только если вы встраиваете один элемент на страницу, где считают каждый килобайт.',
      'install.version': 'Версия',
      'install.pinned': 'Зафиксирована',
      'install.latest': 'Всегда последняя',
      'install.pinnedNote': 'Фиксирует поведение на версии, которую вы протестировали. Выбирайте, когда релиз должен быть согласован до выхода в продакшн.',
      'install.latestNote':
        'Рекомендуется. Всегда самый свежий релиз, поэтому исправление доходит до страницы без передеплоя. Единственное правило, которое остаётся: в одном документе никогда не должно оказаться двух версий — см. ниже.',
      'install.bundler': 'Используете сборщик? npm install adp-web-components — и держите все импорты в приложении на одной версии.',
      'install.copy': 'Копировать',
      'install.copied': 'Скопировано',
      'install.copyLabel': 'Скопировать код',

      'versions.title': 'Версии и совместимость',
      'versions.lede': 'Три правила. Нарушение любого из них ломается во время работы, а не сборки, и симптом редко указывает на причину.',
      'versions.items': [
        {
          name: 'Одна версия на документ',
          body: 'Браузер регистрирует тег пользовательского элемента один раз и не может перерегистрировать его никогда. Если на странице окажутся две версии этого пакета, вторая молча потеряет все теги, общие с первой: в разметку попадёт undefined или возникнет ошибка типа при чтении структуры формы. В одностраничном приложении все маршруты делят один документ, поэтому обновляются вместе.',
        },
        {
          name: 'Не смешивайте способы подключения',
          body: 'Пакет целиком и модули отдельных элементов — два разных рантайма. Подключить оба в одном документе — то же самое, что подключить две версии. Выберите один и держитесь его.',
        },
        {
          name: 'Фиксируйте осознанно',
          body: 'Фиксация точной версии замораживает поведение, @latest смещается при каждой публикации. Поддерживаются оба варианта — не поддерживается ситуация, когда части одной страницы расходятся в том, какой из них используют.',
        },
      ],
      'versions.note': 'Если одна страница действительно не может перейти на общую версию, изолируйте её в iframe: у отдельного документа собственный реестр элементов.',

      'overview.title': 'Что делает элемент, а что делаете вы',
      'overview.lede':
        'Готовые к встраиванию элементы, а не набор инструментов. Каждый — законченный продукт: запрос по VIN, отрисовывающий всю историю обслуживания, поиск детали по трём источникам, форма, создающая настоящее обращение. Вы ставите его и передаёте базовый URL — остальное он делает сам.',
      'overview.elementTitle': 'Элемент берёт на себя',
      'overview.elementItems': [
        'Обращение к платформе и разбор ответа',
        'Собственные состояния загрузки, пустоты и ошибки',
        'Форматирование и направление текста на четырёх языках',
        'Отрисовку внутри shadow root, так что ваш CSS остаётся вашим',
        'Загрузку собственного кода при первом появлении на странице',
      ],
      'overview.youTitle': 'Вы предоставляете',
      'overview.youItems': ['Базовый URL и выданный вместе с ним доступ', 'Одну строку разметки там, где он должен появиться', 'Один вызов метода с VIN или номером детали'],
      'overview.body3':
        'Благодаря этому контракт интеграции остаётся небольшим: элемент, базовый URL и идентификатор, о котором вы спрашиваете. Элементу принадлежат запрос, форма ответа, состояния загрузки и ошибки и язык. Вам — базовый URL и нужный ему доступ. Повторных попыток элемент не делает: неудачный запрос останется неудачным, пока вы не спросите снова.',

      'features.title': 'Что вы получаете',
      'features.lede': 'Что элемент берёт на себя и что ему нужно от вашей страницы.',
      'features.items': [
        {
          name: 'Независимость от фреймворка',
          body: 'Пользовательские элементы, а не компоненты React или Blazor — один и тот же элемент работает в чистом HTML, React, Angular, Vue и Blazor. Строки и логические значения везде передаются атрибутами. Объектные свойства и методы вроде fetchVin требуют ссылки на DOM, а Blazor — вызова setBlazorRef до того, как сработают колбэки.',
        },
        {
          name: 'Изоляция стилей',
          body: 'Встраиваемые элементы отрисовываются в shadow root: ваш CSS не может их сломать, а их CSS не попадёт в вашу таблицу стилей. Одно исключение, которое стоит учесть: элементы форм монтируют свои выпадающие списки и диалоги на document.body, чтобы выйти за обрезающий контейнер, и они наследуют стили вашей страницы.',
        },
        {
          name: 'Поддержка справа налево',
          body: 'Английский, арабский, курдский и русский. Задайте язык — элемент перерисуется вместе с направлением текста; направление читается из файла локали, а не угадывается.',
        },
        {
          name: 'Фиксированное оформление',
          body: 'Элементы поиска несут собственную вёрстку и типографику и сбрасывают унаследованные стили, поэтому таблица стилей хоста не может их сдвинуть — но и не может переоформить. Настраиваемые — это элементы форм: они открывают именованные части и принимают класс темы.',
        },
        {
          name: 'Разработка без данных',
          body: 'Поставьте is-dev на элемент поиска, и он ответит из тестовых данных вместо сети. Контейнерные элементы несут свои данные, отдельный элемент принимает ваши через setMockData. У элементов форм is-dev означает другое — он направляет отправку на непродакшн-адрес.',
        },
      ],

      'groups.vehicle-lookup': 'Поиск по автомобилю',
      'groups.part-lookup': 'Поиск запчастей',
      'groups.forms': 'Формы обращений',
      'blurb.vehicle-lookup': 'Запросы по VIN — комплектация, гарантия, история обслуживания, толщина покрытия и позиции к возмещению.',
      'blurb.part-lookup': 'Поиск по номеру детали в источниках дистрибьютора, неликвидов и производителя.',
      'blurb.forms': 'Формы на основе описанной структуры, создающие обращение: запрос, запись на сервис, тест-драйв и расчёт стоимости.',

      'components.title': 'Компоненты',
      'components.lede':
        'Сгруппированы по той части платформы, из которой они читают. Каждый перечисленный элемент уже есть в пакете — там, где у группы пока нет демо-страницы, не хватает именно демо, а не кода.',
      'components.empty': 'Демо-страницы ещё публикуются. Сами элементы уже в пакете.',
      'components.soon': 'Демо готовится',
      'components.page': 'стр.',
      'components.pages': 'стр.',

      'stack.title': 'Место в экосистеме',
      'stack.lede':
        'Вы обращаетесь к одному пакету. Он обращается к сервисам ADP по HTTPS, а всё, что за ними, — данные, правила, идентификация — остаётся заботой платформы, а не вашего сайта.',
      'stack.items': [
        { name: 'Shift Framework', body: 'ShiftEntity, ShiftIdentity и TypeAuth — сущности, идентификация и типобезопасная авторизация, общие для всех продуктов Shift.' },
        { name: 'Сервисы ADP', body: 'Поиск, синхронизация и доменные модели поверх этой основы. Им принадлежат данные и правила.' },
        { name: 'adp-web-components', body: 'Этот пакет. Пользовательские элементы, которые вызывают эти сервисы и отображают ответ.' },
        { name: 'Ваш сайт', body: 'Тег модуля и элемент.' },
      ],
      'stack.here': 'Этот пакет',
      'stack.cta': 'Как всё связано',
      'chart.sources': {
        title: 'Системы дилеров и дистрибьютора',
        body: 'Системы, которыми дилеры уже пользуются, — подключаемся к ним как есть, только на чтение.',
        chips: ['Система управления дилером', 'ERP дистрибьютора', 'Данные производителя'],
      },
      'chart.sync': { label: 'синхронизация', note: 'прямая база данных · REST, push или pull · файлы (CSV, Parquet, JSON)' },
      'chart.adp': { title: 'ADP — платформа автодистрибьютора', note: 'Один репозиторий · NuGet ShiftSoftware.ADP.*' },
      'chart.pipeline': [
        {
          name: 'Sync Agent',
          chip: 'ADP.SyncAgent',
          facts: ['CSV · EF Core · Cosmos DB · DuckDB'],
          body: 'Конвейеры, загружающие данные. Запуск засчитывается только когда выполнены все шаги; повторы, ошибки на уровне пакетов и журналирование встроены.',
        },
        {
          name: 'Ядро платформы',
          chip: 'микросервисы на Azure',
          facts: ['Идентификация · Автомобили · Запчасти · Сервис', 'Обращения · Клиенты · Меню · Опросы'],
          body: 'SQL на каждый сервис; Cosmos DB и DuckDB / Parquet как общий слой чтения. Rastgo следит за свежестью и корректностью данных.',
        },
        {
          name: 'Lookup Services',
          chip: 'ADP.Lookup.Services',
          facts: ['Автомобиль · Запчасть · Сервисное меню · SSC'],
          body: 'Запросы только на чтение с применением правил платформы — право на услугу, гарантия, отзывные кампании. Бессерверные HTTPS API.',
        },
      ],
      'chart.pipelineArrows': ['загрузка', 'выдача'],
      'chart.framework': {
        title: 'Построено на Shift Framework',
        note: 'NuGet ShiftSoftware.*',
        body: 'Сущности · Идентификация · Авторизация · Панели · Маппинг — единый набор правил для данных, пользователей и доступа, общий для всех продуктов Shift.',
      },
      'chart.https': { label: 'HTTPS · JSON', note: 'за базовым URL, который выдаёт оператор платформы' },
      'chart.elements': {
        title: 'adp-web-components',
        chip: 'пакет npm',
        body: 'Пользовательские элементы, которые вызывают запросы и отображают результат — запрос, состояния, вёрстка и языки включены.',
        families: ['Поиск автомобиля', 'Поиск запчасти', 'Формы обращений', 'Считыватель VIN'],
        note: 'Изолированные стили · en ar ku ru · RTL · закреплённая версия, один модуль на элемент',
      },
      'chart.embed': { label: 'встраивание', note: 'один тег, один элемент, один вызов метода' },
      'chart.hosts': [
        { name: 'Сайт дилера', body: 'запись на сервис, поиск запчастей' },
        { name: 'Портал дистрибьютора', body: 'гарантийный отдел, дилерская сеть' },
        { name: 'Киоск в шоуруме и сервисе', body: 'номер, экран, результат' },
        { name: 'Любая ваша страница', body: 'без сборки, без фреймворка' },
      ],

      'architecture.eyebrow': 'Архитектура',
      'architecture.title': 'Как всё связано',
      'architecture.lede':
        'Вся цепочка на одной картинке: системы, из которых приходят данные, платформа, которой они принадлежат, сервисы, которые их отдают, и пакет, который выводит их на вашу страницу. Читайте сверху вниз — данные идут в ту же сторону.',
      'architecture.layers': 'Слой за слоем',
      'architecture.layersLede': 'Та же цепочка в виде четырёх слоёв, начиная с основания. Вы касаетесь только двух последних.',
      'architecture.more': 'Подробнее',
      'architecture.moreLede': 'Платформа и фреймворк документируют себя сами; вот страницы, на которые опираются элементы.',
      'architecture.links': [
        { name: 'Документация ADP', body: 'Платформа за поисками — автомобили, запчасти, сервис, гарантия, Sync Agent и Rastgo.' },
        { name: 'Shift Framework', body: 'Основание, на котором стоит ADP: сущности, идентификация и типобезопасная авторизация.' },
        { name: 'Результат поиска автомобиля', body: 'Форма ответа, которую отображает каждый элемент по автомобилям, поле за полем.' },
        { name: 'Результат поиска запчасти', body: 'Форма ответа, которую отображают элементы по запчастям.' },
      ],
      'support.title': 'Что дальше',
      'support.ecosystem': 'Ещё от ShiftSoftware',
      'support.items': [
        { name: 'Документация', body: 'Справочник компонентов, руководства по интеграции и схема структуры форм.' },
        { name: 'История релизов', body: 'Опубликованные версии и что вошло в каждую — в реестре.' },
        {
          name: 'Сообщить о проблеме',
          body: 'Пришлите версию пакета, тег элемента, браузер и то, что показывает вкладка сети по неудачному запросу. Версия и домен — это то, что делает отчёт воспроизводимым.',
        },
      ],
      'support.links': ['Открыть документацию', 'Смотреть релизы', null],

      'footer.tagline': 'Часть Auto Distributor Platform от ShiftSoftware, построено на Shift Framework.',
      'footer.built': 'Собрано из',

      'notFound.title': 'Эта страница не опубликована',
      'notFound.body': 'Запрошенная страница пока не входит в этот сайт. Возможно, она ещё в разработке или была перемещена.',
      'notFound.home': 'Вернуться на главную',
    },
  };

  const codes = LANGUAGES.map(language => language.code);
  const directionOf = code => LANGUAGES.find(language => language.code === code)?.dir ?? 'ltr';

  function stored() {
    // A private window and blocked site data both throw rather than return null.
    try {
      return localStorage.getItem(KEY);
    } catch {
      return null;
    }
  }

  /*
   * `?lang=` wins over the stored choice: a link is a deliberate act, and the
   * harness pages already put the language there, so following one of those
   * links back to the landing page lands in the same language.
   */
  function initial() {
    const requested = new URLSearchParams(window.location.search).get('lang');

    if (codes.includes(requested)) return requested;
    if (codes.includes(stored())) return stored();

    // The browser's preference, but only among languages actually translated.
    for (const candidate of navigator.languages ?? []) {
      const code = String(candidate).slice(0, 2).toLowerCase();
      if (codes.includes(code)) return code;
    }

    return 'en';
  }

  function apply(code) {
    const language = codes.includes(code) ? code : 'en';

    document.documentElement.lang = language;
    document.documentElement.dir = directionOf(language);

    try {
      localStorage.setItem(KEY, language);
    } catch {
      // Not persisting is survivable; rendering in the wrong direction is not.
    }

    // Kept in the URL so a reload holds and so links copied out carry the choice.
    const url = new URL(window.location.href);

    url.searchParams.set('lang', language);
    window.history.replaceState({}, '', url);

    return language;
  }

  window.siteLocales = {
    languages: LANGUAGES,
    current: initial,
    direction: directionOf,
    apply,

    /**
     * Falls back to English rather than rendering a key. A missing translation
     * should read as untranslated text, not as a broken page.
     */
    t(code, key) {
      const table = STRINGS[code] ?? STRINGS.en;

      return table[key] ?? STRINGS.en[key] ?? key;
    },
  };

  // Before first paint: direction is layout, and correcting it afterwards is a
  // visible jump on every load in Arabic or Kurdish.
  document.documentElement.lang = initial();
  document.documentElement.dir = directionOf(initial());
})();
