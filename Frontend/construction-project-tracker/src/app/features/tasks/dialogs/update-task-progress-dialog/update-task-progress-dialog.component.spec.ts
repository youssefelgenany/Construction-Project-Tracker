import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UpdateTaskProgressDialogComponent } from './update-task-progress-dialog.component';

describe('UpdateTaskProgressDialogComponent', () => {
  let component: UpdateTaskProgressDialogComponent;
  let fixture: ComponentFixture<UpdateTaskProgressDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpdateTaskProgressDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UpdateTaskProgressDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
