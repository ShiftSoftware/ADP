// Reads the integration manifest that build-templates.mjs copies beside this file.
const MANIFEST = new URL('integration-manifest.json', import.meta.url);

let manifest;

const loadManifest = () =>
  (manifest ??= fetch(MANIFEST).then(response => {
    if (!response.ok) throw new Error(`integration-manifest.json answered ${response.status}`);
    return response.json();
  }));

const CSS_ROLES = [
  { id: 'motion', label: 'Motion', test: /settle|ease/ },
  { id: 'type', label: 'Typography', test: /font|weight|line-height|tracking|transform|decoration/ },
  { id: 'size', label: 'Size and spacing', test: /size|gap|padding|inset|shift|width|offset|radius|round|scale/ },
  { id: 'colour', label: 'Colour', test: /accent|ink|surface|line|muted|bg|tone|focus|ring|dot|shadow|border|halo|opacity/ },
  { id: 'other', label: 'Other', test: /./ },
];

// A contract description "Colour · selected day fill" names its role; without one the name decides.
function cssRoles(properties) {
  const roles = new Map();

  for (const property of properties) {
    const [prefix, ...rest] = String(property.description ?? '').split(' · ');
    const label = rest.length ? prefix : CSS_ROLES.find(role => role.test.test(property.name.replace(/^--shift-[a-z]+-/, ''))).label;

    if (!roles.has(label)) roles.set(label, []);
    roles.get(label).push({ ...property, text: rest.length ? rest.join(' · ') : property.description });
  }

  return [...roles].map(([label, list]) => ({ label, properties: list }));
}

const literal = type => String(type).match(/"([^"]+)"/)?.[1];

function propExample(tag, prop) {
  const type = String(prop.type);

  if (!prop.attribute) return `element.${prop.name} = ${/=>/.test(type) ? 'date => false' : '…'};`;
  if (type === 'boolean') return `<${tag} ${prop.attribute}></${tag}>`;
  if (type === 'number') return `<${tag} ${prop.attribute}="1"></${tag}>`;

  const sample = literal(type) ?? (prop.default && prop.default !== "''" ? prop.default.replace(/^'|'$/g, '') : '…');

  return `<${tag} ${prop.attribute}="${sample}"></${tag}>`;
}

function shortType(type) {
  const text = String(type);
  const short = /=>/.test(text) ? 'function' : /^"[^"]+"( \| "[^"]+")+$/.test(text) ? text.replace(/"/g, '') : text.replace(/\{[^}]*\}/g, '{…}');

  return short.length > 32 ? `${short.slice(0, 31)}…` : short;
}

// `options.groups` orders the properties into named groups: [{ label, props: ['value', …] }]. Anything unlisted lands in "Other properties".
window.apiReference = function apiReference(tag, options = {}) {
  return {
    tag,
    loading: true,
    error: '',
    component: null,
    packageVersion: '',
    open: {},
    shut: {},
    filter: '',

    async init() {
      try {
        const data = await loadManifest();

        this.packageVersion = data.packageVersion;
        this.component = data.components.find(component => component.tag === tag) ?? null;
        if (!this.component) this.error = `${tag} is not in the integration manifest.`;
      } catch (error) {
        manifest = undefined;
        this.error = `The API reference could not be loaded: ${error.message}`;
      } finally {
        this.loading = false;
      }

      if (!this.component) return;

      for (const group of this.groups) if (group.collapsed) this.shut[group.id] = true;

      this.follow(true);
      window.addEventListener('hashchange', () => this.follow(false));
    },

    follow(initial) {
      const id = decodeURIComponent(window.location.hash.slice(1));
      const group = id && this.groups.find(item => item.id === id || item.rows.some(row => row.id === id));

      if (!group) return;

      this.shut[group.id] = false;
      if (id !== group.id) this.open[id] = true;
      this.$nextTick(() => setTimeout(() => document.getElementById(id)?.scrollIntoView({ block: initial ? 'start' : 'nearest' }), 50));
    },

    toggle(row) {
      this.open[row.id] = !this.open[row.id];
      if (this.open[row.id]) window.history.replaceState(window.history.state, '', `#${row.id}`);
    },

    toggleGroup(group) {
      this.shut[group.id] = !this.shut[group.id];
    },

    get props() {
      return (this.component?.api.props ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
    },

    get events() {
      return this.component?.api.events ?? [];
    },

    get methods() {
      return this.component?.api.methods ?? [];
    },

    get parts() {
      return this.component?.api.parts ?? [];
    },

    // A base part's description lists the state parts that sit beside it ("States beside it: a, b.").
    get stateParts() {
      const names = new Set(this.parts.map(part => part.name));
      const found = new Set();

      for (const part of this.parts) {
        const list = String(part.description ?? '').match(/(?:state parts?|states)[^:]*:\s*([^.]+)/i)?.[1] ?? '';

        for (const name of list.split(/[\s,]+/)) if (names.has(name)) found.add(name);
      }

      return found;
    },

    get cssProperties() {
      return (this.component?.api.cssProperties ?? []).filter(property => !String(property.default ?? '').includes('<'));
    },

    get sharedProperties() {
      const own = `--shift-${tag.replace(/^shift-/, '')}-`;
      const found = new Set();

      for (const property of this.cssProperties) {
        for (const name of String(property.default ?? '').match(/--shift-[a-z-]+/g) ?? []) {
          if (!name.startsWith(own) && name !== '--shift-x') found.add(name);
        }
      }

      return [...found].sort();
    },

    get notes() {
      return this.component?.runtimeAssets?.notes ?? [];
    },

    get hostProperties() {
      return new Set(this.component?.host?.properties ?? []);
    },

    flags(prop) {
      return [prop.mutable && 'mutable', prop.reflect && 'reflected', !prop.attribute && 'property only'].filter(Boolean).join(' · ');
    },

    propRow(prop) {
      return {
        id: `api-prop-${prop.name}`,
        name: prop.name,
        tags: [
          shortType(prop.type),
          prop.default != null && `default ${prop.default}`,
          prop.attribute && prop.attribute !== prop.name && `attr ${prop.attribute}`,
          prop.reflect && 'reflected',
          prop.mutable && 'mutable',
          !prop.attribute && 'JS only',
        ].filter(Boolean),
        type: prop.type,
        description: prop.description,
        example: propExample(tag, prop),
      };
    },

    get groups() {
      if (!this.component) return [];

      const props = this.props;
      const listed = new Set();
      const groups = (options.groups ?? []).map(group => {
        const rows = group.props.map(name => props.find(prop => prop.name === name)).filter(Boolean);

        rows.forEach(prop => listed.add(prop.name));

        return { id: `api-group-${group.label.toLowerCase().replace(/[^a-z]+/g, '-')}`, label: group.label, rows: rows.map(prop => this.propRow(prop)) };
      });

      const rest = props.filter(prop => !listed.has(prop.name));

      if (rest.length) groups.push({ id: 'api-group-properties', label: options.groups ? 'Other properties' : 'Properties', rows: rest.map(prop => this.propRow(prop)) });

      groups.push({
        id: 'api-group-events',
        label: 'Events',
        rows: this.events.map(event => ({
          id: `api-event-${event.name}`,
          name: event.name,
          tags: [event.type],
          description: event.description,
          example: `element.addEventListener('${event.name}', event => {\n  console.log(event.detail);\n});`,
        })),
      });

      groups.push({
        id: 'api-group-methods',
        label: 'Methods',
        rows: this.methods.map(method => ({
          id: `api-method-${method.name}`,
          name: `${method.name}()`,
          tags: [method.parameters],
          description: method.description,
          example: `await element.${method.name}();`,
        })),
      });

      if (options.parts !== false)
        groups.push({
          id: 'api-group-parts',
          label: 'Parts',
          rows: this.parts.map(part => ({
            id: `api-part-${part.name}`,
            name: part.name,
            tags: ['::part'],
            description: part.description,
            example: `${tag}::part(${part.name}) {\n  /* … */\n}`,
          })),
        });

      const needle = this.filter.trim().toLowerCase();
      const css = this.cssProperties.filter(property => !needle || property.name.includes(needle));

      groups.push({
        id: 'api-group-css',
        label: 'CSS variables',
        collapsed: true,
        filterable: true,
        count: this.cssProperties.length,
        rows: [],
        roles: cssRoles(css).map(role => ({
          label: role.label,
          rows: role.properties.map(property => ({
            id: `api-css-${property.name.slice(2)}`,
            name: property.name,
            tags: [role.label, `default ${shortType(property.default ?? '—')}`],
            description: [property.text, `Default: ${property.default}`].filter(Boolean).join(' · '),
            example: `${tag} {
  ${property.name}: …;
}`,
          })),
        })),
      });

      for (const group of groups) if (group.roles) group.rows = group.roles.flatMap(role => role.rows);

      // `options.idPrefix` keeps the ids apart when one page shows the API of two elements.
      if (options.idPrefix) {
        for (const item of groups.flatMap(group => [group, ...group.rows])) item.id = item.id.replace(/^api-/, `api-${options.idPrefix}`);
      }

      return groups.filter(group => group.rows.length || group.filterable);
    },
  };
};
