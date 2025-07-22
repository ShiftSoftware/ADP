import { object, string, StringSchema } from 'yup';

export default function yupTypeMapper<const T extends readonly string[]>(keys: T) {
  const shape = keys.reduce(
    (acc, key) => {
      acc[key] = string().required().default('');
      return acc;
    },
    {} as Record<T[number], StringSchema>,
  );

  return object(shape);
}
