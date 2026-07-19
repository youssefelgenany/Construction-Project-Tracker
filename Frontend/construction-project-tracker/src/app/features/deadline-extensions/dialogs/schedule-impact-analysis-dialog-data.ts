import { ScheduleImpactAnalysis } from '../../../core/models/deadline-extension.model';

export interface ScheduleImpactAnalysisDialogData {
  analysis: ScheduleImpactAnalysis;
}

export type ScheduleImpactAnalysisDialogResult =
  | { confirmed: true; confirmProjectExtension: boolean }
  | { confirmed: false };
