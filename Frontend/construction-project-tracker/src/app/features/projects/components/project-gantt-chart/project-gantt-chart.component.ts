import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProjectTimeline } from '../../../../core/models/project-timeline.model';
import { TimelineTask } from '../../../../core/models/task-dependency.model';
import { getTaskStatusLabel } from '../../projects.utils';

interface GanttBar {
  task: TimelineTask;
  leftPercent: number;
  widthPercent: number;
  rowIndex: number;
}

interface DependencyLine {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

@Component({
  selector: 'app-project-gantt-chart',
  standalone: true,
  imports: [MatTooltipModule],
  templateUrl: './project-gantt-chart.component.html',
  styleUrl: './project-gantt-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectGanttChartComponent {
  readonly timeline = input.required<ProjectTimeline>();

  readonly getStatusLabel = getTaskStatusLabel;

  readonly range = computed(() => {
    const data = this.timeline();
    const dates = [
      new Date(data.projectStartDate),
      new Date(data.projectEndDate),
      ...data.tasks.flatMap(task => [new Date(task.startDate), new Date(task.dueDate)])
    ];

    const start = new Date(Math.min(...dates.map(d => d.getTime())));
    const end = new Date(Math.max(...dates.map(d => d.getTime())));
    const totalMs = Math.max(end.getTime() - start.getTime(), 24 * 60 * 60 * 1000);

    return { start, end, totalMs };
  });

  readonly bars = computed<GanttBar[]>(() => {
    const { start, totalMs } = this.range();
    return this.timeline().tasks.map((task, rowIndex) => {
      const taskStart = new Date(task.startDate).getTime();
      const taskEnd = new Date(task.dueDate).getTime();
      const leftPercent = ((taskStart - start.getTime()) / totalMs) * 100;
      const widthPercent = Math.max(((taskEnd - taskStart) / totalMs) * 100, 1.5);

      return { task, leftPercent, widthPercent, rowIndex };
    });
  });

  readonly monthMarkers = computed(() => {
    const { start, end } = this.range();
    const markers: { label: string; leftPercent: number }[] = [];
    const cursor = new Date(start.getFullYear(), start.getMonth(), 1);

    while (cursor.getTime() <= end.getTime()) {
      const leftPercent = ((cursor.getTime() - start.getTime()) / this.range().totalMs) * 100;
      markers.push({
        label: cursor.toLocaleDateString(undefined, { month: 'short', year: '2-digit' }),
        leftPercent: Math.max(0, Math.min(100, leftPercent))
      });
      cursor.setMonth(cursor.getMonth() + 1);
    }

    return markers;
  });

  readonly dependencyLines = computed<DependencyLine[]>(() => {
    const barMap = new Map(this.bars().map(bar => [bar.task.id, bar]));
    const lines: DependencyLine[] = [];

    for (const dependency of this.timeline().dependencies) {
      const successor = barMap.get(dependency.taskId);
      const predecessor = barMap.get(dependency.dependsOnTaskId);
      if (!successor || !predecessor) {
        continue;
      }

      lines.push({
        x1: predecessor.leftPercent + predecessor.widthPercent,
        y1: predecessor.rowIndex,
        x2: successor.leftPercent,
        y2: successor.rowIndex
      });
    }

    return lines;
  });

  barClasses(task: TimelineTask): string {
    const classes = ['gantt-bar'];
    if (task.isOverdue) {
      classes.push('overdue');
    }
    if (task.isCritical) {
      classes.push('critical');
    }
    if (task.isBlocked) {
      classes.push('blocked');
    }
    return classes.join(' ');
  }

  linePath(line: DependencyLine): string {
    const rowHeight = 100 / Math.max(this.bars().length, 1);
    const y1 = (line.y1 + 0.75) * rowHeight;
    const y2 = (line.y2 + 0.25) * rowHeight;
    const midX = (line.x1 + line.x2) / 2;
    return `M ${line.x1} ${y1} C ${midX} ${y1}, ${midX} ${y2}, ${line.x2} ${y2}`;
  }
}
