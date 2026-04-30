import { format } from 'date-fns';

export function formatDateTime(iso: string | undefined | null): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return format(date, 'yyyy-MM-dd hh:mm a');
}
