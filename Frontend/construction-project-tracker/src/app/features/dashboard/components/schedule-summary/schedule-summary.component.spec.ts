import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScheduleSummaryComponent } from './schedule-summary.component';

describe('ScheduleSummaryComponent', () => {
  let component: ScheduleSummaryComponent;
  let fixture: ComponentFixture<ScheduleSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScheduleSummaryComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleSummaryComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('summary', {
      blockedTasksCount: 2,
      criticalTasksCount: 3,
      projectsBehindScheduleCount: 1
    });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
