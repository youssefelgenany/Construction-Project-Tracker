import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { EngineerCreateComponent } from './engineer-create.component';

describe('EngineerCreateComponent', () => {
  let component: EngineerCreateComponent;
  let fixture: ComponentFixture<EngineerCreateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerCreateComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(EngineerCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
