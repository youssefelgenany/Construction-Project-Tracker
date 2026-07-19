import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssignEngineerDialogComponent } from './assign-engineer-dialog.component';

describe('AssignEngineerDialogComponent', () => {
  let component: AssignEngineerDialogComponent;
  let fixture: ComponentFixture<AssignEngineerDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssignEngineerDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssignEngineerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
