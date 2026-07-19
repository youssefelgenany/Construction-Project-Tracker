import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectEngineersTabComponent } from './project-engineers-tab.component';

describe('ProjectEngineersTabComponent', () => {
  let component: ProjectEngineersTabComponent;
  let fixture: ComponentFixture<ProjectEngineersTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectEngineersTabComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProjectEngineersTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
