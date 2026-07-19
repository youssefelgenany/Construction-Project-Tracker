import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReportsChartsComponent } from './reports-charts.component';

describe('ReportsChartsComponent', () => {
  let component: ReportsChartsComponent;
  let fixture: ComponentFixture<ReportsChartsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportsChartsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReportsChartsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
