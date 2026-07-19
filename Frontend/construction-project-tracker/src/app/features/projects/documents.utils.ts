import { DocumentCategory } from '../../core/enums/document-category';
import { Document } from '../../core/models/document';

export const DOCUMENT_MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024;

export const DOCUMENT_ALLOWED_EXTENSIONS = [
  '.pdf',
  '.doc',
  '.docx',
  '.xls',
  '.xlsx',
  '.png',
  '.jpg',
  '.jpeg'
] as const;

export const DOCUMENT_ACCEPT_ATTRIBUTE = DOCUMENT_ALLOWED_EXTENSIONS.join(',');

export const DOCUMENT_CATEGORY_OPTIONS = Object.values(DocumentCategory);

export type DocumentPreviewType = 'image' | 'pdf' | 'office';

const IMAGE_EXTENSIONS = new Set(['.png', '.jpg', '.jpeg']);
const OFFICE_EXTENSIONS = new Set(['.doc', '.docx', '.xls', '.xlsx']);

export function normalizeExtension(extension: string): string {
  const normalized = extension.trim().toLowerCase();
  return normalized.startsWith('.') ? normalized : `.${normalized}`;
}

export function isAllowedDocumentExtension(extension: string): boolean {
  return DOCUMENT_ALLOWED_EXTENSIONS.includes(
    normalizeExtension(extension) as (typeof DOCUMENT_ALLOWED_EXTENSIONS)[number]
  );
}

export function getDocumentPreviewType(extension: string): DocumentPreviewType {
  const normalized = normalizeExtension(extension);

  if (IMAGE_EXTENSIONS.has(normalized)) {
    return 'image';
  }

  if (normalized === '.pdf') {
    return 'pdf';
  }

  return 'office';
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function filterDocuments(documents: Document[], searchTerm: string): Document[] {
  const term = searchTerm.trim().toLowerCase();

  if (!term) {
    return documents;
  }

  return documents.filter(
    document =>
      document.originalFileName.toLowerCase().includes(term) ||
      document.category.toLowerCase().includes(term)
  );
}

export function triggerBlobDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}
