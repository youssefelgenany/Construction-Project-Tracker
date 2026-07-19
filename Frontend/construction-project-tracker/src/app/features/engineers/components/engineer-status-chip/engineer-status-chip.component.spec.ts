import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EngineerStatusChipComponent } from './engineer-status-chip.component';

describe('EngineerStatusChipComponent', () => {
  let component: EngineerStatusChipComponent;
  let fixture: ComponentFixture<EngineerStatusChipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerStatusChipComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EngineerStatusChipComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
