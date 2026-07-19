import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormsModule } from '@angular/forms';
import {
  ScheduleImpactAnalysisDialogData,
  ScheduleImpactAnalysisDialogResult
} from './schedule-impact-analysis-dialog-data';

@Component({
  selector: 'app-schedule-impact-analysis-dialog',
  standalone: true,
  imports: [
    DatePipe,
    NgClass,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule
  ],
  templateUrl: './schedule-impact-analysis-dialog.component.html',
  styleUrl: './schedule-impact-analysis-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScheduleImpactAnalysisDialogComponent {
  private readonly dialogRef = inject(
    MatDialogRef<ScheduleImpactAnalysisDialogComponent, ScheduleImpactAnalysisDialogResult>
  );
  readonly data = inject<ScheduleImpactAnalysisDialogData>(MAT_DIALOG_DATA);

  readonly confirmProjectExtension = signal(false);

  constructor() {
    this.dialogRef.addPanelClass(['ds-premium-dialog-panel', 'schedule-impact-dialog-panel']);
    this.dialogRef.updateSize('920px', 'auto');
    if (!this.data.analysis.requiresProjectExtension) {
      this.confirmProjectExtension.set(true);
    }
  }

  get analysis() {
    return this.data.analysis;
  }

  hasNoImpact(): boolean {
    return this.analysis.affectedTaskCount === 0 && !this.analysis.requiresProjectExtension;
  }

  shiftTone(days: number): string {
    if (days <= 2) {
      return 'shift--green';
    }
    if (days <= 5) {
      return 'shift--orange';
    }
    return 'shift--red';
  }

  cancel(): void {
    this.dialogRef.close({ confirmed: false });
  }

  apply(): void {
    if (this.analysis.requiresProjectExtension && !this.confirmProjectExtension()) {
      return;
    }

    this.dialogRef.close({
      confirmed: true,
      confirmProjectExtension:
        !this.analysis.requiresProjectExtension || this.confirmProjectExtension()
    });
  }
}
