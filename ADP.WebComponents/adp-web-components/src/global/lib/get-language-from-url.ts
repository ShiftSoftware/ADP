import { LanguageKeys } from '~features/multi-lingual';

export default function getLanguageFromUrl(): LanguageKeys {
  const url = document.URL.toLocaleLowerCase();

  if (url.includes('/en') || url.includes('/english')) return 'en';

  if (url.includes('/ar') || url.includes('/arabic')) return 'ar';

  if (url.includes('/ku') || url.includes('/kurdish') || url.includes('/sorani')) return 'ku';

  if (url.includes('/ru') || url.includes('russian')) return 'en';

  return 'en';
}
