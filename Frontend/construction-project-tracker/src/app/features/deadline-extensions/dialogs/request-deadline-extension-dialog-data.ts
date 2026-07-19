export type DeadlineExtensionTarget = 'task' | 'project';

export interface RequestDeadlineExtensionDialogData {
  target: DeadlineExtensionTarget;
  entityId: number;
  entityTitle: string;
  currentDeadline: string;
  maxDeadline?: string | null;
  projectName?: string;
}
