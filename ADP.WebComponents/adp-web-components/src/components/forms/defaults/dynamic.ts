/* 
        Examples:
        1-  Concatenate fields (array value):
            "fullName": ["firstName", " ", "lastName"]

        2- Parse a date string → Date object
            "parse date: dateOfBirth": "dd/MM/yyyy"
            
        3- Format a Date → ISO string 
            "format date: appointmentDate": ""
        
        4- Format a Date → date-fns format
            "format date: appointmentDate": "dd"

        5- Rename a field into additionalData
            "phoneNumber": "contact_phone"
    */
export type TruncatedFields = Partial<Record<string, string | string[]>>;

export type LanguageKey = 'ku' | 'ar' | 'en' | 'ru';
export type DataLocalization = Partial<Record<LanguageKey, Record<'Form submitted successfully.', string>>>;

type StepObject = {
  back?: string;
  stepTitle?: string;
  submitButton?: string;

  stepCell?: string; // number if steps is array

  stepId?: string;
  nextStepId?: string;
};
type ArraySteps = Partial<Record<LanguageKey, StepObject>>[];
type MapperSteps = Partial<Record<string, Partial<Record<LanguageKey, StepObject>>>>;

// deprecated**
type RequiredContext = Record<string, boolean>;

export type Data = {
  brandId: string; // brand id from ticket system as there is static values for toyota and lexus
  requestUrl: string;
  recaptchaKey: string;
  localization: DataLocalization;
  truncatedFields: TruncatedFields;
};

export type DefaultFieldProps = {
  type: string;
};

export type StructureMapper = {
  tag?: string;
  step?: string;
  class?: string;

  children?: Structure[];
};

export type Structure = {
  data: Data;
  version?: string;

  startStepId?: string;
  steps?: ArraySteps | MapperSteps;

  requiredContext?: RequiredContext; // deprecated**

  structure: StructureMapper;
};
