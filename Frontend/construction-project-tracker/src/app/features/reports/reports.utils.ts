import { triggerBlobDownload } from '../../features/projects/documents.utils';

export function downloadReportBlob(blob: Blob, fileName: string): void {
  triggerBlobDownload(blob, fileName);
}

export function buildReportFileName(extension: 'pdf' | 'xlsx'): string {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
  return `construction-report-${timestamp}.${extension}`;
}
