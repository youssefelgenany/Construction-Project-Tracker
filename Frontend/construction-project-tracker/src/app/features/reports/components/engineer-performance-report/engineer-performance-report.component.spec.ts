import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EngineerPerformanceReportComponent } from './engineer-performance-report.component';

describe('EngineerPerformanceReportComponent', () => {
  let component: EngineerPerformanceReportComponent;
  let fixture: ComponentFixture<EngineerPerformanceReportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerPerformanceReportComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(EngineerPerformanceReportComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('rows', []);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
