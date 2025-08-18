import { parseLooseJson } from '~lib/parse-loose-json';
import { FormElementStructure } from './interface';

export const gistLoader = async <T>(gistId: string): Promise<FormElementStructure<T> | undefined> => {
  try {
    const res = await fetch(`https://api.github.com/gists/${gistId}`, {
      headers: { Accept: 'application/vnd.github+json' },
    });

    if (!res.ok) {
      throw new Error(`GitHub API error: ${res.status} ${res.statusText}`);
    }

    const gist = await res.json();

    const jsonFile = Object.values(gist.files).find((file: any) => file?.filename.endsWith('.json')) as any;

    if (!jsonFile) throw new Error('No JSON file found in this gist');

    const resStructure = parseLooseJson(jsonFile?.content);

    return resStructure;
  } catch (err) {
    console.error('Error fetching gist JSON:', err.message);
  }
  return undefined;
};
