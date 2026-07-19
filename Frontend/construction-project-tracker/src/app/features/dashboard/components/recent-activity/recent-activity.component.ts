import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RecentActivity } from '../../../../core/models/dashboard';

@Component({
  selector: 'app-recent-activity',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatListModule, DatePipe],
  templateUrl: './recent-activity.component.html',
  styleUrl: './recent-activity.component.scss'
})
export class RecentActivityComponent {
  readonly activities = input.required<RecentActivity[]>();

  getActivityIcon(activityType: string): string {
    switch (activityType) {
      case 'Project Created':
        return 'business';
      case 'Engineer Assigned':
        return 'group_add';
      case 'Task Created':
        return 'add_task';
      case 'Task Completed':
        return 'task_alt';
      case 'Document Uploaded':
        return 'upload_file';
      default:
        return 'info';
    }
  }
}
