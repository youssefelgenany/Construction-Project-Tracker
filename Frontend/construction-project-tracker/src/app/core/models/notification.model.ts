export enum NotificationType {
  DeadlineExtensionRequest = 0,
  DeadlineExtensionApproved = 1,
  DeadlineExtensionRejected = 2,
  DeadlineExtended = 3
}

export interface AppNotification {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
}
