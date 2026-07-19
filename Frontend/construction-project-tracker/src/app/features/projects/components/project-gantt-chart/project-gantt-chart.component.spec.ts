import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TaskPriority } from '../../../../core/enums/task-priority';
import { TaskStatus } from '../../../../core/enums/task-status';
import { ProjectTimeline } from '../../../../core/models/project-timeline.model';
import { ProjectGanttChartComponent } from './project-gantt-chart.component';

describe('ProjectGanttChartComponent', () => {
  let component: ProjectGanttChartComponent;
  let fixture: ComponentFixture<ProjectGanttChartComponent>;

  const timeline: ProjectTimeline = {
    projectId: 1,
    projectName: 'Test Project',
    projectStartDate: '2026-01-01',
    projectEndDate: '2026-03-01',
    tasks: [
      {
        id: 1,
        title: 'Foundation',
        startDate: '2026-01-01',
        dueDate: '2026-01-15',
        completionPercentage: 50,
        status: TaskStatus.InProgress,
        priority: TaskPriority.High,
        engineerName: 'Engineer A',
        isOverdue: false,
        isCritical: true,
        isBlocked: false,
        dependsOnTaskIds: [],
        incompletePrerequisites: []
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectGanttChartComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectGanttChartComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('timeline', timeline);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
