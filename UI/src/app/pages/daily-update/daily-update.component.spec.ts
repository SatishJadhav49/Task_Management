import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DailyUpdateComponent } from './daily-update.component';

describe('DailyUpdateComponent', () => {
  let component: DailyUpdateComponent;
  let fixture: ComponentFixture<DailyUpdateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DailyUpdateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DailyUpdateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
