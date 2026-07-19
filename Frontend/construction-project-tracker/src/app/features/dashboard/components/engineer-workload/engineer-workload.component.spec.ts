import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EngineerWorkloadComponent } from './engineer-workload.component';

describe('EngineerWorkloadComponent', () => {
  let component: EngineerWorkloadComponent;
  let fixture: ComponentFixture<EngineerWorkloadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerWorkloadComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EngineerWorkloadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
