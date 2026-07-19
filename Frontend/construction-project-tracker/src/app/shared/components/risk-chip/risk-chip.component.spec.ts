import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RiskLevel } from '../../../core/enums/risk-level';
import { RiskChipComponent } from './risk-chip.component';

describe('RiskChipComponent', () => {
  let component: RiskChipComponent;
  let fixture: ComponentFixture<RiskChipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RiskChipComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RiskChipComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('level', RiskLevel.Low);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
