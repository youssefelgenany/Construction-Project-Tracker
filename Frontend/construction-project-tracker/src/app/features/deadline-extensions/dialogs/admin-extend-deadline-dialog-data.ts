export type DeadlineExtensionTarget = 'task' | 'project';

export interface AdminExtendDeadlineDialogData {
  target: DeadlineExtensionTarget;
  entityId: number;
  entityTitle: string;
  currentDeadline: string;
  maxDeadline?: string | null;
}
