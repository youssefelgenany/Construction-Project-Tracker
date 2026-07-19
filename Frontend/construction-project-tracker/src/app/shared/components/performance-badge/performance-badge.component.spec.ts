import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PerformanceTier } from '../../../core/enums/performance-tier';
import { PerformanceBadgeComponent } from './performance-badge.component';

describe('PerformanceBadgeComponent', () => {
  let component: PerformanceBadgeComponent;
  let fixture: ComponentFixture<PerformanceBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PerformanceBadgeComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PerformanceBadgeComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('tier', PerformanceTier.Good);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
