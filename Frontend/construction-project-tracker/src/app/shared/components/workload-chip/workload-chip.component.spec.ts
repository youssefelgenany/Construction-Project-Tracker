import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkloadLevel } from '../../../core/enums/workload-level';
import { WorkloadChipComponent } from './workload-chip.component';

describe('WorkloadChipComponent', () => {
  let component: WorkloadChipComponent;
  let fixture: ComponentFixture<WorkloadChipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkloadChipComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(WorkloadChipComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('level', WorkloadLevel.Low);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
