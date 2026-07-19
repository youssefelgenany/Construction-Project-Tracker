import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RiskLevel } from '../../../../core/enums/risk-level';
import { RiskSummaryComponent } from './risk-summary.component';

describe('RiskSummaryComponent', () => {
  let component: RiskSummaryComponent;
  let fixture: ComponentFixture<RiskSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RiskSummaryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RiskSummaryComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('projectsAtRiskCount', 1);
    fixture.componentRef.setInput('tasksAtRiskCount', 2);
    fixture.componentRef.setInput('overdueTasksCount', 1);
    fixture.componentRef.setInput('pendingReviewsCount', 1);
    fixture.componentRef.setInput('projects', [
      {
        id: 1,
        name: 'Alpha Tower',
        description: 'Sample project',
        budget: 100000,
        startDate: '2026-01-01',
        endDate: '2026-12-31',
        status: 1,
        progressPercentage: 45,
        riskLevel: RiskLevel.High,
        reason: 'Schedule is behind.',
        suggestedAction: 'Escalate work plan.',
        activeTaskCount: 4,
        atRiskTaskCount: 2,
        overdueTaskCount: 1,
        hasCriticalOverdueTask: false
      }
    ]);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
