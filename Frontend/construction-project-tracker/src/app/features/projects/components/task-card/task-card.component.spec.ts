import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TaskPriority } from '../../../../core/enums/task-priority';
import { TaskStatus } from '../../../../core/enums/task-status';
import { Task } from '../../../../core/models/task';
import { TaskCardComponent } from './task-card.component';

describe('TaskCardComponent', () => {
  let component: TaskCardComponent;
  let fixture: ComponentFixture<TaskCardComponent>;

  const task: Task = {
    id: 1,
    projectId: 1,
    projectName: 'Test Project',
    assignedEngineerId: 1,
    engineerName: 'Engineer One',
    title: 'Foundation work',
    description: 'Pour concrete',
    priority: TaskPriority.High,
    completionPercentage: 25,
    status: TaskStatus.InProgress,
    startDate: '2026-01-01',
    dueDate: '2026-02-01'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('task', task);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
