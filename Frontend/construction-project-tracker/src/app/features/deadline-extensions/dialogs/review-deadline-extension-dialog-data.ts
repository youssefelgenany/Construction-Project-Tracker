import { DeadlineExtensionRequest } from '../../../core/models/deadline-extension.model';

export interface ReviewDeadlineExtensionDialogData {
  mode: 'approve' | 'reject';
  request: DeadlineExtensionRequest;
}
