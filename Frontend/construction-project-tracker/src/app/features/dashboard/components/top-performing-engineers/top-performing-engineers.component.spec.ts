import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PerformanceTier } from '../../../../core/enums/performance-tier';
import { TopPerformingEngineersComponent } from './top-performing-engineers.component';

describe('TopPerformingEngineersComponent', () => {
  let component: TopPerformingEngineersComponent;
  let fixture: ComponentFixture<TopPerformingEngineersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopPerformingEngineersComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TopPerformingEngineersComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('engineers', [
      {
        engineerId: 1,
        engineerName: 'Test Engineer',
        email: 'test@example.com',
        position: 'Engineer',
        isActive: true,
        totalProjectsWorkedOn: 2,
        projectsCompleted: 1,
        totalTasksAssigned: 5,
        totalTasksCompleted: 4,
        completionRate: 80,
        tasksFinishedBeforeDeadline: 3,
        tasksFinishedLate: 1,
        onTimeCompletionRate: 75,
        lateRate: 25,
        averageDaysEarlyLate: -1,
        averageTaskDuration: 4,
        averageProgressUpdatesPerTask: 2,
        totalCompletionReportsSubmitted: 4,
        currentActiveTasks: 1,
        performanceScore: 82,
        performanceTier: PerformanceTier.Good
      }
    ]);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
