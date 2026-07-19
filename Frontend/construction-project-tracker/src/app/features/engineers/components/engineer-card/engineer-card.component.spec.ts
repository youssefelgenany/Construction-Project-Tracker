import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EngineerCardComponent } from './engineer-card.component';

describe('EngineerCardComponent', () => {
  let component: EngineerCardComponent;
  let fixture: ComponentFixture<EngineerCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EngineerCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
