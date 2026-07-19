export enum PredictionState {
  Scheduled = 0,
  DelayedStart = 1,
  OnTrack = 2,
  AtRisk = 3,
  Delayed = 4,
  WaitingForPlanning = 5
}

export enum PredictionConfidenceLevel {
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3
}

export interface ProjectDelayPrediction {
  projectId: number;
  projectName: string;
  predictionState: PredictionState;
  expectedProgress: number;
  currentProgress: number;
  velocity: number;
  totalWorkingDays: number;
  elapsedWorkingDays: number;
  remainingWorkingDays: number;
  remainingWork: number;
  estimatedRemainingWorkingDays: number;
  predictedFinishDate: string | null;
  delayWorkingDays: number;
  confidenceLevel: PredictionConfidenceLevel;
  riskMessage: string;
  statusLabel: string;
  willMissDeadline: boolean;
}
