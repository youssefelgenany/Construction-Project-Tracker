export const COMPLETION_REPORT_MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024;

export const COMPLETION_REPORT_ALLOWED_EXTENSIONS = [
  '.pdf',
  '.doc',
  '.docx',
  '.xls',
  '.xlsx',
  '.png',
  '.jpg',
  '.jpeg',
  '.dwg',
  '.zip'
] as const;

export const COMPLETION_REPORT_ACCEPT_ATTRIBUTE = COMPLETION_REPORT_ALLOWED_EXTENSIONS.join(',');

export { formatFileSize, getDocumentPreviewType, triggerBlobDownload, normalizeExtension } from '../projects/documents.utils';

export function isAllowedCompletionReportExtension(extension: string): boolean {
  const normalized = extension.trim().toLowerCase();
  const withDot = normalized.startsWith('.') ? normalized : `.${normalized}`;
  return COMPLETION_REPORT_ALLOWED_EXTENSIONS.includes(
    withDot as (typeof COMPLETION_REPORT_ALLOWED_EXTENSIONS)[number]
  );
}

export function canPreviewCompletionReport(extension: string): boolean {
  const normalized = extension.trim().toLowerCase();
  const withDot = normalized.startsWith('.') ? normalized : `.${normalized}`;
  return ['.pdf', '.png', '.jpg', '.jpeg', '.doc', '.docx', '.xls', '.xlsx'].includes(withDot);
}
