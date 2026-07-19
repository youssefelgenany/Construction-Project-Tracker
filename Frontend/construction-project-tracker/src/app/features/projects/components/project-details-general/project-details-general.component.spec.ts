import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectDetailsGeneralComponent } from './project-details-general.component';

describe('ProjectDetailsGeneralComponent', () => {
  let component: ProjectDetailsGeneralComponent;
  let fixture: ComponentFixture<ProjectDetailsGeneralComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectDetailsGeneralComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProjectDetailsGeneralComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
