export interface Document {
  id: number;
  projectId: number;
  originalFileName: string;
  extension: string;
  contentType: string;
  fileSize: number;
  category: string;
  uploadDate: string;
  uploadedBy: string;
  downloadUrl: string;
}
