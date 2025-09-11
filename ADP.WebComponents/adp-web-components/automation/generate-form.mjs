// scripts/create-form.mjs
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const args = process.argv.slice(2);
if (args.length < 1) {
  console.error('❌ Please provide a tag name. \nExample: \nyarn create:form ${tagName}');
  process.exit(1);
}

const tagName = args[0];
const camelName = tagName.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
const capitalizeName = tagName
  .replace('-', ' ')
  .split(' ')
  .map(namePart => namePart.charAt(0).toUpperCase() + namePart.slice(1))
  .join(' ');
const pascalName = camelName.charAt(0).toUpperCase() + camelName.slice(1);

const formsPath = path.join(__dirname, '../src/components/forms');

const formLibsPath = path.join(formsPath, tagName);

if (!fs.existsSync(formsPath)) fs.mkdirSync(formsPath, { recursive: true });
if (!fs.existsSync(formLibsPath)) fs.mkdirSync(formLibsPath, { recursive: true });

// generate theme
const themesPath = path.join(formLibsPath, 'themes.css');

if (!fs.existsSync(themesPath)) {
  const content = `:host {
  all: initial !important;
  display: block;
}

* {
  font-family: Arial;
}`;
  fs.writeFileSync(themesPath, content);
}
console.log(`📝 Created: ./${tagName}/${path.basename(themesPath)}`);

// generate validations
const validationsPath = path.join(formLibsPath, 'validations.ts');

if (!fs.existsSync(validationsPath)) {
  const content = `import { InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';

import ${camelName}Schema from '~locales/forms/${camelName}/type';

export const phoneValidator = getPhoneValidator();

export const ${camelName}InputsValidation = object({
  name: string()
    .meta({ label: 'Full Name', placeholder: 'Enter a full name' } as FormInputMeta)
    .required('Full name is required.')
    .min(3, 'Must be 3 characters or more.'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required.')
    .test('libphonenumber-validation', 'Please enter a valid phone number', () => phoneValidator.isValid()),
});

export type ${pascalName} = InferType<typeof ${camelName}InputsValidation>;

export type ${pascalName}FormLocale = FormLocale<typeof ${camelName}Schema>;
`;
  fs.writeFileSync(validationsPath, content);
}

console.log(`📝 Created: ./${tagName}/${path.basename(validationsPath)}`);

// generate element-mapper
const elementMapperPath = path.join(formLibsPath, 'element-mapper.tsx');

if (!fs.existsSync(elementMapperPath)) {
  const content = `import { h } from '@stencil/core';

import { FormElementMapper } from '~features/form-hook';

import { ${pascalName}, ${pascalName}FormLocale, phoneValidator } from './validations';

type AdditionalFields = 'submit';

export const ${camelName}Elements: FormElementMapper<${pascalName}, ${pascalName}FormLocale, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => <form-input {...props} />,

  phone: ({ props, isLoading }) => <form-phone-number {...props} isLoading={isLoading} defaultValue={phoneValidator.default} validator={phoneValidator} />,
} as const;

export type ${camelName}ElementNames = keyof typeof ${camelName}Elements;
`;
  fs.writeFileSync(elementMapperPath, content);
}
console.log(`📝 Created: ./${tagName}/${path.basename(elementMapperPath)}`);

// generate form
const formElementPath = path.join(formsPath, `${tagName}.tsx`);

if (!fs.existsSync(formElementPath)) {
  const content = `import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import ${camelName}Schema from '~locales/forms/${camelName}/type';

import { ${camelName}ElementNames, ${camelName}Elements } from './${tagName}/element-mapper';
import { ${pascalName}, ${pascalName}FormLocale, ${camelName}InputsValidation } from './${tagName}/validations';

import { FormHook } from '~features/form-hook/form-hook';
import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';
import getLanguageFromUrl from '~lib/get-language-from-url';
import { fetchJson } from '~lib/fetch-json';

@Component({
  shadow: true,
  tag: '${tagName}-form',
  styleUrl: '${tagName}/themes.css',
})
export class ${pascalName}Form implements FormHookInterface<${pascalName}>, MultiLingual {
  // #region Localization
  @Prop({ mutable: true, reflect: true }) language: LanguageKeys = 'en';

  @State() locale: ${pascalName}FormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...${camelName}Schema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.${camelName}', ${camelName}Schema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.localeLanguage = newLanguage;

    this.form.rerender({ rerenderAll: true });
  }
  // #endregion

  // #region Form Hook logic
  @State() errorMessage: string;
  @State() isLoading: boolean = false;
  @State() localeLanguage: LanguageKeys;

  @Prop() gistId?: string;
  @Prop() structureUrl?: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop({ mutable: true }) structure: FormElementStructure<${camelName}ElementNames> | undefined;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    this.isLoading = isLoading;
    if (this.loadingChanges) this.loadingChanges(true);
  }

  setErrorCallback(error: any) {
    if (error?.message) this.errorMessage = error.message;
    if (this.errorCallback) this.errorCallback(error);
  }

  setSuccessCallback(data: any) {
    if (this.successCallback) this.successCallback(data);
  }

  async componentWillLoad() {
    this.language = getLanguageFromUrl();

    if (this.structure) {
      await this.changeLanguage(this.language);
    } else {
      if (this.gistId) {
        const [newGistStructure] = await Promise.all([gistLoader(this.gistId), this.changeLanguage(this.language)]);
        this.structure = newGistStructure as FormElementStructure<${camelName}ElementNames>;
      } else if (this.structureUrl) {
        await this.changeLanguage(this.language);
        const [newGistStructure] = await Promise.all([fetchJson<FormElementStructure<${camelName}ElementNames>>(this.structureUrl), this.changeLanguage(this.language)]);
        this.structure = newGistStructure;
      }
    }
    this.localeLanguage = this.language;
  }

  async formSubmit(formValues: ${pascalName}) {
    try {
      console.log(formValues);

      this.setIsLoading(true);

      await new Promise(r => setTimeout(r, 3000));

      this.setSuccessCallback({});

      this.form.openDialog();
      setTimeout(() => {
        this.form.reset();
        this.form.rerender({ rerenderForm: true, rerenderAll: true });
      }, 100);
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // #endregion

  // #region Component Logic

  form = new FormHook(this, ${camelName}InputsValidation);

  // #endregion
  render() {
    return (
      <Host>
        <div part={${'`'}${tagName}-\${this.structure?.data?.theme}${'`'}}>
          <form-structure
            form={this.form}
            formLocale={this.locale}
            structure={this.structure}
            isLoading={this.isLoading}
            language={this.localeLanguage}
            errorMessage={this.errorMessage}
            formElementMapper={${camelName}Elements}
            successMessage={this.locale['Form submitted successfully.']}
          >
            <slot></slot>
          </form-structure>
        </div>
      </Host>
    );
  }
}
`;
  fs.writeFileSync(formElementPath, content);
}
console.log(`📝 Created: ./${path.basename(formElementPath)}`);

// #region generate html template
const formTemplatesFolder = path.join(__dirname, '../src/templates/forms');

const newFormTemplatePath = path.join(formTemplatesFolder, `${tagName}.html`);

if (!fs.existsSync(newFormTemplatePath)) {
  const content = `<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>${capitalizeName} Form</title>
    <script nomodule src="/build/shift-components.js"></script>
    <script type="module" src="/build/shift-components.esm.js"></script>
  </head>
  <style>
    body {
      padding: 16px;
    }

    .button-container {
      margin: 50px 0px;
      border: 1px solid #e1e1e1;
      padding: 20px;
      display: flex;
      flex-wrap: wrap;
      align-content: center;
      justify-content: center;
      gap: 10px;
    }

    .sample-button {
      background-color: #428bca;
      padding: 10px 15px;
    }

    .sample-button:hover {
      background-color: #3071a9;
    }

    #${tagName}-form::part(container) {
      margin-left: auto;
      margin-right: auto;
      max-width: 700px;
    }

    #${tagName}-form::part(inputs_wrapper) {
      display: flex;
      flex-direction: column;
      gap: 24px;
      margin-bottom: 24px;
    }

    @media (min-width: 640px) {
      #${tagName}-form::part(inputs_wrapper) {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }
  </style>
  <body>
    <a href="#" onclick="history.back()" style="color: blue; display: block; margin-bottom: 16px">Back</a>
    <h1>${capitalizeName} Form</h1>

    <div class="button-container">
      <button class="sample-button" onclick="updateLang('en')">En</button>
      <button class="sample-button" onclick="updateLang('ku')">Ku</button>
      <button class="sample-button" onclick="updateLang('ar')">Ar</button>
      <button class="sample-button" onclick="updateLang('ru')">Ru</button>
    </div>

    <!-- gist-id="" -->
    <${tagName}-form theme="tiq" language="en" id="${tagName}-form"></${tagName}-form>

    <script>
      let ${camelName}Form;

      document.addEventListener('DOMContentLoaded', function () {
        ${camelName}Form = document.getElementById('${tagName}-form');

        ${camelName}Form.structure = {
          data: {
            theme: "",
          },
          tag: 'div',
          id: 'container',
          children: [
            { tag: 'div', id: 'inputs_wrapper', children: ['name', 'phone'] },
            { name: 'submit', id: 'Submit' },
          ],
        };
      });

      function updateLang(newLang) {
        ${camelName}Form.language = newLang;
      }
    </script>
  </body>
</html>
`;
  fs.writeFileSync(newFormTemplatePath, content);
}
console.log(`📝 Created: ${path.basename(newFormTemplatePath)}`);
//#endregion

const formTemplatesEntryFile = path.join(formTemplatesFolder, 'index.html');

let entryHtml = fs.readFileSync(formTemplatesEntryFile, 'utf8');

const newFormItem = `\n<li>\n  <a href="./${tagName}.html">${capitalizeName} form</a>\n</li>`;

if (entryHtml.includes(`href="./${tagName}.html"`)) {
  console.log('ℹ️ Link already exists at the form templates entry. Nothing changed.');
} else {
  let insertPos = entryHtml.lastIndexOf('</li>');
  if (insertPos === -1) insertPos = entryHtml.lastIndexOf('<ul>');
  if (insertPos !== -1) {
    entryHtml = entryHtml.slice(0, insertPos + 5) + newFormItem + entryHtml.slice(insertPos + 5);
    fs.writeFileSync(formTemplatesEntryFile, entryHtml, 'utf8');
    console.log('✅ Link added to form templates entry.');
  } else {
    console.error('❌ No </li> found to insert after.');
  }
}

execSync(`yarn create:locale ${tagName} ./forms`, { stdio: 'inherit' });

console.log(`✅ Done ${tagName}-form component generated!`);
