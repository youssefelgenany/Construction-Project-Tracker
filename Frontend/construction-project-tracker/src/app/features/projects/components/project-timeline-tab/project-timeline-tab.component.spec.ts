import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { ProjectTimelineTabComponent } from './project-timeline-tab.component';

describe('ProjectTimelineTabComponent', () => {
  let component: ProjectTimelineTabComponent;
  let fixture: ComponentFixture<ProjectTimelineTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectTimelineTabComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideAnimationsAsync()]
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectTimelineTabComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('projectId', 1);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
