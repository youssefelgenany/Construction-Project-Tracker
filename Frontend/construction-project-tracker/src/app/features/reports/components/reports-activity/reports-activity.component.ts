import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { ReportActivity } from '../../../../core/models/executive-reports.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-reports-activity',
  standalone: true,
  imports: [DatePipe, MatIconModule, LoadingSpinnerComponent],
  templateUrl: './reports-activity.component.html',
  styleUrl: './reports-activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsActivityComponent {
  readonly activities = input<ReportActivity[]>([]);
  readonly loading = input(false);

  activityIcon(action: string): string {
    const value = action.toLowerCase();
    if (value.includes('assign')) return 'person_add';
    if (value.includes('complete') || value.includes('completed')) return 'task_alt';
    if (value.includes('progress')) return 'trending_up';
    if (value.includes('document')) return 'description';
    if (value.includes('report')) return 'assignment_turned_in';
    if (value.includes('creat')) return 'add_circle_outline';
    return 'history';
  }
}
