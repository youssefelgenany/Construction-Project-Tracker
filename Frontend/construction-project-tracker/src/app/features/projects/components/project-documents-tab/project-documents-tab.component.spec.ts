import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { ProjectDocumentsTabComponent } from './project-documents-tab.component';

describe('ProjectDocumentsTabComponent', () => {
  let component: ProjectDocumentsTabComponent;
  let fixture: ComponentFixture<ProjectDocumentsTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectDocumentsTabComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideAnimationsAsync()]
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectDocumentsTabComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('projectId', 1);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
