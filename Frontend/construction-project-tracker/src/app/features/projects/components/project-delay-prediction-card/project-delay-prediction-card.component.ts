import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import {
  PredictionConfidenceLevel,
  PredictionState,
  ProjectDelayPrediction
} from '../../../../core/models/project-delay-prediction.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-project-delay-prediction-card',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    NgClass,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    LoadingSpinnerComponent
  ],
  templateUrl: './project-delay-prediction-card.component.html',
  styleUrl: './project-delay-prediction-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDelayPredictionCardComponent {
  readonly prediction = input<ProjectDelayPrediction | null>(null);
  readonly plannedFinishDate = input<string | Date | null>(null);
  readonly loading = input(false);

  readonly stateClass = computed(() => {
    switch (this.prediction()?.predictionState) {
      case PredictionState.WaitingForPlanning:
        return 'state-waiting';
      case PredictionState.Scheduled:
        return 'state-scheduled';
      case PredictionState.DelayedStart:
        return 'state-delayed-start';
      case PredictionState.OnTrack:
        return 'state-on-track';
      case PredictionState.AtRisk:
        return 'state-at-risk';
      case PredictionState.Delayed:
        return 'state-delayed';
      default:
        return '';
    }
  });

  readonly confidenceLabel = computed(() => {
    switch (this.prediction()?.confidenceLevel) {
      case PredictionConfidenceLevel.Low:
        return 'Low';
      case PredictionConfidenceLevel.Medium:
        return 'Medium';
      case PredictionConfidenceLevel.High:
        return 'High';
      default:
        return null;
    }
  });

  readonly finishMessage = computed(() => {
    const data = this.prediction();
    if (!data) {
      return null;
    }

    if (data.predictionState === PredictionState.WaitingForPlanning) {
      return 'Waiting for scheduled tasks.';
    }

    if (data.predictionState === PredictionState.Scheduled && !data.predictedFinishDate) {
      return 'Project has not started yet.';
    }

    return null;
  });

  readonly isWaitingForPlanning = computed(
    () => this.prediction()?.predictionState === PredictionState.WaitingForPlanning
  );
}
