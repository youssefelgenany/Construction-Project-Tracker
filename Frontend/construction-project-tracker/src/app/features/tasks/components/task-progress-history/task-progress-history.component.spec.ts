import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TaskProgressHistoryComponent } from './task-progress-history.component';

describe('TaskProgressHistoryComponent', () => {
  let component: TaskProgressHistoryComponent;
  let fixture: ComponentFixture<TaskProgressHistoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskProgressHistoryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TaskProgressHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
