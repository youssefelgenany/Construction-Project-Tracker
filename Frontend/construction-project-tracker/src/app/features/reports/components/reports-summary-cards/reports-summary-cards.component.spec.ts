import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReportsSummaryCardsComponent } from './reports-summary-cards.component';
import { ExecutiveSummary } from '../../../../core/models/executive-reports.model';

describe('ReportsSummaryCardsComponent', () => {
  let component: ReportsSummaryCardsComponent;
  let fixture: ComponentFixture<ReportsSummaryCardsComponent>;

  const summary: ExecutiveSummary = {
    totalProjects: 0,
    healthyProjects: 0,
    atRiskProjects: 0,
    delayedProjects: 0,
    totalEngineers: 0,
    activeEngineers: 0,
    totalTasks: 0,
    completedTasks: 0,
    overdueTasks: 0,
    averageProjectCompletion: 0,
    onTimeCompletionRate: 0,
    averageEngineerWorkload: 0
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportsSummaryCardsComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ReportsSummaryCardsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('summary', summary);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
