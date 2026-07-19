import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TaskProgressLogService } from './task-progress-log.service';

describe('TaskProgressLogService', () => {
  let service: TaskProgressLogService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TaskProgressLogService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
